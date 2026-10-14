#define COBJMACROS
#include <windows.h>
#include <d3d11.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

#include "xbox_memory_layout.h"
#include "graphics_command_stream.h"
#include "graphics_native_backend.h"
#include "graphics_objects.h"
#include "graphics_kelvin_state.h"
#include "graphics_native_constants.h"

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
    FutGraphicsBindings bindings;
    FutGraphicsKelvinState kelvin;
    uint32_t semaphore_offset;
    FutGraphicsDmaView semaphore_view;
} NativeGraphicsState;

static int native_finish(NativeGraphicsState *state)
{
    if (!fut_native_constants_finish(&state->native_constants,
        state->kelvin.constants_dirty ? state->kelvin.constants : NULL, 2000, backend_stop))
        return 0;
    state->kelvin.constants_dirty = 0;
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
        if (applied == FUT_KELVIN_APPLIED)
            return 1;
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
