#ifndef FUT_GRAPHICS_NATIVE_TEXTURES_H
#define FUT_GRAPHICS_NATIVE_TEXTURES_H
#include <d3d11.h>
#include "graphics_textures.h"
typedef struct {
    ID3D11Texture2D *texture;
    ID3D11ShaderResourceView *view;
    ID3D11SamplerState *sampler;
    FutGraphicsTexturePlan plan;
    FutGraphicsTextureState sampling_state;
    uint32_t texel_or,texel_and; /* Linear texels; conservative FFFFFFFF/0 for BC1/BC2. */
    int valid, sampling_ready,zero_rgb_candidate,quincunx_experiment;
} FutNativeTexture;
/* Refresh from guest including CPU writes. Caller must first write back any
 * overlapping GPU render targets. Failure preserves the existing resources. */
HRESULT fut_native_texture_upload(FutNativeTexture *native, ID3D11Device *device,
    const FutGraphicsTexturePlan *plan, FutGraphicsMemoryRead read, void *context);
/* Matching point/linear MIN/MAG, one mip, wrap/mirror/clamp/border;
 * captured swizzled ARGB8 also supports trilinear MIN6/MAG2 and signed5.8 bias.
 * Convolution (including the observed quincunx), signed channels, color key,
 * alpha kill and unsupported controls are explicitly refused, not approximated.
 * Failure preserves objects but sets sampling_ready=0: do not use old sampler. */
HRESULT fut_native_texture_sampler(FutNativeTexture *native, ID3D11Device *device,
    const FutGraphicsTextureState *state);
/* Partial evaluation, NOT a quincunx sampler: all uploaded RGB must be0,
 * exact observed convolution/clamp state and bounded finite q=1 geometry.
 * Alpha remains unimplemented. Fragment compilation MUST prove the candidate
 * texture resource disappears before creating an executable PS. */
HRESULT fut_native_texture_zero_rgb_candidate(FutNativeTexture *native,
    const FutGraphicsTextureState *state,int coordinates_verified);
/* Explicit experiment opt-in plus exact fullscreen-resolve scope required.
 * Five taps, identity color space, parity-preserving edge clamp; precision,
 * phase and edge behavior are a model, not hardware measurements. */
HRESULT fut_native_texture_quincunx_experiment(FutNativeTexture *native,
    const FutGraphicsTextureState *state,int resolve_verified);
void fut_native_texture_destroy(FutNativeTexture *native);
/* Sampling helpers: projected level zero, or normalized explicit gradients
 * for mip chains. Gradients must precede discards and use logical pixels.
 * Extent is dimensions for linear storage,
 * or (1,1) for BC1/BC2/P8 normalized coordinates.
 * Only valid with an approved sampler; not an original fragment program. */
const char *fut_native_texture_hlsl(void);
#endif
