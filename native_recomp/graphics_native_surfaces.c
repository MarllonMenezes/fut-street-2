#define COBJMACROS
#include "graphics_native_surfaces.h"
#include <string.h>
#include <stdio.h>

void fut_native_surfaces_destroy(FutNativeSurfaces *native)
{
    if (native->target) ID3D11RenderTargetView_Release(native->target);
    if (native->depth_target) ID3D11DepthStencilView_Release(native->depth_target);
    if (native->color) ID3D11Texture2D_Release(native->color);
    if (native->depth) ID3D11Texture2D_Release(native->depth);
    memset(native, 0, sizeof(*native));
}
static int valid_plan(const FutGraphicsSurfacePlan *p)
{
    if (!p || !p->logical_width || !p->logical_height || !p->width || !p->height ||
        p->logical_width > 16384 || p->logical_height > 16384 ||
        (p->samples != 1 && p->samples != 2 && p->samples != 4) ||
        p->width != p->logical_width * (p->samples > 1 ? 2u : 1u) ||
        p->height != p->logical_height * (p->samples == 4 ? 2u : 1u) ||
        p->width > 16384 || p->height > 16384) return 0;
    uint64_t row = (uint64_t)p->width * 4;
    if (p->color_pitch < row || p->depth_pitch < row ||
        ((p->color_pitch | p->depth_pitch | p->color_address | p->depth_address) & 3) ||
        (uint64_t)(p->height - 1) * p->color_pitch + row != p->color_span ||
        (uint64_t)(p->height - 1) * p->depth_pitch + row != p->depth_span) return 0;
    if ((p->color_address & 0xFC000000u) != 0x80000000u ||
        (p->depth_address & 0xFC000000u) != 0x80000000u ||
        p->color_span > 0x84000000u - p->color_address ||
        p->depth_span > 0x84000000u - p->depth_address) return 0;
    return (uint64_t)p->color_address + p->color_span <= p->depth_address ||
        (uint64_t)p->depth_address + p->depth_span <= p->color_address;
}
HRESULT fut_native_surfaces_prepare(FutNativeSurfaces *native, ID3D11Device *device,
    const FutGraphicsSurfacePlan *plan, FutGraphicsMemoryRead read, void *context)
{
    if (!native || !device || !read || !valid_plan(plan)) return E_INVALIDARG;
    /* A failed GPU writeback must never be hidden by a resource replacement.
     * Clean resources can be refreshed from authoritative guest memory. */
    if (native->gpu_dirty) return E_PENDING;
    uint8_t *color = HeapAlloc(GetProcessHeap(), 0, plan->color_span);
    uint8_t *depth = HeapAlloc(GetProcessHeap(), 0, plan->depth_span);
    HRESULT hr = E_OUTOFMEMORY;
    FutNativeSurfaces next = {0};
    if (!color || !depth) goto done;
    hr = E_FAIL;
    if (!read(context, plan->color_address, color, plan->color_span) ||
        !read(context, plan->depth_address, depth, plan->depth_span)) goto done;
    /* Guest Z24S8 = depth in bits31:8, stencil7:0. D3D11 reverses the
     * placement of these fields; preserve every bit rather than converting
     * depth through a rounded float. Padding is not part of any pixel. */
    for (uint32_t y = 0; y < plan->height; ++y) {
        uint32_t *row = (uint32_t *)(depth + (size_t)y * plan->depth_pitch);
        for (uint32_t x = 0; x < plan->width; ++x) row[x] = fut_graphics_depth_to_native(row[x]);
    }
    D3D11_TEXTURE2D_DESC desc = {0};
    desc.Width = plan->width; desc.Height = plan->height;
    desc.MipLevels = 1; desc.ArraySize = 1;
    desc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
    desc.SampleDesc.Count = 1; /* Each packed sample is a stored pixel. */
    desc.Usage = D3D11_USAGE_DEFAULT; desc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
    D3D11_SUBRESOURCE_DATA initial = {color, plan->color_pitch, 0};
    hr = ID3D11Device_CreateTexture2D(device, &desc, &initial, &next.color);
    if (FAILED(hr)) goto done;
    hr = ID3D11Device_CreateRenderTargetView(device, (ID3D11Resource *)next.color, NULL, &next.target);
    if (FAILED(hr)) goto done;
    desc.Format = DXGI_FORMAT_R24G8_TYPELESS;
    desc.BindFlags = D3D11_BIND_DEPTH_STENCIL | D3D11_BIND_SHADER_RESOURCE;
    initial.pSysMem = depth; initial.SysMemPitch = plan->depth_pitch;
    hr = ID3D11Device_CreateTexture2D(device, &desc, &initial, &next.depth);
    if (FAILED(hr)) goto done;
    D3D11_DEPTH_STENCIL_VIEW_DESC view = {0};
    view.Format = DXGI_FORMAT_D24_UNORM_S8_UINT; view.ViewDimension = D3D11_DSV_DIMENSION_TEXTURE2D;
    hr = ID3D11Device_CreateDepthStencilView(device, (ID3D11Resource *)next.depth, &view, &next.depth_target);
    if (SUCCEEDED(hr)) {
        next.plan = *plan; next.valid = 1;
        fut_native_surfaces_destroy(native);
        *native = next; memset(&next, 0, sizeof(next));
    }
done:
    if (color) HeapFree(GetProcessHeap(), 0, color);
    if (depth) HeapFree(GetProcessHeap(), 0, depth);
    fut_native_surfaces_destroy(&next);
    return hr;
}

