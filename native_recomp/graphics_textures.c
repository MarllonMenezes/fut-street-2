#include "graphics_textures.h"
#include <stdlib.h>
#include <string.h>

FutGraphicsTextureStatus fut_graphics_texture_plan(const FutGraphicsTextureState *state,
    const FutGraphicsDmaView *dma, FutGraphicsTexturePlan *plan)
{
    if (!state || !plan) return FUT_TEXTURE_INVALID_RANGE;
    if (!(state->valid & 8)) return FUT_TEXTURE_INCOMPLETE;
    if (!(state->control0 & 0x40000000u)) return FUT_TEXTURE_DISABLED;
    if ((state->valid & 0xBu) != 0xBu) return FUT_TEXTURE_INCOMPLETE;
    unsigned select = state->format & 3;
    if((state->format&~0x0FFF0003u)==0x00000628u) {
        unsigned u=(state->format>>20)&15u,v=(state->format>>24)&15u;
        unsigned levels=(state->format>>16)&15u,largest=u>v?u:v;
        if((select!=1 && select!=2) || u>12 || v>12 || !levels || levels>largest+1)
            return FUT_TEXTURE_UNSUPPORTED;
        FutGraphicsTexturePlan next={0};
        next.width=1u<<u;next.height=1u<<v;next.pitch=next.width*4u;
        next.storage=5;next.bytes_per_pixel=4;next.dma_context=select;next.mip_levels=levels;
        uint64_t span=0;
        for(unsigned level=0;level<levels;++level) {
            unsigned w=next.width>>level,h=next.height>>level;
            span+=(uint64_t)(w?w:1u)*(h?h:1u)*4u;
        }
        if(!dma || ((state->offset|dma->guest_address)&3u) ||
            (dma->guest_address&0xFC000000u)!=0x80000000u || !dma->size ||
            dma->size>0x04000000u-(dma->guest_address&0x03FFFFFFu) ||
            state->offset>dma->size || span>dma->size-state->offset) return FUT_TEXTURE_INVALID_RANGE;
        next.address=dma->guest_address+state->offset;next.span=(uint32_t)span;
        *plan=next;return FUT_TEXTURE_READY;
    }
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
    if ((state->format & ~0x0FFF0003u) == 0x00000C28u ||
        (state->format & ~0x0FFF0003u) == 0x00000F28u ||
        ((state->format & ~0x0FFF0003u) == 0x00000E28u && ((state->format>>16)&15u)>1)) {
        unsigned color=(state->format>>8)&255u;
        unsigned block_bytes=color==0x0Cu?8u:16u;
        unsigned u=(state->format>>20)&15u,v=(state->format>>24)&15u;
        unsigned levels=(state->format>>16)&15u,largest=u>v?u:v;
        if ((select!=1 && select!=2) || u<2 || u>12 || v<2 || v>12 || !levels || levels>largest+1)
            return FUT_TEXTURE_UNSUPPORTED;
        FutGraphicsTexturePlan next={0};
        next.width=1u<<u;next.height=1u<<v;next.pitch=(next.width/4)*block_bytes;
        next.storage=color==0x0Cu?1:color==0x0Eu?3:6;next.dma_context=select;next.mip_levels=levels;
        for(unsigned level=0;level<levels;++level) {
            unsigned w=next.width>>level,h=next.height>>level;
            next.span+=((w?w:1u)+3u)/4u*(((h?h:1u)+3u)/4u)*block_bytes;
        }
        if(!dma || ((state->offset|dma->guest_address)&(block_bytes-1u)) || (dma->guest_address&0xFC000000u)!=0x80000000u ||
            !dma->size || dma->size>0x04000000u-(dma->guest_address&0x03FFFFFFu) ||
            state->offset>dma->size || next.span>dma->size-state->offset)
            return FUT_TEXTURE_INVALID_RANGE;
        next.address=dma->guest_address+state->offset;*plan=next;return FUT_TEXTURE_READY;
    }
    if ((state->format & ~0x0FF00003u) == 0x00010E28u) {
        unsigned u=(state->format>>20)&15u,v=(state->format>>24)&15u;
        if ((select!=1 && select!=2) || u<2 || u>12 || v<2 || v>12)
            return FUT_TEXTURE_UNSUPPORTED;
        FutGraphicsTexturePlan next={0};
        /* DXT23 uses FORMAT's base U/V sizes, like DXT1. IMAGE_RECT and
         * CONTROL1 pitch belong to LU/LC images and may retain older writes.
         * xemu pgraph/texture.c, pinned 36334d95, records this distinction. */
        next.width=1u<<u;next.height=1u<<v;
        next.pitch=(next.width/4u)*16u;next.span=next.pitch*(next.height/4u);
        next.storage=3;next.dma_context=select;
        if(!dma || (state->offset&15u) ||
            (dma->guest_address&0xFC00000Fu)!=0x80000000u || !dma->size ||
            dma->size>0x04000000u-(dma->guest_address&0x03FFFFFFu) ||
            state->offset>dma->size || next.span>dma->size-state->offset)
            return FUT_TEXTURE_INVALID_RANGE;
        next.address=dma->guest_address+state->offset;
        *plan=next;return FUT_TEXTURE_READY;
    }
    if ((state->valid & 0x9Bu) != 0x9Bu) return FUT_TEXTURE_INCOMPLETE;
    /* No guessing swizzled/cube/mip/bordered storage. Linear formats admitted
     * here are A8R8G8B8 (11228) and alpha-only A8 (11F28); DXT23 is handled above. */
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

static int observed_yuy2_state_in_range(const FutGraphicsTextureState *s,
    const FutGraphicsDmaView *dma)
{
    if(!s || !dma || s->format!=0x00012429u || (s->format&3u)!=1u ||
        s->image_rect!=0x028001E0u || s->control0!=0x4003FFC0u ||
        s->control1!=0x05000000u || s->valid!=0x3BFu ||
        (s->offset&3u) || dma->guest_address!=0x80000000u ||
        dma->size!=0x04000000u || s->offset>dma->size ||
        FUT_GRAPHICS_OBSERVED_YUY2_BYTES>dma->size-s->offset ||
        s->offset>UINT32_MAX-dma->guest_address) return 0;
    return 1;
}

int fut_graphics_texture_capture_observed_yuy2(FutGraphicsMemoryRead read,
    void *ctx,const FutGraphicsTextureState *s,const FutGraphicsDmaView *dma,
    uint8_t output[FUT_GRAPHICS_OBSERVED_YUY2_BYTES])
{
    const uint32_t offset=s?s->offset:0;
    uint8_t *captured;
    if(!read || !output || !observed_yuy2_state_in_range(s,dma)) return 0;
    captured=malloc(FUT_GRAPHICS_OBSERVED_YUY2_BYTES);
    if(!captured) return 0;
    if(!read(ctx,dma->guest_address+offset,captured,FUT_GRAPHICS_OBSERVED_YUY2_BYTES)) {
        free(captured);return 0;
    }
    memcpy(output,captured,FUT_GRAPHICS_OBSERVED_YUY2_BYTES);
    free(captured);return 1;
}

int fut_graphics_texture_plan_observed_yuy2_approximation(
    const FutGraphicsTextureState *s,const FutGraphicsDmaView *dma,
    FutGraphicsTexturePlan *plan)
{
    if(!plan || !observed_yuy2_state_in_range(s,dma))
        return 0;
    FutGraphicsTexturePlan next={0};
    next.address=dma->guest_address+s->offset;next.span=FUT_GRAPHICS_OBSERVED_YUY2_BYTES;
    next.pitch=1280;next.width=640;next.height=480;next.bytes_per_pixel=2;
    next.dma_context=1;next.storage=4;
    *plan=next;return 1;
}

static uint8_t yuy2_clip(int value)
{
    return (uint8_t)(value<0?0:value>255?255:value);
}

int fut_graphics_texture_yuy2_bt601_to_bgra(const uint8_t *source,size_t source_size,
    uint8_t *output,size_t output_size)
{
    const size_t input_bytes=FUT_GRAPHICS_OBSERVED_YUY2_BYTES;
    const size_t decoded_bytes=640u*480u*4u;
    if(!source || !output || source_size!=input_bytes || output_size!=decoded_bytes)
        return 0;
    uint8_t *decoded=malloc(decoded_bytes);
    if(!decoded) return 0;
    for(size_t i=0,j=0;i<input_bytes;i+=4,j+=8) {
        int y0=(int)source[i]-16,d=(int)source[i+1]-128;
        int y1=(int)source[i+2]-16,e=(int)source[i+3]-128;
        int c0=y0<0?0:y0,c1=y1<0?0:y1;
        uint8_t *p0=decoded+j,*p1=decoded+j+4;
        p0[0]=yuy2_clip((298*c0+516*d+128)>>8);
        p0[1]=yuy2_clip((298*c0-100*d-208*e+128)>>8);
        p0[2]=yuy2_clip((298*c0+409*e+128)>>8);p0[3]=255;
        p1[0]=yuy2_clip((298*c1+516*d+128)>>8);
        p1[1]=yuy2_clip((298*c1-100*d-208*e+128)>>8);
        p1[2]=yuy2_clip((298*c1+409*e+128)>>8);p1[3]=255;
    }
    memcpy(output,decoded,decoded_bytes);free(decoded);return 1;
}
