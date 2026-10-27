/* Model validation only: not a hardware oracle or authorization to widen scope. */
#define COBJMACROS
#include "graphics_native_textures.h"
#include "graphics_native_constants.h"
#include "graphics_native_immediate.h"
#include <d3dcompiler.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"quincunx experiment test failed: %d\n",n);return n;}}while(0)
static uint32_t pixels[1280*480];
static int read_pixels(void *ctx,uint32_t address,void *out,size_t size)
{
    (void)ctx;
    if(address!=0x80000000u || size!=sizeof(pixels)) return 0;
    memcpy(out,pixels,size);return 1;
}
static int coordinates_test(void)
{
    FutNativePosition p[3]={0};
    const float xy[3][2]={{0,0},{2560,0},{0,1920}};
    for(unsigned i=0;i<3;++i) {
        float x=xy[i][0],y=xy[i][1];
        p[i].screen[0]=x;p[i].screen[1]=y;p[i].screen[3]=1;
        p[i].clip[0]=x/320-1;p[i].clip[1]=1-y/240;p[i].clip[3]=1;
        p[i].texture[0][0]=2*x+.5f;p[i].texture[0][1]=y;p[i].texture[0][3]=1;
    }
    CHECK(fut_native_immediate_quincunx_coordinates(p,3),1);
    CHECK(!fut_native_immediate_quincunx_coordinates(p,2) &&
        !fut_native_immediate_quincunx_coordinates(NULL,3),2);
    for(unsigned i=0;i<3;++i) for(unsigned field=0;field<3;++field) for(unsigned c=0;c<4;++c) {
        float *v=field==0?p[i].screen:field==1?p[i].clip:p[i].texture[0];
        float saved=v[c];v[c]+=.125f;
        CHECK(!fut_native_immediate_quincunx_coordinates(p,3),3);v[c]=saved;
        v[c]=NAN;CHECK(!fut_native_immediate_quincunx_coordinates(p,3),4);v[c]=saved;
    }
    for(unsigned i=0;i<3;++i) p[i].clip[1]=i==2?-0x1.bffffep+2f:0x1.fffffep-1f;
    CHECK(fut_native_immediate_quincunx_coordinates(p,3),31);
    FutNativePosition q[6]={0};
    const unsigned corners[6][2]={{0,0},{1,0},{1,1},{0,0},{1,1},{0,1}};
    for(unsigned i=0;i<6;++i) {
        q[i].screen[0]=corners[i][0]?320:0;q[i].screen[1]=corners[i][1]?0x1.e00002p+7f:0;q[i].screen[3]=1;
        q[i].clip[0]=corners[i][0]?1:-1;q[i].clip[1]=corners[i][1]?-0x1.fffffep-1f:0x1.fffffep-1f;q[i].clip[3]=1;
        q[i].texture[0][0]=corners[i][0]?1280:0;q[i].texture[0][1]=corners[i][1]?480:0;q[i].texture[0][2]=q[i].texture[0][3]=1;
        for(unsigned stage=1;stage<4;++stage)q[i].texture[stage][3]=1;
    }
    CHECK(fut_native_immediate_quincunx_320_coordinates(q,6),40);
    CHECK(!fut_native_immediate_quincunx_320_coordinates(q,3),41);
    for(unsigned i=0;i<6;++i) for(unsigned field=0;field<3;++field) for(unsigned c=0;c<4;++c) {
        float *v=field==0?q[i].screen:field==1?q[i].clip:q[i].texture[0];float save=v[c];
        v[c]+=.125f;CHECK(!fut_native_immediate_quincunx_320_coordinates(q,6),42);v[c]=save;
        v[c]=NAN;CHECK(!fut_native_immediate_quincunx_320_coordinates(q,6),43);v[c]=save;
    }
    return 0;
}
/* Synthetic fullscreen quad; independently compare all RGB565 output pixels
 * with the authorized five-tap model and check read-only D16 depth. */
