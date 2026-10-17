#define COBJMACROS
#include "graphics_native_textures.h"
#include "graphics_native_constants.h"
#include "graphics_native_fragment.h"
#include "graphics_shader_interface.h"
#include <d3dcompiler.h>
#include <math.h>
#include <stdio.h>
#include <string.h>
#define CHECK(expr, code) do { if (!(expr)) { fprintf(stderr,"texture test failed: %d\n",code); return code; } } while (0)
typedef struct { uint8_t bytes[64]; unsigned reads; int fail; } Memory;
static int memory_read(void *context,uint32_t address,void *data,size_t size)
{
    Memory *memory=context; ++memory->reads;
    if (memory->fail || address!=0x80001000 || size>sizeof(memory->bytes)) return 0;
    memcpy(data,memory->bytes,size); return 1;
}
static uint32_t memory_pixel(const Memory *memory,unsigned x,unsigned y)
{
    uint32_t word; memcpy(&word,memory->bytes+y*20+x*4,4); return word;
}
static int verify_upload(ID3D11Device *device,ID3D11DeviceContext *context,FutNativeConstants *completion,
    FutNativeTexture *native,const Memory *memory)
{
    D3D11_TEXTURE2D_DESC desc; ID3D11Texture2D_GetDesc(native->texture,&desc);
    CHECK(desc.Width==4 && desc.Height==3 && desc.MipLevels==1 && desc.Format==DXGI_FORMAT_B8G8R8A8_UNORM,60);
    desc.Usage=D3D11_USAGE_STAGING; desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ; desc.BindFlags=0;
    ID3D11Texture2D *staging=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&desc,NULL,&staging)),61);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)staging,(ID3D11Resource*)native->texture);
    CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),62);
    D3D11_MAPPED_SUBRESOURCE mapped;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)staging,0,D3D11_MAP_READ,0,&mapped)),63);
    int good=1;
    for (unsigned y=0;y<3;++y) for(unsigned x=0;x<4;++x) {
        uint32_t word; memcpy(&word,(uint8_t*)mapped.pData+y*mapped.RowPitch+x*4,4);
        if (word!=memory_pixel(memory,x,y)) good=0;
    }
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)staging,0);
    ID3D11Texture2D_Release(staging); return good?0:64;
}
static int bounded(int index,int maximum) { return index<0?0:(index>maximum?maximum:index); }
static int reference_address(int index,int size,unsigned mode)
{
    if(mode==3) return bounded(index,size-1);
    if(mode==4) return index<0 || index>=size ? -1 : index;
    int period=mode==1?size:2*size;
    int wrapped=index%period;if(wrapped<0) wrapped+=period;
    return wrapped<size?wrapped:period-1-wrapped;
}
static void reference_sample(const Memory *memory,float px,float py,float q,int linear,unsigned mode,float out[4])
{
    float x=px/q, y=py/q;
    int ix=(int)floorf(x-(linear?0.5f:0)),iy=(int)floorf(y-(linear?0.5f:0));
    float fx=linear?x-0.5f-ix:0,fy=linear?y-0.5f-iy:0;
    const unsigned shifts[]={16,8,0,24};
    for(unsigned c=0;c<4;++c) {
        out[c]=0;
        for(unsigned dy=0;dy<2;++dy) for(unsigned dx=0;dx<2;++dx) {
            float weight=(dx?fx:1-fx)*(dy?fy:1-fy);
            int sx=reference_address(ix+dx,4,mode),sy=reference_address(iy+dy,3,mode);
            uint32_t word=(sx<0 || sy<0)?0xFF112233:memory_pixel(memory,sx,sy);
            out[c]+=weight*((word>>shifts[c])&255)/255.0f;
        }
    }
}
static int verify_sampling(ID3D11Device *device,ID3D11DeviceContext *context,FutNativeConstants *completion,
    FutNativeTexture *native,const Memory *memory,int linear,unsigned mode)
{
    char source[4096];
    int n=snprintf(source,sizeof(source),"%sTexture2D<float4> t:register(t0); SamplerState s:register(s0);"
        "RWStructuredBuffer<float4> o:register(u0);"
        "[numthreads(8,1,1)] void main(uint3 id:SV_DispatchThreadID){"
        "float4 p[8]={float4(.5,.5,0,1),float4(3.5,2.5,0,1),float4(1.5,1.5,0,1),float4(3,3,0,2),"
        "float4(2,1,0,1),float4(-1,-1,0,1),float4(5,4,0,1),float4(7,5,0,2)};"
        "o[id.x]=fut_linear_texture(t,s,p[id.x],float2(4,3));}",fut_native_texture_hlsl());
    CHECK(n>0 && n<(int)sizeof(source) && native->sampling_ready,70);
    ID3DBlob *blob=NULL,*errors=NULL;
    HRESULT hr=D3DCompile(source,n,"synthetic_texture_probe",NULL,NULL,"main","cs_5_0",0,0,&blob,&errors);
    if(errors){fprintf(stderr,"%.*s",(int)ID3D10Blob_GetBufferSize(errors),(char*)ID3D10Blob_GetBufferPointer(errors));ID3D10Blob_Release(errors);}
    CHECK(SUCCEEDED(hr),71);
    ID3D11ComputeShader *shader=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateComputeShader(device,ID3D10Blob_GetBufferPointer(blob),ID3D10Blob_GetBufferSize(blob),NULL,&shader)),72);
    D3D11_BUFFER_DESC desc={0};desc.ByteWidth=8*4*sizeof(float);desc.Usage=D3D11_USAGE_DEFAULT;
    desc.BindFlags=D3D11_BIND_UNORDERED_ACCESS;desc.MiscFlags=D3D11_RESOURCE_MISC_BUFFER_STRUCTURED;desc.StructureByteStride=16;
    ID3D11Buffer *output=NULL,*staging=NULL;ID3D11UnorderedAccessView *uav=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&desc,NULL,&output)),73);
    CHECK(SUCCEEDED(ID3D11Device_CreateUnorderedAccessView(device,(ID3D11Resource*)output,NULL,&uav)),74);
    desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.MiscFlags=0;desc.StructureByteStride=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&desc,NULL,&staging)),75);
    ID3D11DeviceContext_CSSetShader(context,shader,NULL,0);
    ID3D11DeviceContext_CSSetShaderResources(context,0,1,&native->view);
    ID3D11DeviceContext_CSSetSamplers(context,0,1,&native->sampler);
    ID3D11DeviceContext_CSSetUnorderedAccessViews(context,0,1,&uav,NULL);
    ID3D11DeviceContext_Dispatch(context,1,1,1);
    ID3D11UnorderedAccessView *empty=NULL;ID3D11DeviceContext_CSSetUnorderedAccessViews(context,0,1,&empty,NULL);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)staging,(ID3D11Resource*)output);
    CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),76);
    D3D11_MAPPED_SUBRESOURCE mapped;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)staging,0,D3D11_MAP_READ,0,&mapped)),77);
    const float points[][3]={{.5,.5,1},{3.5,2.5,1},{1.5,1.5,1},{3,3,2},{2,1,1},{-1,-1,1},{5,4,1},{7,5,2}};
    int good=1;
    for(unsigned i=0;i<8;++i) {
        float wanted[4]; reference_sample(memory,points[i][0],points[i][1],points[i][2],linear,mode,wanted);
        for(unsigned c=0;c<4;++c) {
            float actual=((float*)mapped.pData)[i*4+c];
            if(!isfinite(actual) || fabsf(actual-wanted[c])>0.00001f) {
                fprintf(stderr,"texture sample %u channel%u got%f expected%f\n",i,c,actual,wanted[c]);good=0;
            }
        }
    }
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)staging,0);
    ID3D11DeviceContext_ClearState(context);
    ID3D11Buffer_Release(staging);ID3D11UnorderedAccessView_Release(uav);ID3D11Buffer_Release(output);
    ID3D11ComputeShader_Release(shader);ID3D10Blob_Release(blob);
    return good?0:78;
}
/* Exercise the actual specialized PS, not only its math helper. No texture
 * or sampler is bound: compilation must have removed the unknown alpha Load.
 * Synthetic output only; not a game draw or NV2A raster precision oracle. */
