#define COBJMACROS
#include "graphics_native_immediate.h"
#include "graphics_experiment.h"
#include <string.h>
#include <math.h>
void fut_native_immediate_destroy(FutNativeImmediate *native)
{
    if(native->buffer) ID3D11Buffer_Release(native->buffer);
    if(native->layout) ID3D11InputLayout_Release(native->layout);
    memset(native,0,sizeof(*native));
}
HRESULT fut_native_immediate_upload(FutNativeImmediate *native,ID3D11Device *device,
    ID3D11DeviceContext *context,ID3DBlob *bytecode,const FutGraphicsImmediate *b,uint16_t required)
{
    if(!native || !device || !context || !bytecode || !b || b->active || !b->ready) return E_INVALIDARG;
    if(b->primitive==2) {
        if(!fut_graphics_lines_aa2_count_enabled(b->count) || b->input_kind!=4)
            return E_INVALIDARG;
    } else if(b->primitive!=5 || !b->count || b->count>FUT_IMMEDIATE_DRAW_CAPACITY || b->count%3)
        return E_INVALIDARG;
    if(native->submitted) return E_PENDING;
    for(unsigned i=0;i<b->count;++i) if((b->defined[i]&required)!=required) return E_NOTIMPL;
    FutNativeImmediate next={0};
    D3D11_BUFFER_DESC desc={0};desc.ByteWidth=b->count*sizeof(b->vertices[0]);
    desc.Usage=D3D11_USAGE_IMMUTABLE;desc.BindFlags=D3D11_BIND_VERTEX_BUFFER;
    D3D11_SUBRESOURCE_DATA data={b->vertices,0,0};
    HRESULT hr=ID3D11Device_CreateBuffer(device,&desc,&data,&next.buffer);
    D3D11_INPUT_ELEMENT_DESC elements[16]={0};
    for(unsigned i=0;i<16;++i) {
        elements[i].SemanticName="ATTR";elements[i].SemanticIndex=i;
        elements[i].Format=DXGI_FORMAT_R32G32B32A32_FLOAT;elements[i].AlignedByteOffset=i*16;
        elements[i].InputSlotClass=D3D11_INPUT_PER_VERTEX_DATA;
    }
    if(SUCCEEDED(hr)) hr=ID3D11Device_CreateInputLayout(device,elements,16,
        ID3D10Blob_GetBufferPointer(bytecode),ID3D10Blob_GetBufferSize(bytecode),&next.layout);
    if(FAILED(hr)) {fut_native_immediate_destroy(&next);return hr;}
    next.count=b->count;next.primitive=b->primitive;
    UINT stride=sizeof(b->vertices[0]),offset=0;
    ID3D11DeviceContext_IASetInputLayout(context,next.layout);
    ID3D11DeviceContext_IASetVertexBuffers(context,0,1,&next.buffer,&stride,&offset);
    ID3D11DeviceContext_IASetPrimitiveTopology(context,b->primitive==2?
        D3D11_PRIMITIVE_TOPOLOGY_LINELIST:D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    fut_native_immediate_destroy(native);*native=next;return S_OK;
}
HRESULT fut_native_immediate_probe_positions(const FutNativeImmediate *native,
    ID3D11Device *device,ID3D11DeviceContext *context,ID3D11VertexShader *shader,ID3DBlob *bytecode,
    FutNativeConstants *completion,FutNativePosition *positions,unsigned capacity,
    DWORD timeout_ms,HANDLE stop_event)
{
    if(!native || !native->buffer || !native->layout || !native->count ||
        (native->primitive==2 ? !fut_graphics_lines_aa2_count_enabled(native->count) :
            (native->primitive!=5 || native->count>FUT_IMMEDIATE_DRAW_CAPACITY || native->count%3)) || !device || !context || !shader || !bytecode ||
        !completion || !completion->buffer || !completion->completion || completion->context!=context || !positions || capacity<native->count ||
        !timeout_ms || timeout_ms>2000) return E_INVALIDARG;
    if(stop_event && WaitForSingleObject(stop_event,0)!=WAIT_TIMEOUT) return E_ABORT;
    ID3D11GeometryShader *previous=NULL;
    ID3D11DeviceContext_GSGetShader(context,&previous,NULL,NULL);
    if(previous) {ID3D11GeometryShader_Release(previous);return E_NOTIMPL;}
    ID3D11Buffer *targets[D3D11_SO_BUFFER_SLOT_COUNT]={0};int occupied=0;
    ID3D11DeviceContext_SOGetTargets(context,D3D11_SO_BUFFER_SLOT_COUNT,targets);
    for(unsigned i=0;i<D3D11_SO_BUFFER_SLOT_COUNT;++i) if(targets[i]) {
        occupied=1;ID3D11Buffer_Release(targets[i]);
    }
    if(occupied) return E_NOTIMPL;
    D3D11_SO_DECLARATION_ENTRY decl[]={{0,"SV_POSITION",0,0,4,0},{0,"TEXCOORD",6,0,4,0},
        {0,"TEXCOORD",0,0,4,0},{0,"TEXCOORD",1,0,4,0},
        {0,"TEXCOORD",2,0,4,0},{0,"TEXCOORD",3,0,4,0}};
    UINT stride=sizeof(FutNativePosition),offset=0;ID3D11GeometryShader *stream=NULL;
    HRESULT hr=ID3D11Device_CreateGeometryShaderWithStreamOutput(device,
        ID3D10Blob_GetBufferPointer(bytecode),ID3D10Blob_GetBufferSize(bytecode),
        decl,6,&stride,1,D3D11_SO_NO_RASTERIZED_STREAM,NULL,&stream);
    ID3D11Buffer *output=NULL,*staging=NULL;
    uint32_t sentinel[FUT_IMMEDIATE_DRAW_CAPACITY*sizeof(FutNativePosition)/sizeof(uint32_t)];
    for(unsigned i=0;i<sizeof(sentinel)/sizeof(*sentinel);++i) sentinel[i]=0x7FC00000;
    D3D11_BUFFER_DESC desc={0};desc.ByteWidth=native->count*stride;
    desc.Usage=D3D11_USAGE_DEFAULT;desc.BindFlags=D3D11_BIND_STREAM_OUTPUT;
    D3D11_SUBRESOURCE_DATA initial={sentinel,0,0};
    if(SUCCEEDED(hr)) hr=ID3D11Device_CreateBuffer(device,&desc,&initial,&output);
    desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    if(SUCCEEDED(hr)) hr=ID3D11Device_CreateBuffer(device,&desc,NULL,&staging);
    if(FAILED(hr)) goto done;
    UINT input_stride=16*4*sizeof(float);
    ID3D11DeviceContext_IASetInputLayout(context,native->layout);
    ID3D11DeviceContext_IASetVertexBuffers(context,0,1,&native->buffer,&input_stride,&offset);
    ID3D11DeviceContext_IASetPrimitiveTopology(context,native->primitive==2?
        D3D11_PRIMITIVE_TOPOLOGY_LINELIST:D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    ID3D11DeviceContext_VSSetShader(context,shader,NULL,0);
    ID3D11DeviceContext_GSSetShader(context,stream,NULL,0);
    ID3D11DeviceContext_SOSetTargets(context,1,&output,&offset);
    ID3D11DeviceContext_Draw(context,native->count,0);
    ID3D11DeviceContext_SOSetTargets(context,0,NULL,NULL);
    ID3D11DeviceContext_GSSetShader(context,NULL,NULL,0);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)staging,(ID3D11Resource*)output);
    if(!fut_native_constants_finish(completion,NULL,timeout_ms,stop_event)) {hr=E_FAIL;goto done;}
    D3D11_MAPPED_SUBRESOURCE mapped;
    hr=ID3D11DeviceContext_Map(context,(ID3D11Resource*)staging,0,D3D11_MAP_READ,0,&mapped);
    if(FAILED(hr)) goto done;
    const FutNativePosition *values=mapped.pData;
    for(unsigned i=0;i<native->count;++i) {
        for(unsigned c=0;c<4;++c) if(!isfinite(values[i].screen[c]) || !isfinite(values[i].clip[c])) hr=E_NOTIMPL;
        if(values[i].screen[3]<0x1p-64f || values[i].screen[3]>0x1p64f) hr=E_NOTIMPL;
    }
    if(SUCCEEDED(hr)) memcpy(positions,values,native->count*sizeof(*positions));
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)staging,0);
done:
    if(staging) ID3D11Buffer_Release(staging);
    if(output) ID3D11Buffer_Release(output);
    if(stream) ID3D11GeometryShader_Release(stream);
    return hr;
}
unsigned fut_native_immediate_constant_coordinates(const FutNativePosition *positions,unsigned count)
{
    if(!positions || !count || count>FUT_IMMEDIATE_DRAW_CAPACITY ||
        (count%3 && !fut_graphics_lines_aa2_count_enabled(count))) return 0;
    unsigned mask=15;
    for(unsigned i=0;i<count;++i) {
        const FutNativePosition *p=&positions[i];
        if(p->screen[3]!=1 || p->clip[3]!=1) return 0;
        for(unsigned c=0;c<4;++c) if(!isfinite(p->screen[c]) || !isfinite(p->clip[c]) ||
            (c<2 && (fabsf(p->screen[c])>0x1p20f || fabsf(p->clip[c])>0x1p16f))) return 0;
        for(unsigned stage=0;stage<4;++stage) {
            for(unsigned c=0;c<4;++c) if(!isfinite(p->texture[stage][c]) ||
                fabsf(p->texture[stage][c])>0x1p20f) mask&=~(1u<<stage);
            if(p->texture[stage][3]!=1) mask&=~(1u<<stage);
        }
    }
    return mask;
}

int fut_native_immediate_quincunx_coordinates(const FutNativePosition *p,unsigned count)
{
    if(!p || count!=3 || !(fut_native_immediate_constant_coordinates(p,count)&1)) return 0;
    static const float screen[3][2]={{0,0},{2560,0},{0,1920}};
    for(unsigned i=0;i<3;++i) {
        float x=screen[i][0],y=screen[i][1];
        /* The translated viewport inverse yields these captured one-ULP Y
         * values. Admit those exact values, not an arbitrary tolerance. */
        float observed_y=i==2?-0x1.bffffep+2f:0x1.fffffep-1f;
        if(p[i].screen[0]!=x || p[i].screen[1]!=y || p[i].screen[2]!=0 ||
            p[i].clip[0]!=x/320-1 ||
            (p[i].clip[1]!=1-y/240 && p[i].clip[1]!=observed_y) || p[i].clip[2]!=0 ||
            p[i].texture[0][0]!=2*x+.5f || p[i].texture[0][1]!=y ||
            p[i].texture[0][2]!=0) return 0;
    }
    return 1;
}
