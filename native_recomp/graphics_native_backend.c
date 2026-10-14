#define COBJMACROS
#include <windows.h>
#include <d3d11.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "xbox_memory_layout.h"
#include "graphics_command_stream.h"
#include "graphics_native_backend.h"
#include "graphics_objects.h"
#include "graphics_kelvin_state.h"
#include "graphics_native_constants.h"
#include "graphics_native_vertex.h"
#include "graphics_native_surfaces.h"
#include "graphics_native_textures.h"
#include "graphics_native_fragment.h"
#include "graphics_native_pipeline.h"
#include "graphics_software_state.h"

static HANDLE backend_stop;
static HANDLE backend_thread;

static int read_guest(uint32_t va, void *data, size_t size)
{
    SIZE_T copied = 0;
    if (size > UINT32_MAX || va > UINT32_MAX - size)
        return 0;
    return ReadProcessMemory(GetCurrentProcess(),
        (const void *)((uintptr_t)xbox_GetMemoryOffset() + va), data, size, &copied)
        && copied == size;
}

static int read32(uint32_t va, uint32_t *value)
{
    return read_guest(va, value, sizeof(*value));
}

static int write32(uint32_t va, uint32_t value)
{
    SIZE_T written = 0;
    return WriteProcessMemory(GetCurrentProcess(),
        (void *)((uintptr_t)xbox_GetMemoryOffset() + va), &value, sizeof(value), &written)
        && written == sizeof(value);
}

static int object_memory_write(void *context, uint32_t address, const void *data, size_t size)
{
    (void)context;
    SIZE_T written = 0;
    if (address < 0x80000000u || address >= 0x84000000u || size > 0x84000000u - address) return 0;
    return WriteProcessMemory(GetCurrentProcess(), (void *)((uintptr_t)xbox_GetMemoryOffset() + address),
        data, size, &written) && written == size;
}

static void fail_backend(const char *reason)
{
    fprintf(stderr, "[GRAPHICS] %s\n", reason);
    fflush(NULL);
    _Exit(3);
}

typedef struct {
    ID3D11Device *device;
    ID3D11DeviceContext *context;
    FutNativeConstants native_constants;
    ID3D11VertexShader *vertex_shader;
    ID3DBlob *vertex_bytecode;
    ID3D11PixelShader *pixel_shader;
    ID3DBlob *pixel_bytecode;
    FutGraphicsBindings bindings;
    FutGraphicsKelvinState kelvin;
    FutGraphicsSurfaceState surfaces;
    FutNativeSurfaces native_surfaces;
    FutNativeTexture textures[4];
    FutNativePipeline pipeline;
    FutGraphicsSoftwareState software;
    uint32_t semaphore_offset;
    FutGraphicsDmaView semaphore_view;
} NativeGraphicsState;

