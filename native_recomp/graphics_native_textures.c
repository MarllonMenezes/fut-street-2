#define COBJMACROS
#include "graphics_native_textures.h"
#include <string.h>

void fut_native_texture_destroy(FutNativeTexture *native)
{
    if (native->sampler) ID3D11SamplerState_Release(native->sampler);
    if (native->view) ID3D11ShaderResourceView_Release(native->view);
    if (native->texture) ID3D11Texture2D_Release(native->texture);
    memset(native, 0, sizeof(*native));
}
HRESULT fut_native_texture_upload(FutNativeTexture *native, ID3D11Device *device,
    const FutGraphicsTexturePlan *plan, FutGraphicsMemoryRead read, void *context)
{
    if (!native || !device || !read || !plan || !plan->width || !plan->height ||
        plan->width > 8191 || plan->height > 8191 ||
        (plan->dma_context != 1 && plan->dma_context != 2) ||
        plan->pitch < (uint64_t)plan->width * 4 || plan->pitch > 65535 ||
        ((plan->pitch | plan->address) & 3) ||
        (plan->address & 0xFC000000u) != 0x80000000u ||
        (uint64_t)(plan->height-1)*plan->pitch + (uint64_t)plan->width*4 != plan->span ||
        plan->span > 0x84000000u - plan->address) return E_INVALIDARG;
    uint8_t *bytes = HeapAlloc(GetProcessHeap(), 0, plan->span);
    if (!bytes) return E_OUTOFMEMORY;
    HRESULT hr = E_FAIL;
    FutNativeTexture next = {0};
    if (!read(context, plan->address, bytes, plan->span)) goto done;
    D3D11_TEXTURE2D_DESC desc = {0};
    desc.Width=plan->width; desc.Height=plan->height;
    desc.MipLevels=1; desc.ArraySize=1; desc.SampleDesc.Count=1;
    desc.Format=DXGI_FORMAT_B8G8R8A8_UNORM; desc.Usage=D3D11_USAGE_DEFAULT;
    desc.BindFlags=D3D11_BIND_SHADER_RESOURCE;
    D3D11_SUBRESOURCE_DATA initial = {bytes, plan->pitch, 0};
    hr=ID3D11Device_CreateTexture2D(device,&desc,&initial,&next.texture);
    if (SUCCEEDED(hr)) hr=ID3D11Device_CreateShaderResourceView(device,(ID3D11Resource*)next.texture,NULL,&next.view);
    if (SUCCEEDED(hr)) {
        next.plan=*plan; next.valid=1;
        fut_native_texture_destroy(native); *native=next; memset(&next,0,sizeof(next));
    }
done:
    fut_native_texture_destroy(&next); HeapFree(GetProcessHeap(),0,bytes);
    return hr;
}
HRESULT fut_native_texture_sampler(FutNativeTexture *native, ID3D11Device *device,
    const FutGraphicsTextureState *state)
{
    if (!native) return E_INVALIDARG;
    native->sampling_ready=0;
    if (!device || !state || !native->valid || (state->valid & 0x2C) != 0x2C) return E_INVALIDARG;
    unsigned min=(state->filter>>16)&255, mag=(state->filter>>24)&15;
    /* Fixed LOD zero is faithful here only for one level and matching filters.
     * Never turn convolution MIN=7/MAG=4 into ordinary linear sampling. */
    if (!(state->control0 & 0x40000000u) || (state->control0 & ~0x4003FFC0u) ||
        (state->filter & ~0x0F3F0000u) || (min != 1 && min != 2) || mag != min ||
        (state->address & ~0x00070707u)) return E_NOTIMPL;
    D3D11_SAMPLER_DESC desc = {0};
    desc.Filter=min==1 ? D3D11_FILTER_MIN_MAG_MIP_POINT : D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT;
    unsigned modes[3]={state->address&7,(state->address>>8)&7,(state->address>>16)&7};
    D3D11_TEXTURE_ADDRESS_MODE addresses[3];
    for (unsigned i=0;i<3;++i) {
        switch (modes[i]) {
        case 1: addresses[i]=D3D11_TEXTURE_ADDRESS_WRAP; break;
        case 2: addresses[i]=D3D11_TEXTURE_ADDRESS_MIRROR; break;
        case 3: addresses[i]=D3D11_TEXTURE_ADDRESS_CLAMP; break;
        case 4:
            if (!(state->valid & 0x200)) return E_INVALIDARG;
            addresses[i]=D3D11_TEXTURE_ADDRESS_BORDER; break;
        default: return E_NOTIMPL; /* Includes OGL clamp, not clamp-to-edge. */
        }
    }
    desc.AddressU=addresses[0]; desc.AddressV=addresses[1]; desc.AddressW=addresses[2];
    desc.MaxAnisotropy=1; desc.ComparisonFunc=D3D11_COMPARISON_NEVER;
    desc.MinLOD=0; desc.MaxLOD=0;
    desc.BorderColor[0]=((state->border_color>>16)&255)/255.0f;
    desc.BorderColor[1]=((state->border_color>>8)&255)/255.0f;
    desc.BorderColor[2]=(state->border_color&255)/255.0f;
    desc.BorderColor[3]=(state->border_color>>24)/255.0f;
    ID3D11SamplerState *sampler=NULL;
    HRESULT hr=ID3D11Device_CreateSamplerState(device,&desc,&sampler);
    if (SUCCEEDED(hr)) {
        if (native->sampler) ID3D11SamplerState_Release(native->sampler);
        native->sampler=sampler; native->sampling_ready=1; native->sampling_state=*state;
    }
    return hr;
}
const char *fut_native_texture_hlsl(void)
{
    return "float4 fut_linear_texture(Texture2D<float4> t, SamplerState s, float4 p, float2 extent) {\n"
        "return t.SampleLevel(s, (p.xy / p.w) / extent, 0.0);\n}\n";
}
