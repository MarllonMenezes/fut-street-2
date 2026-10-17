#define COBJMACROS
#include "graphics_native_textures.h"
#include <string.h>
#include "graphics_experiment.h"

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
    int bc1=plan && plan->storage==1;
    int p8=plan && plan->storage==2;
    if (!native || !device || !read || !plan || !plan->width || !plan->height ||
        plan->width > 8191 || plan->height > 8191 ||
        plan->storage>2 || (p8 && (plan->bytes_per_pixel!=1 || plan->palette_span!=1024 ||
            (plan->palette_address&0xFC00003Fu)!=0x80000000u ||
            plan->palette_span>0x84000000u-plan->palette_address || plan->pitch!=plan->width ||
            plan->width>4096 || plan->height>4096 || plan->width<4 || plan->height<4 ||
            (plan->width&(plan->width-1)) || (plan->height&(plan->height-1)))) ||
        (bc1 ? (plan->bytes_per_pixel || (plan->address&7u) ||
            plan->width>4096 || plan->height>4096 || plan->width<4 || plan->height<4 ||
            (plan->width&(plan->width-1)) || (plan->height&(plan->height-1)) ||
            plan->pitch!=plan->width/4*8) : (plan->bytes_per_pixel!=1 && plan->bytes_per_pixel!=4)) ||
        (plan->dma_context != 1 && plan->dma_context != 2) ||
        plan->pitch < (uint64_t)plan->width * plan->bytes_per_pixel || plan->pitch > 65535 ||
        ((plan->pitch | plan->address) & 3) ||
        (plan->address & 0xFC000000u) != 0x80000000u ||
        (bc1 ? (uint64_t)(plan->height/4)*plan->pitch :
            (uint64_t)(plan->height-1)*plan->pitch + (uint64_t)plan->width*plan->bytes_per_pixel) != plan->span ||
        plan->span > 0x84000000u - plan->address) return E_INVALIDARG;
    uint8_t *bytes = HeapAlloc(GetProcessHeap(), 0, plan->span);
    if (!bytes) return E_OUTOFMEMORY;
    HRESULT hr = E_FAIL;
    FutNativeTexture next = {0};
    const uint8_t *upload=bytes;
    uint32_t upload_pitch=plan->pitch;
    uint8_t *expanded=NULL;
    uint32_t palette[256];
    if (!read(context, plan->address, bytes, plan->span)) goto done;
    if (p8 && !read(context,plan->palette_address,palette,sizeof(palette))) goto done;
    if(plan->bytes_per_pixel==1) {
        uint64_t expanded_size=(uint64_t)plan->width*4*plan->height;
        if(expanded_size>SIZE_MAX) {hr=E_OUTOFMEMORY;goto done;}
        expanded=HeapAlloc(GetProcessHeap(),0,(SIZE_T)expanded_size);
        if(!expanded) {hr=E_OUTOFMEMORY;goto done;}
        upload=expanded;upload_pitch=plan->width*4;
        for(unsigned y=0;y<plan->height;++y) for(unsigned x=0;x<plan->width;++x) {
            uint32_t argb;
            if(p8) {
                /* Compact Morton order: dimensions stop contributing bits
                 * when exhausted, including rectangular power-of-two images. */
                unsigned index=0,out_bit=1;
                for(unsigned bit=1;bit<plan->width || bit<plan->height;bit<<=1) {
                    if(bit<plan->width) {if(x&bit) index|=out_bit;out_bit<<=1;}
                    if(bit<plan->height) {if(y&bit) index|=out_bit;out_bit<<=1;}
                }
                argb=palette[bytes[index]];
            } else {
                uint8_t alpha=bytes[(size_t)y*plan->pitch+x];
                argb=0x00FFFFFFu|((uint32_t)alpha<<24);
            }
            memcpy(expanded+(size_t)y*upload_pitch+x*4,&argb,4);
        }
    }
    /* Compressed bytes are not ARGB texels: never certify zero-RGB shortcuts. */
    next.texel_and=bc1?0:UINT32_MAX;next.texel_or=bc1?UINT32_MAX:0;
    for(unsigned y=0;!bc1 && y<plan->height;++y) for(unsigned x=0;x<plan->width;++x) {
        uint32_t word;memcpy(&word,upload+(size_t)y*upload_pitch+x*4,4);
        next.texel_or|=word;next.texel_and&=word;
    }
    D3D11_TEXTURE2D_DESC desc = {0};
    desc.Width=plan->width; desc.Height=plan->height;
    desc.MipLevels=1; desc.ArraySize=1; desc.SampleDesc.Count=1;
    desc.Format=bc1?DXGI_FORMAT_BC1_UNORM:DXGI_FORMAT_B8G8R8A8_UNORM; desc.Usage=D3D11_USAGE_DEFAULT;
    desc.BindFlags=D3D11_BIND_SHADER_RESOURCE;
    D3D11_SUBRESOURCE_DATA initial = {upload, upload_pitch, 0};
    hr=ID3D11Device_CreateTexture2D(device,&desc,&initial,&next.texture);
    if (SUCCEEDED(hr)) hr=ID3D11Device_CreateShaderResourceView(device,(ID3D11Resource*)next.texture,NULL,&next.view);
    if (SUCCEEDED(hr)) {
        next.plan=*plan; next.valid=1;
        fut_native_texture_destroy(native); *native=next; memset(&next,0,sizeof(next));
    }
