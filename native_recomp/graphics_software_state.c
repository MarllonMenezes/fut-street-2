#include "graphics_software_state.h"
int fut_graphics_software_state(FutGraphicsSoftwareState *s, uint32_t notification,
    uint32_t clear_depth, uint32_t clear_color, unsigned clear_valid)
{
    if (!s) return 0;
    unsigned selector = notification & 31;
    if (selector == 8) {
        uint32_t data = notification >> 5;
        s->dxt1_noise = data; s->noise_valid = 1;
        s->rdi_index[0] = 0x00E00050; s->rdi_index[1] = 0x00DF0008;
        s->rdi_value[0] = s->rdi_value[1] = data;
        return 1;
    }
    if (selector == 9 && (clear_valid & 3) == 3) {
        unsigned slot;
        if (clear_depth == 0x00400094) slot = 0;
        else if (clear_depth == 0x00400B80) slot = 1;
        else return 0;
        s->register_words[slot] = clear_color;
        s->register_valid |= 1u << slot;
        return 1;
    }
    return 0;
}
int fut_graphics_software_draw_ready(const FutGraphicsSoftwareState *s)
{
    /* Do not guess that zero or a debug/cache write has no raster effect. */
    return s && !s->noise_valid && !s->register_valid;
}
int fut_graphics_software_linear_draw_ready(const FutGraphicsSoftwareState *s,
    const FutGraphicsKelvinState *k,const FutGraphicsSurfacePlan *surface)
{
    if(!s || !k || !surface) return 0;
    if(fut_graphics_software_draw_ready(s)) return 1;
    if(s->noise_valid>1 || (s->register_valid&~3u)) return 0;
    if(s->noise_valid && (s->dxt1_noise>1 || s->rdi_index[0]!=0xE00050 ||
        s->rdi_index[1]!=0xDF0008 || s->rdi_value[0]!=s->dxt1_noise || s->rdi_value[1]!=s->dxt1_noise)) return 0;
    if((s->register_valid&1) && s->register_words[0]!=4) return 0;
    if((s->register_valid&2) && s->register_words[1]!=0x45EAD10E && s->register_words[1]!=0x45EAD10F) return 0;
    if(k->depth_test || k->stencil_test || surface->samples!=1 || !surface->logical_width ||
        !surface->logical_height || surface->width!=surface->logical_width || surface->height!=surface->logical_height)
        return 0;
    for(unsigned stage=0;stage<4;++stage) {
        const FutGraphicsTextureState *texture=&k->textures[stage];
        if((texture->control0&0x40000000u) &&
            (texture->format!=0x11229 && texture->format!=0x1122A)) return 0;
    }
    return 1;
}
