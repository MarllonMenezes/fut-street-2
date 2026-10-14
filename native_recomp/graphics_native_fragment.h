#ifndef FUT_GRAPHICS_NATIVE_FRAGMENT_H
#define FUT_GRAPHICS_NATIVE_FRAGMENT_H
#include <d3dcompiler.h>
#include "graphics_native_textures.h"
/* Validate used PS inputs against actual VS output register/component,
 * semantic and type mappings. Shared source declarations alone are not proof. */
HRESULT fut_native_fragment_linkage(ID3DBlob *vertex,ID3DBlob *pixel);
/* Supported combiner equations with current approved samplers OR a validated
 * zero-RGB snapshot candidate whose unknown-alpha texture resource is proven
 * absent from the compiled bytecode. Residual candidate reads are refused.
 * Does not implement generic quincunx, RDI or dithering. Compilation and
 * synthetic GPU pixels are not proof of an original rasterized draw. */
HRESULT fut_native_fragment_compile(ID3D11Device *device,const FutGraphicsKelvinState *state,
    const FutNativeTexture textures[4],ID3D11PixelShader **shader,ID3DBlob **bytecode);
#endif
