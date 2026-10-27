#define COBJMACROS
#include "graphics_native_fragment.h"
#include "graphics_native_pipeline.h"
#include "graphics_shader_interface.h"
#include "graphics_sample_pattern.h"
#include "graphics_experiment.h"
#include <d3d11shader.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

static void fragment_compiler_profile(const char *source,size_t length,int aa2,
    unsigned candidates,HRESULT result,LARGE_INTEGER started)
{
    static _Thread_local uint64_t sequence,total_us;
    LARGE_INTEGER ended,frequency;
    if(!started.QuadPart || !QueryPerformanceCounter(&ended) ||
        !QueryPerformanceFrequency(&frequency) || frequency.QuadPart<=0 ||
        ended.QuadPart<started.QuadPart) return;
    uint64_t elapsed=(uint64_t)(ended.QuadPart-started.QuadPart)*1000000u/(uint64_t)frequency.QuadPart;
    total_us+=elapsed;++sequence;
    if(sequence>4096 && (sequence&(sequence-1u))) return;
    uint64_t fingerprint=UINT64_C(14695981039346656037);
    for(size_t i=0;i<length;++i) fingerprint=(fingerprint^(unsigned char)source[i])*UINT64_C(1099511628211);
    fprintf(stderr,"[FRAGMENT-COMPILER-PROFILE] thread=%lu sequence=%llu aa2=%d candidates=%X bytes=%zu source_hash=%016llX elapsed_us=%llu total_us=%llu result=%08lX\n",
        (unsigned long)GetCurrentThreadId(),(unsigned long long)sequence,aa2,candidates,length,
        (unsigned long long)fingerprint,(unsigned long long)elapsed,(unsigned long long)total_us,(unsigned long)result);
}

HRESULT fut_native_fragment_linkage(ID3DBlob *vertex,ID3DBlob *pixel)
{
    if(!vertex || !pixel) return E_INVALIDARG;
    ID3D11ShaderReflection *vs=NULL,*ps=NULL;
    HRESULT hr=D3DReflect(ID3D10Blob_GetBufferPointer(vertex),ID3D10Blob_GetBufferSize(vertex),
        &IID_ID3D11ShaderReflection,(void**)&vs);
    if(SUCCEEDED(hr)) hr=D3DReflect(ID3D10Blob_GetBufferPointer(pixel),ID3D10Blob_GetBufferSize(pixel),
        &IID_ID3D11ShaderReflection,(void**)&ps);
    D3D11_SHADER_DESC v,p;
    if(SUCCEEDED(hr)) hr=vs->lpVtbl->GetDesc(vs,&v);
    if(SUCCEEDED(hr)) hr=ps->lpVtbl->GetDesc(ps,&p);
    /* DXBC version token high16: pixel=0, vertex=1. Older MinGW D3D11
     * headers omit the SHVER macros (D3D12 headers expose the same token). */
    if(SUCCEEDED(hr) && ((v.Version>>16)!=1 || (p.Version>>16)!=0)) hr=E_INVALIDARG;
    for(unsigned i=0;SUCCEEDED(hr) && i<p.InputParameters;++i) {
        D3D11_SIGNATURE_PARAMETER_DESC input;
        hr=ps->lpVtbl->GetInputParameterDesc(ps,i,&input);
        if(FAILED(hr)) break;
        if(!input.ReadWriteMask) continue;
        int matched=0;
        for(unsigned j=0;j<v.OutputParameters;++j) {
            D3D11_SIGNATURE_PARAMETER_DESC output;
            hr=vs->lpVtbl->GetOutputParameterDesc(vs,j,&output);
            if(FAILED(hr)) break;
            if(input.Register==output.Register && input.SemanticIndex==output.SemanticIndex &&
                !_stricmp(input.SemanticName,output.SemanticName) && input.ComponentType==output.ComponentType &&
                input.SystemValueType==output.SystemValueType && input.Stream==output.Stream &&
                (input.ReadWriteMask&output.Mask)==input.ReadWriteMask) {matched=1;break;}
        }
        if(SUCCEEDED(hr) && !matched) hr=E_NOTIMPL;
    }
    if(vs) vs->lpVtbl->Release(vs);
    if(ps) ps->lpVtbl->Release(ps);
    return hr;
}

