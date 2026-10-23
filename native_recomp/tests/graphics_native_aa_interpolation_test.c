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
    for(unsigned projective=0;projective<2;++projective) {
    for(unsigned i=0;i<3;++i)batch.vertices[i][0][3]=projective?(float)(1u<<i):1.0f;
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
        for(unsigned y=0;y<8;++y) for(unsigned x=0;x<16;++x) {
            unsigned logical=x/2,covered=logical+y+(x&1)<8;
            float expected[4]={(logical+.5f)/8,(y+.5f)/8,texture?.75f:.25f,texture?1:.75f};
            if(projective) {
                float l1=expected[0],l2=expected[1],denominator=1-l1-l2+l1/2+l2/4;
                expected[0]=(l1/2)/denominator;expected[1]=(l2/4)/denominator;
            }
            const float *actual=(const float*)((const unsigned char*)mapped.pData+y*mapped.RowPitch)+x*4;
            for(unsigned c=0;c<4;++c) if(!isfinite(actual[c]) || fabsf(actual[c]-(covered?expected[c]:-1))>.00001f) {
                fprintf(stderr,"AA texture=%u stored=%u,%u channel=%u got=%g expected=%g covered=%u\n",texture,x,y,c,actual[c],covered?expected[c]:-1,covered);
                return 18;
            }
        }
        ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)readback,0);
        ID3D11PixelShader_Release(ps);ID3D10Blob_Release(pblob);
    }
    }
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