static int object_memory_read(void *context, uint32_t address, void *data, size_t size);
static int native_writeback_surfaces(NativeGraphicsState *state)
{
    HRESULT hr = fut_native_surfaces_writeback(&state->native_surfaces, state->device,
        state->context, &state->native_constants, object_memory_read, object_memory_write, NULL, backend_stop);
    if (FAILED(hr)) {
        fprintf(stderr, "[GRAPHICS] current surface writeback failed: HRESULT=%08lX dirty=%u\n",
            hr, state->native_surfaces.gpu_dirty);
        return 0;
    }
    return 1;
}
static int native_prepare_surfaces(NativeGraphicsState *state)
{
    if (state->surfaces.valid != 63) {
        fprintf(stderr, "[GRAPHICS] incomplete surface description at use: valid=%u\n", state->surfaces.valid);
        return 0;
    }
    FutGraphicsDmaView color, depth;
    FutGraphicsSurfacePlan plan;
    const unsigned mask = (1u << 5) | (1u << 6);
    if ((state->bindings.reference_masks[0] & mask) != mask ||
        !fut_graphics_dma_surface_view(&state->bindings.references[0][5], &color) ||
        !fut_graphics_dma_surface_view(&state->bindings.references[0][6], &depth)) {
        fprintf(stderr, "[GRAPHICS] unsupported surface DMA descriptors: color=%08X/%08X/%08X depth=%08X/%08X/%08X\n",
            state->bindings.references[0][5].words[0], state->bindings.references[0][5].words[1],
            state->bindings.references[0][5].words[2], state->bindings.references[0][6].words[0],
            state->bindings.references[0][6].words[1], state->bindings.references[0][6].words[2]);
        return 0;
    }
    FutGraphicsSurfaceStatus status = fut_graphics_surface_plan(&state->surfaces, &color, &depth, &plan);
    if (status != FUT_SURFACE_READY || (state->kelvin.control0 & 0x1000u)) {
        fprintf(stderr, "[GRAPHICS] unsupported/invalid surface plan: status=%d format=%08X pitch=%08X control=%08X\n",
            status, state->surfaces.format, state->surfaces.pitch, state->kelvin.control0);
        return 0;
    }
    if (!native_writeback_surfaces(state)) return 0;
    int changed = !state->native_surfaces.valid ||
        memcmp(&state->native_surfaces.plan, &plan, sizeof(plan));
    HRESULT hr = fut_native_surfaces_prepare(&state->native_surfaces, state->device, &plan, object_memory_read, NULL);
    if (FAILED(hr)) {
        fprintf(stderr, "[GRAPHICS] native surface preparation failed: HRESULT=%08lX\n", hr);
        return 0;
    }
    ID3D11DeviceContext_OMSetRenderTargets(state->context, 1, &state->native_surfaces.target,
        state->native_surfaces.depth_target);
    if (changed) fprintf(stderr,
        "[GRAPHICS] native surfaces uploaded: logical=%ux%u stored=%ux%u samples=%u color=%08X depth=%08X; no draw\n",
        plan.logical_width, plan.logical_height, plan.width, plan.height, plan.samples, plan.color_address, plan.depth_address);
    return 1;
}

static int native_prepare_vertex(NativeGraphicsState *state)
{
    if (state->kelvin.program_dirty && (state->kelvin.program_mode & 3u) == 2u) {
        unsigned first = state->kelvin.program_start, count = 0;
        while (first + count < 136 && state->kelvin.program_valid[first + count] == 15) {
            ++count;
            if (state->kelvin.program[first + count - 1][3] & 1u) break;
        }
        ID3D11VertexShader *shader = NULL; ID3DBlob *bytecode = NULL;
        HRESULT hr = fut_native_vertex_compile(state->device, state->kelvin.program + first,
            count, &shader, &bytecode);
        if (FAILED(hr)) {
            fprintf(stderr, "[GRAPHICS] vertex program recompilation failed: start=%u count=%u hr=%08lX\n",
                    first, count, hr);
            return 0;
        }
        if (state->vertex_shader) ID3D11VertexShader_Release(state->vertex_shader);
        if (state->vertex_bytecode) ID3D10Blob_Release(state->vertex_bytecode);
        state->vertex_shader = shader; state->vertex_bytecode = bytecode;
        ID3D11DeviceContext_VSSetShader(state->context, shader, NULL, 0);
        state->kelvin.program_dirty = 0;
        fprintf(stderr, "[GRAPHICS] original vertex program recompiled to native shader: start=%u instructions=%u\n",
                first, count);
    }
    return 1;
}

