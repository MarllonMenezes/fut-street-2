#define COBJMACROS
#include "graphics_native_fragment.h"
#include "graphics_native_pipeline.h"
#include "graphics_native_constants.h"
#include "graphics_shader_interface.h"
#include "graphics_sample_pattern.h"
#include "graphics_vertex_program.h"
#include <math.h>
#include <stdio.h>
#include <stdlib.h>
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
static int dual_mip_read(void *context,uint32_t address,void *output,size_t size)
{
    if(address<0x80000000u || address-0x80000000u>512u ||
        size>512u-(address-0x80000000u)) return 0;
    memcpy(output,(const unsigned char*)context+(address-0x80000000u),size);
    return 1;
}
static int verify_dual_projective_mips(ID3D11Device *device,ID3D11DeviceContext *context,
    FutNativeConstants *completion)
{
    /* Entirely authored BC blocks: BC2 red/green with alpha 1/8:15;
     * BC1 blue/white. Four 8x8 blocks followed by one 4x4 mip block.
     * These colors are the independent oracle, not decoded production output. */
    unsigned char memory[512]={0};
    for(unsigned level=0;level<2;++level) for(unsigned block=0;block<(level?1u:4u);++block) {
        unsigned char *bc2=memory+(level?64u:0u)+block*16u;
        unsigned char *bc1=memory+256u+(level?32u:0u)+block*8u;
        uint16_t color2=level?0x07E0u:0xF800u,color1=level?0xFFFFu:0x001Fu;
        memset(bc2,level?0x88:0xFF,8);
        memcpy(bc2+8,&color2,2);memcpy(bc1,&color1,2);
    }
    FutGraphicsKelvinState state={0};FutNativeTexture textures[4]={0};
    configure(&state.fragment,0x08200920u,0x18201920u,0x21u);
    /* R0 = (T0 + T1)/2, independently for RGB and alpha. */
    state.fragment.color_output[0]=state.fragment.alpha_output[0]=0x30C00u;
    state.draw.valid=0xFFFFF;
    for(unsigned i=0;i<8;++i)state.draw.window_x[i]=state.draw.window_y[i]=7u<<16;
    state.draw.blend_src=1;state.draw.blend_op=0x8006;state.draw.color_mask=0x01010101;
    state.draw.front_mode=state.draw.back_mode=0x1B02;
    state.draw.front_face=0x901;state.draw.cull_face=0x405;
    state.draw.depth_func=state.draw.stencil_func=0x207;
    state.draw.stencil_op[0]=state.draw.stencil_op[1]=state.draw.stencil_op[2]=0x1E00;
    FutGraphicsDmaView dma={0x80000000u,sizeof(memory)};
    for(unsigned stage=0;stage<2;++stage) {
        state.textures[stage]=(FutGraphicsTextureState){.valid=stage?0xFE2Fu:0x3BFu,
            .offset=stage?256u:0u,.format=stage?0x03320C29u:0x03320E29u,
            .address=0x10101u,.control0=0x4003FFC0u,.filter=0x02060000u};
        /* Stage1 deliberately has neither LC_IMAGE nor CONTROL1 initialized. */
        if(!stage) {state.textures[stage].control1=0x14000000u;state.textures[stage].image_rect=0x050001E0u;}
        FutGraphicsTexturePlan plan;
        CHECK(fut_graphics_texture_plan(&state.textures[stage],&dma,&plan)==FUT_TEXTURE_READY &&
            plan.mip_levels==2 && plan.span==(stage?40u:80u),170);
        CHECK(SUCCEEDED(fut_native_texture_upload(&textures[stage],device,&plan,dual_mip_read,memory)),171);
    }
    const char *vs_source=FUT_AA2_COVERAGE_HLSL FUT_GRAPHICS_VARYINGS_HLSL("O")
        "cbuffer DualLod:register(b3){float4 scales;};"
        "O main(uint id:SV_VertexID){O o=(O)0;"
        "float2 p=float2(id==1?3:-1,id==2?3:-1);"
        "o.pos=fut_sample_position(float4(p,.5,1));"
        "float u=(p.x+1)*.5;"
        "o.t0=float4(u*scales.x,.375,0,1);o.t1=float4(u*scales.y,.625,0,1);return o;}";
    ID3DBlob *vblob=NULL;ID3D11VertexShader *vs=NULL;
    CHECK(SUCCEEDED(D3DCompile(vs_source,strlen(vs_source),"authored_dual_mip_geometry",NULL,NULL,
        "main","vs_5_0",D3DCOMPILE_IEEE_STRICTNESS,0,&vblob,NULL)),172);
    CHECK(SUCCEEDED(ID3D11Device_CreateVertexShader(device,ID3D10Blob_GetBufferPointer(vblob),
        ID3D10Blob_GetBufferSize(vblob),NULL,&vs)),173);
    D3D11_BUFFER_DESC bd={0};bd.ByteWidth=16;bd.Usage=D3D11_USAGE_DEFAULT;bd.BindFlags=D3D11_BIND_CONSTANT_BUFFER;
    ID3D11Buffer *scale_buffer=NULL,*passes[2]={0};
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&bd,NULL,&scale_buffer)),174);
    for(unsigned slot=0;slot<2;++slot) {
        FutGraphicsSamplePass pass;CHECK(fut_graphics_aa2_coverage_pass(8,8,slot,&pass),175);
        D3D11_SUBRESOURCE_DATA initial={&pass,0,0};
        CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&bd,&initial,&passes[slot])),176);
    }
    FutGraphicsSurfacePlan surface={.width=16,.height=8,.logical_width=8,.logical_height=8,.samples=2};
    FutNativePipeline pipeline={0};
    CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),177);
    D3D11_TEXTURE2D_DESC td={0};td.Width=16;td.Height=8;td.MipLevels=td.ArraySize=td.SampleDesc.Count=1;
    td.Format=DXGI_FORMAT_R32G32B32A32_FLOAT;td.Usage=D3D11_USAGE_DEFAULT;td.BindFlags=D3D11_BIND_RENDER_TARGET;
    ID3D11Texture2D *target=NULL,*readback=NULL;ID3D11RenderTargetView *view=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&target)),178);
    CHECK(SUCCEEDED(ID3D11Device_CreateRenderTargetView(device,(ID3D11Resource*)target,NULL,&view)),179);
    td.Usage=D3D11_USAGE_STAGING;td.BindFlags=0;td.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&readback)),180);
    static const struct {float scale[2],lod[2];unsigned bias,stages;} cases[]={
        {{.5f,.5f},{0,0},0,0x21},{{2,.5f},{1,0},0,0x21},{{.5f,2},{0,1},0,0x21},
        {{2,2},{1,1},0,0x21},{{1,1},{.5f,.5f},128,0x21},
        /* Observed FILTER=02063F1B: signed bias -229/256. Independent
         * gradients select clamped LOD1 and LOD(1-229/256)=27/256. */
        {{4,2},{1,27.0f/256.0f},0x3F1Bu,0x21},
        /* T1 remains enabled, uploaded and bound as white, but mode0 does
         * not sample it. The combiner must return T0 red without T1. */
        {{.5f,2},{0,1},0,1}};
    _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","1");
    for(unsigned test=0;test<sizeof(cases)/sizeof(cases[0]);++test) {
        int sampled_secondary=cases[test].stages==0x21u;
        configure(&state.fragment,sampled_secondary?0x08200920u:0x08200000u,
            sampled_secondary?0x18201920u:0x18200000u,cases[test].stages);
        if(sampled_secondary)
            state.fragment.color_output[0]=state.fragment.alpha_output[0]=0x30C00u;
        for(unsigned stage=0;stage<2;++stage) {
            state.textures[stage].filter=0x02060000u|cases[test].bias;
            CHECK(SUCCEEDED(fut_native_texture_sampler(&textures[stage],device,&state.textures[stage])),181);
        }
        ID3D11PixelShader *ps=NULL;ID3DBlob *pblob=NULL;
        /* Synchronization is mandatory only when the shader samples T1. */
        if(sampled_secondary) {
            state.textures[1].filter^=1u;
            CHECK(fut_native_fragment_compile_aa2_projective(device,&state,textures,1,NULL,&ps,&pblob,NULL)==E_NOTIMPL &&
                !ps && !pblob,182);
            state.textures[1].filter^=1u;
        }
        CHECK(SUCCEEDED(fut_native_fragment_compile_aa2_projective(device,&state,textures,1,NULL,&ps,&pblob,NULL)),183);
        CHECK(SUCCEEDED(fut_native_fragment_linkage(vblob,pblob)),184);
        float scales[4]={cases[test].scale[0],cases[test].scale[1],0,0};
        ID3D11DeviceContext_UpdateSubresource(context,(ID3D11Resource*)scale_buffer,0,NULL,scales,0,0);
        const float clear[4]={-1,-1,-1,-1};ID3D11DeviceContext_ClearRenderTargetView(context,view,clear);
        ID3D11DeviceContext_OMSetRenderTargets(context,1,&view,NULL);
        ID3D11DeviceContext_IASetInputLayout(context,NULL);
        ID3D11DeviceContext_IASetPrimitiveTopology(context,D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        ID3D11DeviceContext_VSSetShader(context,vs,NULL,0);ID3D11DeviceContext_PSSetShader(context,ps,NULL,0);
        ID3D11DeviceContext_VSSetConstantBuffers(context,3,1,&scale_buffer);
        for(unsigned stage=0;stage<2;++stage) {
            ID3D11DeviceContext_PSSetShaderResources(context,stage,1,&textures[stage].view);
            ID3D11DeviceContext_PSSetSamplers(context,stage,1,&textures[stage].sampler);
        }
        for(unsigned slot=0;slot<2;++slot) {
            ID3D11DeviceContext_VSSetConstantBuffers(context,4,1,&passes[slot]);
            ID3D11DeviceContext_PSSetConstantBuffers(context,4,1,&passes[slot]);
            ID3D11DeviceContext_Draw(context,3,0);
        }
        ID3D11DeviceContext_OMSetRenderTargets(context,0,NULL,NULL);
        ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)readback,(ID3D11Resource*)target);
        CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),185);
        D3D11_MAPPED_SUBRESOURCE mapped;
        CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)readback,0,D3D11_MAP_READ,0,&mapped)),186);
        float l0=cases[test].lod[0],l1=cases[test].lod[1];
        const float expected[4]={sampled_secondary?(1-l0+l1)*.5f:1-l0,
            sampled_secondary?(l0+l1)*.5f:l0,sampled_secondary?.5f:0,
            1-l0*7.0f/(sampled_secondary?30.0f:15.0f)};
        /* Horizontal UV gradients only: omitting the AA2 x2 correction, using
         * stage0's gradients for stage1, or forcing level0 changes the oracle.
         * A small host filtering tolerance is not an NV2A precision claim. */
        for(unsigned y=1;y<7;++y)for(unsigned x=2;x<14;++x) {
            const float *actual=(const float*)((const unsigned char*)mapped.pData+y*mapped.RowPitch)+x*4;
            for(unsigned c=0;c<4;++c)if(!isfinite(actual[c]) || fabsf(actual[c]-expected[c])>.002f) {
                fprintf(stderr,"dual mip case=%u pixel=%u,%u channel=%u actual=%g expected=%g\n",
                    test,x,y,c,actual[c],expected[c]);
                ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)readback,0);return 187;
            }
        }
        ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)readback,0);
        ID3D11PixelShader_Release(ps);ID3D10Blob_Release(pblob);
    }
    /* Authored single-mip BC2 red and linear ARGB8 blue, sampled together
     * through the production affine AA2 fragment adapter on both passes. */
    for(unsigned i=0;i<64;++i){uint32_t blue=0xFF0000FFu;memcpy(memory+256+i*4,&blue,4);}
    state.textures[0].format=0x03310E29u;state.textures[0].filter=0x02062000u;
    state.textures[1]=(FutGraphicsTextureState){.valid=0xFEBFu,.offset=256u,.format=0x00011229u,
        .address=0x30303u,.control0=0x4003FFC0u,.control1=32u<<16,.filter=0x02062000u,.image_rect=(8u<<16)|8u};
    for(unsigned stage=0;stage<2;++stage) {
        FutGraphicsTexturePlan plan;
        CHECK(fut_graphics_texture_plan(&state.textures[stage],&dma,&plan)==FUT_TEXTURE_READY &&
            SUCCEEDED(fut_native_texture_upload(&textures[stage],device,&plan,dual_mip_read,memory)) &&
            SUCCEEDED(fut_native_texture_sampler(&textures[stage],device,&state.textures[stage])),188);
    }
    configure(&state.fragment,0x08200920u,0x18201920u,0x21u);
    state.fragment.color_output[0]=state.fragment.alpha_output[0]=0x30C00u;
    ID3D11PixelShader *mixed_ps=NULL;ID3DBlob *mixed_blob=NULL;
    CHECK(SUCCEEDED(fut_native_fragment_compile_aa2_affine(device,&state,textures,1,&mixed_ps,&mixed_blob)) &&
        SUCCEEDED(fut_native_fragment_linkage(vblob,mixed_blob)),189);
    const float scales[4]={.5f,4.0f,0,0},clear[4]={-1,-1,-1,-1};
    ID3D11DeviceContext_UpdateSubresource(context,(ID3D11Resource*)scale_buffer,0,NULL,scales,0,0);
    ID3D11DeviceContext_ClearRenderTargetView(context,view,clear);ID3D11DeviceContext_OMSetRenderTargets(context,1,&view,NULL);
    ID3D11DeviceContext_VSSetShader(context,vs,NULL,0);ID3D11DeviceContext_PSSetShader(context,mixed_ps,NULL,0);
    ID3D11DeviceContext_VSSetConstantBuffers(context,3,1,&scale_buffer);
    for(unsigned stage=0;stage<2;++stage) {
        ID3D11DeviceContext_PSSetShaderResources(context,stage,1,&textures[stage].view);
        ID3D11DeviceContext_PSSetSamplers(context,stage,1,&textures[stage].sampler);
    }
    for(unsigned slot=0;slot<2;++slot) {
        ID3D11DeviceContext_VSSetConstantBuffers(context,4,1,&passes[slot]);
        ID3D11DeviceContext_PSSetConstantBuffers(context,4,1,&passes[slot]);
        ID3D11DeviceContext_Draw(context,3,0);
    }
    ID3D11DeviceContext_OMSetRenderTargets(context,0,NULL,NULL);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)readback,(ID3D11Resource*)target);
    CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),190);D3D11_MAPPED_SUBRESOURCE mix;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)readback,0,D3D11_MAP_READ,0,&mix)),191);
    for(unsigned y=1;y<7;++y)for(unsigned x=2;x<14;++x) {
        const float *v=(const float*)((const unsigned char*)mix.pData+y*mix.RowPitch)+x*4;
        CHECK(fabsf(v[0]-.5f)<.002f && fabsf(v[1])<.002f && fabsf(v[2]-.5f)<.002f && fabsf(v[3]-1)<.002f,192);
    }
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)readback,0);ID3D11PixelShader_Release(mixed_ps);ID3D10Blob_Release(mixed_blob);
    _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","0");
    ID3D11DeviceContext_ClearState(context);fut_native_pipeline_destroy(&pipeline);
    for(unsigned stage=0;stage<2;++stage)fut_native_texture_destroy(&textures[stage]);
    for(unsigned slot=0;slot<2;++slot)ID3D11Buffer_Release(passes[slot]);
    ID3D11Buffer_Release(scale_buffer);ID3D11RenderTargetView_Release(view);
    ID3D11Texture2D_Release(target);ID3D11Texture2D_Release(readback);
    ID3D11VertexShader_Release(vs);ID3D10Blob_Release(vblob);
    puts("Authored BC2/BC1: dual two-level color/LOD, trilinear blend and enabled-but-unsampled T1 verified on WARP; no original-draw/NV2A proof");
    return 0;
}
static int verify_programmable_specular(ID3D11Device *device,ID3D11DeviceContext *context,
    FutNativeConstants *completion)
{
    /* Authored original-format MOV oPos,v0; MOV oD1,v4. No hand-written
     * secondary-color shader bypasses the production VP output adapter. */
    const uint32_t program[2][4]={
        {0,(1u<<21)|0x1Bu,2u<<26,(15u<<12)|0x800u},
        {0,(1u<<21)|(4u<<9)|0x1Bu,2u<<26,(15u<<12)|0x800u|(4u<<3)|1u}};
    char source[16384];size_t length;uint16_t required=0;
    CHECK(fut_vertex_program_input_mask(program,2,&required) && required==0x11u,200);
    CHECK(fut_vertex_program_hlsl(program,2,source,sizeof(source),&length),201);
    ID3DBlob *vblob=NULL;ID3D11VertexShader *vs=NULL;
    CHECK(SUCCEEDED(D3DCompile(source,length,"authored_original_specular_words",NULL,NULL,
        "main","vs_5_0",D3DCOMPILE_IEEE_STRICTNESS,0,&vblob,NULL)),202);
    CHECK(SUCCEEDED(ID3D11Device_CreateVertexShader(device,ID3D10Blob_GetBufferPointer(vblob),
        ID3D10Blob_GetBufferSize(vblob),NULL,&vs)),203);
    /* The production generator declares all 16 input semantics. Only ATTR0
     * and ATTR4 are consumed, as checked by the original-word input mask. */
    D3D11_INPUT_ELEMENT_DESC elements[16]={0};
    for(unsigned attribute=0;attribute<16;++attribute) {
        elements[attribute].SemanticName="ATTR";elements[attribute].SemanticIndex=attribute;
        elements[attribute].Format=DXGI_FORMAT_R32G32B32A32_FLOAT;
        elements[attribute].AlignedByteOffset=attribute*16;
        elements[attribute].InputSlotClass=D3D11_INPUT_PER_VERTEX_DATA;
    }
    ID3D11InputLayout *layout=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateInputLayout(device,elements,16,
        ID3D10Blob_GetBufferPointer(vblob),ID3D10Blob_GetBufferSize(vblob),&layout)),204);
    float vertices[3][16][4]={0};
    for(unsigned vertex=0;vertex<3;++vertex) {
        const float position[4]={vertex==1?3:-1,vertex==2?3:-1,.5f,1};
        memcpy(vertices[vertex][0],position,sizeof(position));
    }
    D3D11_BUFFER_DESC bd={0};bd.ByteWidth=sizeof(vertices);bd.Usage=D3D11_USAGE_DEFAULT;
    bd.BindFlags=D3D11_BIND_VERTEX_BUFFER;ID3D11Buffer *buffer=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&bd,NULL,&buffer)),205);
    FutGraphicsKelvinState state={0};FutNativeTexture textures[4]={0};
    configure(&state.fragment,0x05200000u,0x15200000u,0);
    state.program_mode=6;state.specular_enable=1;state.draw.valid=0xFFFFF;
    state.draw.blend_src=1;state.draw.blend_op=0x8006;state.draw.color_mask=0x01010101;
    state.draw.front_mode=state.draw.back_mode=0x1B02;
    state.draw.front_face=0x901;state.draw.cull_face=0x405;
    state.draw.depth_func=state.draw.stencil_func=0x207;
    state.draw.stencil_op[0]=state.draw.stencil_op[1]=state.draw.stencil_op[2]=0x1E00;
    state.draw.alpha_func=0x204;state.draw.alpha_ref=127;
    FutGraphicsSurfacePlan surface={.width=1,.height=1,.logical_width=1,.logical_height=1,.samples=1};
    FutNativePipeline pipeline={0};FutNativeFragmentCache cache={0};
    D3D11_TEXTURE2D_DESC td={0};td.Width=td.Height=td.MipLevels=td.ArraySize=td.SampleDesc.Count=1;
    td.Format=DXGI_FORMAT_R32G32B32A32_FLOAT;td.Usage=D3D11_USAGE_DEFAULT;td.BindFlags=D3D11_BIND_RENDER_TARGET;
    ID3D11Texture2D *target=NULL,*readback=NULL;ID3D11RenderTargetView *view=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&target)),206);
    CHECK(SUCCEEDED(ID3D11Device_CreateRenderTargetView(device,(ID3D11Resource*)target,NULL,&view)),207);
    td.Usage=D3D11_USAGE_STAGING;td.BindFlags=0;td.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&readback)),208);
    /* Explicit independent values: toggling bit17 changes only alpha;
     * localeye/separate bits do not add fixed-function lighting in VP mode. */
    static const struct {float input[4],expected[4];unsigned control,alpha_test;int hit;} cases[]={
        {{.25f,.5f,.75f,.375f},{.25f,.5f,.75f,.375f},0x20000,0,0},
        {{.25f,.5f,.75f,.375f},{.25f,.5f,.75f,1},0,0,0},
        {{1.25f,-.25f,.625f,1.25f},{1,0,.625f,1},0x20000,0,1},
        {{.125f,.875f,.25f,-.25f},{.125f,.875f,.25f,1},0,0,1},
        {{.25f,.5f,.75f,.375f},{.25f,.5f,.75f,.375f},0x30001,0,1},
        {{.25f,.5f,.75f,.375f},{.25f,.5f,.75f,1},0x10001,0,1},
        {{.25f,.5f,.75f,.375f},{-1,-1,-1,-1},0x20000,1,0},
        {{.25f,.5f,.75f,.375f},{.25f,.5f,.75f,1},0,1,0}};
    for(unsigned test=0;test<sizeof(cases)/sizeof(cases[0]);++test) {
        state.light_control=cases[test].control;state.draw.alpha_test=cases[test].alpha_test;
        for(unsigned vertex=0;vertex<3;++vertex)memcpy(vertices[vertex][4],cases[test].input,16);
        ID3D11DeviceContext_UpdateSubresource(context,(ID3D11Resource*)buffer,0,NULL,vertices,0,0);
        CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),209);
        ID3D11PixelShader *ps=NULL;ID3DBlob *pblob=NULL;int hit=-1;
        CHECK(SUCCEEDED(fut_native_fragment_compile_cached(device,&state,textures,&cache,&ps,&pblob,&hit)) &&
            hit==cases[test].hit,210);
        CHECK(SUCCEEDED(fut_native_fragment_linkage(vblob,pblob)),211);
        const float clear[4]={-1,-1,-1,-1};ID3D11DeviceContext_ClearRenderTargetView(context,view,clear);
        ID3D11DeviceContext_OMSetRenderTargets(context,1,&view,NULL);
        ID3D11DeviceContext_IASetInputLayout(context,layout);
        ID3D11DeviceContext_IASetPrimitiveTopology(context,D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        UINT stride=sizeof(vertices[0]),offset=0;
        ID3D11DeviceContext_IASetVertexBuffers(context,0,1,&buffer,&stride,&offset);
        ID3D11DeviceContext_VSSetShader(context,vs,NULL,0);ID3D11DeviceContext_PSSetShader(context,ps,NULL,0);
        ID3D11DeviceContext_Draw(context,3,0);
        ID3D11DeviceContext_OMSetRenderTargets(context,0,NULL,NULL);
        ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)readback,(ID3D11Resource*)target);
        CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),212);
        D3D11_MAPPED_SUBRESOURCE mapped;
        CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)readback,0,D3D11_MAP_READ,0,&mapped)),213);
        const float *actual=mapped.pData;int good=1;
        for(unsigned c=0;c<4;++c)if(!isfinite(actual[c]) || fabsf(actual[c]-cases[test].expected[c])>.00001f) {
            fprintf(stderr,"specular case=%u light_control=%08X alpha_test=%u channel=%u actual=%g expected=%g\n",
                test,state.light_control,state.draw.alpha_test,c,actual[c],cases[test].expected[c]);good=0;
        }
        ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)readback,0);
        CHECK(good,214);ID3D11PixelShader_Release(ps);ID3D10Blob_Release(pblob);
    }
    ID3D11DeviceContext_ClearState(context);fut_native_pipeline_destroy(&pipeline);
    fut_native_fragment_cache_destroy(&cache);ID3D11InputLayout_Release(layout);
    ID3D11Buffer_Release(buffer);ID3D11RenderTargetView_Release(view);
    ID3D11Texture2D_Release(target);ID3D11Texture2D_Release(readback);
    ID3D11VertexShader_Release(vs);ID3D10Blob_Release(vblob);
    puts("Authored original MOV oD1: secondary RGB/alpha, bit17, clamp, cache and alpha discard verified on WARP; no NV2A fidelity proof");
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
    /* Captured two-stage AB dot/blue-to-alpha; authored input and independent
     * dot-product oracle. No NV2A precision claim. */
    FutGraphicsFragmentState blue={0};blue.valid=15;blue.control=0x11102;blue.stage_program=1;
    blue.color_input_valid=blue.alpha_input_valid=blue.color_output_valid=blue.alpha_output_valid=255;
    blue.factor_valid[0]=blue.factor_valid[1]=255;blue.final_factor_valid=3;
    blue.color_input[0]=0xC8C10000;blue.color_output[0]=0x000820C0;blue.factors[0][0]=0x004C951D;
    blue.color_input[1]=0xCC20C140;blue.alpha_input[1]=0xDC30D150;
    blue.color_output[1]=blue.alpha_output[1]=0x00000C00;
    blue.final_words[0]=0x0C010000;blue.final_words[1]=0x00001C80;blue.final_factor[0]=0xFFFFFFFF;
    float dot=(.125f*76+.25f*149+.5f*29)/255.0f;
    CHECK(verify_math(device,context,&completion,&blue,(float[]){dot,dot,dot,dot})==0,195);
    const uint32_t factors[]={0x00FF0000u,0x0000FF00u,0x000000FFu,0};
    const float dots[]={.125f,.25f,.5f,0};
    for(unsigned i=0;i<4;++i) {
        FutGraphicsFragmentState v=blue;v.factors[0][0]=factors[i];float d=dots[i];
        CHECK(verify_math(device,context,&completion,&v,(float[]){d,d,d,d})==0,199);
    }
    FutGraphicsFragmentState scaled=blue;scaled.color_output[0]|=0x10000u;
    CHECK(verify_math(device,context,&completion,&scaled,(float[]){2*dot,2*dot,2*dot,2*dot})==0,200);
    FutGraphicsFragmentState invalid_blue=blue;invalid_blue.color_output[0]&=~0x80000u;
    CHECK(verify_math(device,context,&completion,&invalid_blue,(float[]){dot,dot,dot,.875f})==0,196);
    invalid_blue=blue;invalid_blue.alpha_output[0]=0xC0;
    CHECK(!fut_fragment_program_hlsl(&invalid_blue,text,sizeof(text),&length),197);
    invalid_blue=blue;invalid_blue.color_output[0]|=0x40000;
    CHECK(!fut_fragment_program_hlsl(&invalid_blue,text,sizeof(text),&length),198);
    CHECK(verify_alpha_test(device,context,&completion)==0,135);
    CHECK(verify_dual_projective_mips(device,context,&completion)==0,188);
    CHECK(verify_programmable_specular(device,context,&completion)==0,215);
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
    /* Compile the 320-specific adapter with its independent opt-in. The
     * non-null sentinel is only inspected by the compiler, never bound. */
    configure(&kelvin.fragment,0x08200000,0x14200000,1);
    textures[0].valid=1;textures[0].view=(ID3D11ShaderResourceView*)(uintptr_t)1;
    textures[0].plan.width=1280;textures[0].plan.height=480;textures[0].quincunx_experiment=2;
    textures[0].sampling_state=kelvin.textures[0];shader=NULL;bytecode=NULL;
    _putenv_s("RECOMP_QUINCUNX_APPROXIMATION","1");_putenv_s("RECOMP_QUINCUNX_320_APPROXIMATION","0");
    CHECK(fut_native_fragment_compile(device,&kelvin,textures,&shader,&bytecode)==E_NOTIMPL && !shader && !bytecode,193);
    _putenv_s("RECOMP_QUINCUNX_APPROXIMATION","0");_putenv_s("RECOMP_QUINCUNX_320_APPROXIMATION","1");
    CHECK(SUCCEEDED(fut_native_fragment_compile(device,&kelvin,textures,&shader,&bytecode)) && shader && bytecode,194);
    ID3D11PixelShader_Release(shader);ID3D10Blob_Release(bytecode);
    _putenv_s("RECOMP_QUINCUNX_320_APPROXIMATION","0");
    fut_native_constants_destroy(&completion);ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    puts("Combiner HLSL: parallel RGB/alpha, input mapping, output bias/scale/dot and strict rejection verified on WARP; no NV2A precision/original draw proof");
    return 0;
}
