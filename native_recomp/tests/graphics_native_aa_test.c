/* Compare the host coverage construction with an externally supplied, hashed
 * hardware framebuffer mask. No XBE, guest CPU or game resources are loaded. */
#define COBJMACROS
#include "graphics_sample_pattern.h"
#include "graphics_native_constants.h"
#include <d3dcompiler.h>
#include <stdio.h>
#include <string.h>
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"AA coverage failure %u\n",n);return n;}}while(0)
int main(int argc,char **argv)
{
    int uniform=argc==3 && !strcmp(argv[2],"--uniform-grid");
    if(argc!=2 && !uniform) {fputs("usage: graphics_native_aa_test hardware-mask.bin [--uniform-grid]\n",stderr);return 2;}
    FILE *expected=fopen(argv[1],"rb");CHECK(expected,3);
    uint32_t header[4];CHECK(fread(header,sizeof(header),1,expected)==1,4);
    CHECK(header[0]==0x32414146 && header[1]==1280 && header[2]==90 && header[3]==121,5);
    ID3D11Device *device=NULL;ID3D11DeviceContext *context=NULL;D3D_FEATURE_LEVEL level;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,
        D3D11_SDK_VERSION,&device,&level,&context)),6);
    FutNativeConstants completion;CHECK(SUCCEEDED(fut_native_constants_init(&completion,device,context)),7);
    const char *source=FUT_AA2_COVERAGE_HLSL
        "float4 vs(uint id:SV_VertexID):SV_POSITION{"
        "float2 p[3]={float2(64,240),float2(320,64),float2(576,240)};"
        "float2 c=p[id]/float2(640,480)*2-1;return fut_sample_position(float4(c.x,-c.y,.5,1));}"
        "float ps(float4 p:SV_POSITION):SV_TARGET{if(!fut_sample_column(p.x))discard;return 1;}";
    ID3DBlob *vblob=NULL,*pblob=NULL;ID3D11VertexShader *vs=NULL;ID3D11PixelShader *ps=NULL;
    CHECK(SUCCEEDED(D3DCompile(source,strlen(source),"aa_coverage",NULL,NULL,"vs","vs_5_0",0,0,&vblob,NULL)),8);
    CHECK(SUCCEEDED(D3DCompile(source,strlen(source),"aa_coverage",NULL,NULL,"ps","ps_5_0",0,0,&pblob,NULL)),9);
    CHECK(SUCCEEDED(ID3D11Device_CreateVertexShader(device,ID3D10Blob_GetBufferPointer(vblob),ID3D10Blob_GetBufferSize(vblob),NULL,&vs)),10);
    CHECK(SUCCEEDED(ID3D11Device_CreatePixelShader(device,ID3D10Blob_GetBufferPointer(pblob),ID3D10Blob_GetBufferSize(pblob),NULL,&ps)),11);
    D3D11_TEXTURE2D_DESC td={0};td.Width=1280;td.Height=480;td.MipLevels=td.ArraySize=td.SampleDesc.Count=1;
    td.Format=DXGI_FORMAT_R8_UNORM;td.Usage=D3D11_USAGE_DEFAULT;td.BindFlags=D3D11_BIND_RENDER_TARGET;
    ID3D11Texture2D *color=NULL,*readback=NULL;ID3D11RenderTargetView *target=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&color)),12);
    CHECK(SUCCEEDED(ID3D11Device_CreateRenderTargetView(device,(ID3D11Resource*)color,NULL,&target)),13);
    td.Usage=D3D11_USAGE_STAGING;td.BindFlags=0;td.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&readback)),14);
    D3D11_RASTERIZER_DESC rd={0};rd.FillMode=D3D11_FILL_SOLID;rd.CullMode=D3D11_CULL_NONE;rd.DepthClipEnable=TRUE;
    ID3D11RasterizerState *raster=NULL;CHECK(SUCCEEDED(ID3D11Device_CreateRasterizerState(device,&rd,&raster)),15);
    ID3D11DeviceContext_RSSetState(context,raster);
    D3D11_VIEWPORT viewport={0,0,1280,480,0,1};ID3D11DeviceContext_RSSetViewports(context,1,&viewport);
    ID3D11DeviceContext_IASetPrimitiveTopology(context,D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    ID3D11DeviceContext_VSSetShader(context,vs,NULL,0);ID3D11DeviceContext_PSSetShader(context,ps,NULL,0);
    ID3D11DeviceContext_OMSetRenderTargets(context,1,&target,NULL);
    const float black[4]={0};ID3D11DeviceContext_ClearRenderTargetView(context,target,black);
    ID3D11Buffer *passes[2]={0};
    for(unsigned slot=0;slot<2;++slot) {
        FutGraphicsSamplePass pass;CHECK(fut_graphics_aa2_coverage_pass(640,480,slot,&pass),16);
        if(uniform) pass.clip_shift[0]=pass.clip_shift[1]=0;
        D3D11_BUFFER_DESC bd={0};bd.ByteWidth=sizeof(pass);bd.Usage=D3D11_USAGE_IMMUTABLE;bd.BindFlags=D3D11_BIND_CONSTANT_BUFFER;
        D3D11_SUBRESOURCE_DATA data={&pass,0,0};
        CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&bd,&data,&passes[slot])),17);
        ID3D11DeviceContext_VSSetConstantBuffers(context,4,1,&passes[slot]);
        ID3D11DeviceContext_PSSetConstantBuffers(context,4,1,&passes[slot]);
        ID3D11DeviceContext_Draw(context,3,0);
    }
    ID3D11DeviceContext_OMSetRenderTargets(context,0,NULL,NULL);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)readback,(ID3D11Resource*)color);
    CHECK(fut_native_constants_finish(&completion,NULL,2000,NULL),18);
    D3D11_MAPPED_SUBRESOURCE mapped;CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)readback,0,D3D11_MAP_READ,0,&mapped)),19);
    unsigned mismatches=0,ones[2]={0},tested=0;
    for(unsigned y=90;y<211;++y) for(unsigned x=0;x<1280;++x) {
        int wanted=fgetc(expected);CHECK(wanted==0 || wanted==1,20);
        unsigned actual=((const unsigned char*)mapped.pData)[y*mapped.RowPitch+x]!=0;
        if(actual!=(unsigned)wanted) {if(mismatches<8) fprintf(stderr,"coverage mismatch stored=%u,%u actual=%u expected=%d\n",x,y,actual,wanted);++mismatches;}
        ones[x&1]+=actual;++tested;
    }
    CHECK(fgetc(expected)==EOF && !ferror(expected),21);fclose(expected);
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)readback,0);
    ID3D11DeviceContext_ClearState(context);fut_native_constants_destroy(&completion);
    ID3D11Buffer_Release(passes[0]);ID3D11Buffer_Release(passes[1]);ID3D11RasterizerState_Release(raster);
    ID3D11RenderTargetView_Release(target);ID3D11Texture2D_Release(color);ID3D11Texture2D_Release(readback);
    ID3D11VertexShader_Release(vs);ID3D11PixelShader_Release(ps);ID3D10Blob_Release(vblob);ID3D10Blob_Release(pblob);
    ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    printf("AA2 %s coverage compared=%u mismatches=%u covered even/odd=%u/%u; coverage only, no resolve/interpolation/dither claim\n",
        uniform?"uniform-grid":"corner-center",tested,mismatches,ones[0],ones[1]);
    return mismatches?22:0;
}
