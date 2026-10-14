#define COBJMACROS
#include "graphics_native_vertex.h"
#include "graphics_vertex_program.h"
#include <stdio.h>
#include <string.h>

static HRESULT compile(ID3D11Device *device, const uint32_t (*words)[4],
    unsigned count, ID3D11VertexShader **shader, ID3DBlob **bytecode,int screen)
{
    if(!shader || !bytecode) return E_INVALIDARG;
    *shader = NULL; *bytecode = NULL;
    if(!device) return E_INVALIDARG;
    char hlsl[65536]; size_t length;
    if (!(screen ? fut_vertex_program_hlsl_screen(words,count,hlsl,sizeof(hlsl),&length) :
        fut_vertex_program_hlsl(words, count, hlsl, sizeof(hlsl), &length)))
        return E_NOTIMPL;
    ID3DBlob *errors = NULL;
    HRESULT hr = D3DCompile(hlsl, length, "recompiled_vertex_program", NULL, NULL,
        "main", "vs_5_0", D3DCOMPILE_IEEE_STRICTNESS, 0, bytecode, &errors);
    if (errors) {
        fprintf(stderr, "[VERTEX-COMPILER] %.*s\n", (int)ID3D10Blob_GetBufferSize(errors),
                (const char *)ID3D10Blob_GetBufferPointer(errors));
        ID3D10Blob_Release(errors);
    }
    if (SUCCEEDED(hr))
        hr = ID3D11Device_CreateVertexShader(device, ID3D10Blob_GetBufferPointer(*bytecode),
            ID3D10Blob_GetBufferSize(*bytecode), NULL, shader);
    if (FAILED(hr) && *bytecode) { ID3D10Blob_Release(*bytecode); *bytecode = NULL; }
    return hr;
}
HRESULT fut_native_vertex_compile(ID3D11Device *device,const uint32_t (*words)[4],
    unsigned count,ID3D11VertexShader **shader,ID3DBlob **bytecode)
{
    return compile(device,words,count,shader,bytecode,0);
}
HRESULT fut_native_vertex_compile_screen(ID3D11Device *device,const uint32_t (*words)[4],
    unsigned count,ID3D11VertexShader **shader,ID3DBlob **bytecode)
{
    return compile(device,words,count,shader,bytecode,1);
}
void fut_native_vertex_surface_destroy(FutNativeVertexSurface *native)
{
    if(native->buffer) ID3D11Buffer_Release(native->buffer);
    memset(native,0,sizeof(*native));
}
HRESULT fut_native_vertex_surface_prepare(FutNativeVertexSurface *native,
    ID3D11Device *device,ID3D11DeviceContext *context,const FutGraphicsSurfacePlan *surface)
{
    if(!native || !device || !context || !surface || !surface->logical_width ||
        !surface->logical_height || surface->logical_width>16384 || surface->logical_height>16384)
        return E_INVALIDARG;
    if(surface->samples!=1 || surface->width!=surface->logical_width || surface->height!=surface->logical_height)
        return E_NOTIMPL;
    if(!native->buffer || native->width!=surface->width || native->height!=surface->height) {
        const float values[4]={(float)surface->width,(float)surface->height,16777215.0f,0};
        D3D11_BUFFER_DESC desc={0};desc.ByteWidth=sizeof(values);
        desc.Usage=D3D11_USAGE_IMMUTABLE;desc.BindFlags=D3D11_BIND_CONSTANT_BUFFER;
        D3D11_SUBRESOURCE_DATA data={values,0,0};ID3D11Buffer *buffer=NULL;
        HRESULT hr=ID3D11Device_CreateBuffer(device,&desc,&data,&buffer);
        if(FAILED(hr)) return hr;
        fut_native_vertex_surface_destroy(native);native->buffer=buffer;
        native->width=surface->width;native->height=surface->height;
    }
    ID3D11DeviceContext_VSSetConstantBuffers(context,3,1,&native->buffer);
    return S_OK;
}
