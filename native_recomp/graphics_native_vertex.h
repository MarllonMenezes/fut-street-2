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
HRESULT fut_native_vertex_compile_screen_aa2(ID3D11Device *device,const uint32_t (*words)[4],
    unsigned count,ID3D11VertexShader **shader,ID3DBlob **bytecode);

#define FUT_NATIVE_VERTEX_CACHE_CAPACITY 32u
typedef struct {
    ID3D11Device *device; /* Kept alive by the retained shader. */
    uint32_t words[136][4];
    unsigned count;
    int aa2;
    ID3D11VertexShader *shader;
    ID3DBlob *bytecode;
} FutNativeVertexCacheEntry;
typedef struct {
    FutNativeVertexCacheEntry entries[FUT_NATIVE_VERTEX_CACHE_CAPACITY];
    unsigned next;
} FutNativeVertexCache;
/* Exact instruction/device/mode key. NULL cache compiles without retaining.
 * Successful outputs own references independent of cache eviction/destruction.
 * The cache is confined to its owning graphics thread. */
HRESULT fut_native_vertex_compile_screen_cached(ID3D11Device *device,
    const uint32_t (*words)[4],unsigned count,int aa2,FutNativeVertexCache *cache,
    ID3D11VertexShader **shader,ID3DBlob **bytecode,int *cache_hit);
void fut_native_vertex_cache_destroy(FutNativeVertexCache *cache);
typedef struct {ID3D11Buffer *buffer;unsigned width,height,rgb565_z16;} FutNativeVertexSurface;
/* Integer Z24S8 or the explicit RGB565/Z16 plan. Invalid/unsupported
 * plans preserve old resources and do not bind misleading dimensions. */
HRESULT fut_native_vertex_surface_prepare(FutNativeVertexSurface *native,
    ID3D11Device *device,ID3D11DeviceContext *context,const FutGraphicsSurfacePlan *surface);
void fut_native_vertex_surface_destroy(FutNativeVertexSurface *native);
#endif
