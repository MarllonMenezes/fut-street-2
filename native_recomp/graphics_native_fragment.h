#ifndef FUT_GRAPHICS_NATIVE_FRAGMENT_H
#define FUT_GRAPHICS_NATIVE_FRAGMENT_H
#include <d3dcompiler.h>
#include "graphics_native_textures.h"
typedef struct {
    ID3D11Device *device;
    char *source;
    size_t source_length;
    unsigned candidates;
    int aa2;
    ID3D11PixelShader *shader;
    ID3DBlob *bytecode;
} FutNativeFragmentCache;
/* Validate used PS inputs against actual VS output register/component,
 * semantic and type mappings. Shared source declarations alone are not proof. */
HRESULT fut_native_fragment_linkage(ID3DBlob *vertex,ID3DBlob *pixel);
/* Supported combiner equations with current approved samplers OR a validated
 * zero-RGB snapshot candidate whose unknown-alpha texture resource is proven
 * absent from the compiled bytecode. Residual candidate reads are refused.
 * Does not implement generic quincunx, RDI or dithering. Compilation and
 * synthetic GPU pixels are not proof of an original rasterized draw. */
/* Enabled alpha test is compiled into this shader, including discard before
 * host Z/stencil updates. Comparison uses rounded 8-bit final alpha. */
HRESULT fut_native_fragment_compile(ID3D11Device *device,const FutGraphicsKelvinState *state,
    const FutNativeTexture textures[4],ID3D11PixelShader **shader,ID3DBlob **bytecode);
/* Explicit AA2 coverage/center-interpolation model. Caller must prove every
 * unshifted VS output has W=1, bind the matching AA2 VS and VS/PS b4 pass.
 * Does not approve dithering, raw PGRAPH controls or generic NV2A fidelity. */
HRESULT fut_native_fragment_compile_aa2_affine(ID3D11Device *device,const FutGraphicsKelvinState *state,
    const FutNativeTexture textures[4],int unit_w_verified,ID3D11PixelShader **shader,ID3DBlob **bytecode);
/* Exact generated-HLSL cache for the explicitly scoped AA2 experiment. */
HRESULT fut_native_fragment_compile_aa2_affine_cached(ID3D11Device *device,
    const FutGraphicsKelvinState *state,const FutNativeTexture textures[4],int unit_w_verified,
    FutNativeFragmentCache *cache,ID3D11PixelShader **shader,ID3DBlob **bytecode,int *cache_hit);
void fut_native_fragment_cache_destroy(FutNativeFragmentCache *cache);
#endif
