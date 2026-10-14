#define COBJMACROS
#include "graphics_native_surfaces.h"
#include <string.h>

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
    if (native->valid && !memcmp(&native->plan, plan, sizeof(*plan))) return S_OK;
    /* Changed resources must be downloaded before reuse once draws exist.
     * Until that path is implemented, never silently discard an old target. */
    if (native->valid) return E_NOTIMPL;
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
    if (SUCCEEDED(hr)) { next.plan = *plan; next.valid = 1; *native = next; memset(&next, 0, sizeof(next)); }
done:
    if (color) HeapFree(GetProcessHeap(), 0, color);
    if (depth) HeapFree(GetProcessHeap(), 0, depth);
    fut_native_surfaces_destroy(&next);
    return hr;
}
