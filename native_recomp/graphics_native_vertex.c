#define COBJMACROS
#include "graphics_native_vertex.h"
#include "graphics_vertex_program.h"
#include <stdio.h>

HRESULT fut_native_vertex_compile(ID3D11Device *device, const uint32_t (*words)[4],
    unsigned count, ID3D11VertexShader **shader, ID3DBlob **bytecode)
{
    *shader = NULL; *bytecode = NULL;
    char hlsl[65536]; size_t length;
    if (!fut_vertex_program_hlsl(words, count, hlsl, sizeof(hlsl), &length))
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
