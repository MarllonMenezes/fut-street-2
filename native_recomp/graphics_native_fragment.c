#define COBJMACROS
#include "graphics_native_fragment.h"
#include "graphics_native_pipeline.h"
#include <stdio.h>
#include <string.h>

HRESULT fut_native_fragment_compile(ID3D11Device *device,const FutGraphicsKelvinState *state,
    const FutNativeTexture textures[4],ID3D11PixelShader **shader,ID3DBlob **bytecode)
{
    if(!shader || !bytecode) return E_INVALIDARG;
    *shader=NULL;*bytecode=NULL;
    if(!device || !state || !textures) return E_INVALIDARG;
    char math[65536],source[73728];size_t length;
    if(!fut_fragment_program_hlsl(&state->fragment,math,sizeof(math),&length)) return E_NOTIMPL;
    int n=snprintf(source,sizeof(source),"%s%s%s"
        "struct PSIn{float4 pos:SV_POSITION;float4 col0:COLOR0;float4 col1:COLOR1;"
        "float fog:FOG;float4 t0:TEXCOORD0;float4 t1:TEXCOORD1;float4 t2:TEXCOORD2;float4 t3:TEXCOORD3;};\n"
        "float4 main(PSIn input):SV_TARGET{if(!fut_window_visible(input.pos.xy))discard;\n",
        fut_native_window_hlsl(),fut_native_texture_hlsl(),math);
    if(n<0 || (size_t)n>=sizeof(source)) return E_OUTOFMEMORY;
    size_t used=n;
    for(unsigned stage=0;stage<4;++stage) {
        unsigned mode=(state->fragment.stage_program>>(stage*5))&31;
        /* Texture/sampler declarations must precede entry point: insert them
         * in a separate prefix below, not within the function. */
        if(mode==1) {
            const FutNativeTexture *native=&textures[stage];
            const FutGraphicsTextureState *current=&state->textures[stage];
            if(!native->valid || !native->sampling_ready || !native->view || !native->sampler ||
                memcmp(&native->sampling_state,current,sizeof(*current))) return E_NOTIMPL;
            n=snprintf(source+used,sizeof(source)-used,
                "float4 T%u=fut_linear_texture(tex%u,samp%u,input.t%u,float2(%u,%u));\n",
                stage,stage,stage,stage,native->plan.width,native->plan.height);
        } else if(mode==4) n=snprintf(source+used,sizeof(source)-used,"float4 T%u=input.t%u;\n",stage,stage);
        else if(mode==0) n=snprintf(source+used,sizeof(source)-used,"float4 T%u=float4(0,0,0,1);\n",stage);
        else return E_NOTIMPL;
        if(n<0 || (size_t)n>=sizeof(source)-used) return E_OUTOFMEMORY;
        used+=n;
    }
    n=snprintf(source+used,sizeof(source)-used,"return fut_fragment_math(input.col0,input.col1,T0,T1,T2,T3);}\n");
    if(n<0 || (size_t)n>=sizeof(source)-used) return E_OUTOFMEMORY;
    used+=n;
    const char *prefix="Texture2D<float4> tex0:register(t0);SamplerState samp0:register(s0);\n"
        "Texture2D<float4> tex1:register(t1);SamplerState samp1:register(s1);\n"
        "Texture2D<float4> tex2:register(t2);SamplerState samp2:register(s2);\n"
        "Texture2D<float4> tex3:register(t3);SamplerState samp3:register(s3);\n";
    size_t prefix_length=strlen(prefix);
    if(used+prefix_length>=sizeof(source)) return E_OUTOFMEMORY;
    memmove(source+prefix_length,source,used+1);memcpy(source,prefix,prefix_length);used+=prefix_length;
    ID3DBlob *errors=NULL;
    HRESULT hr=D3DCompile(source,used,"recompiled_fragment_program",NULL,NULL,"main","ps_5_0",
        D3DCOMPILE_IEEE_STRICTNESS,0,bytecode,&errors);
    if(errors) {
        fprintf(stderr,"[FRAGMENT-COMPILER] %.*s\n",(int)ID3D10Blob_GetBufferSize(errors),(char*)ID3D10Blob_GetBufferPointer(errors));
        ID3D10Blob_Release(errors);
    }
    if(SUCCEEDED(hr)) hr=ID3D11Device_CreatePixelShader(device,ID3D10Blob_GetBufferPointer(*bytecode),ID3D10Blob_GetBufferSize(*bytecode),NULL,shader);
    if(FAILED(hr) && *bytecode) {ID3D10Blob_Release(*bytecode);*bytecode=NULL;}
    return hr;
}
