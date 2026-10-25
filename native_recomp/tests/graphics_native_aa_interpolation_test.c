#define COBJMACROS
#include "graphics_native_fragment.h"
#include "graphics_native_vertex.h"
#include "graphics_native_immediate.h"
#include "graphics_native_pipeline.h"
#include "graphics_sample_pattern.h"
#include <stdio.h>
#include <math.h>
#include <string.h>
#include <stdlib.h>
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"AA interpolation failure %u\n",n);return n;}}while(0)
static int authored_argb_read(void *memory,uint32_t address,void *output,size_t size)
{
    if(address!=0x80001000u || size!=64)return 0;
    memcpy(output,memory,64);return 1;
}
int main(void)
{
    ID3D11Device *device=NULL;ID3D11DeviceContext *context=NULL;D3D_FEATURE_LEVEL level;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,
        D3D11_SDK_VERSION,&device,&level,&context)),1);
    FutNativeConstants completion;CHECK(SUCCEEDED(fut_native_constants_init(&completion,device,context)),2);
    /* Original instruction encoding: MOV oPos,v0; MOV oD0,v3; MOV oT0,v9. */
    const uint32_t program[3][4]={
        {0,(1u<<21)|0x1Bu,2u<<26,(15u<<12)|0x800u},
        {0,(1u<<21)|(3u<<9)|0x1Bu,2u<<26,(15u<<12)|0x800u|(3u<<3)},
        {0,(1u<<21)|(9u<<9)|0x1Bu,2u<<26,(15u<<12)|0x800u|(9u<<3)|1u}};
    ID3D11VertexShader *vs=NULL;ID3DBlob *vblob=NULL;
    CHECK(SUCCEEDED(fut_native_vertex_compile_screen_aa2(device,program,3,&vs,&vblob)),3);
    FutGraphicsImmediate batch={0};batch.ready=1;batch.primitive=5;batch.count=3;
    for(unsigned i=0;i<3;++i) {
        float p[4]={i==1?8:0,i==2?8:0,0,1};
        float color[4]={i==1?1:0,i==2?1:0,.25f,.75f};
        float tex[4]={color[0],color[1],.75f,1};
        memcpy(batch.vertices[i][0],p,sizeof(p));memcpy(batch.vertices[i][3],color,sizeof(color));
        memcpy(batch.vertices[i][9],tex,sizeof(tex));batch.defined[i]=0x209;
    }
    FutNativeImmediate immediate={0};
    CHECK(SUCCEEDED(fut_native_immediate_upload(&immediate,device,context,vblob,&batch,0x209)),4);
    FutGraphicsSurfacePlan surface={0};surface.logical_width=surface.logical_height=8;
    surface.width=16;surface.height=8;surface.samples=2;
    FutNativeVertexSurface vertex_surface={0};
    CHECK(SUCCEEDED(fut_native_vertex_surface_prepare(&vertex_surface,device,context,&surface)),5);
    FutGraphicsKelvinState state={0};state.draw.valid=0xFFFFF;
    for(unsigned i=0;i<8;++i) state.draw.window_x[i]=state.draw.window_y[i]=7u<<16;
    state.draw.blend_src=1;state.draw.blend_dst=0;state.draw.blend_op=0x8006;
    state.draw.color_mask=0x01010101;state.draw.front_mode=state.draw.back_mode=0x1B02;
    state.draw.front_face=0x901;state.draw.cull_face=0x405;
    state.draw.depth_func=state.draw.stencil_func=0x207;
    state.draw.stencil_op[0]=state.draw.stencil_op[1]=state.draw.stencil_op[2]=0x1E00;
    FutNativePipeline pipeline={0};
    CHECK(SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),6);
    D3D11_TEXTURE2D_DESC td={0};td.Width=16;td.Height=8;td.MipLevels=td.ArraySize=td.SampleDesc.Count=1;
    td.Format=DXGI_FORMAT_R32G32B32A32_FLOAT;td.Usage=D3D11_USAGE_DEFAULT;td.BindFlags=D3D11_BIND_RENDER_TARGET;
    ID3D11Texture2D *target=NULL,*readback=NULL;ID3D11RenderTargetView *view=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&target)),7);
    CHECK(SUCCEEDED(ID3D11Device_CreateRenderTargetView(device,(ID3D11Resource*)target,NULL,&view)),8);
    td.Usage=D3D11_USAGE_STAGING;td.BindFlags=0;td.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&readback)),9);
    ID3D11Buffer *passes[2]={0};
    for(unsigned slot=0;slot<2;++slot) {
        FutGraphicsSamplePass pass;CHECK(fut_graphics_aa2_coverage_pass(8,8,slot,&pass),10);
        D3D11_BUFFER_DESC bd={0};bd.ByteWidth=sizeof(pass);bd.Usage=D3D11_USAGE_IMMUTABLE;bd.BindFlags=D3D11_BIND_CONSTANT_BUFFER;
        D3D11_SUBRESOURCE_DATA initial={&pass,0,0};
        CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&bd,&initial,&passes[slot])),11);
    }
    FutNativeTexture textures[4]={0};
    for(unsigned projective=0;projective<4;++projective) {
    for(unsigned i=0;i<3;++i) {
        batch.vertices[i][0][0]=i==1?(projective==3?-6291456.0f:projective==2?1572864.0f:8.0f):0;
        batch.vertices[i][0][3]=projective?(i==1 && projective>=2?(projective==3?-.001f:.001f):(float)(1u<<i)):1.0f;
    }
    CHECK(SUCCEEDED(fut_native_immediate_upload(&immediate,device,context,vblob,&batch,0x209)),19);
    for(unsigned texture=0;texture<2;++texture) {
        const uint32_t commands[][2]={{0x1E60,1},{0x1E70,texture?4:0},
            {0x260,texture?0x18200000:0x14200000},{0xAC0,texture?0x08200000:0x04200000},
            {0xAA0,0xC00},{0x1E40,0xC00},{0x288,0xC},{0x28C,0x1C00}};
        memset(&state.fragment,0,sizeof(state.fragment));
        for(unsigned i=0;i<sizeof(commands)/sizeof(commands[0]);++i)
            CHECK(fut_graphics_fragment_state(&state.fragment,commands[i][0],commands[i][1])==FUT_FRAGMENT_APPLIED,12);
        ID3D11PixelShader *ps=NULL;ID3DBlob *pblob=NULL;
        CHECK(fut_native_fragment_compile_aa2_affine(device,&state,textures,0,&ps,&pblob)==E_NOTIMPL && !ps && !pblob,13);
        if(projective) {
            _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","0");
            CHECK(fut_native_fragment_compile_aa2_projective(device,&state,textures,1,NULL,&ps,&pblob,NULL)==E_NOTIMPL && !ps && !pblob,20);
            _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","1");
            CHECK(SUCCEEDED(fut_native_fragment_compile_aa2_projective(device,&state,textures,1,NULL,&ps,&pblob,NULL)),21);
        } else CHECK(SUCCEEDED(fut_native_fragment_compile_aa2_affine(device,&state,textures,1,&ps,&pblob)),14);
        CHECK(SUCCEEDED(fut_native_fragment_linkage(vblob,pblob)),15);
        const float clear[4]={-1,-1,-1,-1};ID3D11DeviceContext_ClearRenderTargetView(context,view,clear);
        ID3D11DeviceContext_OMSetRenderTargets(context,1,&view,NULL);
        ID3D11DeviceContext_VSSetShader(context,vs,NULL,0);ID3D11DeviceContext_PSSetShader(context,ps,NULL,0);
        for(unsigned slot=0;slot<2;++slot) {
            ID3D11DeviceContext_VSSetConstantBuffers(context,4,1,&passes[slot]);
            ID3D11DeviceContext_PSSetConstantBuffers(context,4,1,&passes[slot]);
            ID3D11DeviceContext_Draw(context,3,0);
        }
        ID3D11DeviceContext_OMSetRenderTargets(context,0,NULL,NULL);
        ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)readback,(ID3D11Resource*)target);
        CHECK(fut_native_constants_finish(&completion,NULL,2000,NULL),16);
        D3D11_MAPPED_SUBRESOURCE mapped;CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)readback,0,D3D11_MAP_READ,0,&mapped)),17);
        /* Clipped far-screen geometry is a separate host approximation:
         * permit at most one normalized16-bit step; retain the old tolerance
         * for ordinary geometry. This is not an NV2A precision measurement. */
        float tolerance=projective>=2?1.0f/65536.0f:.00001f;
        float maximum_error=0;unsigned mismatches=0;
        for(unsigned y=0;y<8;++y) for(unsigned x=0;x<16;++x) {
            unsigned logical=x/2,covered=projective>=2?1:logical+y+(x&1)<8;
            float expected[4]={(logical+.5f)/8,(y+.5f)/8,texture?.75f:.25f,texture?1:.75f};
            if(projective) {
                double l1=(logical+.5)/(projective==3?-6291456.0:projective==2?1572864.0:8.0),l2=(y+.5)/8.0;
                double w1=projective==3?(double)-.001f:projective==2?(double).001f:2.0,denominator=1-l1-l2+l1/w1+l2/4;
                expected[0]=(float)((l1/w1)/denominator);expected[1]=(float)((l2/4)/denominator);
            }
            const float *actual=(const float*)((const unsigned char*)mapped.pData+y*mapped.RowPitch)+x*4;
            for(unsigned c=0;c<4;++c) {
                float error=fabsf(actual[c]-(covered?expected[c]:-1));
                if(error>maximum_error)maximum_error=error;
                if(!isfinite(actual[c]) || error>tolerance) {
                    if(!mismatches)fprintf(stderr,"AA texture=%u stored=%u,%u channel=%u got=%g expected=%g covered=%u\n",texture,x,y,c,actual[c],covered?expected[c]:-1,covered);
                    ++mismatches;
                }
            }
        }
        ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)readback,0);
        if(projective>=2)fprintf(stderr,"Far-screen clipping: texture=%u maximum_error=%g mismatches=%u\n",texture,maximum_error,mismatches);
        if(mismatches)return 18;
        ID3D11PixelShader_Release(ps);ID3D10Blob_Release(pblob);
    }
    }
    /* Compose authored ARGB8 sampling with actual AA2 depth testing. This
     * remains a small dither0 GPU fixture, separate from the full640 gate. */
    uint32_t argb[16];
    static const unsigned swizzled[4][4]={{0,1,4,5},{2,3,6,7},{8,9,12,13},{10,11,14,15}};
    for(unsigned y=0;y<4;++y)for(unsigned x=0;x<4;++x)
        argb[swizzled[y][x]]=((128u+x*16u+y*8u)<<24)|(x*64u<<16)|(y*64u<<8)|(32u+x*8u+y*16u);
    state.textures[0]=(FutGraphicsTextureState){.valid=0x3BF,.format=0x02210629,.offset=0x1000,
        .control0=0x4003FFC0,.control1=0x14000000,.image_rect=0x050001E0,.address=0x30303,.filter=0x02062000};
    FutGraphicsDmaView texture_dma={0x80000000u,0x1040};FutGraphicsTexturePlan texture_plan;
    CHECK(fut_graphics_texture_plan(&state.textures[0],&texture_dma,&texture_plan)==FUT_TEXTURE_READY &&
        texture_plan.span==64 && texture_plan.storage==5,22);
    CHECK(SUCCEEDED(fut_native_texture_upload(&textures[0],device,&texture_plan,authored_argb_read,argb)) &&
        SUCCEEDED(fut_native_texture_sampler(&textures[0],device,&state.textures[0])),23);
    D3D11_SAMPLER_DESC sampler;ID3D11SamplerState_GetDesc(textures[0].sampler,&sampler);
    CHECK(sampler.AddressU==D3D11_TEXTURE_ADDRESS_CLAMP && sampler.AddressV==D3D11_TEXTURE_ADDRESS_CLAMP &&
        sampler.AddressW==D3D11_TEXTURE_ADDRESS_CLAMP && sampler.MinLOD==0 && sampler.MaxLOD==0 && sampler.MipLODBias==0,24);
    memset(&state.fragment,0,sizeof(state.fragment));
    const uint32_t texture_commands[][2]={{0x1E60,1},{0x1E70,1},{0x260,0x18200000},
        {0xAC0,0x08200000},{0xAA0,0xC00},{0x1E40,0xC00},{0x288,0xC},{0x28C,0x1C00}};
    for(unsigned i=0;i<sizeof(texture_commands)/sizeof(texture_commands[0]);++i)
        CHECK(fut_graphics_fragment_state(&state.fragment,texture_commands[i][0],texture_commands[i][1])==FUT_FRAGMENT_APPLIED,25);
    ID3D11PixelShader *texture_ps=NULL;ID3DBlob *texture_blob=NULL;
    CHECK(SUCCEEDED(fut_native_fragment_compile_aa2_affine(device,&state,textures,1,&texture_ps,&texture_blob)) &&
        SUCCEEDED(fut_native_fragment_linkage(vblob,texture_blob)),26);
    for(unsigned i=0;i<3;++i) {
        const float position[4]={i==1?8:0,i==2?8:0,8388607.5f,1};
        const float uv[4]={i==1?1.25f:-.25f,i==2?1.25f:-.25f,0,1};
        memcpy(batch.vertices[i][0],position,16);memcpy(batch.vertices[i][9],uv,16);
    }
    CHECK(SUCCEEDED(fut_native_immediate_upload(&immediate,device,context,vblob,&batch,0x209)),27);
    td.Format=DXGI_FORMAT_R24G8_TYPELESS;td.Usage=D3D11_USAGE_DEFAULT;
    td.BindFlags=D3D11_BIND_DEPTH_STENCIL;td.CPUAccessFlags=0;
    ID3D11Texture2D *depth=NULL,*depth_readback=NULL;ID3D11DepthStencilView *depth_view=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&depth)),28);
    D3D11_DEPTH_STENCIL_VIEW_DESC dsv={0};dsv.Format=DXGI_FORMAT_D24_UNORM_S8_UINT;
    dsv.ViewDimension=D3D11_DSV_DIMENSION_TEXTURE2D;
    CHECK(SUCCEEDED(ID3D11Device_CreateDepthStencilView(device,(ID3D11Resource*)depth,&dsv,&depth_view)),29);
    td.Usage=D3D11_USAGE_STAGING;td.BindFlags=0;td.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&td,NULL,&depth_readback)),30);
    state.depth_test=1;state.stencil_test=0;state.draw.depth_write=state.draw.dither=0;
    for(unsigned compare=0;compare<2;++compare) {
        state.draw.depth_func=0x203; /* Observed LEQUAL: .5 passes .75 and rejects .375. */
        CHECK(surface.width==16 && surface.height==8 && surface.logical_width==8 && surface.logical_height==8 &&
            surface.samples==2 && SUCCEEDED(fut_native_pipeline_apply(&pipeline,device,context,&state,&surface)),31);
        D3D11_DEPTH_STENCIL_DESC zstate;ID3D11DepthStencilState_GetDesc(pipeline.depth,&zstate);
        CHECK(zstate.DepthEnable && !zstate.StencilEnable && zstate.DepthFunc==D3D11_COMPARISON_LESS_EQUAL &&
            zstate.DepthWriteMask==D3D11_DEPTH_WRITE_MASK_ZERO,32);
        const float clear[4]={-1,-1,-1,-1};ID3D11DeviceContext_ClearRenderTargetView(context,view,clear);
        ID3D11DeviceContext_ClearDepthStencilView(context,depth_view,D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL,compare?.375f:.75f,0xA5);
        ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)depth_readback,(ID3D11Resource*)depth);
        CHECK(fut_native_constants_finish(&completion,NULL,2000,NULL),33);
        D3D11_MAPPED_SUBRESOURCE map;uint32_t before[128];
        CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)depth_readback,0,D3D11_MAP_READ,0,&map)),34);
        for(unsigned y=0;y<8;++y)memcpy(before+y*16,(const uint8_t*)map.pData+y*map.RowPitch,64);
        ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)depth_readback,0);
        ID3D11DeviceContext_OMSetRenderTargets(context,1,&view,depth_view);
        ID3D11DeviceContext_VSSetShader(context,vs,NULL,0);ID3D11DeviceContext_PSSetShader(context,texture_ps,NULL,0);
        ID3D11DeviceContext_PSSetShaderResources(context,0,1,&textures[0].view);
        ID3D11DeviceContext_PSSetSamplers(context,0,1,&textures[0].sampler);
        for(unsigned slot=0;slot<2;++slot) {
            ID3D11DeviceContext_VSSetConstantBuffers(context,4,1,&passes[slot]);
            ID3D11DeviceContext_PSSetConstantBuffers(context,4,1,&passes[slot]);ID3D11DeviceContext_Draw(context,3,0);
        }
        ID3D11DeviceContext_OMSetRenderTargets(context,0,NULL,NULL);
        ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)readback,(ID3D11Resource*)target);
        ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)depth_readback,(ID3D11Resource*)depth);
        CHECK(fut_native_constants_finish(&completion,NULL,2000,NULL),35);
        CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)readback,0,D3D11_MAP_READ,0,&map)),36);
        for(unsigned y=0;y<8;++y)for(unsigned x=0;x<16;++x) {
            unsigned logical=x/2;int covered=!compare && logical+y+(x&1)<8;
            float sx=-1.125f+.75f*logical,sy=-1.125f+.75f*y;
            sx=fminf(3,fmaxf(0,sx));sy=fminf(3,fmaxf(0,sy));
            const float expected[4]={sx*64/255,sy*64/255,(32+sx*8+sy*16)/255,(128+sx*16+sy*8)/255};
            const float *actual=(const float*)((const uint8_t*)map.pData+y*map.RowPitch)+x*4;
            for(unsigned c=0;c<4;++c)if(!isfinite(actual[c]) || fabsf(actual[c]-(covered?expected[c]:-1))>.00001f) {
                fprintf(stderr,"ARGB AA2 Zread case=%u pixel=%u,%u channel=%u actual=%g expected=%g\n",
                    compare,x,y,c,actual[c],covered?expected[c]:-1);return 37;
            }
        }
        ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)readback,0);
        CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)depth_readback,0,D3D11_MAP_READ,0,&map)),38);
        for(unsigned y=0;y<8;++y)CHECK(!memcmp(before+y*16,(const uint8_t*)map.pData+y*map.RowPitch,64),39);
        ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)depth_readback,0);
    }
    ID3D11PixelShader_Release(texture_ps);ID3D10Blob_Release(texture_blob);fut_native_texture_destroy(&textures[0]);
    ID3D11DepthStencilView_Release(depth_view);ID3D11Texture2D_Release(depth);ID3D11Texture2D_Release(depth_readback);
    puts("Authored ARGB8 clamp + AA2 LEQUAL/writeZERO: two parity passes shade or fail Z with all D24S8 bits preserved");
    _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","0");
    ID3D11DeviceContext_ClearState(context);fut_native_pipeline_destroy(&pipeline);
    fut_native_immediate_destroy(&immediate);fut_native_vertex_surface_destroy(&vertex_surface);
    fut_native_constants_destroy(&completion);ID3D11Buffer_Release(passes[0]);ID3D11Buffer_Release(passes[1]);
    ID3D11RenderTargetView_Release(view);ID3D11Texture2D_Release(target);ID3D11Texture2D_Release(readback);
    ID3D11VertexShader_Release(vs);ID3D10Blob_Release(vblob);
    ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    puts("AA2 translated VS/PS: affine and rational projective pixel-center color/UV, coverage, untouched outside pixels and projective opt-in verified; synthetic host model");
    return 0;
}
