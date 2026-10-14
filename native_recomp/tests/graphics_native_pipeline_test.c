#define COBJMACROS
#include "graphics_native_pipeline.h"
#include "graphics_native_constants.h"
#include <d3dcompiler.h>
#include <stdio.h>
#include <string.h>
#define CHECK(expr, code) do { if (!(expr)) { fprintf(stderr, "pipeline test failed: %d\n", code); return code; } } while (0)

static int verify(ID3D11Device *device, ID3D11DeviceContext *context, FutNativeConstants *completion,
    ID3D11Texture2D *texture, int depth, int exclusive)
{
    D3D11_TEXTURE2D_DESC desc; ID3D11Texture2D_GetDesc(texture, &desc);
    desc.Usage = D3D11_USAGE_STAGING; desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ; desc.BindFlags = 0;
    ID3D11Texture2D *staging = NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device, &desc, NULL, &staging)), 30);
    ID3D11DeviceContext_CopyResource(context, (ID3D11Resource *)staging, (ID3D11Resource *)texture);
    CHECK(fut_native_constants_finish(completion, NULL, 2000, NULL), 31);
    D3D11_MAPPED_SUBRESOURCE mapped;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context, (ID3D11Resource *)staging, 0, D3D11_MAP_READ, 0, &mapped)), 32);
    int good = 1;
    for (unsigned y = 0; y < 8; ++y) for (unsigned x = 0; x < 16; ++x) {
        /* Independent logical rectangles, including both endpoint pixels.
         * Four stored samples map to each logical pixel (2x2). */
        unsigned lx = x / 2, ly = y / 2;
        int selected = ly >= 1 && ly <= 2 && ((lx >= 1 && lx <= 2) || lx == 5);
        if (exclusive) selected = !selected;
        uint32_t word; memcpy(&word, (uint8_t *)mapped.pData+y*mapped.RowPitch+x*4, 4);
        if (depth) {
            uint32_t z = word & 0xFFFFFF, stencil = word >> 24;
            uint32_t wanted_z = selected ? 0x800000 : 0xFFFFFF, wanted_stencil = selected ? 0x33 : 0;
            if (stencil != wanted_stencil || (z > wanted_z ? z-wanted_z : wanted_z-z) > 1) good = 0;
        } else {
            unsigned b=word&255, g=(word>>8)&255, r=(word>>16)&255, a=word>>24;
            unsigned wb=selected?128:255, wr=selected?128:0, wa=selected?191:255;
            if ((b>wb?b-wb:wb-b)>1 || (r>wr?r-wr:wr-r)>1 || (a>wa?a-wa:wa-a)>1 || g!=64) good = 0;
        }
        if (!good) { fprintf(stderr, "pipeline pixel %u,%u depth=%d raw=%08X selected=%d\n",x,y,depth,word,selected); break; }
    }
    ID3D11DeviceContext_Unmap(context, (ID3D11Resource *)staging, 0);
    ID3D11Texture2D_Release(staging);
    return good ? 0 : 33;
}
int main(void)
{
    FutGraphicsKelvinState state = {0};
    const uint32_t commands[][2] = {
        {0x2B4,0},{0x2C0,0x00020001},{0x2E0,0x00020001},
        {0x2C4,0x00050005},{0x2E4,0x00020001},
        {0x300,0},{0x304,1},{0x308,0},{0x30C,1},{0x32C,1},
        {0x344,0x302},{0x348,0x303},{0x350,0x8006},{0x354,0x201},
        {0x358,0x01010001},{0x35C,1},{0x360,0xFF},{0x364,0x207},{0x368,0x33},{0x36C,0xFF},
        {0x370,0x1E00},{0x374,0x1E00},{0x378,0x1E01},
        {0x37C,0x1D01},{0x38C,0x1B02},{0x390,0x1B02},{0x39C,0x405},{0x3A0,0x901}
    };
    for (unsigned i=0; i<sizeof(commands)/sizeof(commands[0]); ++i)
        CHECK(fut_graphics_kelvin_state(&state,commands[i][0],commands[i][1])==FUT_KELVIN_APPLIED,1);
    CHECK(fut_graphics_draw_ready(&state.draw),2);
    ID3D11Device *device=NULL; ID3D11DeviceContext *context=NULL; D3D_FEATURE_LEVEL level;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,D3D11_SDK_VERSION,&device,&level,&context)),3);
    FutNativeConstants completion; CHECK(SUCCEEDED(fut_native_constants_init(&completion,device,context)),4);
    FutGraphicsSurfacePlan surface={0}; surface.width=16; surface.height=8;
    surface.logical_width=8; surface.logical_height=4; surface.samples=4;
    FutNativePipeline pipeline={0};
    CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),5);
    D3D11_BLEND_DESC blend; ID3D11BlendState_GetDesc(pipeline.blend,&blend);
    CHECK(blend.RenderTarget[0].SrcBlend==D3D11_BLEND_SRC_ALPHA && blend.RenderTarget[0].DestBlend==D3D11_BLEND_INV_SRC_ALPHA &&
        blend.RenderTarget[0].RenderTargetWriteMask==(D3D11_COLOR_WRITE_ENABLE_RED|D3D11_COLOR_WRITE_ENABLE_BLUE|D3D11_COLOR_WRITE_ENABLE_ALPHA),6);
    D3D11_DEPTH_STENCIL_DESC ds; ID3D11DepthStencilState_GetDesc(pipeline.depth,&ds);
    CHECK(ds.DepthFunc==D3D11_COMPARISON_LESS && ds.FrontFace.StencilPassOp==D3D11_STENCIL_OP_REPLACE && ds.StencilReadMask==255,7);
    D3D11_RASTERIZER_DESC rs; ID3D11RasterizerState_GetDesc(pipeline.raster,&rs);
    CHECK(rs.CullMode==D3D11_CULL_NONE && rs.FrontCounterClockwise && rs.ScissorEnable,8);
    const char *vertex="float4 main(uint i:SV_VertexID):SV_POSITION{float2 p=float2((i<<1)&2,i&2);return float4(p*float2(2,-2)+float2(-1,1),0.5,1);}";
    char pixel[4096]; int length=snprintf(pixel,sizeof(pixel),"%sfloat4 main(float4 p:SV_POSITION):SV_TARGET{if(!fut_window_visible(p.xy))discard;return float4(1,0,0,0.5);}",fut_native_window_hlsl());
    CHECK(length>0 && length<(int)sizeof(pixel),9);
    ID3DBlob *vs_blob=NULL,*ps_blob=NULL,*errors=NULL;
    CHECK(SUCCEEDED(D3DCompile(vertex,strlen(vertex),"synthetic_vs",NULL,NULL,"main","vs_5_0",0,0,&vs_blob,&errors)),10);
    if(errors){ID3D10Blob_Release(errors);errors=NULL;}
    HRESULT hr=D3DCompile(pixel,length,"synthetic_ps",NULL,NULL,"main","ps_5_0",0,0,&ps_blob,&errors);
    if(errors){fprintf(stderr,"%.*s",(int)ID3D10Blob_GetBufferSize(errors),(char*)ID3D10Blob_GetBufferPointer(errors));ID3D10Blob_Release(errors);}
    CHECK(SUCCEEDED(hr),11);
    ID3D11VertexShader *vs=NULL; ID3D11PixelShader *ps=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateVertexShader(device,ID3D10Blob_GetBufferPointer(vs_blob),ID3D10Blob_GetBufferSize(vs_blob),NULL,&vs)),12);
    CHECK(SUCCEEDED(ID3D11Device_CreatePixelShader(device,ID3D10Blob_GetBufferPointer(ps_blob),ID3D10Blob_GetBufferSize(ps_blob),NULL,&ps)),13);
    D3D11_TEXTURE2D_DESC tex={0};tex.Width=16;tex.Height=8;tex.MipLevels=1;tex.ArraySize=1;tex.SampleDesc.Count=1;tex.Usage=D3D11_USAGE_DEFAULT;
    tex.Format=DXGI_FORMAT_B8G8R8A8_UNORM;tex.BindFlags=D3D11_BIND_RENDER_TARGET;
    ID3D11Texture2D *color=NULL,*depth=NULL;ID3D11RenderTargetView *rt=NULL;ID3D11DepthStencilView *dt=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&tex,NULL,&color)),14);
    CHECK(SUCCEEDED(ID3D11Device_CreateRenderTargetView(device,(ID3D11Resource*)color,NULL,&rt)),15);
    tex.Format=DXGI_FORMAT_R24G8_TYPELESS;tex.BindFlags=D3D11_BIND_DEPTH_STENCIL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&tex,NULL,&depth)),16);
    D3D11_DEPTH_STENCIL_VIEW_DESC dsv={0};dsv.Format=DXGI_FORMAT_D24_UNORM_S8_UINT;dsv.ViewDimension=D3D11_DSV_DIMENSION_TEXTURE2D;
    CHECK(SUCCEEDED(ID3D11Device_CreateDepthStencilView(device,(ID3D11Resource*)depth,&dsv,&dt)),17);
    ID3D11DeviceContext_OMSetRenderTargets(context,1,&rt,dt);
    ID3D11DeviceContext_IASetPrimitiveTopology(context,D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    ID3D11DeviceContext_VSSetShader(context,vs,NULL,0);ID3D11DeviceContext_PSSetShader(context,ps,NULL,0);
    const float initial[]={0,0.25f,1,1};
    for(unsigned exclusive=0;exclusive<2;++exclusive){
        CHECK(fut_graphics_kelvin_state(&state,0x2B4,exclusive)==FUT_KELVIN_APPLIED,18);
        CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),19);
        ID3D11DeviceContext_ClearRenderTargetView(context,rt,initial);
        ID3D11DeviceContext_ClearDepthStencilView(context,dt,D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL,1,0);
        ID3D11DeviceContext_Draw(context,3,0);
        /* Equal depth must reject the second draw. Otherwise blending twice
         * visibly changes red/blue and the independently expected pixels. */
        ID3D11DeviceContext_Draw(context,3,0);
        CHECK(verify(device,context,&completion,color,0,exclusive)==0,20);
        CHECK(verify(device,context,&completion,depth,1,exclusive)==0,21);
    }
    ID3D11BlendState *saved=pipeline.blend; state.draw.alpha_test=1;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL && pipeline.blend==saved,22);
    state.draw.alpha_test=0; state.draw.front_mode=0x1B03;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_INVALIDARG && pipeline.blend==saved,23);
    state.draw.front_mode=0x1B02; surface.logical_width=0;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_INVALIDARG && pipeline.blend==saved,24);
    surface.logical_width=8;
    state.point_params_enable=1;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL && pipeline.blend==saved,26);
    state.point_params_enable=0;state.point_smooth=1;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL && pipeline.blend==saved,27);
    state.point_smooth=0;
    state.fog_enable=1;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL && pipeline.blend==saved,28);
    state.fog_enable=0;state.lighting_enable=1;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL && pipeline.blend==saved,29);
    state.lighting_enable=0;state.texture_matrix_enable[3]=1;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL && pipeline.blend==saved,30);
    state.texture_matrix_enable[3]=0;
    state.specular_enable=1;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL && pipeline.blend==saved,31);
    state.specular_enable=0;state.light_enable_mask=1;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL && pipeline.blend==saved,32);
    state.light_enable_mask=0;
    ID3D11BlendState *bound=NULL; float blend_factor[4]; UINT sample_mask;
    ID3D11DeviceContext_OMGetBlendState(context,&bound,blend_factor,&sample_mask);
    CHECK(bound==saved,25); ID3D11BlendState_Release(bound);
    ID3D11DeviceContext_ClearState(context);
    fut_native_pipeline_destroy(&pipeline);fut_native_constants_destroy(&completion);
    ID3D11Texture2D_Release(color);ID3D11Texture2D_Release(depth);ID3D11RenderTargetView_Release(rt);ID3D11DepthStencilView_Release(dt);
    ID3D11VertexShader_Release(vs);ID3D11PixelShader_Release(ps);ID3D10Blob_Release(vs_blob);ID3D10Blob_Release(ps_blob);
    ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    puts("Native blend/depth/stencil/color masks and inclusive/exclusive packed-sample clipping verified by pixel readback; synthetic draw only");
    return 0;
}