static int render_320_test(ID3D11Device *device,ID3D11DeviceContext *context,
    FutNativeConstants *completion,ID3D11ShaderResourceView *source)
{
    const char *vs_text="struct O{float4 p:SV_POSITION;float4 t:TEXCOORD0;};"
        "O main(uint i:SV_VertexID){float2 p[6]={float2(0,0),float2(1,0),float2(1,1),float2(0,0),float2(1,1),float2(0,1)};"
        "O o;o.p=float4(p[i]*float2(2,-2)+float2(-1,1),.5,1);o.t=float4(p[i]*float2(1280,480),1,1);return o;}";
    char ps_text[4096];int len=snprintf(ps_text,sizeof(ps_text),"%sTexture2D<float4> t:register(t0);"
        "float4 main(float4 p:SV_POSITION,float4 uv:TEXCOORD0):SV_TARGET{return fut_quincunx_experiment(t,float4(uv.xy+0.001f,uv.zw));}",fut_native_texture_hlsl());
    ID3DBlob *vb=NULL,*pb=NULL;ID3D11VertexShader *vs=NULL;ID3D11PixelShader *ps=NULL;
    CHECK(len>0 && len<sizeof(ps_text) && SUCCEEDED(D3DCompile(vs_text,strlen(vs_text),"quad320vs",NULL,NULL,"main","vs_5_0",0,0,&vb,NULL)) &&
        SUCCEEDED(D3DCompile(ps_text,len,"quad320ps",NULL,NULL,"main","ps_5_0",0,0,&pb,NULL)) &&
        SUCCEEDED(ID3D11Device_CreateVertexShader(device,ID3D10Blob_GetBufferPointer(vb),ID3D10Blob_GetBufferSize(vb),NULL,&vs)) &&
        SUCCEEDED(ID3D11Device_CreatePixelShader(device,ID3D10Blob_GetBufferPointer(pb),ID3D10Blob_GetBufferSize(pb),NULL,&ps)),50);
    D3D11_TEXTURE2D_DESC td={0};td.Width=320;td.Height=240;td.MipLevels=td.ArraySize=td.SampleDesc.Count=1;
    td.Format=DXGI_FORMAT_B5G6R5_UNORM;td.BindFlags=D3D11_BIND_RENDER_TARGET;td.Usage=D3D11_USAGE_DEFAULT;
    ID3D11Texture2D *color=NULL,*readback=NULL,*depth=NULL,*depth_read=NULL;ID3D11RenderTargetView *rt=NULL;ID3D11DepthStencilView *dt=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&color)) &&
        SUCCEEDED(ID3D11Device_CreateRenderTargetView(device,(ID3D11Resource*)color,NULL,&rt)),51);
    td.Usage=D3D11_USAGE_STAGING;td.BindFlags=0;td.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&readback)),52);
    td.Usage=D3D11_USAGE_DEFAULT;td.CPUAccessFlags=0;td.BindFlags=D3D11_BIND_DEPTH_STENCIL;td.Format=DXGI_FORMAT_D16_UNORM;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&depth)) &&
        SUCCEEDED(ID3D11Device_CreateDepthStencilView(device,(ID3D11Resource*)depth,NULL,&dt)),53);
    td.Usage=D3D11_USAGE_STAGING;td.BindFlags=0;td.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&depth_read)),54);
    D3D11_DEPTH_STENCIL_DESC ds={0};ds.DepthEnable=TRUE;ds.DepthWriteMask=D3D11_DEPTH_WRITE_MASK_ZERO;ds.DepthFunc=D3D11_COMPARISON_LESS;
    ID3D11DepthStencilState *depth_state=NULL;CHECK(SUCCEEDED(ID3D11Device_CreateDepthStencilState(device,&ds,&depth_state)),55);
    D3D11_RASTERIZER_DESC rd={0};rd.FillMode=D3D11_FILL_SOLID;rd.CullMode=D3D11_CULL_NONE;rd.DepthClipEnable=TRUE;
    ID3D11RasterizerState *rs=NULL;CHECK(SUCCEEDED(ID3D11Device_CreateRasterizerState(device,&rd,&rs)),56);
    const float clear[4]={0,0,0,0};ID3D11DeviceContext_ClearRenderTargetView(context,rt,clear);
    ID3D11DeviceContext_ClearDepthStencilView(context,dt,D3D11_CLEAR_DEPTH,.75f,0);
    D3D11_VIEWPORT vp={0,0,320,240,0,1};ID3D11DeviceContext_RSSetViewports(context,1,&vp);ID3D11DeviceContext_RSSetState(context,rs);
    ID3D11DeviceContext_OMSetDepthStencilState(context,depth_state,0);ID3D11DeviceContext_OMSetRenderTargets(context,1,&rt,dt);
    ID3D11DeviceContext_IASetInputLayout(context,NULL);ID3D11DeviceContext_IASetPrimitiveTopology(context,D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    ID3D11DeviceContext_VSSetShader(context,vs,NULL,0);ID3D11DeviceContext_PSSetShader(context,ps,NULL,0);
    ID3D11DeviceContext_PSSetShaderResources(context,0,1,&source);ID3D11DeviceContext_Draw(context,6,0);
    ID3D11DeviceContext_OMSetRenderTargets(context,0,NULL,NULL);ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)readback,(ID3D11Resource*)color);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)depth_read,(ID3D11Resource*)depth);
    CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),57);D3D11_MAPPED_SUBRESOURCE cm,zm;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)readback,0,D3D11_MAP_READ,0,&cm)) &&
        SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)depth_read,0,D3D11_MAP_READ,0,&zm)),58);
    int good=1;for(unsigned y=0;y<240 && good;++y)for(unsigned x=0;x<320 && good;++x) {
        unsigned sx=4*x+2,sy=2*y+1,sx1=sx+2>1278?1278:sx+2,sy1=sy+1>479?479:sy+1;
        uint32_t taps[]={pixels[sy*1280+sx+1],pixels[sy*1280+sx],pixels[sy*1280+sx1],pixels[sy1*1280+sx],pixels[sy1*1280+sx1]};
        uint16_t actual=((uint16_t*)((uint8_t*)cm.pData+y*cm.RowPitch))[x],z=((uint16_t*)((uint8_t*)zm.pData+y*zm.RowPitch))[x];
        const unsigned shifts[]={16,8,0},bits[]={5,6,5},offset[]={11,5,0};
        for(unsigned c=0;c<3;++c){unsigned sum=4*((taps[0]>>shifts[c])&255);for(unsigned t=1;t<5;++t)sum+=(taps[t]>>shifts[c])&255;
            int want=(int)lround((double)sum*((1u<<bits[c])-1)/2040.0),got=(actual>>offset[c])&((1u<<bits[c])-1);
            if(abs(got-want)>1){fprintf(stderr,"quad320 pixel %u,%u channel%u got%d expected%d\n",x,y,c,got,want);good=0;}}
        if(z!=0xBFFF && z!=0xC000)good=0;
    }
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)depth_read,0);ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)readback,0);
    ID3D11DeviceContext_ClearState(context);ID3D11RasterizerState_Release(rs);ID3D11DepthStencilState_Release(depth_state);
    ID3D11DepthStencilView_Release(dt);ID3D11RenderTargetView_Release(rt);ID3D11Texture2D_Release(depth_read);ID3D11Texture2D_Release(depth);
    ID3D11Texture2D_Release(readback);ID3D11Texture2D_Release(color);ID3D11VertexShader_Release(vs);ID3D11PixelShader_Release(ps);
    ID3D10Blob_Release(vb);ID3D10Blob_Release(pb);CHECK(good,59);return 0;
}
int main(void)
{
    CHECK(!coordinates_test(),5);
    ID3D11Device *device=NULL;ID3D11DeviceContext *context=NULL;D3D_FEATURE_LEVEL level;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,D3D11_SDK_VERSION,
        &device,&level,&context)),6);
    FutNativeConstants completion;
    CHECK(SUCCEEDED(fut_native_constants_init(&completion,device,context)),7);
    char source[4096];
    int n=snprintf(source,sizeof(source),"%sTexture2D<float4> t:register(t0);"
        "RWStructuredBuffer<float4> o:register(u0);[numthreads(8,8,1)]"
        "void main(uint3 id:SV_DispatchThreadID){"
        "o[id.y*640+id.x]=fut_quincunx_experiment(t,float4(id.x*2+1.5,id.y+.5,0,1));}",
        fut_native_texture_hlsl());
    CHECK(n>0 && n<sizeof(source),8);
    ID3DBlob *blob=NULL,*errors=NULL;ID3D11ComputeShader *cs=NULL;
    HRESULT hr=D3DCompile(source,n,"five_tap_experimental_model",NULL,NULL,"main","cs_5_0",0,0,&blob,&errors);
    if(errors) {fprintf(stderr,"%.*s",(int)ID3D10Blob_GetBufferSize(errors),(char*)ID3D10Blob_GetBufferPointer(errors));ID3D10Blob_Release(errors);}
    CHECK(SUCCEEDED(hr) && SUCCEEDED(ID3D11Device_CreateComputeShader(device,
        ID3D10Blob_GetBufferPointer(blob),ID3D10Blob_GetBufferSize(blob),NULL,&cs)),9);
    n=snprintf(source,sizeof(source),"%sTexture2D<float4> t:register(t0);"
        "RWStructuredBuffer<float4> o:register(u0);[numthreads(8,8,1)]"
        "void main(uint3 id:SV_DispatchThreadID){"
        "o[id.y*320+id.x]=fut_quincunx_experiment(t,float4(id.x*4+2,id.y*2+1,1,1));}",
        fut_native_texture_hlsl());
    ID3DBlob *blob320=NULL;ID3D11ComputeShader *cs320=NULL;
    CHECK(n>0 && n<sizeof(source) && SUCCEEDED(D3DCompile(source,n,"quincunx_320_model",NULL,NULL,"main","cs_5_0",0,0,&blob320,NULL)) &&
        SUCCEEDED(ID3D11Device_CreateComputeShader(device,ID3D10Blob_GetBufferPointer(blob320),ID3D10Blob_GetBufferSize(blob320),NULL,&cs320)),47);
    D3D11_BUFFER_DESC bd={0};bd.ByteWidth=640*480*16;bd.Usage=D3D11_USAGE_DEFAULT;
    bd.BindFlags=D3D11_BIND_UNORDERED_ACCESS;bd.MiscFlags=D3D11_RESOURCE_MISC_BUFFER_STRUCTURED;bd.StructureByteStride=16;
    ID3D11Buffer *output=NULL,*staging=NULL;ID3D11UnorderedAccessView *uav=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&bd,NULL,&output)) &&
        SUCCEEDED(ID3D11Device_CreateUnorderedAccessView(device,(ID3D11Resource*)output,NULL,&uav)),10);
    bd.Usage=D3D11_USAGE_STAGING;bd.BindFlags=bd.MiscFlags=bd.StructureByteStride=0;bd.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&bd,NULL,&staging)),11);
    FutGraphicsTexturePlan plan={0x80000000u,sizeof(pixels),5120,1280,480,4,1};
    FutGraphicsTextureState state={0};state.valid=0xBF;state.format=0x11229;
    state.filter=0x04072000;state.address=0x30303;state.control0=0x4003FFC0;
    state.control1=5120u<<16;state.image_rect=(1280u<<16)|480;
    FutNativeTexture native={0};unsigned tested=0;
    for(unsigned pattern=0;pattern<5;++pattern) {
        for(unsigned i=0;i<1280*480;++i) pixels[i]=pattern==0?(i*1664525u+1013904223u):0;
        if(pattern==1) pixels[30*1280+41]=0xFF804020; /* Center impulse. */
        if(pattern==2) pixels[30*1280+40]=0x208040FF; /* Shared corner impulse. */
        if(pattern==3) {pixels[479*1280+1278]=0xFFFFFFFF;pixels[479*1280+1279]=0xFF00FF00;}
        if(pattern==4) for(unsigned i=0;i<1280*480;++i) pixels[i]=0x80402010;
        CHECK(SUCCEEDED(fut_native_texture_upload(&native,device,&plan,read_pixels,NULL)),12);
        _putenv("RECOMP_QUINCUNX_320_APPROXIMATION=0");
        CHECK(fut_native_texture_quincunx_experiment(&native,&state,2)==E_NOTIMPL,44);
        _putenv("RECOMP_QUINCUNX_320_APPROXIMATION=1");
        state.address=0x10303;CHECK(fut_native_texture_quincunx_experiment(&native,&state,2)==E_NOTIMPL,45);
        state.address=0x30303;CHECK(SUCCEEDED(fut_native_texture_quincunx_experiment(&native,&state,2)) && native.quincunx_experiment==2,46);
        _putenv("RECOMP_QUINCUNX_320_APPROXIMATION=0");
        _putenv("RECOMP_QUINCUNX_APPROXIMATION=0");
        CHECK(fut_native_texture_quincunx_experiment(&native,&state,1)==E_NOTIMPL && !native.quincunx_experiment,13);
        _putenv("RECOMP_QUINCUNX_APPROXIMATION=1");
        CHECK(fut_native_texture_quincunx_experiment(&native,&state,0)==E_NOTIMPL,14);
        state.address=0x10303;CHECK(SUCCEEDED(fut_native_texture_quincunx_experiment(&native,&state,1)),15);
        state.address=0x30301;CHECK(fut_native_texture_quincunx_experiment(&native,&state,1)==E_NOTIMPL,37);
        state.address=0x30103;CHECK(fut_native_texture_quincunx_experiment(&native,&state,1)==E_NOTIMPL,38);state.address=0x30303;
        native.plan.width=1278;CHECK(fut_native_texture_quincunx_experiment(&native,&state,1)==E_NOTIMPL,16);native.plan.width=1280;
        state.filter=0x04074000;CHECK(fut_native_texture_quincunx_experiment(&native,&state,1)==E_NOTIMPL,32);state.filter=0x04072000;
        state.format=0x11F29;CHECK(fut_native_texture_quincunx_experiment(&native,&state,1)==E_NOTIMPL,33);state.format=0x11229;
        state.control0|=4;CHECK(fut_native_texture_quincunx_experiment(&native,&state,1)==E_NOTIMPL,34);state.control0&=~4u;
        native.plan.height=479;CHECK(fut_native_texture_quincunx_experiment(&native,&state,1)==E_NOTIMPL,35);native.plan.height=480;
        _putenv("RECOMP_QUINCUNX_APPROXIMATION=true");
        CHECK(fut_native_texture_quincunx_experiment(&native,&state,1)==E_NOTIMPL,36);
        _putenv("RECOMP_QUINCUNX_APPROXIMATION=1");
        state.address=(pattern&1)?0x10303:0x30303;
        CHECK(SUCCEEDED(fut_native_texture_quincunx_experiment(&native,&state,1)) && native.quincunx_experiment && !native.sampling_ready,17);
        ID3D11DeviceContext_CSSetShader(context,cs,NULL,0);
        ID3D11DeviceContext_CSSetShaderResources(context,0,1,&native.view);
        ID3D11DeviceContext_CSSetUnorderedAccessViews(context,0,1,&uav,NULL);
        ID3D11DeviceContext_Dispatch(context,80,60,1);
        ID3D11UnorderedAccessView *empty=NULL;ID3D11DeviceContext_CSSetUnorderedAccessViews(context,0,1,&empty,NULL);
        ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)staging,(ID3D11Resource*)output);
        CHECK(fut_native_constants_finish(&completion,NULL,2000,NULL),18);
        D3D11_MAPPED_SUBRESOURCE mapped;
        CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)staging,0,D3D11_MAP_READ,0,&mapped)),19);
        const unsigned shifts[]={16,8,0,24};int good=1;
        for(unsigned y=0;y<480 && good;++y) for(unsigned x=0;x<640 && good;++x) {
            unsigned x1=x==639?x:x+1,y1=y==479?y:y+1;
            uint32_t taps[]={pixels[y*1280+2*x+1],pixels[y*1280+2*x],pixels[y*1280+2*x1],
                pixels[y1*1280+2*x],pixels[y1*1280+2*x1]};
            for(unsigned c=0;c<4;++c) {
                unsigned sum=4*((taps[0]>>shifts[c])&255);
                for(unsigned t=1;t<5;++t) sum+=(taps[t]>>shifts[c])&255;
                float expected=(float)sum/2040,actual=((float*)mapped.pData)[(y*640+x)*4+c];
                if(!isfinite(actual) || fabsf(expected-actual)>0.000001f) {
                    fprintf(stderr,"model mismatch pattern%u (%u,%u) ch%u: %g vs %g\n",pattern,x,y,c,actual,expected);good=0;
                }
                ++tested;
            }
        }
        ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)staging,0);CHECK(good,20);
        ID3D11DeviceContext_CSSetShader(context,cs320,NULL,0);
        ID3D11DeviceContext_CSSetUnorderedAccessViews(context,0,1,&uav,NULL);
        ID3D11DeviceContext_Dispatch(context,40,30,1);
        ID3D11DeviceContext_CSSetUnorderedAccessViews(context,0,1,&empty,NULL);
        ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)staging,(ID3D11Resource*)output);
        CHECK(fut_native_constants_finish(&completion,NULL,2000,NULL) &&
            SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)staging,0,D3D11_MAP_READ,0,&mapped)),48);
        for(unsigned y=0;y<240 && good;++y) for(unsigned x=0;x<320 && good;++x) {
            unsigned sx=4*x+2,sy=2*y+1,sx1=sx+2>1278?1278:sx+2,sy1=sy+1>479?479:sy+1;
            uint32_t taps[]={pixels[sy*1280+sx+1],pixels[sy*1280+sx],pixels[sy*1280+sx1],pixels[sy1*1280+sx],pixels[sy1*1280+sx1]};
            for(unsigned c=0;c<4;++c) {
                unsigned sum=4*((taps[0]>>shifts[c])&255);for(unsigned t=1;t<5;++t)sum+=(taps[t]>>shifts[c])&255;
                float actual=((float*)mapped.pData)[(y*320+x)*4+c];
                if(!isfinite(actual) || fabsf(actual-(float)sum/2040)>0.000001f)good=0;
                ++tested;
            }
        }
        ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)staging,0);CHECK(good,49);
        ID3D11DeviceContext_ClearState(context);
        CHECK(render_320_test(device,context,&completion,native.view)==0,60);
    }
    _putenv("RECOMP_QUINCUNX_APPROXIMATION=0");
    fut_native_texture_destroy(&native);ID3D11Buffer_Release(staging);ID3D11Buffer_Release(output);
    ID3D11UnorderedAccessView_Release(uav);ID3D11ComputeShader_Release(cs);ID3D10Blob_Release(blob);
    ID3D11ComputeShader_Release(cs320);ID3D10Blob_Release(blob320);
    fut_native_constants_destroy(&completion);ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    printf("Experimental quincunx model: %u GPU/CPU channel comparisons, impulse/edge/constant patterns and scope refusals; NOT hardware validation\n",tested);
    return 0;
}
