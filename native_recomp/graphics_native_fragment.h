#ifndef FUT_GRAPHICS_NATIVE_FRAGMENT_H
#define FUT_GRAPHICS_NATIVE_FRAGMENT_H
#include <d3dcompiler.h>
#include "graphics_native_textures.h"
/* Produces a native shader only for supported combiner equations AND current
 * approved texture samplers. Does not translate geometry, RDI, dithering or
 * unsupported filters. Bytecode compilation is not proof of an original draw. */
HRESULT fut_native_fragment_compile(ID3D11Device *device,const FutGraphicsKelvinState *state,
    const FutNativeTexture textures[4],ID3D11PixelShader **shader,ID3DBlob **bytecode);
#endif
