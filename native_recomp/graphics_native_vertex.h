#ifndef FUT_GRAPHICS_NATIVE_VERTEX_H
#define FUT_GRAPHICS_NATIVE_VERTEX_H
#include <windows.h>
#include <d3d11.h>
#include <d3dcompiler.h>
#include <stdint.h>
#include "graphics_surfaces.h"

HRESULT fut_native_vertex_compile(ID3D11Device *device, const uint32_t (*words)[4],
    unsigned count, ID3D11VertexShader **shader, ID3DBlob **bytecode);
HRESULT fut_native_vertex_compile_screen(ID3D11Device *device,const uint32_t (*words)[4],
    unsigned count,ID3D11VertexShader **shader,ID3DBlob **bytecode);
typedef struct {ID3D11Buffer *buffer;unsigned width,height;} FutNativeVertexSurface;
/* Only the existing integerZ24S8 single-sample plan. Invalid/unsupported
 * plans preserve old resources and do not bind misleading dimensions. */
HRESULT fut_native_vertex_surface_prepare(FutNativeVertexSurface *native,
    ID3D11Device *device,ID3D11DeviceContext *context,const FutGraphicsSurfacePlan *surface);
void fut_native_vertex_surface_destroy(FutNativeVertexSurface *native);
#endif