static int native_prepare_textures(NativeGraphicsState *state)
{
    /* Texture uploads read authoritative guest data, including any earlier
     * GPU render target used as input. Never sample an obsolete CPU snapshot. */
    if (!native_writeback_surfaces(state)) return 0;
    for (unsigned stage=0;stage<4;++stage) {
        const FutGraphicsTextureState *texture=&state->kelvin.textures[stage];
        if (!(texture->control0 & 0x40000000u)) {
            state->textures[stage].sampling_ready=0;
            ID3D11ShaderResourceView *empty_view=NULL;
            ID3D11SamplerState *empty_sampler=NULL;
            ID3D11DeviceContext_PSSetShaderResources(state->context,stage,1,&empty_view);
            ID3D11DeviceContext_PSSetSamplers(state->context,stage,1,&empty_sampler);
            continue;
        }
        unsigned select=texture->format & 3;
        FutGraphicsDmaView dma;
        if ((select != 1 && select != 2) ||
            !(state->bindings.reference_masks[0] & (1u<<select)) ||
            (!fut_graphics_dma_surface_view(&state->bindings.references[0][select],&dma) &&
             !fut_graphics_dma_view(&state->bindings.references[0][select],&dma))) {
            fprintf(stderr,"[GRAPHICS] texture DMA context missing/unsupported: stage=%u context=%u\n",stage,select);
            return 0;
        }
        FutGraphicsTexturePlan plan;
        FutGraphicsTextureStatus status=fut_graphics_texture_plan(texture,&dma,&plan);
        if (status != FUT_TEXTURE_READY) {
            fprintf(stderr,"[GRAPHICS] texture storage unsupported/invalid: stage=%u status=%d format=%08X pitch=%08X\n",
                stage,status,texture->format,texture->control1);
            return 0;
        }
        HRESULT hr=fut_native_texture_upload(&state->textures[stage],state->device,&plan,object_memory_read,NULL);
        if (FAILED(hr)) {
            fprintf(stderr,"[GRAPHICS] texture upload failed: stage=%u HRESULT=%08lX\n",stage,hr);
            return 0;
        }
        fprintf(stderr,"[GRAPHICS] native texture uploaded: stage=%u size=%ux%u pitch=%u source=%08X; no sample/draw\n",
            stage,plan.width,plan.height,plan.pitch,plan.address);
        hr=fut_native_texture_sampler(&state->textures[stage],state->device,texture);
        if (FAILED(hr)) {
            fprintf(stderr,"[GRAPHICS] texture sampling translation pending: stage=%u filter=%08X address=%08X control=%08X HRESULT=%08lX\n",
                stage,texture->filter,texture->address,texture->control0,hr);
            return 0;
        }
        ID3D11DeviceContext_PSSetShaderResources(state->context,stage,1,&state->textures[stage].view);
        ID3D11DeviceContext_PSSetSamplers(state->context,stage,1,&state->textures[stage].sampler);
    }
    return 1;
}

static int native_finish(NativeGraphicsState *state)
{
    /* WAIT_IDLE/semaphore concerns submitted work, not setters for the next
     * target. In particular, never validate a new pitch with an old format. */
    if (!native_writeback_surfaces(state)) return 0;
    if (!fut_native_constants_finish(&state->native_constants,
        state->kelvin.constants_dirty ? state->kelvin.constants : NULL, 2000, backend_stop))
        return 0;
    state->kelvin.constants_dirty = 0;
    return 1;
}

static int native_prepare_fragment(NativeGraphicsState *state)
{
    ID3D11PixelShader *shader=NULL;ID3DBlob *bytecode=NULL;
    HRESULT hr=fut_native_fragment_compile(state->device,&state->kelvin,state->textures,&shader,&bytecode);
    if(FAILED(hr)) {
        fprintf(stderr,"[GRAPHICS] fragment-program translation pending/failed: control=%08X HRESULT=%08lX\n",
            state->kelvin.fragment.control,hr);
        return 0;
    }
    if(state->pixel_shader) ID3D11PixelShader_Release(state->pixel_shader);
    if(state->pixel_bytecode) ID3D10Blob_Release(state->pixel_bytecode);
    state->pixel_shader=shader;state->pixel_bytecode=bytecode;state->kelvin.fragment.dirty=0;
    ID3D11DeviceContext_PSSetShader(state->context,shader,NULL,0);
    fprintf(stderr,"[GRAPHICS] register-combiner equations compiled to native pixel shader; no draw\n");
    return 1;
}

static int object_memory_read(void *context, uint32_t address, void *data, size_t size)
{
    (void)context;
    return read_guest(address, data, size);
}

