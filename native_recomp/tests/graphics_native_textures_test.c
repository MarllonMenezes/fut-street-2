#define COBJMACROS
#include "graphics_native_textures.h"
#include "graphics_native_constants.h"
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
        plan.width==4 && plan.height==3 && plan.pitch==20 && plan.span==56 && plan.dma_context==1,4);
    FutGraphicsTextureState bad=*state;
    bad.offset=UINT32_MAX;
    CHECK(fut_graphics_texture_plan(&bad,&dma,&sentinel)==FUT_TEXTURE_INVALID_RANGE && sentinel.width==123,5);
    bad=*state;bad.control1=12u<<16;
    CHECK(fut_graphics_texture_plan(&bad,&dma,&sentinel)==FUT_TEXTURE_INVALID_RANGE && sentinel.width==123,6);
    bad=*state;bad.format=0x1122A;
    CHECK(fut_graphics_texture_plan(&bad,&dma,&plan)==FUT_TEXTURE_READY && plan.dma_context==2,7);
    bad.format=0x10629;
    CHECK(fut_graphics_texture_plan(&bad,&dma,&sentinel)==FUT_TEXTURE_UNSUPPORTED && sentinel.width==123,8);
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
    FutNativeTexture native={0};
    FutGraphicsTexturePlan invalid=plan;invalid.span++;
    CHECK(fut_native_texture_upload(&native,device,&invalid,memory_read,&memory)==E_INVALIDARG && !memory.reads,15);
    CHECK(SUCCEEDED(fut_native_texture_upload(&native,device,&plan,memory_read,&memory)) && native.valid && native.view && !native.sampling_ready,16);
    CHECK(verify_upload(device,context,&completion,&native,&memory)==0 && !memcmp(memory.bytes,before.bytes,64),17);
    CHECK(SUCCEEDED(fut_native_texture_sampler(&native,device,state)),18);
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
    fut_native_texture_destroy(&native);fut_native_constants_destroy(&completion);
    ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    puts("Linear texture storage: exact upload/readback, projected point/linear samples, range checks and convolution refusal OK; no original draw");
    return 0;
}
