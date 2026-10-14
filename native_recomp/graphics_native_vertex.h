#ifndef FUT_GRAPHICS_NATIVE_VERTEX_H
#define FUT_GRAPHICS_NATIVE_VERTEX_H
#include <windows.h>
#include <d3d11.h>
#include <d3dcompiler.h>
#include <stdint.h>

HRESULT fut_native_vertex_compile(ID3D11Device *device, const uint32_t (*words)[4],
    unsigned count, ID3D11VertexShader **shader, ID3DBlob **bytecode);
#endif
