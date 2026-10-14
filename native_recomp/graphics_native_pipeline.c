#define COBJMACROS
#include "graphics_native_pipeline.h"
#include <string.h>
#include <math.h>

static D3D11_BLEND blend(uint32_t value, int alpha)
{
    switch (value) {
    case 0: return D3D11_BLEND_ZERO;
    case 1: return D3D11_BLEND_ONE;
    case 0x300: return alpha ? D3D11_BLEND_SRC_ALPHA : D3D11_BLEND_SRC_COLOR;
    case 0x301: return alpha ? D3D11_BLEND_INV_SRC_ALPHA : D3D11_BLEND_INV_SRC_COLOR;
    case 0x302: return D3D11_BLEND_SRC_ALPHA;
    case 0x303: return D3D11_BLEND_INV_SRC_ALPHA;
    case 0x304: return D3D11_BLEND_DEST_ALPHA;
    case 0x305: return D3D11_BLEND_INV_DEST_ALPHA;
    case 0x306: return alpha ? D3D11_BLEND_DEST_ALPHA : D3D11_BLEND_DEST_COLOR;
    case 0x307: return alpha ? D3D11_BLEND_INV_DEST_ALPHA : D3D11_BLEND_INV_DEST_COLOR;
    case 0x308: return alpha ? D3D11_BLEND_ONE : D3D11_BLEND_SRC_ALPHA_SAT;
    case 0x8001: case 0x8003: return D3D11_BLEND_BLEND_FACTOR;
    case 0x8002: case 0x8004: return D3D11_BLEND_INV_BLEND_FACTOR;
    default: return 0;
    }
}
static D3D11_BLEND_OP equation(uint32_t v)
{
    switch (v) {
    case 0x8006: return D3D11_BLEND_OP_ADD;
    case 0x8007: return D3D11_BLEND_OP_MIN;
    case 0x8008: return D3D11_BLEND_OP_MAX;
    case 0x800A: return D3D11_BLEND_OP_SUBTRACT;
    case 0x800B: return D3D11_BLEND_OP_REV_SUBTRACT;
    default: return 0;
    }
}
static D3D11_STENCIL_OP stencil(uint32_t v)
{
    switch (v) {
    case 0x1E00: return D3D11_STENCIL_OP_KEEP;
    case 0: return D3D11_STENCIL_OP_ZERO;
    case 0x1E01: return D3D11_STENCIL_OP_REPLACE;
    case 0x1E02: return D3D11_STENCIL_OP_INCR_SAT;
    case 0x1E03: return D3D11_STENCIL_OP_DECR_SAT;
    case 0x150A: return D3D11_STENCIL_OP_INVERT;
    case 0x8507: return D3D11_STENCIL_OP_INCR;
    case 0x8508: return D3D11_STENCIL_OP_DECR;
    default: return 0;
    }
}
void fut_native_pipeline_destroy(FutNativePipeline *native)
{
    if (native->blend) ID3D11BlendState_Release(native->blend);
    if (native->depth) ID3D11DepthStencilState_Release(native->depth);
    if (native->raster) ID3D11RasterizerState_Release(native->raster);
    if (native->windows) ID3D11Buffer_Release(native->windows);
    memset(native, 0, sizeof(*native));
}
const char *fut_native_window_hlsl(void)
{
    return "cbuffer WindowClip:register(b2){uint4 windows[8];uint4 windowSettings;};\n"
        "bool fut_window_visible(float2 position){uint2 pixel=uint2(position)/windowSettings.xy;"
        "bool hit=false;[unroll]for(uint i=0;i<8;i++){uint4 w=windows[i];"
        "hit=hit||(pixel.x>=w.x&&pixel.y>=w.y&&pixel.x<=w.z&&pixel.y<=w.w);}"
        "return windowSettings.z!=0?!hit:hit;}\n";
}
HRESULT fut_native_pipeline_apply(FutNativePipeline *native, ID3D11Device *device,
    ID3D11DeviceContext *context, const FutGraphicsKelvinState *state, const FutGraphicsSurfacePlan *surface)
{
    if (!native || !device || !context || !state || !surface ||
        !fut_graphics_draw_ready(&state->draw) || !surface->width || !surface->height ||
        !surface->logical_width || !surface->logical_height || surface->logical_width > 16384 || surface->logical_height > 16384 ||
        surface->width > 16384 || surface->height > 16384 ||
        (surface->samples != 1 && surface->samples != 2 && surface->samples != 4) ||
        surface->width != surface->logical_width * (surface->samples > 1 ? 2u : 1u) ||
        surface->height != surface->logical_height * (surface->samples == 4 ? 2u : 1u)) return E_INVALIDARG;
    const FutGraphicsDrawState *s = &state->draw;
    if (s->window_exclusive > 1 || state->depth_test > 1 || state->stencil_test > 1 ||
        s->blend_enable > 1 || s->cull_enable > 1 || s->depth_write > 1 ||
        s->front_mode < 0x1B00 || s->front_mode > 0x1B02 || s->back_mode < 0x1B00 || s->back_mode > 0x1B02 ||
        (s->front_face != 0x900 && s->front_face != 0x901) ||
        (s->cull_face != 0x404 && s->cull_face != 0x405 && s->cull_face != 0x408) ||
        ((s->z_control >> 4) & 15) > 1) return E_INVALIDARG;
    float factor, bias; memcpy(&factor, &s->offset_factor, 4); memcpy(&bias, &s->offset_bias, 4);
    if (s->alpha_test || s->dither || s->logic_op || s->stipple || s->line_smooth || s->polygon_smooth ||
        s->front_mode != s->back_mode || s->front_mode == 0x1B00 ||
        (s->cull_enable && s->cull_face == 0x408) ||
        ((s->offset_enable[0] || s->offset_enable[1] || s->offset_enable[2]) &&
            (!isfinite(factor) || !isfinite(bias) || factor != 0 || bias != 0))) return E_NOTIMPL;
    int constant_color = s->blend_src == 0x8001 || s->blend_src == 0x8002 || s->blend_dst == 0x8001 || s->blend_dst == 0x8002;
    int constant_alpha = s->blend_src == 0x8003 || s->blend_src == 0x8004 || s->blend_dst == 0x8003 || s->blend_dst == 0x8004;
    if ((constant_color && constant_alpha) || s->blend_dst == 0x308) return E_NOTIMPL;
    D3D11_BLEND_DESC blend_desc = {0};
    D3D11_RENDER_TARGET_BLEND_DESC *rt = &blend_desc.RenderTarget[0];
    rt->BlendEnable = s->blend_enable;
    rt->SrcBlend = blend(s->blend_src, 0); rt->DestBlend = blend(s->blend_dst, 0);
    rt->SrcBlendAlpha = blend(s->blend_src, 1); rt->DestBlendAlpha = blend(s->blend_dst, 1);
    rt->BlendOp = rt->BlendOpAlpha = equation(s->blend_op);
    if (!rt->SrcBlend || !rt->DestBlend || !rt->BlendOp) return E_INVALIDARG;
    rt->RenderTargetWriteMask = ((s->color_mask & 0x10000) ? D3D11_COLOR_WRITE_ENABLE_RED : 0) |
        ((s->color_mask & 0x100) ? D3D11_COLOR_WRITE_ENABLE_GREEN : 0) |
        ((s->color_mask & 1) ? D3D11_COLOR_WRITE_ENABLE_BLUE : 0) |
        ((s->color_mask & 0x1000000) ? D3D11_COLOR_WRITE_ENABLE_ALPHA : 0);
    D3D11_DEPTH_STENCIL_DESC depth_desc = {0};
    depth_desc.DepthEnable = state->depth_test;
    depth_desc.DepthWriteMask = s->depth_write ? D3D11_DEPTH_WRITE_MASK_ALL : D3D11_DEPTH_WRITE_MASK_ZERO;
    if (s->depth_func < 0x200 || s->depth_func > 0x207 || s->stencil_func < 0x200 || s->stencil_func > 0x207) return E_INVALIDARG;
    depth_desc.DepthFunc = (D3D11_COMPARISON_FUNC)(s->depth_func - 0x200 + 1);
    depth_desc.StencilEnable = state->stencil_test;
    depth_desc.StencilReadMask = (UINT8)s->stencil_read; depth_desc.StencilWriteMask = (UINT8)s->stencil_write;
    depth_desc.FrontFace.StencilFunc = (D3D11_COMPARISON_FUNC)(s->stencil_func - 0x200 + 1);
    depth_desc.FrontFace.StencilFailOp = stencil(s->stencil_op[0]);
    depth_desc.FrontFace.StencilDepthFailOp = stencil(s->stencil_op[1]);
    depth_desc.FrontFace.StencilPassOp = stencil(s->stencil_op[2]);
    if (!depth_desc.FrontFace.StencilFailOp || !depth_desc.FrontFace.StencilDepthFailOp || !depth_desc.FrontFace.StencilPassOp) return E_INVALIDARG;
    depth_desc.BackFace = depth_desc.FrontFace;
    D3D11_RASTERIZER_DESC raster_desc = {0};
    raster_desc.FillMode = s->front_mode == 0x1B01 ? D3D11_FILL_WIREFRAME : D3D11_FILL_SOLID;
    raster_desc.CullMode = !s->cull_enable ? D3D11_CULL_NONE : s->cull_face == 0x404 ? D3D11_CULL_FRONT : D3D11_CULL_BACK;
    raster_desc.FrontCounterClockwise = s->front_face == 0x901;
    raster_desc.DepthClipEnable = !(s->z_control & 0xF0);
    raster_desc.ScissorEnable = TRUE;
    uint32_t clip[9][4] = {{0}};
    for (unsigned i = 0; i < 8; ++i) {
        clip[i][0] = s->window_x[i] & 0xFFF; clip[i][2] = (s->window_x[i] >> 16) & 0xFFF;
        clip[i][1] = s->window_y[i] & 0xFFF; clip[i][3] = (s->window_y[i] >> 16) & 0xFFF;
    }
    clip[8][0] = surface->samples > 1 ? 2 : 1; clip[8][1] = surface->samples == 4 ? 2 : 1;
    clip[8][2] = s->window_exclusive;
    FutNativePipeline next = {0};
    HRESULT hr = ID3D11Device_CreateBlendState(device, &blend_desc, &next.blend);
    if (SUCCEEDED(hr)) hr = ID3D11Device_CreateDepthStencilState(device, &depth_desc, &next.depth);
    if (SUCCEEDED(hr)) hr = ID3D11Device_CreateRasterizerState(device, &raster_desc, &next.raster);
    D3D11_BUFFER_DESC buffer = {0}; buffer.ByteWidth = sizeof(clip); buffer.Usage = D3D11_USAGE_DEFAULT; buffer.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
    D3D11_SUBRESOURCE_DATA initial = {clip,0,0};
    if (SUCCEEDED(hr)) hr = ID3D11Device_CreateBuffer(device, &buffer, &initial, &next.windows);
    if (FAILED(hr)) { fut_native_pipeline_destroy(&next); return hr; }
    float color[4] = {((s->blend_color >> 16) & 255)/255.0f, ((s->blend_color >> 8) & 255)/255.0f,
        (s->blend_color & 255)/255.0f, (s->blend_color >> 24)/255.0f};
    if (constant_alpha) color[0] = color[1] = color[2] = color[3];
    ID3D11DeviceContext_OMSetBlendState(context, next.blend, color, UINT32_MAX);
    ID3D11DeviceContext_OMSetDepthStencilState(context, next.depth, s->stencil_ref & 255);
    ID3D11DeviceContext_RSSetState(context, next.raster);
    D3D11_VIEWPORT viewport = {0,0,(float)surface->width,(float)surface->height,0,1};
    D3D11_RECT scissor = {0,0,(LONG)surface->width,(LONG)surface->height};
    ID3D11DeviceContext_RSSetViewports(context, 1, &viewport);
    ID3D11DeviceContext_RSSetScissorRects(context, 1, &scissor);
    ID3D11DeviceContext_PSSetConstantBuffers(context, 2, 1, &next.windows);
    fut_native_pipeline_destroy(native); *native = next;
    return S_OK;
}