static HRESULT download_pixels(ID3D11Device *device, ID3D11DeviceContext *context,
    FutNativeConstants *completion, ID3D11Texture2D *texture, const FutGraphicsSurfacePlan *plan,
    uint8_t *bytes, int depth, uint32_t mask, uint32_t expected, HANDLE stop)
{
    D3D11_TEXTURE2D_DESC desc; ID3D11Texture2D_GetDesc(texture, &desc);
    desc.Usage = D3D11_USAGE_STAGING; desc.BindFlags = 0; desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
    ID3D11Texture2D *staging = NULL;
    HRESULT hr = fut_native_readback_texture(completion, device, &desc, &staging);
    if (FAILED(hr)) return hr;
    ID3D11DeviceContext_CopyResource(context, (ID3D11Resource *)staging, (ID3D11Resource *)texture);
    if (!fut_native_constants_finish(completion, NULL, 2000, stop)) hr = E_FAIL;
    D3D11_MAPPED_SUBRESOURCE mapped;
    if (SUCCEEDED(hr)) hr = ID3D11DeviceContext_Map(context, (ID3D11Resource *)staging, 0, D3D11_MAP_READ, 0, &mapped);
    if (SUCCEEDED(hr)) {
        uint32_t pitch = depth ? plan->depth_pitch : plan->color_pitch;
        for (uint32_t y = 0; y < plan->height; ++y) for (uint32_t x = 0; x < plan->width; ++x) {
            uint32_t word; memcpy(&word, (const uint8_t *)mapped.pData + (size_t)y*mapped.RowPitch+x*4, 4);
            if (depth) word = fut_graphics_depth_to_guest(word);
            if ((word & mask) != (expected & mask)) hr = E_FAIL;
            memcpy(bytes + (size_t)y*pitch+x*4, &word, 4);
        }
        ID3D11DeviceContext_Unmap(context, (ID3D11Resource *)staging, 0);
    }
    ID3D11Texture2D_Release(staging);
    return hr;
}
void fut_native_surfaces_mark_dirty(FutNativeSurfaces *native, unsigned attachments)
{
    if (native && native->valid) native->gpu_dirty |= attachments & 3u;
}
HRESULT fut_native_surfaces_writeback(FutNativeSurfaces *native, ID3D11Device *device,
    ID3D11DeviceContext *context, FutNativeConstants *completion,
    FutGraphicsMemoryRead read, FutGraphicsMemoryWrite write,
    void *memory_context, HANDLE stop_event)
{
    if (!native || (native->gpu_dirty & ~3u)) return E_INVALIDARG;
    if (!native->gpu_dirty) return S_OK;
    if (!native->valid || !valid_plan(&native->plan) || !device || !context ||
        !completion || !read || !write) return E_INVALIDARG;
    if (stop_event && WaitForSingleObject(stop_event, 0) != WAIT_TIMEOUT) return E_ABORT;
    const FutGraphicsSurfacePlan *p = &native->plan;
    unsigned attachments = native->gpu_dirty;
    uint8_t *color = (attachments & 1) ? HeapAlloc(GetProcessHeap(), 0, p->color_span) : NULL;
    uint8_t *depth = (attachments & 2) ? HeapAlloc(GetProcessHeap(), 0, p->depth_span) : NULL;
    HRESULT hr = E_OUTOFMEMORY;
    if (((attachments & 1) && !color) || ((attachments & 2) && !depth)) goto done;
    hr = E_FAIL;
    /* Read the padding before overlaying pixels; never publish D3D row pitch. */
    if (((attachments & 1) && !read(memory_context, p->color_address, color, p->color_span)) ||
        ((attachments & 2) && !read(memory_context, p->depth_address, depth, p->depth_span))) goto done;
    hr = S_OK;
    if (attachments & 1) hr = download_pixels(device, context, completion, native->color, p,
        color, 0, 0, 0, stop_event);
    if (SUCCEEDED(hr) && (attachments & 2)) hr = download_pixels(device, context, completion, native->depth, p,
        depth, 1, 0, 0, stop_event);
    if (SUCCEEDED(hr) && stop_event && WaitForSingleObject(stop_event, 0) != WAIT_TIMEOUT) hr = E_ABORT;
    if (SUCCEEDED(hr) && (attachments & 1)) {
        if (write(memory_context, p->color_address, color, p->color_span)) native->gpu_dirty &= ~1u;
        else hr = E_FAIL;
    }
    if (SUCCEEDED(hr) && (attachments & 2)) {
        if (write(memory_context, p->depth_address, depth, p->depth_span)) native->gpu_dirty &= ~2u;
        else hr = E_FAIL;
    }
done:
    if (color) HeapFree(GetProcessHeap(), 0, color);
    if (depth) HeapFree(GetProcessHeap(), 0, depth);
    return hr;
}
HRESULT fut_native_surfaces_capture_bmp(const FutNativeSurfaces *native,
    ID3D11Device *device, ID3D11DeviceContext *context,
    FutNativeConstants *completion, const char *path, HANDLE stop_event)
{
    if (!native || !native->valid || !native->color || !valid_plan(&native->plan) ||
        native->plan.samples != 1 || native->plan.width > 1920 || native->plan.height > 1080 ||
        native->plan.color_span > 16u * 1024u * 1024u || !device || !context ||
        !completion || !path || !path[0]) return E_INVALIDARG;
    if (stop_event && WaitForSingleObject(stop_event, 0) != WAIT_TIMEOUT) return E_ABORT;
    const FutGraphicsSurfacePlan *p = &native->plan;
    D3D11_TEXTURE2D_DESC desc;
    ID3D11Texture2D_GetDesc(native->color, &desc);
    if (desc.Width != p->width || desc.Height != p->height ||
        desc.Format != DXGI_FORMAT_B8G8R8A8_UNORM || desc.SampleDesc.Count != 1 ||
        desc.ArraySize != 1 || desc.MipLevels != 1) return E_INVALIDARG;
    uint8_t *pixels = HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, p->color_span);
    if (!pixels) return E_OUTOFMEMORY;
    HRESULT hr = download_pixels(device, context, completion, native->color, p,
        pixels, 0, 0, 0, stop_event);
    if (SUCCEEDED(hr) && stop_event && WaitForSingleObject(stop_event, 0) != WAIT_TIMEOUT) hr = E_ABORT;
    if (SUCCEEDED(hr)) {
        BITMAPFILEHEADER file = {0};
        BITMAPINFOHEADER info = {0};
        file.bfType = 0x4D42u;
        file.bfOffBits = sizeof(file) + sizeof(info);
        file.bfSize = file.bfOffBits + p->width * p->height * 4u;
        info.biSize = sizeof(info); info.biWidth = p->width; info.biHeight = -(LONG)p->height;
        info.biPlanes = 1; info.biBitCount = 32; info.biCompression = BI_RGB;
        info.biSizeImage = p->width * p->height * 4u;
        FILE *output = fopen(path, "wb");
        if (!output) hr = E_FAIL;
        else {
            int ok = fwrite(&file, sizeof(file), 1, output) == 1 &&
                fwrite(&info, sizeof(info), 1, output) == 1;
            for (unsigned y = 0; ok && y < p->height; ++y)
                ok = fwrite(pixels + (size_t)y * p->color_pitch, 4u, p->width, output) == p->width;
            if (fclose(output) != 0) ok = 0;
            if (!ok) { remove(path); hr = E_FAIL; }
        }
    }
    HeapFree(GetProcessHeap(), 0, pixels);
    return hr;
}

