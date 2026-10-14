#define COBJMACROS
#include "graphics_native_immediate.h"
#include "graphics_native_vertex.h"
#include "graphics_native_constants.h"
#include "graphics_vertex_program.h"
#include <stdio.h>
#include <string.h>
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"native immediate test failed: %d\n",n);return n;}}while(0)
static FutGraphicsImmediate batch;
static FutGraphicsKelvinState kelvin;
static int pair(unsigned slot,float x,float y)
{
    uint32_t a,b;memcpy(&a,&x,4);memcpy(&b,&y,4);
    return fut_graphics_immediate_method(&batch,&kelvin,0x1880+slot*8,a)==FUT_IMMEDIATE_APPLIED &&
        fut_graphics_immediate_method(&batch,&kelvin,0x1884+slot*8,b)==FUT_IMMEDIATE_APPLIED;
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
    ID3D11Buffer_Release(staging);ID3D11Buffer_Release(output);
    ID3D11GeometryShader_Release(stream);fut_native_immediate_destroy(&native);
    fut_native_constants_destroy(&constants);ID3D11VertexShader_Release(shader);ID3D10Blob_Release(bytecode);
    ID3D11DeviceContext_ClearState(context);ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    puts("Three synthetic2F vertices executed by translated VS: exact positions/UV/default alpha1; no rasterization");
    return 0;
}
