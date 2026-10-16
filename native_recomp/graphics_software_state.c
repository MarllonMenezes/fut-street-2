#include "graphics_software_state.h"
#include "graphics_experiment.h"
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
int fut_graphics_software_rop_controls(const FutGraphicsSoftwareState *s,
    FutGraphicsRopControls *controls)
{
    if(!s || !controls || s->register_valid!=3) return 0;
    uint32_t a=s->register_words[0]&~8u;
    uint32_t b=s->register_words[1]&~0x08100000u;
    if(a!=4 || (b!=0x45EAD10E && b!=0x45EAD10F)) return 0;
    /* XDK5849 dispatch indices163/164/165 (normalized Cxbx164/165/166).
     * Original setters 28ECC0/28ECE0/28ED00 feed 28E5E0, respectively:
     * 400B80 bit20, 400B80 bit27, 400094 bit3. Not stencil-cull IDs:
     * this XDK removed MULTISAMPLETYPE, shifting later normalized indices.
     * This records the dataflow, not semantics of other baseline bits. */
    *controls=(FutGraphicsRopControls){(s->register_words[1]>>20)&1,
        (s->register_words[1]>>27)&1,(s->register_words[0]>>3)&1,{a,b}};
    return 1;
}
int fut_graphics_software_linear_draw_ready(const FutGraphicsSoftwareState *s,
    const FutGraphicsKelvinState *k,const FutGraphicsSurfacePlan *surface)
{
    if(!s || !k || !surface) return 0;
    if(s->noise_valid>1 || (s->register_valid&~3u)) return 0;
    if(s->noise_valid && (s->dxt1_noise>1 || s->rdi_index[0]!=0xE00050 ||
        s->rdi_index[1]!=0xDF0008 || s->rdi_value[0]!=s->dxt1_noise || s->rdi_value[1]!=s->dxt1_noise)) return 0;
    if((s->register_valid&1) && s->register_words[0]!=4) return 0;
    if((s->register_valid&2) && s->register_words[1]!=0x45EAD10E && s->register_words[1]!=0x45EAD10F) return 0;
    if(k->anti_aliasing_control!=0 && k->anti_aliasing_control!=0xFFFF0000) return 0;
    if(k->depth_test || k->stencil_test || surface->samples!=1 ||
        !surface->logical_width || !surface->logical_height ||
        surface->width!=surface->logical_width ||
        surface->height!=surface->logical_height)
        return 0;
    for(unsigned stage=0;stage<4;++stage) {
        const FutGraphicsTextureState *texture=&k->textures[stage];
        if((texture->control0&0x40000000u) &&
            (texture->format!=0x11229 && texture->format!=0x1122A &&
             texture->format!=0x11F29 && texture->format!=0x11F2A)) return 0;
    }
    return 1;
}
int fut_graphics_software_aa2_draw_ready(const FutGraphicsSoftwareState *s,
    const FutGraphicsKelvinState *k,const FutGraphicsSurfacePlan *surface)
{
    if(!s || !k || !surface ||
        k->anti_aliasing_control!=0xFFFF0001 || surface->samples!=2 ||
        !surface->logical_width || surface->logical_width>8192 ||
        !surface->logical_height || surface->logical_height>16384 ||
        surface->width!=2*surface->logical_width ||
        surface->height!=surface->logical_height) return 0;
    if(!fut_graphics_software_draw_ready(s)) {
        /* User-approved experiment, ONLY the observed ARGB8 AA2 baseline.
         * Ordinary texture/shader/Z/S validation remains mandatory. */
        if(!fut_graphics_aa2_approximation_enabled() || surface->logical_width!=640 ||
            surface->logical_height!=480 || s->register_valid!=3 ||
            s->register_words[0]!=4 || s->register_words[1]!=0x45EAD10E ||
            s->noise_valid!=1 || s->dxt1_noise!=1 ||
            s->rdi_index[0]!=0xE00050 || s->rdi_index[1]!=0xDF0008 ||
            s->rdi_value[0]!=1 || s->rdi_value[1]!=1) return 0;
        for(unsigned stage=0;stage<4;++stage) if(k->textures[stage].control0&0x40000000u) {
            uint32_t format=k->textures[stage].format;
            if(format!=0x11229 && format!=0x1122A && format!=0x11F29 && format!=0x11F2A) return 0;
        }
    }
    /* Texture translation and ordinary Z/S pipeline validation remain separate
     * mandatory checks. Dithering is still refused by the AA2 pipeline. */
    return 1;
}