static HRESULT no_candidate_resource(ID3DBlob *bytecode,unsigned candidates)
{
    ID3D11ShaderReflection *reflection=NULL;
    HRESULT hr=D3DReflect(ID3D10Blob_GetBufferPointer(bytecode),ID3D10Blob_GetBufferSize(bytecode),
        &IID_ID3D11ShaderReflection,(void**)&reflection);
    if(FAILED(hr)) return hr;
    D3D11_SHADER_DESC shader;
    hr=reflection->lpVtbl->GetDesc(reflection,&shader);
    for(unsigned i=0;SUCCEEDED(hr) && i<shader.BoundResources;++i) {
        D3D11_SHADER_INPUT_BIND_DESC binding;
        hr=reflection->lpVtbl->GetResourceBindingDesc(reflection,i,&binding);
        if(FAILED(hr)) break;
        if(binding.Type!=D3D_SIT_TEXTURE && binding.Type!=D3D_SIT_SAMPLER) continue;
        for(unsigned stage=0;stage<4;++stage) if((candidates&(1u<<stage)) &&
            stage>=binding.BindPoint && stage-binding.BindPoint<binding.BindCount) hr=E_NOTIMPL;
    }
    reflection->lpVtbl->Release(reflection);
    return hr;
}

static HRESULT compile_fragment(ID3D11Device *device,const FutGraphicsKelvinState *state,
    const FutNativeTexture textures[4],ID3D11PixelShader **shader,ID3DBlob **bytecode,int aa2,
    FutNativeFragmentCache *cache,int *cache_hit)
{
    if(cache_hit) *cache_hit=0;
    if(!shader || !bytecode) return E_INVALIDARG;
    *shader=NULL;*bytecode=NULL;
    if(!device || !state || !textures) return E_INVALIDARG;
    if(state->draw.alpha_test>1 || (state->draw.alpha_test &&
        (state->draw.alpha_func<0x200 || state->draw.alpha_func>0x207 ||
         state->draw.alpha_ref>255))) return E_NOTIMPL;
    if(state->specular_enable && (state->specular_enable!=1 ||
        (state->program_mode&3u)!=2u || (state->light_control&~0x00030001u))) return E_NOTIMPL;
    char math[65536],source[73728];size_t length;
    if(!fut_fragment_program_hlsl(&state->fragment,math,sizeof(math),&length)) return E_NOTIMPL;
    int n=snprintf(source,sizeof(source),"%s%s%s%s"
        FUT_GRAPHICS_VARYINGS_HLSL("PSIn")
        "float4 main(PSIn input):SV_TARGET{\n",
        aa2==2?FUT_AA2_COVERAGE_HLSL FUT_AA2_PROJECTIVE_HLSL:
            aa2?FUT_AA2_COVERAGE_HLSL FUT_AA2_AFFINE_HLSL:"",
        fut_native_window_hlsl(),fut_native_texture_hlsl(),math);
    if(n<0 || (size_t)n>=sizeof(source)) return E_OUTOFMEMORY;
    size_t used=n;unsigned candidates=0;
    if(aa2==2) {
        n=snprintf(source+used,sizeof(source)-used,
            "float rhw=1.0/input.pos.w;"
            "input.col0=fut_sample_center_projective(input.col0,rhw);"
            "input.col1=fut_sample_center_projective(input.col1,rhw);"
            "input.t0=fut_sample_center_projective(input.t0,rhw);"
            "input.t1=fut_sample_center_projective(input.t1,rhw);"
            "input.t2=fut_sample_center_projective(input.t2,rhw);"
            "input.t3=fut_sample_center_projective(input.t3,rhw);\n");
        if(n<0 || (size_t)n>=sizeof(source)-used)return E_OUTOFMEMORY;
        used+=n;
    } else if(aa2) {
        n=snprintf(source+used,sizeof(source)-used,
            "input.col0=fut_sample_center(input.col0);input.col1=fut_sample_center(input.col1);"
            "input.t0=fut_sample_center(input.t0);input.t1=fut_sample_center(input.t1);"
            "input.t2=fut_sample_center(input.t2);input.t3=fut_sample_center(input.t3);\n");
        if(n<0 || (size_t)n>=sizeof(source)-used) return E_OUTOFMEMORY;
        used+=n;
    }
    /* Derivatives precede all discards. The AA2 raster grid has two stored
     * columns per logical pixel; restore logical-pixel gradients for LOD. */
    for(unsigned stage=0;stage<4;++stage) if(((state->fragment.stage_program>>(stage*5))&31)==1 &&
        textures[stage].plan.mip_levels>1) {
        n=snprintf(source+used,sizeof(source)-used,
            "float2 mipUv%u=input.t%u.xy/input.t%u.w;"
            "float2 mipDx%u=ddx(mipUv%u)*%u.0,mipDy%u=ddy(mipUv%u);\n",
            stage,stage,stage,stage,stage,aa2?2u:1u,stage,stage);
        if(n<0 || (size_t)n>=sizeof(source)-used)return E_OUTOFMEMORY;
        used+=n;
    }
    if(aa2) {
        n=snprintf(source+used,sizeof(source)-used,"if(!fut_sample_column(input.pos.x))discard;\n");
        if(n<0 || (size_t)n>=sizeof(source)-used)return E_OUTOFMEMORY;
        used+=n;
    }
    n=snprintf(source+used,sizeof(source)-used,"if(!fut_window_visible(input.pos.xy))discard;\n");
    if(n<0 || (size_t)n>=sizeof(source)-used) return E_OUTOFMEMORY;
    used+=n;
    for(unsigned stage=0;stage<4;++stage) {
        unsigned mode=(state->fragment.stage_program>>(stage*5))&31;
        /* Texture/sampler declarations must precede entry point: insert them
         * in a separate prefix below, not within the function. */
        if(mode==1) {
            const FutNativeTexture *native=&textures[stage];
            const FutGraphicsTextureState *current=&state->textures[stage];
            if(!native->valid || !native->view ||
                memcmp(&native->sampling_state,current,sizeof(*current))) return E_NOTIMPL;
            if(native->zero_rgb_candidate && !native->sampling_ready && !(native->texel_or&0xFFFFFFu)) {
                candidates|=1u<<stage;
                /* This Load is an UNIMPLEMENTED alpha dependency, not a
                 * substitute filter. NEVER execute it: reflection below must
                 * prove the whole resource absent from optimized bytecode. */
                n=snprintf(source+used,sizeof(source)-used,
                    "float4 T%u=float4(0,0,0,tex%u.Load(int3(0,0,0)).a);\n",stage,stage);
            } else if(native->quincunx_experiment) {
                if(aa2 || stage!=0 || !((native->quincunx_experiment==1 && fut_graphics_quincunx_approximation_enabled()) ||
                    (native->quincunx_experiment==2 && fut_graphics_quincunx_320_approximation_enabled())) ||
                    native->sampling_ready || native->plan.width!=1280 || native->plan.height!=480)
                    return E_NOTIMPL;
                if(native->quincunx_experiment==2)
                    n=snprintf(source+used,sizeof(source)-used,
                        "float4 T%u=fut_quincunx_experiment(tex%u,float4(input.t%u.xy+0.001f,input.t%u.zw));\n",stage,stage,stage,stage);
                else n=snprintf(source+used,sizeof(source)-used,
                    "float4 T%u=fut_quincunx_experiment(tex%u,input.t%u);\n",stage,stage,stage);
            } else {
                if(!native->sampling_ready || !native->sampler) return E_NOTIMPL;
                if(native->plan.mip_levels>1)
                    n=snprintf(source+used,sizeof(source)-used,
                        "float4 T%u=fut_mip_texture(tex%u,samp%u,mipUv%u,mipDx%u,mipDy%u);\n",
                        stage,stage,stage,stage,stage,stage);
                else n=snprintf(source+used,sizeof(source)-used,
                    "float4 T%u=fut_linear_texture(tex%u,samp%u,input.t%u,float2(%u,%u));\n",
                    stage,stage,stage,stage,(native->plan.storage && native->plan.storage!=7)?1:native->plan.width,
                    (native->plan.storage && native->plan.storage!=7)?1:native->plan.height);
            }
        } else if(mode==4) n=snprintf(source+used,sizeof(source)-used,"float4 T%u=input.t%u;\n",stage,stage);
        else if(mode==0) n=snprintf(source+used,sizeof(source)-used,"float4 T%u=float4(0,0,0,1);\n",stage);
        else return E_NOTIMPL;
        if(n<0 || (size_t)n>=sizeof(source)-used) return E_OUTOFMEMORY;
        used+=n;
    }
    /* With programmable specular enabled, RGB is the original oD1 output.
     * LIGHT_CONTROL bit17 selects its alpha; otherwise exported alpha is 1.
     * A uniform 1 after interpolation equals an interpolated vertex constant.
     * The exact generated source is also the fragment-cache key. */
    if(state->specular_enable && !(state->light_control&0x00020000u)) {
        n=snprintf(source+used,sizeof(source)-used,"input.col1.a=1.0;\n");
        if(n<0 || (size_t)n>=sizeof(source)-used) return E_OUTOFMEMORY;
        used+=n;
    }
    n=snprintf(source+used,sizeof(source)-used,"float4 result=fut_fragment_math(input.col0,input.col1,T0,T1,T2,T3);\n");
    if(n<0 || (size_t)n>=sizeof(source)-used) return E_OUTOFMEMORY;
    used+=n;
    if(state->draw.alpha_test) {
        /* Integer alpha comparison after the final combiner, before any
         * color/depth/stencil writes. The 8-bit rounding model agrees with
         * xemu's pgraph/glsl/psh.c; NV2A half-step precision is unmeasured.
         * This implementation is independently expressed in HLSL. */
        static const char *comparison[]={"false","a < ref","a == ref","a <= ref",
            "a > ref","a != ref","a >= ref","true"};
        n=snprintf(source+used,sizeof(source)-used,
            "int a=int(round(saturate(result.a)*255.0));int ref=%u;"
            "if(!(%s))discard;\n",state->draw.alpha_ref,
            comparison[state->draw.alpha_func-0x200]);
        if(n<0 || (size_t)n>=sizeof(source)-used) return E_OUTOFMEMORY;
        used+=n;
    }
    n=snprintf(source+used,sizeof(source)-used,"return result;}\n");
    if(n<0 || (size_t)n>=sizeof(source)-used) return E_OUTOFMEMORY;
    used+=n;
    const char *prefix="Texture2D<float4> tex0:register(t0);SamplerState samp0:register(s0);\n"
        "Texture2D<float4> tex1:register(t1);SamplerState samp1:register(s1);\n"
        "Texture2D<float4> tex2:register(t2);SamplerState samp2:register(s2);\n"
        "Texture2D<float4> tex3:register(t3);SamplerState samp3:register(s3);\n";
    size_t prefix_length=strlen(prefix);
    if(used+prefix_length>=sizeof(source)) return E_OUTOFMEMORY;
    memmove(source+prefix_length,source,used+1);memcpy(source,prefix,prefix_length);used+=prefix_length;
    if(cache) for(unsigned i=0;i<FUT_NATIVE_FRAGMENT_CACHE_CAPACITY;++i) {
        FutNativeFragmentCacheEntry *entry=&cache->entries[i];
        if(entry->device==device && entry->candidates==candidates && entry->aa2==aa2 &&
            entry->source && entry->source_length==used && entry->shader && entry->bytecode &&
            memcmp(entry->source,source,used)==0) {
            ID3D11PixelShader_AddRef(entry->shader);
            ID3D10Blob_AddRef(entry->bytecode);
            *shader=entry->shader;*bytecode=entry->bytecode;
            if(cache_hit) *cache_hit=1;
            return S_OK;
        }
    }
    ID3DBlob *errors=NULL;
    LARGE_INTEGER profile_started={0};
    const char *profile=getenv("RECOMP_FRAGMENT_COMPILER_PROFILE");
    if(profile && !strcmp(profile,"1")) QueryPerformanceCounter(&profile_started);
    HRESULT hr=D3DCompile(source,used,"recompiled_fragment_program",NULL,NULL,"main","ps_5_0",
        D3DCOMPILE_IEEE_STRICTNESS,0,bytecode,&errors);
    if(errors) {
        fprintf(stderr,"[FRAGMENT-COMPILER] %.*s\n",(int)ID3D10Blob_GetBufferSize(errors),(char*)ID3D10Blob_GetBufferPointer(errors));
        ID3D10Blob_Release(errors);
    }
    if(SUCCEEDED(hr) && candidates) hr=no_candidate_resource(*bytecode,candidates);
    if(SUCCEEDED(hr)) hr=ID3D11Device_CreatePixelShader(device,ID3D10Blob_GetBufferPointer(*bytecode),ID3D10Blob_GetBufferSize(*bytecode),NULL,shader);
    if(FAILED(hr) && *bytecode) {ID3D10Blob_Release(*bytecode);*bytecode=NULL;}
    fragment_compiler_profile(source,used,aa2,candidates,hr,profile_started);
    if(SUCCEEDED(hr) && cache && *shader && *bytecode) {
        char *source_copy=(char *)malloc(used);
        if(source_copy) {
            FutNativeFragmentCacheEntry *entry=&cache->entries[cache->next];
            memcpy(source_copy,source,used);
            if(entry->shader) ID3D11PixelShader_Release(entry->shader);
            if(entry->bytecode) ID3D10Blob_Release(entry->bytecode);
            free(entry->source);
            entry->device=device;entry->source=source_copy;entry->source_length=used;
            entry->candidates=candidates;entry->aa2=aa2;
            entry->shader=*shader;ID3D11PixelShader_AddRef(entry->shader);
            entry->bytecode=*bytecode;ID3D10Blob_AddRef(entry->bytecode);
            cache->next=(cache->next+1u)%FUT_NATIVE_FRAGMENT_CACHE_CAPACITY;
        }
    }
    return hr;
}
HRESULT fut_native_fragment_compile(ID3D11Device *device,const FutGraphicsKelvinState *state,
    const FutNativeTexture textures[4],ID3D11PixelShader **shader,ID3DBlob **bytecode)
{
    return compile_fragment(device,state,textures,shader,bytecode,0,NULL,NULL);
}
HRESULT fut_native_fragment_compile_cached(ID3D11Device *device,const FutGraphicsKelvinState *state,
    const FutNativeTexture textures[4],FutNativeFragmentCache *cache,
    ID3D11PixelShader **shader,ID3DBlob **bytecode,int *cache_hit)
{
    return compile_fragment(device,state,textures,shader,bytecode,0,cache,cache_hit);
}
HRESULT fut_native_fragment_compile_aa2_affine(ID3D11Device *device,const FutGraphicsKelvinState *state,
    const FutNativeTexture textures[4],int unit_w_verified,ID3D11PixelShader **shader,ID3DBlob **bytecode)
{
    if(!shader || !bytecode) return E_INVALIDARG;
    *shader=NULL;*bytecode=NULL;
    if(unit_w_verified!=1) return E_NOTIMPL;
    return compile_fragment(device,state,textures,shader,bytecode,1,NULL,NULL);
}
HRESULT fut_native_fragment_compile_aa2_affine_cached(ID3D11Device *device,
    const FutGraphicsKelvinState *state,const FutNativeTexture textures[4],int unit_w_verified,
    FutNativeFragmentCache *cache,ID3D11PixelShader **shader,ID3DBlob **bytecode,int *cache_hit)
{
    if(!shader || !bytecode) return E_INVALIDARG;
    *shader=NULL;*bytecode=NULL;if(cache_hit) *cache_hit=0;
    if(unit_w_verified!=1) return E_NOTIMPL;
    return compile_fragment(device,state,textures,shader,bytecode,1,cache,cache_hit);
}
HRESULT fut_native_fragment_compile_aa2_projective(ID3D11Device *device,
    const FutGraphicsKelvinState *state,const FutNativeTexture textures[4],int coordinates_verified,
    FutNativeFragmentCache *cache,ID3D11PixelShader **shader,ID3DBlob **bytecode,int *cache_hit)
{
    if(!shader || !bytecode)return E_INVALIDARG;
    *shader=NULL;*bytecode=NULL;if(cache_hit)*cache_hit=0;
    if(coordinates_verified!=1 || !fut_graphics_projective_aa2_approximation_enabled())return E_NOTIMPL;
    return compile_fragment(device,state,textures,shader,bytecode,2,cache,cache_hit);
}
void fut_native_fragment_cache_destroy(FutNativeFragmentCache *cache)
{
    if(!cache) return;
    for(unsigned i=0;i<FUT_NATIVE_FRAGMENT_CACHE_CAPACITY;++i) {
        FutNativeFragmentCacheEntry *entry=&cache->entries[i];
        if(entry->shader) ID3D11PixelShader_Release(entry->shader);
        if(entry->bytecode) ID3D10Blob_Release(entry->bytecode);
        free(entry->source);
    }
    memset(cache,0,sizeof(*cache));
}