static int native_method(void *context, unsigned subchannel, uint32_t method,
                         uint32_t value, size_t offset)
{
    NativeGraphicsState *state = context;
    FutGraphicsBindingStatus binding = fut_graphics_apply_binding(&state->bindings,
        subchannel, method, value);
    if (binding == FUT_BINDING_APPLIED) {
        if (method == 0) {
            const FutGraphicsObject *object = &state->bindings.objects[subchannel];
            fprintf(stderr, "[GRAPHICS] bound sub=%u handle=%08X class=%02X instance=%08X\n",
                    subchannel, value, (unsigned)(object->words[0] & 0xFFu), object->instance);
        } else {
            fprintf(stderr, "[GRAPHICS] binding state sub=%u method=%04X value=%08X\n",
                    subchannel, method, value);
        }
        return 1;
    }
    if (binding == FUT_BINDING_REJECTED) {
        fprintf(stderr, "[GRAPHICS] invalid object/context binding: sub=%u method=%04X value=%08X\n",
                subchannel, method, value);
        return 0;
    }
    if (subchannel == 0 && method == 0x1D6Cu &&
        (state->bindings.objects[0].words[0] & 0xFFu) == 0x97u &&
        (state->bindings.reference_masks[0] & (1u << 9))) {
        FutGraphicsDmaView view;
        if ((value & 3u) || !fut_graphics_dma_view(
                &state->bindings.references[0][9], &view) ||
            value > view.size || view.size - value < 4) {
            fprintf(stderr, "[GRAPHICS] unsupported semaphore descriptor/offset=%08X\n", value);
            return 0;
        }
        state->semaphore_offset = value;
        state->semaphore_view = view;
        fprintf(stderr, "[GRAPHICS] semaphore destination=%08X size=%u; no release performed\n",
                view.guest_address + value, view.size);
        return 1;
    }
    const unsigned class_id = state->bindings.objects[subchannel].words[0] & 0xFFu;
    if (class_id == 0x44u && method == 0x310u) {
        state->kelvin.pattern_color = value;
        return 1;
    }
    if (class_id == 0x97u) {
        if (method == 0x100u && value) {
            if (!fut_graphics_software_state(&state->software, value, state->kelvin.clear_depth,
                    state->kelvin.clear_color, state->kelvin.clear_valid)) return 0;
            fprintf(stderr, "[GRAPHICS] software state retained: selector=%u data=%u; RDI-dependent draws remain gated\n",
                value & 31, value >> 5);
            return 1;
        }
        if ((method == 0x17FCu && value) || method == 0x1810u || method == 0x1818u || method == 0x1800u || method == 0x1808u) {
            /* Materialize descriptions only at use. This does not permit a
             * draw: the independent raster/shader/geometry gates still apply. */
            if (!native_prepare_surfaces(state)) return 0;
            if (!native_prepare_textures(state)) return 0;
            if (!fut_graphics_software_draw_ready(&state->software)) {
                fprintf(stderr, "[GRAPHICS] original draw blocked: SAMPLEALPHA/RDI/debug state lacks native rasterization translation\n");
                return 0;
            }
            if (!native_prepare_vertex(state)) return 0;
            if (!native_prepare_fragment(state)) return 0;
            if (!fut_graphics_draw_ready(&state->kelvin.draw)) {
                fprintf(stderr, "[GRAPHICS] incomplete native draw state at use\n");
                return 0;
            }
            HRESULT hr = fut_native_pipeline_apply(&state->pipeline, state->device, state->context,
                &state->kelvin, &state->native_surfaces.plan);
            if (FAILED(hr)) {
                fprintf(stderr, "[GRAPHICS] native draw-state preparation failed: HRESULT=%08lX\n", hr);
                return 0;
            }
            state->kelvin.draw.dirty = 0;
        }
        if (method == 0x1D94u) {
            if (!native_prepare_surfaces(state)) return 0;
            HRESULT hr = fut_native_surfaces_clear(&state->native_surfaces, state->device, state->context,
                &state->native_constants, &state->kelvin, value, object_memory_read, object_memory_write, NULL, backend_stop);
            if (FAILED(hr)) { fprintf(stderr, "[GRAPHICS] native clear/readback failed: flags=%08X HRESULT=%08lX\n", value, hr); return 0; }
            fprintf(stderr, "[GRAPHICS] original clear completed with exact GPU readback and guest writeback: flags=%08X color=%08X zstencil=%08X\n",
                value, state->kelvin.clear_color, state->kelvin.clear_depth);
            return 1;
        }
        if (method == 0x110u) {
            if (value || !native_finish(state)) return 0;
            return 1; /* Wait for actual native work; no guest marker written. */
        }
        if (fut_graphics_surface_state(&state->surfaces, method, value))
            return 1; /* Description only; allocation/range validation at use. */
        if (method == 0x1D70u) {
            FutGraphicsDmaView view;
            if (!(state->bindings.reference_masks[subchannel] & (1u << 9)) ||
                !fut_graphics_dma_view(&state->bindings.references[subchannel][9], &view) ||
                state->semaphore_offset > view.size || view.size - state->semaphore_offset < 4 ||
                !native_finish(state) || !write32(view.guest_address + state->semaphore_offset, value))
                return 0;
            fprintf(stderr, "[GRAPHICS] native GPU completion confirmed; semaphore=%08X value=%u\n",
                    view.guest_address + state->semaphore_offset, value);
            return 1;
        }
        FutGraphicsKelvinStatus applied = fut_graphics_kelvin_state(&state->kelvin, method, value);
        if (applied == FUT_KELVIN_APPLIED) {
            if (method >= 0xB00u && method < 0xB80u &&
                ((method - 0xB00u) / 4) % 4 == 3 && (value & 1u) &&
                !native_prepare_vertex(state))
                return 0;
            return 1;
        }
        if (applied == FUT_KELVIN_REJECTED) {
            fprintf(stderr, "[GRAPHICS] invalid/unsupported state method=%04X value=%08X\n", method, value);
            return 0;
        }
    }
    fprintf(stderr, "[GRAPHICS] native handler missing: sub=%u method=%04X "
            "value=%08X offset=%zu\n", subchannel, method, value, offset);
    return 0;
}

