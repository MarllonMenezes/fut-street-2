#define COBJMACROS
#include "graphics_native_draw.h"
#include "graphics_native_vertex.h"
#include <stdio.h>
#include <string.h>
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"native draw test failed: %d\n",n);return n;}}while(0)
typedef struct {
    uint8_t color[96],depth[96];
    FutGraphicsImmediate *batch;
    FutNativeImmediate *native;
    unsigned writes,fail_address,bad_order;
} Memory;
static int read_memory(void *opaque,uint32_t address,void *data,size_t size)
{
    Memory *m=opaque;
    if(size>96) return 0;
    if(address==0x80001000) memcpy(data,m->color,size);
    else if(address==0x80002000) memcpy(data,m->depth,size);
    else return 0;
    return 1;
}
static int write_memory(void *opaque,uint32_t address,const void *data,size_t size)
{
    Memory *m=opaque;++m->writes;
    if(!m->batch->ready || m->batch->count!=3 || !m->native->submitted) m->bad_order=1;
    if(address==m->fail_address || size>96) return 0;
    if(address==0x80001000) memcpy(m->color,data,size);
    else if(address==0x80002000) memcpy(m->depth,data,size);
    else return 0;
    return 1;
}
static FutGraphicsImmediate batch;
int main(void)
{
    ID3D11Device *device=NULL;ID3D11DeviceContext *context=NULL;D3D_FEATURE_LEVEL level;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,
        D3D11_SDK_VERSION,&device,&level,&context)),1);
    FutNativeConstants completion;
    CHECK(SUCCEEDED(fut_native_constants_init(&completion,device,context)),2);
    /* Synthetic MOV oPos,v0; no original game code or hardware fidelity claim. */
    uint32_t program[1][4]={{0,(1u<<21)|0x1Bu,2u<<26,(15u<<12)|0x800u|1u}};
    ID3D11VertexShader *vertex=NULL;ID3DBlob *vs=NULL,*ps=NULL,*errors=NULL;
    CHECK(SUCCEEDED(fut_native_vertex_compile_screen(device,program,1,&vertex,&vs)),3);
    char source[4096];
    int length=snprintf(source,sizeof(source),"%sfloat4 main(float4 p:SV_POSITION):SV_TARGET{if(!fut_window_visible(p.xy))discard;return float4(1,0,0,1);}",fut_native_window_hlsl());
    CHECK(length>0 && length<(int)sizeof(source),4);
    CHECK(SUCCEEDED(D3DCompile(source,strlen(source),"synthetic_draw_ps",NULL,NULL,
        "main","ps_5_0",D3DCOMPILE_ENABLE_STRICTNESS,0,&ps,&errors)),5);
    if(errors) ID3D10Blob_Release(errors);
    ID3D11PixelShader *pixel=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreatePixelShader(device,ID3D10Blob_GetBufferPointer(ps),
        ID3D10Blob_GetBufferSize(ps),NULL,&pixel)),6);
    FutGraphicsKelvinState state={0};FutGraphicsSoftwareState software={0};
    const uint32_t commands[][2]={
        {0x2B4,0},{0x2C0,3u<<16},{0x2E0,2u<<16},
        {0x300,0},{0x304,0},{0x308,0},{0x30C,0},{0x310,0},{0x32C,0},
        {0x344,1},{0x348,0},{0x350,0x8006},{0x354,0x207},
        {0x358,0x01010101},{0x35C,1},{0x360,0xFF},{0x364,0x207},{0x368,0},{0x36C,0xFF},
        {0x370,0x1E00},{0x374,0x1E00},{0x378,0x1E00},{0x37C,0x1D01},
        {0x38C,0x1B02},{0x390,0x1B02},{0x39C,0x405},{0x3A0,0x901}
    };
    for(unsigned i=0;i<sizeof(commands)/sizeof(commands[0]);++i)
        CHECK(fut_graphics_kelvin_state(&state,commands[i][0],commands[i][1])==FUT_KELVIN_APPLIED,7);
    state.program_mode=2;
    CHECK(fut_graphics_draw_ready(&state.draw),8);
    FutGraphicsSurfacePlan plan={.width=4,.height=3,.logical_width=4,.logical_height=3,.samples=1,
        .color_address=0x80001000,.depth_address=0x80002000,.color_pitch=24,.depth_pitch=28,
        .color_span=64,.depth_span=72};
    FutNativeVertexSurface viewport={0};
    CHECK(SUCCEEDED(fut_native_vertex_surface_prepare(&viewport,device,context,&plan)),9);
    batch.primitive=5;batch.ready=1;batch.count=3;
    const float points[3][4]={{0,0,0,1},{8,0,0,1},{0,6,0,1}};
    for(unsigned i=0;i<3;++i) {memcpy(batch.vertices[i][0],points[i],16);batch.defined[i]=1;}
    FutGraphicsImmediate original=batch;
    FutNativeImmediate native={0};FutNativeSurfaces surfaces={0};FutNativePipeline pipeline={0};
    CHECK(SUCCEEDED(fut_native_immediate_upload(&native,device,context,vs,&batch,1)),10);
    Memory m={.batch=&batch,.native=&native};
    memset(m.color,0xCD,sizeof(m.color));memset(m.depth,0xAB,sizeof(m.depth));
    uint8_t depth_before[96];memcpy(depth_before,m.depth,sizeof(depth_before));
    CHECK(SUCCEEDED(fut_native_surfaces_prepare(&surfaces,device,&plan,read_memory,&m)),11);