done:
    fut_native_texture_destroy(&next); if(expanded) HeapFree(GetProcessHeap(),0,expanded);
    HeapFree(GetProcessHeap(),0,bytes);
    return hr;
}
HRESULT fut_native_texture_sampler(FutNativeTexture *native, ID3D11Device *device,
    const FutGraphicsTextureState *state)
{
    if (!native) return E_INVALIDARG;
    native->sampling_ready=0;
    native->zero_rgb_candidate=0;
    native->quincunx_experiment=0;
    if (!device || !state || !native->valid || (state->valid & 0x2C) != 0x2C) return E_INVALIDARG;
    unsigned min=(state->filter>>16)&255, mag=(state->filter>>24)&15;
    unsigned levels=(state->format>>16)&15;
    /* A one-level texture cannot select between mip levels. MIN=3..6 therefore
     * reduces exactly to the corresponding within-level nearest/linear mode.
     * Never turn convolution MIN=7/MAG=4 into ordinary filtering. */
    unsigned spatial_min;
    if(min==1 || min==3 || min==5) spatial_min=1;
    else if(min==2 || min==4 || min==6) spatial_min=2;
    else spatial_min=0;
    if (!(state->control0 & 0x40000000u) || (state->control0 & ~0x4003FFC0u) ||
        /* The captured single-level A8 state sets reserved filter bit 13;
         * the NV2A LOD-bias mask excludes it, so it has no represented effect. */
        (state->filter & ~0x0F3F2000u) || levels!=1 || !spatial_min ||
        (mag!=1 && mag!=2) || mag!=spatial_min ||
        (state->address & ~0x00070707u)) return E_NOTIMPL;
    D3D11_SAMPLER_DESC desc = {0};
    desc.Filter=spatial_min==1 ? D3D11_FILTER_MIN_MAG_MIP_POINT : D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT;
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
HRESULT fut_native_texture_zero_rgb_candidate(FutNativeTexture *native,
    const FutGraphicsTextureState *state,int coordinates_verified)
{
    if(!native) return E_INVALIDARG;
    native->zero_rgb_candidate=0;
    native->sampling_ready=0;
    native->quincunx_experiment=0;
    if(!state || !native->valid || !native->texture || !native->view) return E_INVALIDARG;
    if(coordinates_verified!=1 || (native->texel_or&0xFFFFFFu) ||
        state->filter!=0x04072000u || state->address!=0x10303u || state->control0!=0x4003FFC0u ||
        (state->valid&0xBF)!=0xBF || native->plan.bytes_per_pixel!=4 ||
        state->format!=(0x11228u|native->plan.dma_context) ||
        state->image_rect!=((native->plan.width<<16)|native->plan.height) ||
        state->control1!=(native->plan.pitch<<16)) return E_NOTIMPL;
    native->zero_rgb_candidate=1;native->sampling_state=*state;
    return S_OK;
}
const char *fut_native_texture_hlsl(void)
{
    return "float4 fut_quincunx_experiment(Texture2D<float4> t,float4 p){\n"
        "int2 q=clamp(int2(floor(p.xy/float2(2,1))),int2(0,0),int2(639,479));\n"
        "int x=q.x*2,y=q.y,x1=min(x+2,1278),y1=min(y+1,479);\n"
        "return t.Load(int3(x+1,y,0))*.5+"
        "(t.Load(int3(x,y,0))+t.Load(int3(x1,y,0))+"
        "t.Load(int3(x,y1,0))+t.Load(int3(x1,y1,0)))*.125;\n}\n"
        "float4 fut_linear_texture(Texture2D<float4> t, SamplerState s, float4 p, float2 extent) {\n"
        "return t.SampleLevel(s, (p.xy / p.w) / extent, 0.0);\n}\n";
}

HRESULT fut_native_texture_quincunx_experiment(FutNativeTexture *n,
    const FutGraphicsTextureState *s,int verified)
{
    if(!n) return E_INVALIDARG;
    n->sampling_ready=n->zero_rgb_candidate=n->quincunx_experiment=0;
    if(!s || !n->valid || !n->texture || !n->view) return E_INVALIDARG;
    if(!fut_graphics_quincunx_approximation_enabled() || verified!=1 ||
        n->plan.width!=1280 || n->plan.height!=480 || n->plan.pitch!=5120 ||
        n->plan.bytes_per_pixel!=4 || (s->valid&0xBF)!=0xBF ||
        s->format!=(0x11228u|n->plan.dma_context) ||
        s->filter!=0x04072000u || s->address!=0x00030303u || s->control0!=0x4003FFC0u ||
        s->control1!=(5120u<<16) || s->image_rect!=((1280u<<16)|480u)) return E_NOTIMPL;
    n->quincunx_experiment=1;n->sampling_state=*s;return S_OK;
}
