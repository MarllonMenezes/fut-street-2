#define COBJMACROS
#include "graphics_native_immediate.h"
#include "graphics_native_vertex.h"
#include "graphics_native_constants.h"
#include "graphics_vertex_program.h"
#include <stdio.h>
#include <string.h>
#include <math.h>
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"native immediate test failed: %d\n",n);return n;}}while(0)
static FutGraphicsImmediate batch;
static FutGraphicsKelvinState kelvin;
static int pair(unsigned slot,float x,float y)
{
    uint32_t a,b;memcpy(&a,&x,4);memcpy(&b,&y,4);
    return fut_graphics_immediate_method(&batch,&kelvin,0x1880+slot*8,a)==FUT_IMMEDIATE_APPLIED &&
        fut_graphics_immediate_method(&batch,&kelvin,0x1884+slot*8,b)==FUT_IMMEDIATE_APPLIED;
}
static int verify_screen(ID3D11Device *device,ID3D11DeviceContext *context,FutNativeConstants *completion,
    const uint32_t (*program)[4])
{
    ID3D11DeviceContext_ClearState(context);
    ID3D11VertexShader *shader=NULL;ID3DBlob *bytecode=NULL;
    CHECK(SUCCEEDED(fut_native_vertex_compile_screen(device,program,2,&shader,&bytecode)),30);
    ID3D11DeviceContext_VSSetShader(context,shader,NULL,0);
    FutNativeVertexSurface surface={0};
    FutGraphicsSurfacePlan plan={.width=64,.height=32,.logical_width=64,.logical_height=32,.samples=1};
    CHECK(SUCCEEDED(fut_native_vertex_surface_prepare(&surface,device,context,&plan)),31);
    ID3D11Buffer *previous=surface.buffer;
    plan.samples=2;plan.width=128;
    CHECK(SUCCEEDED(fut_native_vertex_surface_prepare(&surface,device,context,&plan)) &&
        surface.buffer==previous && surface.width==64,32);
    ID3D11Buffer *bound_surface=NULL;
    ID3D11DeviceContext_VSGetConstantBuffers(context,3,1,&bound_surface);
    CHECK(bound_surface==previous,45);ID3D11Buffer_Release(bound_surface);
    plan.samples=1;plan.width=64;
    const float points[3][4]={{-0.0624f,-0.0626f,0,1},{64,32,16777215,2},
        {128.0624f,64.0626f,8388607.5f,-0.5f}};
    for(unsigned i=0;i<3;++i) memcpy(batch.vertices[i][0],points[i],sizeof(points[i]));
    FutNativeImmediate native={0};
    CHECK(SUCCEEDED(fut_native_immediate_upload(&native,device,context,bytecode,&batch,0x201)),33);
    FutNativePosition positions[3],saved[3];memset(positions,0xA5,sizeof(positions));memcpy(saved,positions,sizeof(saved));
    CHECK(fut_native_immediate_probe_positions(&native,device,context,shader,bytecode,completion,
        positions,2,2000,NULL)==E_INVALIDARG && !memcmp(positions,saved,sizeof(saved)),34);
    CHECK(SUCCEEDED(fut_native_immediate_probe_positions(&native,device,context,shader,bytecode,completion,
        positions,3,2000,NULL)),35);
    plan.samples=2;plan.width=128;
    CHECK(SUCCEEDED(fut_native_vertex_surface_prepare(&surface,device,context,&plan)),58);
    FutNativePosition expanded[3];
    CHECK(SUCCEEDED(fut_native_immediate_probe_positions(&native,device,context,shader,bytecode,completion,
        expanded,3,2000,NULL)),59);
    for(unsigned i=0;i<3;++i) for(unsigned c=0;c<4;++c)
        CHECK(fabsf(expanded[i].clip[c]-positions[i].clip[c])<0.000002f,60);
    plan.samples=1;plan.width=64;
    for(unsigned i=0;i<3;++i) {
        /* Independent inverse viewport after signed truncation (not floor).
         * Screen W is undone then restored by D3D perspective division. */
        const float *p=points[i];
        float x=(float)(int)(p[0]*16)/16,y=(float)(int)(p[1]*16)/16;
        float expected[4]={(x/64*2-1)*p[3],(1-y/32*2)*p[3],p[2]/16777215*p[3],p[3]};
        CHECK(!memcmp(positions[i].screen,p,16),36);
        CHECK(!memcmp(positions[i].texture[0],batch.vertices[i][9],16) &&
            positions[i].texture[1][3]==1 && positions[i].texture[2][3]==1 && positions[i].texture[3][3]==1,52);
        for(unsigned c=0;c<4;++c) CHECK(fabsf(positions[i].clip[c]-expected[c])<0.000002f,37);
        CHECK(fabsf((positions[i].clip[0]/p[3]+1)*32-x)<0.00002f &&
            fabsf((1-positions[i].clip[1]/p[3])*16-y)<0.00002f,38);
    }
    CHECK(!fut_native_immediate_constant_coordinates(positions,3),53); /* W2/-.5 outside specialization domain. */
    CHECK(fut_native_immediate_projective_coordinates(positions,3)==15,61);
    FutNativePosition domain[3];memcpy(domain,positions,sizeof(domain));
    for(unsigned i=0;i<3;++i) domain[i].screen[3]=domain[i].clip[3]=1;
    CHECK(fut_native_immediate_constant_coordinates(domain,3)==15,54);
    domain[1].texture[0][3]=0;
    CHECK(fut_native_immediate_constant_coordinates(domain,3)==14,55);
    domain[2].texture[2][0]=NAN;
    CHECK(fut_native_immediate_constant_coordinates(domain,3)==10,56);
    domain[0].clip[0]=INFINITY;
    CHECK(!fut_native_immediate_constant_coordinates(domain,3),57);
    ID3D11GeometryShader *bound=NULL;ID3D11Buffer *target=NULL;
    ID3D11DeviceContext_GSGetShader(context,&bound,NULL,NULL);
    ID3D11DeviceContext_SOGetTargets(context,1,&target);
    CHECK(!bound && !target && batch.ready,39);
    memcpy(saved,positions,sizeof(saved));
    D3D11_BUFFER_DESC occupied_desc={0};occupied_desc.ByteWidth=32;
    occupied_desc.Usage=D3D11_USAGE_DEFAULT;occupied_desc.BindFlags=D3D11_BIND_STREAM_OUTPUT;
    ID3D11Buffer *occupied=NULL;UINT offset=0;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&occupied_desc,NULL,&occupied)),46);
    ID3D11DeviceContext_SOSetTargets(context,1,&occupied,&offset);
    CHECK(fut_native_immediate_probe_positions(&native,device,context,shader,bytecode,completion,
        positions,3,2000,NULL)==E_NOTIMPL && !memcmp(positions,saved,sizeof(saved)),47);
    ID3D11DeviceContext_SOGetTargets(context,1,&target);
    CHECK(target==occupied,48);ID3D11Buffer_Release(target);
    ID3D11DeviceContext_SOSetTargets(context,0,NULL,NULL);ID3D11Buffer_Release(occupied);
    D3D11_SO_DECLARATION_ENTRY decl[]={{0,"SV_POSITION",0,0,4,0},{0,"TEXCOORD",6,0,4,0}};
    ID3D11GeometryShader *occupied_gs=NULL;UINT stride=32;
    CHECK(SUCCEEDED(ID3D11Device_CreateGeometryShaderWithStreamOutput(device,
        ID3D10Blob_GetBufferPointer(bytecode),ID3D10Blob_GetBufferSize(bytecode),decl,2,
        &stride,1,D3D11_SO_NO_RASTERIZED_STREAM,NULL,&occupied_gs)),49);
    ID3D11DeviceContext_GSSetShader(context,occupied_gs,NULL,0);
    CHECK(fut_native_immediate_probe_positions(&native,device,context,shader,bytecode,completion,
        positions,3,2000,NULL)==E_NOTIMPL && !memcmp(positions,saved,sizeof(saved)),50);
    ID3D11DeviceContext_GSGetShader(context,&bound,NULL,NULL);
    CHECK(bound==occupied_gs,51);ID3D11GeometryShader_Release(bound);
    ID3D11DeviceContext_GSSetShader(context,NULL,NULL,0);ID3D11GeometryShader_Release(occupied_gs);
    const float invalid_w[]={0,-0.0f,0x1p-65f,0x1p65f,INFINITY,NAN};
    for(unsigned i=0;i<sizeof(invalid_w)/sizeof(invalid_w[0]);++i) {
        batch.vertices[0][0][3]=invalid_w[i];
        CHECK(SUCCEEDED(fut_native_immediate_upload(&native,device,context,bytecode,&batch,0x201)),40);
        memcpy(saved,positions,sizeof(saved));
        CHECK(fut_native_immediate_probe_positions(&native,device,context,shader,bytecode,completion,
            positions,3,2000,NULL)==E_NOTIMPL && !memcmp(positions,saved,sizeof(saved)) && batch.ready,41);
    }
    memcpy(batch.vertices[0][0],points[0],sizeof(points[0]));
    CHECK(SUCCEEDED(fut_native_immediate_upload(&native,device,context,bytecode,&batch,0x201)),42);
    HANDLE stop=CreateEventA(NULL,TRUE,TRUE,NULL);CHECK(stop,43);
    CHECK(FAILED(fut_native_immediate_probe_positions(&native,device,context,shader,bytecode,completion,
        positions,3,2000,stop)) && !memcmp(positions,saved,sizeof(saved)),44);
    CloseHandle(stop);
    fut_native_immediate_destroy(&native);fut_native_vertex_surface_destroy(&surface);
    ID3D11DeviceContext_ClearState(context);ID3D11VertexShader_Release(shader);ID3D10Blob_Release(bytecode);
    return 0;
}
int main(void)
{
    /* Synthetic two-MOV program, not game bytecode. Untouched outputs must
     * start 0001 on EVERY invocation. No pixel shader or rasterized stream. */
    uint32_t program[2][4]={{0,(1u<<21)|0x1Bu,2u<<26,(15u<<12)|0x800u},
        {0,(1u<<21)|(9u<<9)|0x1Bu,2u<<26,(15u<<12)|0x800u|(9u<<3)|1u}};
    uint16_t mask=0;
    CHECK(fut_vertex_program_input_mask(program,2,&mask) && mask==0x201,1);
    uint32_t invalid[2][4];memcpy(invalid,program,sizeof(invalid));invalid[1][3]&=~1u;
    mask=0xFFFF;CHECK(!fut_vertex_program_input_mask(invalid,2,&mask) && !mask,2);
    CHECK(fut_vertex_program_input_mask(program,2,&mask),3);
    CHECK(fut_graphics_immediate_method(&batch,&kelvin,0x17FC,5)==FUT_IMMEDIATE_APPLIED,4);
    for(unsigned i=0;i<3;++i) CHECK(pair(9,0.5f+i*4,2.0f+i) && pair(0,1.0f+i,3.0f+i*2),5);
    CHECK(fut_graphics_immediate_method(&batch,&kelvin,0x17FC,0)==FUT_IMMEDIATE_READY,6);
    ID3D11Device *device=NULL;ID3D11DeviceContext *context=NULL;D3D_FEATURE_LEVEL level;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,
        D3D11_SDK_VERSION,&device,&level,&context)),7);
    ID3D11VertexShader *shader=NULL;ID3DBlob *bytecode=NULL;
    CHECK(SUCCEEDED(fut_native_vertex_compile(device,program,2,&shader,&bytecode)),8);
    FutNativeImmediate native={0};FutNativeConstants constants;
    CHECK(SUCCEEDED(fut_native_constants_init(&constants,device,context)),9);
    CHECK(SUCCEEDED(fut_native_immediate_upload(&native,device,context,bytecode,&batch,mask)) &&
        native.count==3 && batch.ready && batch.count==3,10);
    ID3D11Buffer *original=native.buffer;
    batch.defined[1]&=~1u;
    CHECK(fut_native_immediate_upload(&native,device,context,bytecode,&batch,mask)==E_NOTIMPL &&
        native.buffer==original && batch.ready,11);
    batch.defined[1]|=1u;
    D3D11_SO_DECLARATION_ENTRY declaration[]={{0,"SV_POSITION",0,0,4,0},
        {0,"COLOR",0,0,4,0},{0,"COLOR",1,0,4,0},
        {0,"TEXCOORD",0,0,4,0},{0,"TEXCOORD",1,0,4,0}};
    UINT stride=80,offset=0;ID3D11GeometryShader *stream=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateGeometryShaderWithStreamOutput(device,
        ID3D10Blob_GetBufferPointer(bytecode),ID3D10Blob_GetBufferSize(bytecode),
        declaration,5,&stride,1,D3D11_SO_NO_RASTERIZED_STREAM,NULL,&stream)),12);
    D3D11_BUFFER_DESC desc={0};desc.ByteWidth=3*stride;
    desc.Usage=D3D11_USAGE_DEFAULT;desc.BindFlags=D3D11_BIND_STREAM_OUTPUT;
    ID3D11Buffer *output=NULL,*staging=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&desc,NULL,&output)),13);
    desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&desc,NULL,&staging)),14);
    ID3D11DeviceContext_VSSetShader(context,shader,NULL,0);
    ID3D11DeviceContext_GSSetShader(context,stream,NULL,0);
    ID3D11DeviceContext_SOSetTargets(context,1,&output,&offset);
    ID3D11DeviceContext_Draw(context,3,0);
    CHECK(fut_native_constants_finish(&constants,NULL,2000,NULL),15);
    ID3D11DeviceContext_SOSetTargets(context,0,NULL,NULL);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)staging,(ID3D11Resource*)output);
    CHECK(fut_native_constants_finish(&constants,NULL,2000,NULL),16);
    D3D11_MAPPED_SUBRESOURCE mapped;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)staging,0,D3D11_MAP_READ,0,&mapped)),17);
    for(unsigned i=0;i<3;++i) {
        float expected[20]={1.0f+i,3.0f+i*2,0,1, 0,0,0,1, 0,0,0,1,
            0.5f+i*4,2.0f+i,0,1, 0,0,0,1};
        CHECK(!memcmp((const float*)mapped.pData+i*20,expected,sizeof(expected)),18);
    }
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)staging,0);
    CHECK(batch.ready && batch.count==3,19); /* SO test does not consume original draw. */
    CHECK(verify_screen(device,context,&constants,program)==0,20);
    FutGraphicsImmediate saved_batch=batch;
    memset(&batch,0,sizeof(batch));batch.primitive=2;batch.input_kind=4;batch.ready=1;batch.count=32;
    for(unsigned i=0;i<32;++i) {
        batch.vertices[i][0][0]=(float)(i&7u);batch.vertices[i][0][1]=(float)(i>>3);
        batch.vertices[i][0][2]=0;batch.vertices[i][0][3]=1;batch.defined[i]=1;
    }
    CHECK(_putenv_s("RECOMP_LINES_16SEGMENT_AA2_APPROXIMATION","0")==0,61);
    CHECK(fut_native_immediate_upload(&native,device,context,bytecode,&batch,1)==E_INVALIDARG &&
        native.count==3 && batch.ready,62);
    CHECK(_putenv_s("RECOMP_LINES_16SEGMENT_AA2_APPROXIMATION","1")==0,63);
    batch.input_kind=3;
    CHECK(fut_native_immediate_upload(&native,device,context,bytecode,&batch,1)==E_INVALIDARG,64);
    batch.input_kind=4;batch.count=31;
    CHECK(fut_native_immediate_upload(&native,device,context,bytecode,&batch,1)==E_INVALIDARG,65);
    batch.count=32;
    CHECK(SUCCEEDED(fut_native_immediate_upload(&native,device,context,bytecode,&batch,1)) &&
        native.count==32 && native.primitive==2,66);
    D3D11_PRIMITIVE_TOPOLOGY topology=D3D11_PRIMITIVE_TOPOLOGY_UNDEFINED;
    ID3D11DeviceContext_IAGetPrimitiveTopology(context,&topology);
    CHECK(topology==D3D11_PRIMITIVE_TOPOLOGY_LINELIST,67);
    CHECK(_putenv_s("RECOMP_LINES_16SEGMENT_AA2_APPROXIMATION","0")==0,68);
    CHECK(fut_native_immediate_upload(&native,device,context,bytecode,&batch,1)==E_INVALIDARG &&
        native.count==32,69);
    batch=saved_batch;
    ID3D11Buffer_Release(staging);ID3D11Buffer_Release(output);
    ID3D11GeometryShader_Release(stream);fut_native_immediate_destroy(&native);
    fut_native_constants_destroy(&constants);ID3D11VertexShader_Release(shader);ID3D10Blob_Release(bytecode);
    ID3D11DeviceContext_ClearState(context);ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    puts("Translated VS: raw attributes/defaults, screen/clip viewport inverse, signed1/16 truncation, signedW domain and bounded SO-only execution verified; no game frame");
    return 0;
}
