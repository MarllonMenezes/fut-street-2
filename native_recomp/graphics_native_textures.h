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
    int valid, sampling_ready;
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
void fut_native_texture_destroy(FutNativeTexture *native);
/* Projected texel-space coordinates for this LINEAR 2D storage, level zero.
 * Only valid with an approved sampler; not an original fragment program. */
const char *fut_native_texture_hlsl(void);
#endif
