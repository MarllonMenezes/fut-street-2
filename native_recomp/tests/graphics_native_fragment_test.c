#define COBJMACROS
#include "graphics_native_fragment.h"
#include "graphics_native_pipeline.h"
#include "graphics_native_constants.h"
#include "graphics_shader_interface.h"
#include <math.h>
#include <stdio.h>
#include <string.h>
#define CHECK(expr, code) do { if (!(expr)) { fprintf(stderr,"fragment test failed: %d\n",code); return code; } } while (0)
static void configure(FutGraphicsFragmentState *s,uint32_t color_input,uint32_t alpha_input,uint32_t modes)
{
    memset(s,0,sizeof(*s));
    const uint32_t commands[][2]={{0x1E60,1},{0x1E70,modes},{0x260,alpha_input},{0xAC0,color_input},
        {0xAA0,0xC00},{0x1E40,0xC00},{0x288,0xC},{0x28C,0x1C00}};
    for(unsigned i=0;i<sizeof(commands)/sizeof(commands[0]);++i)
        fut_graphics_fragment_state(s,commands[i][0],commands[i][1]);
}
static int verify_math(ID3D11Device *device,ID3D11DeviceContext *context,FutNativeConstants *completion,
    const FutGraphicsFragmentState *state,const float expected[4])
{
    char math[65536],source[66048];size_t length;
    CHECK(fut_fragment_program_hlsl(state,math,sizeof(math),&length),60);
    int n=snprintf(source,sizeof(source),"%sRWStructuredBuffer<float4> outColor:register(u0);"
        "[numthreads(1,1,1)]void main(){outColor[0]=fut_fragment_math("
        "float4(.25,.5,.75,.5),float4(.5,.25,.125,.75),float4(.125,.25,.5,.875),"
        "float4(.5,.5,.5,.5),float4(.5,.5,.5,.5),float4(.5,.5,.5,.5));}",math);
    CHECK(n>0 && n<(int)sizeof(source),61);
    ID3DBlob *blob=NULL,*errors=NULL;
    HRESULT hr=D3DCompile(source,n,"synthetic_combiner_inputs",NULL,NULL,"main","cs_5_0",D3DCOMPILE_IEEE_STRICTNESS,0,&blob,&errors);
    if(errors){fprintf(stderr,"%.*s",(int)ID3D10Blob_GetBufferSize(errors),(char*)ID3D10Blob_GetBufferPointer(errors));ID3D10Blob_Release(errors);}
    CHECK(SUCCEEDED(hr),62);
    ID3D11ComputeShader *shader=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateComputeShader(device,ID3D10Blob_GetBufferPointer(blob),ID3D10Blob_GetBufferSize(blob),NULL,&shader)),63);
    D3D11_BUFFER_DESC desc={0};desc.ByteWidth=16;desc.Usage=D3D11_USAGE_DEFAULT;
    desc.BindFlags=D3D11_BIND_UNORDERED_ACCESS;desc.MiscFlags=D3D11_RESOURCE_MISC_BUFFER_STRUCTURED;desc.StructureByteStride=16;
    ID3D11Buffer *output=NULL,*staging=NULL;ID3D11UnorderedAccessView *uav=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&desc,NULL,&output)),64);
    CHECK(SUCCEEDED(ID3D11Device_CreateUnorderedAccessView(device,(ID3D11Resource*)output,NULL,&uav)),65);
    desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.MiscFlags=0;desc.StructureByteStride=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&desc,NULL,&staging)),66);
    ID3D11DeviceContext_CSSetShader(context,shader,NULL,0);
    ID3D11DeviceContext_CSSetUnorderedAccessViews(context,0,1,&uav,NULL);
    ID3D11DeviceContext_Dispatch(context,1,1,1);
    ID3D11UnorderedAccessView *empty=NULL;ID3D11DeviceContext_CSSetUnorderedAccessViews(context,0,1,&empty,NULL);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)staging,(ID3D11Resource*)output);
    CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),67);
    D3D11_MAPPED_SUBRESOURCE mapped;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)staging,0,D3D11_MAP_READ,0,&mapped)),68);
    int good=1;
    for(unsigned c=0;c<4;++c) {
        float actual=((float*)mapped.pData)[c];
        if(!isfinite(actual) || fabsf(actual-expected[c])>0.00001f) {
            fprintf(stderr,"fragment channel%u got%f expected%f\n",c,actual,expected[c]);good=0;
        }
    }
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)staging,0);ID3D11DeviceContext_ClearState(context);
    ID3D11Buffer_Release(staging);ID3D11UnorderedAccessView_Release(uav);ID3D11Buffer_Release(output);
    ID3D11ComputeShader_Release(shader);ID3D10Blob_Release(blob);return good?0:69;
}
static int verify_alpha_test(ID3D11Device *device,ID3D11DeviceContext *context,
    FutNativeConstants *completion)
{
    FutGraphicsKelvinState state={0};FutNativeTexture textures[4]={0};
    FutNativeFragmentCache cache={0};
    configure(&state.fragment,0x04200000,0x14200000,0);
    state.draw.valid=0xFFFFF;state.draw.alpha_test=1;state.draw.alpha_ref=16;
    state.draw.blend_src=1;state.draw.blend_dst=0;state.draw.blend_op=0x8006;
    state.draw.color_mask=0x01010101;state.draw.front_mode=state.draw.back_mode=0x1B02;
    state.draw.front_face=0x901;state.draw.cull_face=0x405;
    state.depth_test=state.stencil_test=1;state.draw.depth_write=1;
    state.draw.depth_func=state.draw.stencil_func=0x207;
    state.draw.stencil_read=state.draw.stencil_write=255;state.draw.stencil_ref=0x3A;
    state.draw.stencil_op[0]=state.draw.stencil_op[1]=0x1E00;
    state.draw.stencil_op[2]=0x1E01;
    FutGraphicsSurfacePlan surface={0};surface.width=surface.height=1;
    surface.logical_width=surface.logical_height=surface.samples=1;
    FutNativePipeline pipeline={0};
    const char *vs_source=FUT_GRAPHICS_VARYINGS_HLSL("O")
        "cbuffer TestAlpha:register(b3){float alpha;};"
        "O main(uint id:SV_VertexID){O o=(O)0;"
        "float2 p=float2(id==1?3:-1,id==2?3:-1);o.pos=float4(p,.5,1);"
        "o.col0=float4(1,0,0,alpha);return o;}";
    ID3DBlob *vs_blob=NULL;ID3D11VertexShader *vs=NULL;
    CHECK(SUCCEEDED(D3DCompile(vs_source,strlen(vs_source),"alpha_fixture",NULL,NULL,
        "main","vs_5_0",0,0,&vs_blob,NULL)),120);
    CHECK(SUCCEEDED(ID3D11Device_CreateVertexShader(device,ID3D10Blob_GetBufferPointer(vs_blob),
        ID3D10Blob_GetBufferSize(vs_blob),NULL,&vs)),121);
    D3D11_BUFFER_DESC bd={0};bd.ByteWidth=16;bd.Usage=D3D11_USAGE_DEFAULT;
    bd.BindFlags=D3D11_BIND_CONSTANT_BUFFER;ID3D11Buffer *alpha_buffer=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&bd,NULL,&alpha_buffer)),122);
    D3D11_TEXTURE2D_DESC td={0};td.Width=td.Height=td.MipLevels=td.ArraySize=1;
    td.SampleDesc.Count=1;td.Usage=D3D11_USAGE_DEFAULT;td.Format=DXGI_FORMAT_R8G8B8A8_UNORM;
    td.BindFlags=D3D11_BIND_RENDER_TARGET;
    ID3D11Texture2D *color=NULL,*depth=NULL,*color_read=NULL,*depth_read=NULL;
    ID3D11RenderTargetView *rt=NULL;ID3D11DepthStencilView *ds=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&color)),123);
    CHECK(SUCCEEDED(ID3D11Device_CreateRenderTargetView(device,(ID3D11Resource*)color,NULL,&rt)),124);
    td.Usage=D3D11_USAGE_STAGING;td.BindFlags=0;td.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&color_read)),125);
    td.Format=DXGI_FORMAT_R24G8_TYPELESS;td.Usage=D3D11_USAGE_DEFAULT;
    td.BindFlags=D3D11_BIND_DEPTH_STENCIL;td.CPUAccessFlags=0;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&depth)),126);
    D3D11_DEPTH_STENCIL_VIEW_DESC dd={0};dd.Format=DXGI_FORMAT_D24_UNORM_S8_UINT;
    dd.ViewDimension=D3D11_DSV_DIMENSION_TEXTURE2D;
    CHECK(SUCCEEDED(ID3D11Device_CreateDepthStencilView(device,(ID3D11Resource*)depth,&dd,&ds)),127);
    td.Usage=D3D11_USAGE_STAGING;td.BindFlags=0;td.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&depth_read)),128);
    /* Independent truth table for input alpha 15,16,17 against reference16. */
    static const unsigned pass[8][3]={{0,0,0},{1,0,0},{0,1,0},{1,1,0},
        {0,0,1},{1,0,1},{0,1,1},{1,1,1}};
    ID3D11PixelShader *invalid=NULL;ID3DBlob *invalid_blob=NULL;
    state.draw.alpha_func=0x208;
    CHECK(fut_native_fragment_compile(device,&state,textures,&invalid,&invalid_blob)==E_NOTIMPL &&
        !invalid && !invalid_blob,136);
    state.draw.alpha_func=0x204;state.draw.alpha_ref=256;
    CHECK(fut_native_fragment_compile(device,&state,textures,&invalid,&invalid_blob)==E_NOTIMPL &&
        !invalid && !invalid_blob,137);
    state.draw.alpha_ref=16;
    for(unsigned func=0;func<8;++func) {
        state.draw.alpha_func=0x200+func;
        ID3D11PixelShader *ps=NULL;ID3DBlob *blob=NULL;
        int hit=99;
        CHECK(SUCCEEDED(fut_native_fragment_compile_cached(device,&state,textures,&cache,&ps,&blob,&hit)) && !hit,129);
        ID3D11PixelShader_Release(ps);ID3D10Blob_Release(blob);
        CHECK(SUCCEEDED(fut_native_fragment_compile_cached(device,&state,textures,&cache,&ps,&blob,&hit)) && hit,140);
        CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),130);
        ID3D11DeviceContext_VSSetShader(context,vs,NULL,0);
        ID3D11DeviceContext_VSSetConstantBuffers(context,3,1,&alpha_buffer);
        ID3D11DeviceContext_PSSetShader(context,ps,NULL,0);
        ID3D11DeviceContext_IASetInputLayout(context,NULL);
        ID3D11DeviceContext_IASetPrimitiveTopology(context,D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        for(unsigned input=0;input<3;++input) {
            float values[4]={(15.0f+input)/255.0f,0,0,0};
            ID3D11DeviceContext_UpdateSubresource(context,(ID3D11Resource*)alpha_buffer,0,NULL,values,0,0);
            const float blue[4]={0,0,1,1};
            ID3D11DeviceContext_ClearRenderTargetView(context,rt,blue);
            ID3D11DeviceContext_ClearDepthStencilView(context,ds,D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL,1,0x5A);
            ID3D11DeviceContext_OMSetRenderTargets(context,1,&rt,ds);
            ID3D11DeviceContext_Draw(context,3,0);
            ID3D11DeviceContext_OMSetRenderTargets(context,0,NULL,NULL);
            ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)color_read,(ID3D11Resource*)color);
            ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)depth_read,(ID3D11Resource*)depth);
            CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),131);
            D3D11_MAPPED_SUBRESOURCE mapped;uint32_t rgba,zs;
            CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)color_read,0,D3D11_MAP_READ,0,&mapped)),132);
            memcpy(&rgba,mapped.pData,4);ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)color_read,0);
            CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)depth_read,0,D3D11_MAP_READ,0,&mapped)),133);
            memcpy(&zs,mapped.pData,4);ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)depth_read,0);
            uint32_t expected=pass[func][input]?((15u+input)<<24)|255u:0xFFFF0000u;
            if(rgba!=expected || (zs>>24)!=(pass[func][input]?0x3Au:0x5Au) ||
                (pass[func][input]?((zs&0xFFFFFF)<0x7FFFFF || (zs&0xFFFFFF)>0x800001):((zs&0xFFFFFF)!=0xFFFFFF))) {
                fprintf(stderr,"alpha func=%u input=%u rgba=%08X zs=%08X expected=%08X\n",func,input,rgba,zs,expected);
                return 134;
            }
        }
        ID3D11PixelShader_Release(ps);ID3D10Blob_Release(blob);
    }
    ID3D11DeviceContext_ClearState(context);fut_native_pipeline_destroy(&pipeline);
    fut_native_fragment_cache_destroy(&cache);
    ID3D11RenderTargetView_Release(rt);ID3D11DepthStencilView_Release(ds);
    ID3D11Texture2D_Release(color);ID3D11Texture2D_Release(depth);
    ID3D11Texture2D_Release(color_read);ID3D11Texture2D_Release(depth_read);
    ID3D11Buffer_Release(alpha_buffer);ID3D11VertexShader_Release(vs);ID3D10Blob_Release(vs_blob);
    puts("Alpha test: eight comparisons at below/equal/above reference; discarded pixels preserve color, depth and stencil on WARP");
    return 0;
}
static int verify_linkage(void)
{
    const char *old_vs="struct O{float4 pos:SV_POSITION;float4 col0:COLOR0;};"
        "O main(float4 p:POSITION){O o;o.pos=p;o.col0=float4(.25,.5,.75,1);return o;}";
    const char *new_vs=FUT_GRAPHICS_VARYINGS_HLSL("O")
        "O main(float4 p:POSITION){O o=(O)0;o.pos=p;o.screen=p;o.col0=float4(.25,.5,.75,1);return o;}";
    const char *new_ps=FUT_GRAPHICS_VARYINGS_HLSL("O")
        "float4 main(O i):SV_TARGET{return i.col0;}";
    ID3DBlob *old=NULL,*current=NULL,*pixel=NULL;
    CHECK(SUCCEEDED(D3DCompile(old_vs,strlen(old_vs),"old_signature",NULL,NULL,"main","vs_5_0",0,0,&old,NULL)),110);
    CHECK(SUCCEEDED(D3DCompile(new_vs,strlen(new_vs),"shared_signature",NULL,NULL,"main","vs_5_0",0,0,&current,NULL)),111);
    CHECK(SUCCEEDED(D3DCompile(new_ps,strlen(new_ps),"shared_signature",NULL,NULL,"main","ps_5_0",0,0,&pixel,NULL)),112);
    CHECK(fut_native_fragment_linkage(old,pixel)==E_NOTIMPL,113);
    CHECK(SUCCEEDED(fut_native_fragment_linkage(current,pixel)),114);
    CHECK(fut_native_fragment_linkage(NULL,pixel)==E_INVALIDARG &&
        fut_native_fragment_linkage(pixel,current)==E_INVALIDARG,115);
    ID3D10Blob_Release(old);ID3D10Blob_Release(current);ID3D10Blob_Release(pixel);
    return 0;
}
static int verify_cache(ID3D11Device *device)
{
    FutNativeFragmentCache cache={0};FutGraphicsKelvinState state={0},base;
    FutNativeTexture textures[4]={0};
    configure(&state.fragment,0x04200000,0x14200000,0);base=state;
    ID3D11PixelShader *first=NULL,*ps=NULL;ID3DBlob *first_blob=NULL,*blob=NULL;int hit=99;
    CHECK(SUCCEEDED(fut_native_fragment_compile_cached(device,&state,textures,&cache,&first,&first_blob,&hit)) && !hit,150);
    state.fragment.color_input[0]=0x05200000;
    CHECK(SUCCEEDED(fut_native_fragment_compile_cached(device,&state,textures,&cache,&ps,&blob,&hit)) && !hit,151);
    ID3D11PixelShader_Release(ps);ID3D10Blob_Release(blob);state=base;
    CHECK(SUCCEEDED(fut_native_fragment_compile_cached(device,&state,textures,&cache,&ps,&blob,&hit)) && hit && ps==first && blob==first_blob,152);
    ID3D11PixelShader_Release(ps);ID3D10Blob_Release(blob);
    CHECK(SUCCEEDED(fut_native_fragment_compile_aa2_affine_cached(device,&state,textures,1,&cache,&ps,&blob,&hit)) && !hit,153);
    ID3D11PixelShader_Release(ps);ID3D10Blob_Release(blob);
    CHECK(SUCCEEDED(fut_native_fragment_compile_aa2_affine_cached(device,&state,textures,1,&cache,&ps,&blob,&hit)) && hit,154);
    ID3D11PixelShader_Release(ps);ID3D10Blob_Release(blob);
    unsigned next=cache.next;
    CHECK(fut_native_fragment_compile_aa2_affine_cached(device,&state,textures,0,&cache,&ps,&blob,&hit)==E_NOTIMPL && !ps && !blob && !hit && cache.next==next,155);
    ID3D11Device *other=NULL;D3D_FEATURE_LEVEL level;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,D3D11_SDK_VERSION,&other,&level,NULL)),156);
    CHECK(SUCCEEDED(fut_native_fragment_compile_cached(other,&state,textures,&cache,&ps,&blob,&hit)) && !hit,157);
    ID3D11Device *owner=NULL;ID3D11PixelShader_GetDevice(ps,&owner);CHECK(owner==other,158);ID3D11Device_Release(owner);
    ID3D11PixelShader_Release(ps);ID3D10Blob_Release(blob);
    next=cache.next;
    CHECK(SUCCEEDED(fut_native_fragment_compile_cached(device,&state,textures,NULL,&ps,&blob,&hit)) && !hit && cache.next==next,159);
    CHECK(ID3D10Blob_GetBufferSize(blob)==ID3D10Blob_GetBufferSize(first_blob) &&
        !memcmp(ID3D10Blob_GetBufferPointer(blob),ID3D10Blob_GetBufferPointer(first_blob),ID3D10Blob_GetBufferSize(blob)),160);
    ID3D11PixelShader_Release(ps);ID3D10Blob_Release(blob);
    for(unsigned invalid=0;invalid<5;++invalid) {
        state=base;
        if(invalid==0){state.draw.alpha_test=1;state.draw.alpha_ref=256;state.draw.alpha_func=0x204;}
        if(invalid==1){state.draw.alpha_test=1;state.draw.alpha_func=0x208;}
        if(invalid==2)state.fragment.valid=0;
        CHECK(FAILED(fut_native_fragment_compile_cached(invalid==3?NULL:device,invalid==4?NULL:&state,textures,&cache,&ps,&blob,&hit)) && !hit && !ps && !blob && cache.next==next,161);
    }
    fut_native_fragment_cache_destroy(&cache);
    state=base;state.draw.alpha_test=1;state.draw.alpha_func=0x204;
    for(unsigned i=0;i<=FUT_NATIVE_FRAGMENT_CACHE_CAPACITY;++i) {
        state.draw.alpha_ref=i;
        CHECK(SUCCEEDED(fut_native_fragment_compile_cached(device,&state,textures,&cache,&ps,&blob,&hit)) && !hit,162);
        ID3D11PixelShader_Release(ps);ID3D10Blob_Release(blob);
    }
    CHECK(cache.next==1,163);
    CHECK(SUCCEEDED(fut_native_fragment_compile_cached(device,&state,textures,&cache,&ps,&blob,&hit)) && hit,164);
    ID3D11PixelShader_Release(ps);ID3D10Blob_Release(blob);state.draw.alpha_ref=0;
    CHECK(SUCCEEDED(fut_native_fragment_compile_cached(device,&state,textures,&cache,&ps,&blob,&hit)) && !hit,165);
    ID3D11PixelShader_Release(ps);ID3D10Blob_Release(blob);
    fut_native_fragment_cache_destroy(&cache);fut_native_fragment_cache_destroy(&cache);
    for(unsigned i=0;i<FUT_NATIVE_FRAGMENT_CACHE_CAPACITY;++i)
        CHECK(!cache.entries[i].source && !cache.entries[i].shader && !cache.entries[i].bytecode,166);
    CHECK(!cache.next && ID3D10Blob_GetBufferSize(first_blob)>0,167);
    ID3D11PixelShader_GetDevice(first,&owner);CHECK(owner==device,168);ID3D11Device_Release(owner);
    ID3D11PixelShader_Release(first);ID3D10Blob_Release(first_blob);ID3D11Device_Release(other);
    puts("Exact fragment cache: alternation, source/mode/device separation, invalid states, FIFO and COM ownership verified");
    return 0;
}
int main(void)
{
    CHECK(verify_linkage()==0,116);
    FutGraphicsFragmentState state;
    configure(&state,0x04200000,0x14200000,0);
    FutGraphicsFragmentState before=state;
    CHECK(fut_graphics_fragment_state(&state,0x261,0)==FUT_FRAGMENT_REJECTED &&
        fut_graphics_fragment_state(&state,0x284,0)==FUT_FRAGMENT_NOT_HANDLED && !memcmp(&before,&state,sizeof(state)),1);
    CHECK(fut_graphics_fragment_state(&state,0x1E74,0)==FUT_FRAGMENT_APPLIED &&
        state.dot_rgbmapping==0,11);
    char text[65536];size_t length=999;
    CHECK(fut_fragment_program_hlsl(&state,text,sizeof(text),&length) && length>0,2);
    state.color_output[0]|=0x2000; /* AB dot output. */
    state.dot_rgbmapping=1;
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),12);
    state=before;
    CHECK(!fut_fragment_program_hlsl(&state,text,4,&length) && !length && !text[0],3);
    state.color_input_valid=0;
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length) && !length && !text[0],4);state=before;
    state.color_input[0]=0x0D200000; /* Initial scratch R1 is not a proven zero. */
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),5);state=before;
    state.color_output[0]=0xC0C; /* Conflicting writes cannot be invented. */
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),6);state=before;
    state.color_output[0]=0x4C00; /* MUX requires fixed-point selector semantics. */
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),7);state=before;
    state.final_words[0]=0xE;
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),8);state=before;
    state.control=9;CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),9);state=before;
    state.stage_program=6;CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),10);state=before;
    ID3D11Device *device=NULL;ID3D11DeviceContext *context=NULL;D3D_FEATURE_LEVEL level;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,D3D11_SDK_VERSION,&device,&level,&context)),11);
    FutNativeConstants completion;CHECK(SUCCEEDED(fut_native_constants_init(&completion,device,context)),12);
    CHECK(verify_alpha_test(device,context,&completion)==0,135);
    CHECK(verify_math(device,context,&completion,&state,(float[]){.25,.5,.75,.5})==0,13);
    /* Color and alpha read the PREVIOUS stage simultaneously. An in-place
     * RGB write would incorrectly change the subsequent alpha from .75 to .5. */
    fut_graphics_fragment_state(&state,0x1E60,2);
    fut_graphics_fragment_state(&state,0xAC4,0x1C200000);
    fut_graphics_fragment_state(&state,0x264,0x0C200000);
    fut_graphics_fragment_state(&state,0x1E44,0xC00);
    fut_graphics_fragment_state(&state,0xAA4,0xC00);
    CHECK(verify_math(device,context,&completion,&state,(float[]){.5,.5,.5,.75})==0,14);
    const float mapped[8][3]={{.25,.5,.75},{.75,.5,.25},{0,0,.5},{.5,0,0},{0,0,.25},{.25,0,0},{.25,.5,.75},{0,0,0}};
    for(unsigned mapping=0;mapping<8;++mapping) {
        configure(&state,((mapping*32u+4u)<<24)|0x00200000,0x14200000,0);
        float expected[]={mapped[mapping][0],mapped[mapping][1],mapped[mapping][2],.5};
        CHECK(verify_math(device,context,&completion,&state,expected)==0,15);
    }
    const unsigned output_modes[]={0,8,16,24,32,48};
    for(unsigned i=0;i<6;++i) {
        configure(&state,0x04200000,0x14200000,0);state.color_output[0]=(output_modes[i]<<12)|0xC00;
        float expected[4]={0,0,0,.5};
        for(unsigned c=0;c<3;++c) {
            float value=(c+1)*.25f;
            if(output_modes[i]&8)value-=.5f;
            value*=i==4?4:(i==5?.5f:(output_modes[i]&16?2:1));
            expected[c]=value<0?0:(value>1?1:value);
        }
        CHECK(verify_math(device,context,&completion,&state,expected)==0,16);
    }
    configure(&state,0x04050000,0x14200000,0);state.color_output[0]=0x2C00;
    CHECK(verify_math(device,context,&completion,&state,(float[]){.34375,.34375,.34375,.5})==0,17);
    configure(&state,0x01200000,0x14200000,0);
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),21); /* Missing referenced factor. */
    fut_graphics_fragment_state(&state,0xA60,0xFF204080);
    fut_graphics_fragment_state(&state,0xA64,0xFF804020);
    fut_graphics_fragment_state(&state,0x1E60,0x1002);
    fut_graphics_fragment_state(&state,0xAC4,0x0C010000);
    fut_graphics_fragment_state(&state,0x264,0x14200000);
    fut_graphics_fragment_state(&state,0x1E44,0xC00);fut_graphics_fragment_state(&state,0xAA4,0xC00);
    CHECK(verify_math(device,context,&completion,&state,
        (float[]){32.0f*128/(255*255),64.0f*64/(255*255),128.0f*32/(255*255),.5})==0,22);
    fut_graphics_fragment_state(&state,0x1E60,2);
    CHECK(verify_math(device,context,&completion,&state,
        (float[]){32.0f*32/(255*255),64.0f*64/(255*255),128.0f*128/(255*255),.5})==0,23);
    configure(&state,0x04200000,0x14200000,0);
    for(unsigned stage=1;stage<8;++stage) {
        fut_graphics_fragment_state(&state,0xAC0+stage*4,0x0C200000);
        fut_graphics_fragment_state(&state,0x260+stage*4,0x1C200000);
        fut_graphics_fragment_state(&state,0x1E40+stage*4,0xC00);
        fut_graphics_fragment_state(&state,0xAA0+stage*4,0xC00);
    }
    fut_graphics_fragment_state(&state,0x1E60,8);
    CHECK(verify_math(device,context,&completion,&state,(float[]){.25,.5,.75,.5})==0,24);
    /* Captured words, SYNTHETIC sampled color. Final D=R0.rgb, NOT zero:
     * this program genuinely depends on the texture's filtered RGB. */
    configure(&state,0x08200000,0x14200000,1);state.final_words[0]=0xC;state.final_words[1]=0x1C80;
    CHECK(verify_math(device,context,&completion,&state,(float[]){.125,.25,.5,.5})==0,18);
    CHECK(verify_cache(device)==0,169);
    FutGraphicsKelvinState kelvin={0};kelvin.fragment=state;
    FutNativeTexture textures[4]={0};ID3D11PixelShader *shader=NULL;ID3DBlob *bytecode=NULL;
    CHECK(fut_native_fragment_compile(device,&kelvin,textures,&shader,&bytecode)==E_NOTIMPL && !shader && !bytecode,19);
    /* No-texture supported shader adapter must compile to actual PS bytecode. */
    configure(&kelvin.fragment,0x04200000,0x14200000,0);
    CHECK(SUCCEEDED(fut_native_fragment_compile(device,&kelvin,textures,&shader,&bytecode)) && shader && bytecode,20);
    ID3D11PixelShader_Release(shader);ID3D10Blob_Release(bytecode);
    fut_native_constants_destroy(&completion);ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    puts("Combiner HLSL: parallel RGB/alpha, input mapping, output bias/scale/dot and strict rejection verified on WARP; no NV2A precision/original draw proof");
    return 0;
}