#define DRAW() fut_native_draw_immediate(&native,&batch,&surfaces,&pipeline,&state,&software,device,context,vertex,pixel,&completion,read_memory,write_memory,&m,stop)
    HANDLE stop=CreateEventA(NULL,TRUE,TRUE,NULL);CHECK(stop,12);
    CHECK(DRAW()==E_ABORT && batch.ready && !native.submitted && !surfaces.gpu_dirty && !m.writes,13);
    ResetEvent(stop);
    state.draw.dither=1;
    CHECK(DRAW()==E_NOTIMPL && batch.ready && !native.submitted && !surfaces.gpu_dirty && !m.writes,14);
    state.draw.dither=0;
    surfaces.plan.samples=2;
    CHECK(DRAW()==E_NOTIMPL && batch.ready && !native.submitted && !m.writes,15);
    surfaces.plan.samples=1;
    CHECK(SUCCEEDED(DRAW()) && !batch.ready && !batch.count && !native.submitted &&
        !native.count && !surfaces.gpu_dirty && m.writes==2 && !m.bad_order,16);
    CHECK(!memcmp(m.depth,depth_before,sizeof(depth_before)),17);
    for(unsigned y=0;y<3;++y) for(unsigned x=0;x<4;++x) {
        uint32_t word;memcpy(&word,m.color+y*24+x*4,4);CHECK(word==0xFFFF0000,18);
    }
    for(unsigned i=0;i<96;++i) if(i>=64 || i%24>=16) CHECK(m.color[i]==0xCD,19);
    CHECK(DRAW()==E_INVALIDARG && m.writes==2,20);
    /* A failure writing the second attachment must not acknowledge the batch
     * or permit replay of a partially published draw (e.g. alpha blending). */
    batch=original;
    CHECK(SUCCEEDED(fut_native_immediate_upload(&native,device,context,vs,&batch,1)),21);
    CHECK(SUCCEEDED(fut_native_surfaces_prepare(&surfaces,device,&plan,read_memory,&m)),22);
    m.fail_address=plan.depth_address;
    CHECK(FAILED(DRAW()) && batch.ready && batch.count==3 && native.submitted &&
        surfaces.gpu_dirty==2 && m.writes==4 && !m.bad_order,23);
    m.fail_address=0;
    CHECK(DRAW()==E_PENDING && m.writes==4 && batch.ready,24);
    CHECK(fut_native_immediate_upload(&native,device,context,vs,&batch,1)==E_PENDING,25);
    CHECK(fut_native_surfaces_prepare(&surfaces,device,&plan,read_memory,&m)==E_PENDING,26);
    CloseHandle(stop);ID3D11DeviceContext_ClearState(context);
    fut_native_immediate_destroy(&native);fut_native_surfaces_destroy(&surfaces);
    fut_native_pipeline_destroy(&pipeline);fut_native_vertex_surface_destroy(&viewport);
    fut_native_constants_destroy(&completion);ID3D11VertexShader_Release(vertex);
    ID3D11PixelShader_Release(pixel);ID3D10Blob_Release(vs);ID3D10Blob_Release(ps);
    ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    puts("Synthetic translated-VS draw: GPU completion, guest pixels/padding/depth, consume ordering and fail-closed replay latch verified; no game frame");
    return 0;
}
