#define COBJMACROS
#include "graphics_native_pipeline.h"
#include "graphics_native_constants.h"
#include <d3dcompiler.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#define CHECK(expr, code) do { if (!(expr)) { fprintf(stderr, "pipeline test failed: %d\n", code); return code; } } while (0)

static int read_depth_words(ID3D11Device *device,ID3D11DeviceContext *context,FutNativeConstants *completion,
    ID3D11Texture2D *texture,uint32_t words[128])
{
    D3D11_TEXTURE2D_DESC desc;ID3D11Texture2D_GetDesc(texture,&desc);
    CHECK(desc.Width==16 && desc.Height==8,40);
    desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    ID3D11Texture2D *staging=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&desc,NULL,&staging)),41);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)staging,(ID3D11Resource*)texture);
    CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),42);
    D3D11_MAPPED_SUBRESOURCE mapped;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)staging,0,D3D11_MAP_READ,0,&mapped)),43);
    for(unsigned y=0;y<8;++y) memcpy(words+y*16,(const uint8_t*)mapped.pData+y*mapped.RowPitch,16*4);
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)staging,0);ID3D11Texture2D_Release(staging);
    return 0;
}
static int verify(ID3D11Device *device, ID3D11DeviceContext *context, FutNativeConstants *completion,
    ID3D11Texture2D *texture, int depth, int exclusive, int clipped)
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
        if(clipped) selected=selected && lx>=2 && lx<6 && ly>=1 && ly<3;
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
static int verify_aa2_stencil(ID3D11Device *device,ID3D11DeviceContext *context,
    FutNativeConstants *completion,ID3D11Texture2D *color,ID3D11Texture2D *depth,
    ID3D11RenderTargetView *rt,ID3D11DepthStencilView *dt,
    ID3D11VertexShader *vs,ID3D11PixelShader *ps,const FutGraphicsKelvinState *initial_state)
{
    FutGraphicsKelvinState state=*initial_state;FutNativePipeline pipeline={0};
    FutGraphicsSurfacePlan surface={.width=16,.height=8,.logical_width=8,.logical_height=8,.samples=2};
    state.depth_test=state.stencil_test=1;state.draw.depth_func=0x207;
    state.draw.dither=state.draw.alpha_test=state.draw.blend_enable=state.draw.window_exclusive=0;
    state.draw.color_mask=0x01010101;
    for(unsigned i=0;i<8;++i) {
        state.draw.window_x[i]=i?0x00050005u:0x00020001u;
        state.draw.window_y[i]=0x00020001u;
    }
    state.draw.stencil_op[0]=state.draw.stencil_op[1]=0x1E00;
    char parity_source[2048];int length=snprintf(parity_source,sizeof(parity_source),
        "%scbuffer Parity:register(b4){uint slot;};float4 main(float4 p:SV_POSITION):SV_TARGET{"
        "if((uint(p.x)&1u)!=slot || !fut_window_visible(p.xy))discard;return float4(1,0,0,.5);}",
        fut_native_window_hlsl());
    CHECK(length>0 && length<(int)sizeof(parity_source),67);
    ID3DBlob *parity_blob=NULL;ID3D11PixelShader *parity_ps=NULL;
    CHECK(SUCCEEDED(D3DCompile(parity_source,length,"authored_aa2_stencil_parity",NULL,NULL,
        "main","ps_5_0",0,0,&parity_blob,NULL)),68);
    CHECK(SUCCEEDED(ID3D11Device_CreatePixelShader(device,ID3D10Blob_GetBufferPointer(parity_blob),
        ID3D10Blob_GetBufferSize(parity_blob),NULL,&parity_ps)),69);
    D3D11_BUFFER_DESC bd={0};bd.ByteWidth=16;bd.Usage=D3D11_USAGE_DEFAULT;bd.BindFlags=D3D11_BIND_CONSTANT_BUFFER;
    ID3D11Buffer *parity_buffer=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&bd,NULL,&parity_buffer)),70);
    static const struct {unsigned ref,write_mask,depth_write,passes;} cases[]={
        {0xB3,0xF0,0,1},{0xB4,0xF0,0,0},{0xB3,0xF0,1,1},{0xB3,0x0F,0,1},{0,0xFF,0,1}};
    for(unsigned test=0;test<sizeof(cases)/sizeof(cases[0]);++test) {
        int parity=test==4;
        state.draw.stencil_func=parity?0x207:0x202;
        state.draw.stencil_read=parity?0xFF:0x0F;
        state.draw.stencil_write=cases[test].write_mask;state.draw.stencil_ref=cases[test].ref;
        state.draw.depth_write=cases[test].depth_write;
        state.draw.stencil_op[2]=parity?0x1E02:0x1E01;
        CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),71);
        D3D11_DEPTH_STENCIL_DESC desc;ID3D11DepthStencilState_GetDesc(pipeline.depth,&desc);
        CHECK(desc.DepthEnable && desc.StencilEnable && desc.StencilReadMask==state.draw.stencil_read &&
            desc.StencilWriteMask==cases[test].write_mask && desc.FrontFace.StencilFunc==
                (parity?D3D11_COMPARISON_ALWAYS:D3D11_COMPARISON_EQUAL),72);
        const float blue[4]={0,0,1,1};ID3D11DeviceContext_ClearRenderTargetView(context,rt,blue);
        ID3D11DeviceContext_ClearDepthStencilView(context,dt,D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL,
            .375f,parity?0:0xA3);
        uint32_t before[128],after[128],rgba[128];
        CHECK(!read_depth_words(device,context,completion,depth,before),73);
        ID3D11DeviceContext_OMSetRenderTargets(context,1,&rt,dt);
        ID3D11DeviceContext_IASetInputLayout(context,NULL);
        ID3D11DeviceContext_IASetPrimitiveTopology(context,D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        ID3D11DeviceContext_VSSetShader(context,vs,NULL,0);
        ID3D11DeviceContext_PSSetShader(context,parity?parity_ps:ps,NULL,0);
        for(unsigned slot=0;slot<(parity?2u:1u);++slot) {
            if(parity) {
                const uint32_t values[4]={slot,0,0,0};
                ID3D11DeviceContext_UpdateSubresource(context,(ID3D11Resource*)parity_buffer,0,NULL,values,0,0);
                ID3D11DeviceContext_PSSetConstantBuffers(context,4,1,&parity_buffer);
            }
            ID3D11DeviceContext_Draw(context,3,0);
        }
        ID3D11DeviceContext_OMSetRenderTargets(context,0,NULL,NULL);
        /* The readback helper copies raw DWORDs for both16x8 formats. */
        CHECK(!read_depth_words(device,context,completion,color,rgba) &&
            !read_depth_words(device,context,completion,depth,after),74);
        for(unsigned y=0;y<8;++y)for(unsigned x=0;x<16;++x) {
            unsigned lx=x/2,ly=y,index=y*16+x;
            int selected=cases[test].passes && ly>=1 && ly<=2 &&
                ((lx>=1 && lx<=2) || lx==5);
            unsigned stencil=parity?(selected?1u:0u):selected?(test==3?0xA3u:0xB3u):0xA3u;
            uint32_t z=after[index]&0xFFFFFFu;
            uint32_t wanted_z=selected && cases[test].depth_write?0x800000u:before[index]&0xFFFFFFu;
            unsigned error=z>wanted_z?z-wanted_z:wanted_z-z;
            /* AA2 stores two X columns per logical pixel and one Y row.
             * Two parity-discard passes must increment once, never twice. */
            if(rgba[index]!=(selected?0x80FF0000u:0xFF0000FFu) || (after[index]>>24)!=stencil ||
                error>(selected && cases[test].depth_write?1u:0u)) {
                fprintf(stderr,"AA2 stencil case=%u pixel=%u,%u logical=%u,%u rgba=%08X zs=%08X expected_stencil=%02X expected_z=%06X\n",
                    test,x,y,lx,ly,rgba[index],after[index],stencil,wanted_z);return 75;
            }
        }
    }
    ID3D11Buffer *empty=NULL;ID3D11DeviceContext_PSSetConstantBuffers(context,4,1,&empty);
    ID3D11DeviceContext_OMSetRenderTargets(context,1,&rt,dt);
    ID3D11DeviceContext_PSSetShader(context,ps,NULL,0);
    fut_native_pipeline_destroy(&pipeline);ID3D11Buffer_Release(parity_buffer);
    ID3D11PixelShader_Release(parity_ps);ID3D10Blob_Release(parity_blob);
    puts("AA2 stencil host composition: masked EQUAL/REPLACE, Z write/preserve and parity discard INCR once verified; no NV2A fidelity proof");
    return 0;
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
    state.draw.dither=1;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL &&
        !pipeline.raster && state.draw.dither==1,44);
    state.draw.dither=0;
    CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),5);
    D3D11_BLEND_DESC blend; ID3D11BlendState_GetDesc(pipeline.blend,&blend);
    CHECK(blend.RenderTarget[0].SrcBlend==D3D11_BLEND_SRC_ALPHA && blend.RenderTarget[0].DestBlend==D3D11_BLEND_INV_SRC_ALPHA &&
        blend.RenderTarget[0].RenderTargetWriteMask==(D3D11_COLOR_WRITE_ENABLE_RED|D3D11_COLOR_WRITE_ENABLE_BLUE|D3D11_COLOR_WRITE_ENABLE_ALPHA),6);
    D3D11_DEPTH_STENCIL_DESC ds; ID3D11DepthStencilState_GetDesc(pipeline.depth,&ds);
    CHECK(ds.DepthFunc==D3D11_COMPARISON_LESS && ds.FrontFace.StencilPassOp==D3D11_STENCIL_OP_REPLACE && ds.StencilReadMask==255,7);
    D3D11_RASTERIZER_DESC rs; ID3D11RasterizerState_GetDesc(pipeline.raster,&rs);
    CHECK(rs.CullMode==D3D11_CULL_NONE && rs.FrontCounterClockwise && rs.ScissorEnable,8);
    /* The game's observed surface is AA2 (1280x480 for logical 640x480).
     * Validate host Z/stencil state at that experimental expanded grid while
     * keeping the unaccepted AA2+dither combination fail-fast. */
    surface.width=16;surface.height=4;surface.logical_width=8;surface.logical_height=4;surface.samples=2;
    state.draw.dither=1;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL,48);
    state.draw.dither=0;
    CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),49);
    ID3D11DepthStencilState_GetDesc(pipeline.depth,&ds);
    CHECK(ds.DepthEnable && ds.StencilEnable && ds.DepthFunc==D3D11_COMPARISON_LESS &&
        ds.DepthWriteMask==D3D11_DEPTH_WRITE_MASK_ALL &&
        ds.FrontFace.StencilPassOp==D3D11_STENCIL_OP_REPLACE,50);
    D3D11_VIEWPORT aa2_viewport;UINT viewport_count=1;
    ID3D11DeviceContext_RSGetViewports(context,&viewport_count,&aa2_viewport);
    CHECK(viewport_count==1 && aa2_viewport.Width==16 && aa2_viewport.Height==4,51);
    surface.width=16;surface.height=8;surface.logical_width=8;surface.logical_height=4;surface.samples=4;
    state.draw.dither=0;
    CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),52);
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
        CHECK(verify(device,context,&completion,color,0,exclusive,0)==0,20);
        CHECK(verify(device,context,&completion,depth,1,exclusive,0)==0,21);
    }
    surface.clip_x=2;surface.clip_y=1;surface.clip_width=4;surface.clip_height=2;
    CHECK(fut_graphics_kelvin_state(&state,0x2B4,0)==FUT_KELVIN_APPLIED,48);
    CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),49);
    viewport_count=1;UINT scissor_count=1;D3D11_VIEWPORT viewport;D3D11_RECT scissor;
    ID3D11DeviceContext_RSGetViewports(context,&viewport_count,&viewport);
    ID3D11DeviceContext_RSGetScissorRects(context,&scissor_count,&scissor);
    CHECK(viewport.Width==16 && viewport.Height==8 && scissor.left==4 && scissor.top==2 && scissor.right==12 && scissor.bottom==6,50);
    ID3D11DeviceContext_ClearRenderTargetView(context,rt,initial);
    ID3D11DeviceContext_ClearDepthStencilView(context,dt,D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL,1,0);
    ID3D11DeviceContext_Draw(context,3,0);
    CHECK(!verify(device,context,&completion,color,0,0,1) && !verify(device,context,&completion,depth,1,0,1),51);
    surface.clip_x=surface.clip_y=surface.clip_width=surface.clip_height=0;
    /* A retained depth-write mask ofALL cannot enable disabled Z/stencil.
     * Compare every bit before/after a real synthetic Draw, not guessed clear
     * depth quantization. This is the D3D host side, not NV2A Zcull emulation. */
    state.depth_test=state.stencil_test=0;state.draw.depth_write=1;state.compress_depth=1;
    CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),44);
    ID3D11DepthStencilState_GetDesc(pipeline.depth,&ds);
    CHECK(!ds.DepthEnable && !ds.StencilEnable && ds.DepthWriteMask==D3D11_DEPTH_WRITE_MASK_ALL,45);
    ID3D11DeviceContext_ClearDepthStencilView(context,dt,D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL,.25f,0x5A);
    uint32_t before_depth[128],after_depth[128];
    CHECK(!read_depth_words(device,context,&completion,depth,before_depth),46);
    ID3D11DeviceContext_Draw(context,3,0);
    CHECK(!read_depth_words(device,context,&completion,depth,after_depth) &&
        !memcmp(before_depth,after_depth,sizeof(before_depth)),47);
    /* Enabled Z testing with write mask ZERO must still shade color while
     * preserving every D24S8 bit. ALWAYS passes even though fragment Z=.5
     * is greater than the cleared .375 depth, unlike the previous LESS. */
    FutGraphicsKelvinState saved_depth_state=state;
    state.depth_test=1;state.stencil_test=0;
    state.draw.depth_write=0;state.draw.depth_func=0x207;
    CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),61);
    ID3D11DepthStencilState_GetDesc(pipeline.depth,&ds);
    CHECK(ds.DepthEnable && !ds.StencilEnable && ds.DepthFunc==D3D11_COMPARISON_ALWAYS &&
        ds.DepthWriteMask==D3D11_DEPTH_WRITE_MASK_ZERO,62);
    ID3D11DeviceContext_ClearRenderTargetView(context,rt,initial);
    ID3D11DeviceContext_ClearDepthStencilView(context,dt,D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL,.375f,0xA5);
    CHECK(!read_depth_words(device,context,&completion,depth,before_depth),63);
    ID3D11DeviceContext_Draw(context,3,0);
    CHECK(!verify(device,context,&completion,color,0,0,0),64);
    CHECK(!read_depth_words(device,context,&completion,depth,after_depth) &&
        !memcmp(before_depth,after_depth,sizeof(before_depth)),65);
    CHECK(!verify_aa2_stencil(device,context,&completion,color,depth,rt,dt,vs,ps,&state),76);
    state=saved_depth_state;
    CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),66);
    ID3D11BlendState *saved=pipeline.blend; state.draw.alpha_test=1;
    state.draw.alpha_func=0x208;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_INVALIDARG && pipeline.blend==saved,22);
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
    state.specular_enable=1;state.program_mode=6;
    const uint32_t light_controls[]={0,1,0x10000,0x20000,0x30001};
    for(unsigned i=0;i<sizeof(light_controls)/sizeof(light_controls[0]);++i) {
        state.light_control=light_controls[i];
        CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),59);
    }
    saved=pipeline.blend;
    for(unsigned bad=0;bad<7;++bad) {
        state.program_mode=6;state.specular_enable=1;state.light_control=0x20001;
        switch(bad) {
        case 0:state.program_mode=0;break;
        case 1:state.program_mode=1;break;
        case 2:state.program_mode=3;break;
        case 3:state.specular_enable=2;break;
        case 4:state.light_control|=2;break;
        case 5:state.fog_enable=1;break;
        case 6:state.lighting_enable=1;break;
        }
        CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL &&
            pipeline.blend==saved,60);
        state.fog_enable=state.lighting_enable=0;
    }
    state.program_mode=state.specular_enable=state.light_control=0;
    surface.samples=2;surface.logical_width=640;surface.logical_height=480;
    surface.width=1280;surface.height=480;state.anti_aliasing_control=0xFFFF0001;
    state.draw.dither=1;
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL,51);
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)) && state.draw.dither==1,52);
    surface.samples=4;surface.height=960;
    CHECK(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)==E_NOTIMPL,53);
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    ID3D11DeviceContext_ClearState(context);
    fut_native_pipeline_destroy(&pipeline);fut_native_constants_destroy(&completion);
    ID3D11Texture2D_Release(color);ID3D11Texture2D_Release(depth);ID3D11RenderTargetView_Release(rt);ID3D11DepthStencilView_Release(dt);
    ID3D11VertexShader_Release(vs);ID3D11PixelShader_Release(ps);ID3D10Blob_Release(vs_blob);ID3D10Blob_Release(ps_blob);
    ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    puts("Native blend/depth/stencil/color masks and inclusive/exclusive packed-sample clipping verified by pixel readback; synthetic draw only");
    return 0;
}