static int verify_zero_rgb_pixel_shader(ID3D11Device *device,ID3D11DeviceContext *context,
    FutNativeConstants *completion,ID3D11PixelShader *pixel_shader)
{
    ID3D11DeviceContext_ClearState(context);
    const char *source=FUT_GRAPHICS_VARYINGS_HLSL("O")
        "O main(uint i:SV_VertexID){O o=(O)0;float2 v=float2((i<<1)&2,i&2);"
        "o.pos=float4(v*float2(2,-2)+float2(-1,1),.5,1);o.col0=float4(.25,.5,.75,.5);return o;}";
    ID3DBlob *blob=NULL;ID3D11VertexShader *vertex=NULL;
    CHECK(SUCCEEDED(D3DCompile(source,strlen(source),"zero_rgb_synthetic_vs",NULL,NULL,"main","vs_5_0",0,0,&blob,NULL)),100);
    CHECK(SUCCEEDED(ID3D11Device_CreateVertexShader(device,ID3D10Blob_GetBufferPointer(blob),ID3D10Blob_GetBufferSize(blob),NULL,&vertex)),101);
    uint32_t windows[9][4]={{0,0,3,2}};windows[8][0]=windows[8][1]=1;
    D3D11_BUFFER_DESC buffer_desc={0};buffer_desc.ByteWidth=sizeof(windows);
    buffer_desc.Usage=D3D11_USAGE_IMMUTABLE;buffer_desc.BindFlags=D3D11_BIND_CONSTANT_BUFFER;
    D3D11_SUBRESOURCE_DATA data={windows,0,0};ID3D11Buffer *window_buffer=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&buffer_desc,&data,&window_buffer)),102);
    D3D11_TEXTURE2D_DESC desc={0};desc.Width=4;desc.Height=3;desc.MipLevels=desc.ArraySize=desc.SampleDesc.Count=1;
    desc.Format=DXGI_FORMAT_R32G32B32A32_FLOAT;desc.Usage=D3D11_USAGE_DEFAULT;desc.BindFlags=D3D11_BIND_RENDER_TARGET;
    ID3D11Texture2D *output=NULL,*staging=NULL;ID3D11RenderTargetView *target=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&desc,NULL,&output)) &&
        SUCCEEDED(ID3D11Device_CreateRenderTargetView(device,(ID3D11Resource*)output,NULL,&target)),103);
    desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&desc,NULL,&staging)),104);
    D3D11_RASTERIZER_DESC raster={0};raster.FillMode=D3D11_FILL_SOLID;raster.CullMode=D3D11_CULL_NONE;raster.DepthClipEnable=TRUE;
    ID3D11RasterizerState *raster_state=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateRasterizerState(device,&raster,&raster_state)),105);
    D3D11_DEPTH_STENCIL_DESC depth={0};depth.DepthFunc=D3D11_COMPARISON_ALWAYS;
    depth.FrontFace.StencilFunc=D3D11_COMPARISON_ALWAYS;
    depth.FrontFace.StencilFailOp=depth.FrontFace.StencilDepthFailOp=depth.FrontFace.StencilPassOp=D3D11_STENCIL_OP_KEEP;
    depth.BackFace=depth.FrontFace;ID3D11DepthStencilState *depth_state=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateDepthStencilState(device,&depth,&depth_state)),106);
    const float red[4]={1,0,0,1};ID3D11DeviceContext_ClearRenderTargetView(context,target,red);
    D3D11_VIEWPORT viewport={0,0,4,3,0,1};
    ID3D11DeviceContext_RSSetViewports(context,1,&viewport);ID3D11DeviceContext_RSSetState(context,raster_state);
    ID3D11DeviceContext_OMSetRenderTargets(context,1,&target,NULL);
    ID3D11DeviceContext_OMSetDepthStencilState(context,depth_state,0);
    ID3D11DeviceContext_PSSetShader(context,pixel_shader,NULL,0);
    ID3D11DeviceContext_PSSetConstantBuffers(context,2,1,&window_buffer);
    ID3D11DeviceContext_VSSetShader(context,vertex,NULL,0);
    ID3D11DeviceContext_IASetPrimitiveTopology(context,D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    ID3D11DeviceContext_Draw(context,3,0);
    ID3D11DeviceContext_OMSetRenderTargets(context,0,NULL,NULL);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)staging,(ID3D11Resource*)output);
    CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),107);
    D3D11_MAPPED_SUBRESOURCE mapped;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)staging,0,D3D11_MAP_READ,0,&mapped)),108);
    const float expected[4]={0,0,0,.5f};
    for(unsigned y=0;y<3;++y) for(unsigned x=0;x<4;++x)
        CHECK(!memcmp((uint8_t*)mapped.pData+y*mapped.RowPitch+x*16,expected,16),109);
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)staging,0);
    ID3D11DeviceContext_ClearState(context);
    ID3D11RasterizerState_Release(raster_state);ID3D11DepthStencilState_Release(depth_state);
    ID3D11RenderTargetView_Release(target);ID3D11Texture2D_Release(staging);ID3D11Texture2D_Release(output);
    ID3D11Buffer_Release(window_buffer);ID3D11VertexShader_Release(vertex);ID3D10Blob_Release(blob);
    return 0;
}
static int verify_bc1(ID3D11Device *device,ID3D11DeviceContext *context,FutNativeConstants *completion)
{
    FutGraphicsTextureState state={0};state.valid=0x2Fu;state.control0=0x4003FFC0;
    state.format=0x03310C29;state.offset=0x1000;state.address=0x30303;state.filter=0x02062000;
    FutGraphicsDmaView dma={0x80000000,0x1020};FutGraphicsTexturePlan plan={0},sentinel={.width=99};
    CHECK(fut_graphics_texture_plan(&state,&dma,&plan)==FUT_TEXTURE_READY && plan.storage==1 &&
        plan.width==8 && plan.height==8 && plan.pitch==16 && plan.span==32,120);
    dma.size--;
    CHECK(fut_graphics_texture_plan(&state,&dma,&sentinel)==FUT_TEXTURE_INVALID_RANGE && sentinel.width==99,121);
    FutGraphicsTextureState bad=state;bad.format|=4;bad.valid=0xFFFFu;
    CHECK(fut_graphics_texture_plan(&bad,&dma,&sentinel)==FUT_TEXTURE_UNSUPPORTED && sentinel.width==99,122);
    Memory memory={0};
    /* Four row-major blocks: red, green, blue, transparent. No proprietary data. */
    uint16_t colors[]={0xF800,0x07E0,0x001F,0};
    for(unsigned i=0;i<4;++i) memcpy(memory.bytes+i*8,&colors[i],2);
    memory.bytes[28]=memory.bytes[29]=memory.bytes[30]=memory.bytes[31]=255;
    FutNativeTexture native={0};
    CHECK(SUCCEEDED(fut_native_texture_upload(&native,device,&plan,memory_read,&memory)) &&
        native.texel_or==UINT32_MAX && !native.texel_and,123);
    ID3D11Texture2D *old=native.texture;
    FutGraphicsTexturePlan invalid=plan;invalid.span--;
    unsigned reads=memory.reads;
    CHECK(fut_native_texture_upload(&native,device,&invalid,memory_read,&memory)==E_INVALIDARG &&
        native.texture==old && memory.reads==reads,124);
    CHECK(SUCCEEDED(fut_native_texture_sampler(&native,device,&state)),125);
    char source[4096];snprintf(source,sizeof(source),"%s\nTexture2D<float4> t:register(t0);"
        "SamplerState s:register(s0);RWStructuredBuffer<float4> o:register(u0);"
        "[numthreads(4,1,1)]void main(uint3 id:SV_DispatchThreadID){"
        "float2 uv=float2((id.x&1)*.5+.25,(id.x>>1)*.5+.25);"
        "o[id.x]=fut_linear_texture(t,s,float4(uv,0,1),float2(1,1));}",fut_native_texture_hlsl());
    ID3DBlob *blob=NULL;ID3D11ComputeShader *shader=NULL;
    CHECK(SUCCEEDED(D3DCompile(source,strlen(source),"bc1_test",NULL,NULL,"main","cs_5_0",0,0,&blob,NULL)),126);
    CHECK(SUCCEEDED(ID3D11Device_CreateComputeShader(device,ID3D10Blob_GetBufferPointer(blob),
        ID3D10Blob_GetBufferSize(blob),NULL,&shader)),127);
    D3D11_BUFFER_DESC desc={0};desc.ByteWidth=64;desc.Usage=D3D11_USAGE_DEFAULT;
    desc.BindFlags=D3D11_BIND_UNORDERED_ACCESS;desc.MiscFlags=D3D11_RESOURCE_MISC_BUFFER_STRUCTURED;desc.StructureByteStride=16;
    ID3D11Buffer *output=NULL,*staging=NULL;ID3D11UnorderedAccessView *uav=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&desc,NULL,&output)) &&
        SUCCEEDED(ID3D11Device_CreateUnorderedAccessView(device,(ID3D11Resource*)output,NULL,&uav)),128);
    desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.MiscFlags=0;desc.StructureByteStride=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&desc,NULL,&staging)),129);
    ID3D11DeviceContext_CSSetShader(context,shader,NULL,0);
    ID3D11DeviceContext_CSSetShaderResources(context,0,1,&native.view);
    ID3D11DeviceContext_CSSetSamplers(context,0,1,&native.sampler);
    ID3D11DeviceContext_CSSetUnorderedAccessViews(context,0,1,&uav,NULL);
    ID3D11DeviceContext_Dispatch(context,1,1,1);ID3D11DeviceContext_ClearState(context);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)staging,(ID3D11Resource*)output);
    CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),130);
    D3D11_MAPPED_SUBRESOURCE mapped;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)staging,0,D3D11_MAP_READ,0,&mapped)),131);
    const float expected[4][4]={{1,0,0,1},{0,1,0,1},{0,0,1,1},{0,0,0,0}};
    int good=!memcmp(mapped.pData,expected,sizeof(expected));
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)staging,0);
    ID3D11Buffer_Release(staging);ID3D11Buffer_Release(output);ID3D11UnorderedAccessView_Release(uav);
    ID3D11ComputeShader_Release(shader);ID3D10Blob_Release(blob);fut_native_texture_destroy(&native);
    CHECK(good,132);return 0;
}
int main(void)
{
    FutGraphicsKelvinState kelvin={0};FutGraphicsDmaView dma={0x80000000,0x4000};
    FutGraphicsTexturePlan plan={0},sentinel={.width=123};
    CHECK(fut_graphics_texture_plan(&kelvin.textures[0],&dma,&sentinel)==FUT_TEXTURE_INCOMPLETE && sentinel.width==123,1);
    CHECK(fut_graphics_kelvin_state(&kelvin,0x1B0C,0)==FUT_KELVIN_APPLIED &&
        fut_graphics_texture_plan(&kelvin.textures[0],NULL,&sentinel)==FUT_TEXTURE_DISABLED && sentinel.width==123,2);
    const uint32_t commands[][2]={{0x1B00,0x1000},{0x1B04,0x11229},{0x1B10,20u<<16},
        {0x1B1C,(4u<<16)|3},{0x1B08,0x10303},{0x1B0C,0x4003FFC0},{0x1B14,0x01010000},{0x1B24,0xFF112233}};
    for(unsigned i=0;i<sizeof(commands)/sizeof(commands[0]);++i)
        CHECK(fut_graphics_kelvin_state(&kelvin,commands[i][0],commands[i][1])==FUT_KELVIN_APPLIED,3);
    FutGraphicsTextureState *state=&kelvin.textures[0];
    CHECK(fut_graphics_texture_plan(state,&dma,&plan)==FUT_TEXTURE_READY && plan.address==0x80001000 &&
        plan.width==4 && plan.height==3 && plan.pitch==20 && plan.span==56 &&
        plan.bytes_per_pixel==4 && plan.dma_context==1,4);
    FutGraphicsTextureState bad=*state;
    bad.offset=UINT32_MAX;
    CHECK(fut_graphics_texture_plan(&bad,&dma,&sentinel)==FUT_TEXTURE_INVALID_RANGE && sentinel.width==123,5);
    bad=*state;bad.control1=12u<<16;
    CHECK(fut_graphics_texture_plan(&bad,&dma,&sentinel)==FUT_TEXTURE_INVALID_RANGE && sentinel.width==123,6);
    bad=*state;bad.format=0x1122A;
    CHECK(fut_graphics_texture_plan(&bad,&dma,&plan)==FUT_TEXTURE_READY && plan.dma_context==2,7);
    bad.format=0x10629;
    CHECK(fut_graphics_texture_plan(&bad,&dma,&sentinel)==FUT_TEXTURE_UNSUPPORTED && sentinel.width==123,8);
    bad=*state;bad.format=0x11F29;bad.control1=8u<<16;
    CHECK(fut_graphics_texture_plan(&bad,&dma,&plan)==FUT_TEXTURE_READY && plan.bytes_per_pixel==1 &&
        plan.pitch==8 && plan.span==20,50);
    bad=*state;bad.valid&=~128u;
    CHECK(fut_graphics_texture_plan(&bad,&dma,&sentinel)==FUT_TEXTURE_INCOMPLETE,9);
    CHECK(fut_graphics_texture_plan(state,&(FutGraphicsDmaView){0x80000000,0x1030},&sentinel)==FUT_TEXTURE_INVALID_RANGE,10);
    bad=*state;bad.offset=0x2C04000;bad.control1=0x14000000;bad.image_rect=0x050001E0;
    CHECK(fut_graphics_texture_plan(&bad,&(FutGraphicsDmaView){0x80000000,0x4000000},&plan)==FUT_TEXTURE_READY &&
        plan.width==1280 && plan.height==480 && plan.pitch==5120 && plan.span==2457600,11);
    CHECK(fut_graphics_texture_plan(state,&dma,&plan)==FUT_TEXTURE_READY,12);
    Memory memory={0};memset(memory.bytes,0xCD,64);
    for(unsigned y=0;y<3;++y) for(unsigned x=0;x<4;++x) {
        uint32_t color=0xC0000040u | (x*64u<<16) | (y*64u<<8);
        memcpy(memory.bytes+y*20+x*4,&color,4);
    }
    Memory before=memory;
    ID3D11Device *device=NULL;ID3D11DeviceContext *context=NULL;D3D_FEATURE_LEVEL level;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,D3D11_SDK_VERSION,&device,&level,&context)),13);
    FutNativeConstants completion;CHECK(SUCCEEDED(fut_native_constants_init(&completion,device,context)),14);
    CHECK(verify_bc1(device,context,&completion)==0,133);
    FutNativeTexture native={0};
    FutGraphicsTexturePlan invalid=plan;invalid.span++;
    CHECK(fut_native_texture_upload(&native,device,&invalid,memory_read,&memory)==E_INVALIDARG && !memory.reads,15);
    CHECK(SUCCEEDED(fut_native_texture_upload(&native,device,&plan,memory_read,&memory)) && native.valid && native.view && !native.sampling_ready,16);
    CHECK(verify_upload(device,context,&completion,&native,&memory)==0 && !memcmp(memory.bytes,before.bytes,64),17);
    CHECK(SUCCEEDED(fut_native_texture_sampler(&native,device,state)),18);
    state->filter=0x01052000; /* nearest_mipmap_linear reduces to nearest with one level */
    CHECK(SUCCEEDED(fut_native_texture_sampler(&native,device,state)) &&
        verify_sampling(device,context,&completion,&native,&memory,0,3)==0,58);
    state->filter=0x01010000;
    CHECK(SUCCEEDED(fut_native_texture_sampler(&native,device,state)),59);
    const uint32_t fragment_commands[][2]={{0x1E60,1},{0x1E70,1},{0xAC0,0x08200000},
        {0x260,0x14200000},{0x1E40,0xC00},{0xAA0,0xC00},{0x288,0xC},{0x28C,0x1C80}};
    for(unsigned i=0;i<sizeof(fragment_commands)/sizeof(fragment_commands[0]);++i)
        CHECK(fut_graphics_kelvin_state(&kelvin,fragment_commands[i][0],fragment_commands[i][1])==FUT_KELVIN_APPLIED,33);
    FutNativeTexture approved[4]={native};ID3D11PixelShader *pixel_shader=NULL;ID3DBlob *pixel_blob=NULL;
    CHECK(SUCCEEDED(fut_native_fragment_compile(device,&kelvin,approved,&pixel_shader,&pixel_blob)) && pixel_shader && pixel_blob,34);
    ID3D11PixelShader_Release(pixel_shader);ID3D10Blob_Release(pixel_blob);
    state->filter=0x04072000; /* Mutating state must not reuse an old approved sampler. */
    CHECK(fut_native_fragment_compile(device,&kelvin,approved,&pixel_shader,&pixel_blob)==E_NOTIMPL && !pixel_shader && !pixel_blob,35);
    state->filter=0x01010000;
    CHECK(verify_sampling(device,context,&completion,&native,&memory,0,3)==0,19);
    state->filter=0x02020000;
    CHECK(SUCCEEDED(fut_native_texture_sampler(&native,device,state)) && verify_sampling(device,context,&completion,&native,&memory,1,3)==0,20);
    ID3D11SamplerState *saved_sampler=native.sampler;
    state->filter=0x04072000;
    CHECK(fut_native_texture_sampler(&native,device,state)==E_NOTIMPL && !native.sampling_ready && native.sampler==saved_sampler,21);
    state->filter=0x02020000;state->control0|=4;
    CHECK(fut_native_texture_sampler(&native,device,state)==E_NOTIMPL && !native.sampling_ready,22);
    state->control0&=~4u;state->address=0x10305;
    CHECK(fut_native_texture_sampler(&native,device,state)==E_NOTIMPL,23);
    state->address=0x10404;
    CHECK(SUCCEEDED(fut_native_texture_sampler(&native,device,state)),24);
    D3D11_SAMPLER_DESC sampler;ID3D11SamplerState_GetDesc(native.sampler,&sampler);
    CHECK(sampler.AddressU==D3D11_TEXTURE_ADDRESS_BORDER && sampler.AddressV==D3D11_TEXTURE_ADDRESS_BORDER &&
        sampler.BorderColor[0]==17.0f/255 && sampler.BorderColor[2]==51.0f/255,25);
    CHECK(verify_sampling(device,context,&completion,&native,&memory,1,4)==0,28);
    state->address=0x10101;
    CHECK(SUCCEEDED(fut_native_texture_sampler(&native,device,state)) && verify_sampling(device,context,&completion,&native,&memory,1,1)==0,29);
    state->address=0x10202;
    CHECK(SUCCEEDED(fut_native_texture_sampler(&native,device,state)) && verify_sampling(device,context,&completion,&native,&memory,1,2)==0,30);
    state->filter=0x02010000;
    CHECK(fut_native_texture_sampler(&native,device,state)==E_NOTIMPL && !native.sampling_ready,31);
    state->filter=0x82020000;
    CHECK(fut_native_texture_sampler(&native,device,state)==E_NOTIMPL && !native.sampling_ready,32);
    uint32_t updated=0xDEADBEEF;memcpy(memory.bytes,&updated,4);
    CHECK(SUCCEEDED(fut_native_texture_upload(&native,device,&plan,memory_read,&memory)) &&
        verify_upload(device,context,&completion,&native,&memory)==0 && !native.sampling_ready && !native.sampler,26);
    ID3D11Texture2D *saved=native.texture;memory.fail=1;
    CHECK(FAILED(fut_native_texture_upload(&native,device,&plan,memory_read,&memory)) && native.texture==saved && native.valid,27);
    /* All REAL texels RGB0, varied alpha. Nonzero pitch padding must not
     * invalidate the proof, nor may the first pixel alone establish it. */
    memory.fail=0;memset(memory.bytes,0xCD,sizeof(memory.bytes));
    for(unsigned y=0;y<3;++y) for(unsigned x=0;x<4;++x) {
        uint32_t color=(128u+x+y*4)<<24;memcpy(memory.bytes+y*20+x*4,&color,4);
    }
    CHECK(SUCCEEDED(fut_native_texture_upload(&native,device,&plan,memory_read,&memory)) &&
        !(native.texel_or&0xFFFFFFu) && !(native.texel_and&0xFFFFFFu) && !native.zero_rgb_candidate,36);
    state->filter=0x04072000;state->address=0x10303;
    CHECK(fut_native_texture_sampler(&native,device,state)==E_NOTIMPL &&
        fut_native_texture_zero_rgb_candidate(&native,state,0)==E_NOTIMPL && !native.zero_rgb_candidate,37);
    CHECK(SUCCEEDED(fut_native_texture_zero_rgb_candidate(&native,state,1)) && native.zero_rgb_candidate &&
        !native.sampling_ready && !native.sampler,38);
    approved[0]=native;
    CHECK(SUCCEEDED(fut_native_fragment_compile(device,&kelvin,approved,&pixel_shader,&pixel_blob)),39);
    CHECK(verify_zero_rgb_pixel_shader(device,context,&completion,pixel_shader)==0,40);
    ID3D11PixelShader_Release(pixel_shader);ID3D10Blob_Release(pixel_blob);
    /* Unknown alpha used as output alpha or RGB MUST remain blocked. */
    CHECK(fut_graphics_kelvin_state(&kelvin,0x260,0x18200000)==FUT_KELVIN_APPLIED,41);
    CHECK(fut_native_fragment_compile(device,&kelvin,approved,&pixel_shader,&pixel_blob)==E_NOTIMPL && !pixel_shader && !pixel_blob,42);
    CHECK(fut_graphics_kelvin_state(&kelvin,0x260,0x14200000)==FUT_KELVIN_APPLIED &&
        fut_graphics_kelvin_state(&kelvin,0xAC0,0x18200000)==FUT_KELVIN_APPLIED,43);
    CHECK(fut_native_fragment_compile(device,&kelvin,approved,&pixel_shader,&pixel_blob)==E_NOTIMPL && !pixel_shader && !pixel_blob,44);
    CHECK(fut_graphics_kelvin_state(&kelvin,0xAC0,0x08200000)==FUT_KELVIN_APPLIED,45);
    state->address=0x10404;
    CHECK(fut_native_fragment_compile(device,&kelvin,approved,&pixel_shader,&pixel_blob)==E_NOTIMPL &&
        fut_native_texture_zero_rgb_candidate(&native,state,1)==E_NOTIMPL && !native.zero_rgb_candidate,46);
    state->address=0x10303;state->filter=0x04074000;
    CHECK(fut_native_texture_zero_rgb_candidate(&native,state,1)==E_NOTIMPL,47);
    state->filter=0x04072000;
    CHECK(SUCCEEDED(fut_native_texture_zero_rgb_candidate(&native,state,1)),48);
    memory.bytes[2*20+3*4]=1; /* Last real pixel, not the first or padding. */
    CHECK(SUCCEEDED(fut_native_texture_upload(&native,device,&plan,memory_read,&memory)) &&
        !native.zero_rgb_candidate && (native.texel_or&0xFFFFFFu)==1 &&
        fut_native_texture_zero_rgb_candidate(&native,state,1)==E_NOTIMPL,49);
    state->format=0x11F29;state->control1=8u<<16;state->image_rect=(4u<<16)|3u;
    for(unsigned y=0;y<3;++y) for(unsigned x=0;x<4;++x) memory.bytes[y*8+x]=(uint8_t)(40+y*4+x);
    CHECK(fut_graphics_texture_plan(state,&dma,&plan)==FUT_TEXTURE_READY && plan.bytes_per_pixel==1,51);
    FutNativeTexture alpha={0};
    CHECK(SUCCEEDED(fut_native_texture_upload(&alpha,device,&plan,memory_read,&memory)) && alpha.valid,52);
    D3D11_TEXTURE2D_DESC alpha_desc;ID3D11Texture2D_GetDesc(alpha.texture,&alpha_desc);
    CHECK(alpha_desc.Format==DXGI_FORMAT_B8G8R8A8_UNORM && alpha_desc.Width==4 && alpha_desc.Height==3,53);
    alpha_desc.Usage=D3D11_USAGE_STAGING;alpha_desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;alpha_desc.BindFlags=0;
    ID3D11Texture2D *alpha_staging=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&alpha_desc,NULL,&alpha_staging)),54);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)alpha_staging,(ID3D11Resource*)alpha.texture);
    CHECK(fut_native_constants_finish(&completion,NULL,2000,NULL),55);
    D3D11_MAPPED_SUBRESOURCE alpha_map;CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,
        (ID3D11Resource*)alpha_staging,0,D3D11_MAP_READ,0,&alpha_map)),56);
    int alpha_ok=1;
    for(unsigned y=0;y<3;++y) for(unsigned x=0;x<4;++x) {
        uint32_t pixel;memcpy(&pixel,(uint8_t*)alpha_map.pData+y*alpha_map.RowPitch+x*4,4);
        if(pixel!=(((uint32_t)memory.bytes[y*8+x]<<24)|0x00FFFFFFu)) alpha_ok=0;
    }
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)alpha_staging,0);
    ID3D11Texture2D_Release(alpha_staging);fut_native_texture_destroy(&alpha);
    CHECK(alpha_ok,57);
    fut_native_texture_destroy(&native);fut_native_constants_destroy(&completion);
    ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    puts("Texture upload/samplers and zero-RGB snapshot specialization verified: actual PS pixels, varied unknown alpha, padding ignored, changed last texel/state and live-alpha dependencies refused; generic convolution unsupported");
    return 0;
}
