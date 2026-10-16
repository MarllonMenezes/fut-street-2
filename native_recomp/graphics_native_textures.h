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
    uint32_t texel_or,texel_and; /* All uploaded texels, excluding pitch padding. */
    int valid, sampling_ready,zero_rgb_candidate,quincunx_experiment;
} FutNativeTexture;
/* Refresh from guest including CPU writes. Caller must first write back any
 * overlapping GPU render targets. Failure preserves the existing resources. */
HRESULT fut_native_texture_upload(FutNativeTexture *native, ID3D11Device *device,
    const FutGraphicsTexturePlan *plan, FutGraphicsMemoryRead read, void *context);
/* Matching point/linear MIN/MAG, one mip, wrap/mirror/clamp/border only.
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
/* Projected texel-space coordinates for this LINEAR 2D storage, level zero.
 * Only valid with an approved sampler; not an original fragment program. */
const char *fut_native_texture_hlsl(void);
#endif