static DWORD WINAPI native_worker(void *unused)
{
    (void)unused;
    ID3D11Device *device = NULL;
    ID3D11DeviceContext *context = NULL;
    D3D_FEATURE_LEVEL feature_level;
    HRESULT hr = D3D11CreateDevice(NULL, D3D_DRIVER_TYPE_HARDWARE, NULL, 0,
        NULL, 0, D3D11_SDK_VERSION, &device, &feature_level, &context);
    if (FAILED(hr)) {
        fprintf(stderr, "[GRAPHICS] hardware device unavailable: HRESULT=%08lX; trying WARP\n", hr);
        hr = D3D11CreateDevice(NULL, D3D_DRIVER_TYPE_WARP, NULL, 0,
            NULL, 0, D3D11_SDK_VERSION, &device, &feature_level, &context);
    }
    if (FAILED(hr))
        fail_backend("native D3D11 device initialization failed");
    fprintf(stderr, "[GRAPHICS] native D3D11 device initialized: feature_level=%04X\n",
            (unsigned)feature_level);
    NativeGraphicsState state = {0};
    state.device = device;
    state.context = context;
    state.bindings.read = object_memory_read;
    if (FAILED(fut_native_constants_init(&state.native_constants, device, context)))
        fail_backend("cannot create native shader constants/completion query");

    uint32_t ring_start = 0, ring_end = 0, channel = 0, cursor = 0;
    while (WaitForSingleObject(backend_stop, 5) == WAIT_TIMEOUT) {
        uint32_t title_device = 0, start = 0, end = 0, write_ptr = 0;
        uint32_t title_channel = 0, put = 0;
        if (!read32(0x0029E968u, &title_device) || !title_device ||
            title_device > 0x04000000u - 0x1C28u)
            continue;
        if (!read32(title_device + 0x24u, &start) ||
            !read32(title_device + 0x28u, &end) || !read32(title_device, &write_ptr) ||
            !read32(title_device + 0x1C20u, &title_channel) ||
            title_channel != 0xFD800000u || !read32(title_channel + 0x40u, &put))
            continue;
        if (!cursor) {
            /* Initialize an empty native submission queue. There are no
             * commands between start and PUT, so no work is acknowledged.
             * The title's consumer semaphore is deliberately untouched. */
            if (!fut_graphics_empty_queue(start, end, write_ptr, put))
                continue;
            ring_start = start; ring_end = end; channel = title_channel; cursor = start;
            if (!write32(channel + 0x44u, put))
                fail_backend("cannot initialize empty native submission queue");
            fprintf(stderr, "[GRAPHICS] empty queue initialized: ring=[%08X,%08X) "
                    "cursor=%08X; no commands/fences completed\n", start, end, cursor);
            continue;
        }
        if (start != ring_start || end != ring_end || title_channel != channel)
            fail_backend("submission queue reconfiguration is not implemented");
        uint32_t submitted = put | 0x80000000u;
        if (submitted == cursor)
            continue;
        if (submitted < cursor || submitted > ring_end || (submitted - cursor) % 4u)
            fail_backend("submission wrap or invalid range is not implemented");
        uint8_t bytes[65536];
        size_t size = submitted - cursor;
        if (size > sizeof(bytes) || !read_guest(cursor, bytes, size))
            fail_backend("submission capture exceeds native decoder bounds");
        FutGraphicsDecodeResult decoded;
        FutGraphicsDecodeStatus status = fut_graphics_decode(bytes, size,
            native_method, &state, &decoded);
        if (status != FUT_GRAPHICS_DECODED) {
            FILE *capture = fopen("graphics-rejected.bin", "wb");
            if (capture) { fwrite(bytes, 1, size, capture); fclose(capture); }
            fprintf(stderr, "[GRAPHICS] submission blocked: start=%08X end=%08X "
                    "status=%d offset=%zu header=%08X bytes=%zu; completion unchanged\n",
                    cursor, submitted, status, decoded.blocked_offset, decoded.blocked_header, size);
            fail_backend("implement the reported native graphics method before continuing");
        }
        if (!native_finish(&state) || !write32(channel + 0x44u, put))
            fail_backend("native completion or submission cursor update failed");
        fprintf(stderr, "[GRAPHICS] submission completed: start=%08X end=%08X methods=%zu\n",
                cursor, submitted, decoded.methods);
        cursor = submitted;
    }
    fut_native_constants_destroy(&state.native_constants);
    fut_native_surfaces_destroy(&state.native_surfaces);
    for (unsigned stage=0;stage<4;++stage) fut_native_texture_destroy(&state.textures[stage]);
    fut_native_pipeline_destroy(&state.pipeline);
    if (state.vertex_shader) ID3D11VertexShader_Release(state.vertex_shader);
    if (state.vertex_bytecode) ID3D10Blob_Release(state.vertex_bytecode);
    if (state.pixel_shader) ID3D11PixelShader_Release(state.pixel_shader);
    if (state.pixel_bytecode) ID3D10Blob_Release(state.pixel_bytecode);
    ID3D11DeviceContext_Release(context);
    ID3D11Device_Release(device);
    return 0;
}

int fut_graphics_native_start(void)
{
    backend_stop = CreateEventA(NULL, TRUE, FALSE, NULL);
    if (!backend_stop)
        return 0;
    backend_thread = CreateThread(NULL, 0, native_worker, NULL, 0, NULL);
    if (!backend_thread) {
        CloseHandle(backend_stop); backend_stop = NULL;
        return 0;
    }
    return 1;
}

void fut_graphics_native_stop(void)
{
    if (backend_thread) {
        SetEvent(backend_stop);
        WaitForSingleObject(backend_thread, INFINITE);
        CloseHandle(backend_thread); backend_thread = NULL;
    }
    if (backend_stop) { CloseHandle(backend_stop); backend_stop = NULL; }
}
