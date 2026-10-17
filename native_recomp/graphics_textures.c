#include "graphics_textures.h"

FutGraphicsTextureStatus fut_graphics_texture_plan(const FutGraphicsTextureState *state,
    const FutGraphicsDmaView *dma, FutGraphicsTexturePlan *plan)
{
    if (!state || !plan) return FUT_TEXTURE_INVALID_RANGE;
    if (!(state->valid & 8)) return FUT_TEXTURE_INCOMPLETE;
    if (!(state->control0 & 0x40000000u)) return FUT_TEXTURE_DISABLED;
    if ((state->valid & 0xBu) != 0xBu) return FUT_TEXTURE_INCOMPLETE;
    unsigned select = state->format & 3;
    if ((state->format & ~0x0FF00003u) == 0x00010B28u) {
        unsigned u=(state->format>>20)&15u,v=(state->format>>24)&15u;
        if (!(state->valid&0x100u)) return FUT_TEXTURE_INCOMPLETE;
        if ((select!=1 && select!=2) || u<2 || u>12 || v<2 || v>12 ||
            (state->palette&0x3Eu) || (state->palette&1u)+1u!=select)
            return FUT_TEXTURE_UNSUPPORTED;
        FutGraphicsTexturePlan next={0};
        next.width=1u<<u;next.height=1u<<v;next.pitch=next.width;
        next.span=next.width*next.height;next.storage=2;next.bytes_per_pixel=1;
        next.dma_context=select;next.palette_span=1024;
        uint32_t palette_offset=state->palette&0xFFFFFFC0u;
        if(!dma || (state->offset&3u) || (dma->guest_address&0xFC000003u)!=0x80000000u ||
            !dma->size || dma->size>0x04000000u-(dma->guest_address&0x03FFFFFFu) ||
            state->offset>dma->size || next.span>dma->size-state->offset ||
            palette_offset>dma->size || next.palette_span>dma->size-palette_offset)
            return FUT_TEXTURE_INVALID_RANGE;
        next.address=dma->guest_address+state->offset;
        next.palette_address=dma->guest_address+palette_offset;
        *plan=next;return FUT_TEXTURE_READY;
    }
    if ((state->format & ~0x0FF00003u) == 0x00010C28u) {
        unsigned u=(state->format>>20)&15u,v=(state->format>>24)&15u;
        if ((select!=1 && select!=2) || u<2 || u>12 || v<2 || v>12)
            return FUT_TEXTURE_UNSUPPORTED;
        FutGraphicsTexturePlan next={0};
        next.width=1u<<u;next.height=1u<<v;next.pitch=(next.width/4)*8;
        next.span=next.pitch*(next.height/4);next.storage=1;next.dma_context=select;
        if(!dma || (state->offset&7u) || (dma->guest_address&0xFC000000u)!=0x80000000u ||
            !dma->size || dma->size>0x04000000u-(dma->guest_address&0x03FFFFFFu) ||
            state->offset>dma->size || next.span>dma->size-state->offset)
            return FUT_TEXTURE_INVALID_RANGE;
        next.address=dma->guest_address+state->offset;*plan=next;return FUT_TEXTURE_READY;
    }
    if ((state->valid & 0x9Bu) != 0x9Bu) return FUT_TEXTURE_INCOMPLETE;
    /* No guessing swizzled/cube/mip/bordered storage. The two
     * observed linear layouts are A8R8G8B8 (11228) and alpha-only A8 (11F28). */
    uint32_t format=state->format&~3u;
    unsigned bytes_per_pixel=format==0x00011228u?4:
        format==0x00011F28u?1:0;
    if ((select != 1 && select != 2) || !bytes_per_pixel ||
        (state->control1 & 0xFFFFu)) return FUT_TEXTURE_UNSUPPORTED;
    FutGraphicsTexturePlan next = {0};
    next.width = state->image_rect >> 16; next.height = state->image_rect & 0xFFFF;
    next.pitch = state->control1 >> 16; next.dma_context = select;
    next.bytes_per_pixel=bytes_per_pixel;
    uint64_t row = (uint64_t)next.width * bytes_per_pixel;
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
