#ifndef FUT_GRAPHICS_TEXTURES_H
#define FUT_GRAPHICS_TEXTURES_H
#include "graphics_kelvin_state.h"
#include "graphics_objects.h"

typedef struct {
    uint32_t address, span, pitch, width, height;
    unsigned bytes_per_pixel; /* A8=1, A8R8G8B8=4; zero for BC1 block storage. */
    unsigned dma_context; /* Texture context A=1 (method184), B=2 (method188). */
    unsigned storage; /* 0=linear texels, 1=BC1, 2=swizzled P8. 1/2 use normalized UV. */
    uint32_t palette_address, palette_span; /* P8 only, 256 A8R8G8B8 entries. */
} FutGraphicsTexturePlan;
typedef enum {
    FUT_TEXTURE_READY, FUT_TEXTURE_DISABLED, FUT_TEXTURE_INCOMPLETE,
    FUT_TEXTURE_UNSUPPORTED, FUT_TEXTURE_INVALID_RANGE
} FutGraphicsTextureStatus;
/* Storage description only: linear A8/ARGB8 or power-of-two 2D BC1/P8, one mip.
 * P8 currently requires a 256-entry ARGB8 palette in the same selected DMA.
 * Filter/texgen/fragment-program equivalence is not implied by a READY plan.
 * Failed/disabled plans never modify output. DMA is the selected context. */
FutGraphicsTextureStatus fut_graphics_texture_plan(const FutGraphicsTextureState *state,
    const FutGraphicsDmaView *dma, FutGraphicsTexturePlan *plan);
#endif