HRESULT fut_native_surfaces_clear(FutNativeSurfaces *native, ID3D11Device *device,
    ID3D11DeviceContext *context, FutNativeConstants *completion,
    const FutGraphicsKelvinState *state, uint32_t flags, FutGraphicsMemoryRead read,
    FutGraphicsMemoryWrite write, void *memory_context, HANDLE stop_event)
{
    if (!native || !native->valid || !valid_plan(&native->plan) || !device || !context ||
        !completion || !state || !read || !write || (flags & ~0xF3u)) return E_INVALIDARG;
    if (stop_event && WaitForSingleObject(stop_event, 0) != WAIT_TIMEOUT) return E_ABORT;
    if (!flags) return S_OK;
    unsigned colors = flags & 0xF0, depth_flags = flags & 3;
    unsigned required = 12 | (colors ? 2 : 0) | (depth_flags ? 1 : 0);
    const FutGraphicsSurfacePlan *p = &native->plan;
    if(p->clip_x || p->clip_y || (p->clip_width && p->clip_width!=p->logical_width) ||
        (p->clip_height && p->clip_height!=p->logical_height)) return E_NOTIMPL;
    if ((state->clear_valid & required) != required || (state->control0 & 0x1000) ||
        (colors && colors != 0xF0) || state->clear_x != ((p->logical_width - 1) << 16) ||
        state->clear_y != ((p->logical_height - 1) << 16) ||
        ((flags & 1) && (state->clear_depth >> 8) != 0 && (state->clear_depth >> 8) != 0xFFFFFF)) return E_NOTIMPL;
    uint8_t *color = colors ? HeapAlloc(GetProcessHeap(), 0, p->color_span) : NULL;
    uint8_t *depth = depth_flags ? HeapAlloc(GetProcessHeap(), 0, p->depth_span) : NULL;
    HRESULT hr = E_OUTOFMEMORY;
    if ((colors && !color) || (depth_flags && !depth)) goto done;
    hr = E_FAIL;
    if ((colors && !read(memory_context, p->color_address, color, p->color_span)) ||
        (depth_flags && !read(memory_context, p->depth_address, depth, p->depth_span))) goto done;
    fut_native_surfaces_mark_dirty(native, (colors ? 1u : 0u) | (depth_flags ? 2u : 0u));
    if (colors) {
        float rgba[4] = {((state->clear_color>>16)&255)/255.0f, ((state->clear_color>>8)&255)/255.0f,
            (state->clear_color&255)/255.0f, (state->clear_color>>24)/255.0f};
        ID3D11DeviceContext_ClearRenderTargetView(context, native->target, rgba);
    }
    if (depth_flags) ID3D11DeviceContext_ClearDepthStencilView(context, native->depth_target,
        ((flags&1)?D3D11_CLEAR_DEPTH:0)|((flags&2)?D3D11_CLEAR_STENCIL:0),
        (state->clear_depth>>8) ? 1.0f : 0.0f, (UINT8)state->clear_depth);
    hr = fut_native_constants_finish(completion, NULL, 2000, stop_event) ? S_OK : E_FAIL;
    if (SUCCEEDED(hr) && colors) hr = download_pixels(device, context, completion, native->color, p,
        color, 0, UINT32_MAX, state->clear_color, stop_event);
    uint32_t z_mask = ((flags&1)?0xFFFFFF00u:0)|((flags&2)?255u:0);
    if (SUCCEEDED(hr) && depth_flags) hr = download_pixels(device, context, completion, native->depth, p,
        depth, 1, z_mask, state->clear_depth, stop_event);
    if (SUCCEEDED(hr) && stop_event && WaitForSingleObject(stop_event, 0) != WAIT_TIMEOUT) hr = E_ABORT;
    if (SUCCEEDED(hr) && colors) {
        if (write(memory_context, p->color_address, color, p->color_span)) native->gpu_dirty &= ~1u;
        else hr = E_FAIL;
    }
    if (SUCCEEDED(hr) && depth_flags) {
        if (write(memory_context, p->depth_address, depth, p->depth_span)) native->gpu_dirty &= ~2u;
        else hr = E_FAIL;
    }
done:
    if (color) HeapFree(GetProcessHeap(), 0, color);
    if (depth) HeapFree(GetProcessHeap(), 0, depth);
    return hr;
}
