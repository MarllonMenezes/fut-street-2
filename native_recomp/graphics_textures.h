#ifndef FUT_GRAPHICS_TEXTURES_H
#define FUT_GRAPHICS_TEXTURES_H
#include "graphics_kelvin_state.h"
#include "graphics_objects.h"

typedef struct {
    uint32_t address, span, pitch, width, height;
    unsigned bytes_per_pixel; /* A8=1, A8R8G8B8=4; zero for BC1/BC2/BC3 block storage. */
    unsigned dma_context; /* Texture context A=1 (method184), B=2 (method188). */
    unsigned storage; /* 0=linear, 1=BC1, 2=P8, 3=BC2, 4=YUY2 experiment, 5=swizzled ARGB8 mips, 6=BC3 mips. */
    uint32_t palette_address, palette_span; /* P8 only, 256 A8R8G8B8 entries. */
    unsigned mip_levels; /* BC1/BC2/BC3 or swizzled ARGB8 chain; zero means legacy one mip. */
} FutGraphicsTexturePlan;
typedef enum {
    FUT_TEXTURE_READY, FUT_TEXTURE_DISABLED, FUT_TEXTURE_INCOMPLETE,
    FUT_TEXTURE_UNSUPPORTED, FUT_TEXTURE_INVALID_RANGE
} FutGraphicsTextureStatus;
/* Storage description only: linear A8/ARGB8 or power-of-two 2D BC1/BC2/BC3/P8;
 * BC1/BC2/BC3 and swizzled ARGB8 support bounded concatenated mip chains.
 * P8 currently requires a 256-entry ARGB8 palette in the same selected DMA.
 * Filter/texgen/fragment-program equivalence is not implied by a READY plan.
 * Failed/disabled plans never modify output. DMA is the selected context. */
FutGraphicsTextureStatus fut_graphics_texture_plan(const FutGraphicsTextureState *state,
    const FutGraphicsDmaView *dma, FutGraphicsTexturePlan *plan);

/* Passive capture for the observed bounded YUY2 640x480 linear state. This
 * reads guest bytes transactionally and does not translate or sample them. */
#define FUT_GRAPHICS_OBSERVED_YUY2_BYTES 614400u
int fut_graphics_texture_capture_observed_yuy2(FutGraphicsMemoryRead read,
    void *memory_context, const FutGraphicsTextureState *state,
    const FutGraphicsDmaView *dma, uint8_t output[FUT_GRAPHICS_OBSERVED_YUY2_BYTES]);
/* Exact captured state gate and explicitly approximate BT.601-limited decode. */
int fut_graphics_texture_plan_observed_yuy2_approximation(
    const FutGraphicsTextureState *state,const FutGraphicsDmaView *dma,
    FutGraphicsTexturePlan *plan);
int fut_graphics_texture_yuy2_bt601_to_bgra(const uint8_t *source,size_t source_size,
    uint8_t *output,size_t output_size);
#endif
