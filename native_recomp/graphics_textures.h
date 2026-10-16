#ifndef FUT_GRAPHICS_TEXTURES_H
#define FUT_GRAPHICS_TEXTURES_H
#include "graphics_kelvin_state.h"
#include "graphics_objects.h"

typedef struct {
    uint32_t address, span, pitch, width, height;
    unsigned bytes_per_pixel; /* Exact guest storage size: A8 or A8R8G8B8. */
    unsigned dma_context; /* Texture context A=1 (method184), B=2 (method188). */
} FutGraphicsTexturePlan;
typedef enum {
    FUT_TEXTURE_READY, FUT_TEXTURE_DISABLED, FUT_TEXTURE_INCOMPLETE,
    FUT_TEXTURE_UNSUPPORTED, FUT_TEXTURE_INVALID_RANGE
} FutGraphicsTextureStatus;
/* Storage description only: observed linear 2D A8R8G8B8, one mip level.
 * Filter/texgen/fragment-program equivalence is not implied by a READY plan.
 * Failed/disabled plans never modify output. DMA is the selected context. */
FutGraphicsTextureStatus fut_graphics_texture_plan(const FutGraphicsTextureState *state,
    const FutGraphicsDmaView *dma, FutGraphicsTexturePlan *plan);
#endif
