#define COBJMACROS
#include "graphics_native_immediate.h"
#include <string.h>
void fut_native_immediate_destroy(FutNativeImmediate *native)
{
    if(native->buffer) ID3D11Buffer_Release(native->buffer);
    if(native->layout) ID3D11InputLayout_Release(native->layout);
    memset(native,0,sizeof(*native));
}
HRESULT fut_native_immediate_upload(FutNativeImmediate *native,ID3D11Device *device,
    ID3D11DeviceContext *context,ID3DBlob *bytecode,const FutGraphicsImmediate *b,uint16_t required)
{
    if(!native || !device || !context || !bytecode || !b || b->active || !b->ready ||
        b->primitive!=5 || !b->count || b->count>FUT_IMMEDIATE_CAPACITY || b->count%3) return E_INVALIDARG;
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
    next.count=b->count;
    UINT stride=sizeof(b->vertices[0]),offset=0;
    ID3D11DeviceContext_IASetInputLayout(context,next.layout);
    ID3D11DeviceContext_IASetVertexBuffers(context,0,1,&next.buffer,&stride,&offset);
    ID3D11DeviceContext_IASetPrimitiveTopology(context,D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    fut_native_immediate_destroy(native);*native=next;return S_OK;
}
