#include "graphics_textures.h"

FutGraphicsTextureStatus fut_graphics_texture_plan(const FutGraphicsTextureState *state,
    const FutGraphicsDmaView *dma, FutGraphicsTexturePlan *plan)
{
    if (!state || !plan) return FUT_TEXTURE_INVALID_RANGE;
    if (!(state->valid & 8)) return FUT_TEXTURE_INCOMPLETE;
    if (!(state->control0 & 0x40000000u)) return FUT_TEXTURE_DISABLED;
    if ((state->valid & 0x9Bu) != 0x9Bu) return FUT_TEXTURE_INCOMPLETE;
    unsigned select = state->format & 3;
    /* No guessing swizzled/compressed/cube/mip/bordered storage. The exact
     * non-DMA fields here match the title's observed 0x00011229 description. */
    if ((select != 1 && select != 2) || (state->format & ~3u) != 0x00011228u ||
        (state->control1 & 0xFFFFu)) return FUT_TEXTURE_UNSUPPORTED;
    FutGraphicsTexturePlan next = {0};
    next.width = state->image_rect >> 16; next.height = state->image_rect & 0xFFFF;
    next.pitch = state->control1 >> 16; next.dma_context = select;
    uint64_t row = (uint64_t)next.width * 4;
    if (!dma || !next.width || !next.height || next.width > 8191 || next.height > 8191 ||
        next.pitch < row || ((state->offset | next.pitch | dma->guest_address) & 3) ||
        (dma->guest_address & 0xFC000000u) != 0x80000000u || !dma->size ||
        dma->size > 0x04000000u - (dma->guest_address & 0x03FFFFFFu)) return FUT_TEXTURE_INVALID_RANGE;
    uint64_t bytes = (uint64_t)(next.height - 1) * next.pitch + row;
    if (state->offset > dma->size || bytes > dma->size - state->offset) return FUT_TEXTURE_INVALID_RANGE;
    next.address = dma->guest_address + state->offset; next.span = (uint32_t)bytes;
    *plan = next;
    return FUT_TEXTURE_READY;
}
