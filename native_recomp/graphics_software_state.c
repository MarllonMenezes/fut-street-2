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
/* Compressed projective storage uses FORMAT dimensions and a tightly packed
 * mip chain. CONTROL1/IMAGE_RECT describe linear storage and may be unwritten
 * on a secondary compressed stage. Validate the fields actually consumed. */
static int projective_compressed_stage(const FutGraphicsTextureState *t)
{
    unsigned header=t->format&~0x0FFF0000u;
    int bc1=header==0x00000C29u,bc2=header==0x00000E29u;
    unsigned u=(t->format>>20)&15u,v=(t->format>>24)&15u;
    unsigned levels=(t->format>>16)&15u,largest=u>v?u:v;
    if((!bc1 && !bc2) || !fut_graphics_projective_aa2_approximation_enabled() ||
        (bc1 && !fut_graphics_dxt1_noise_approximation_enabled()) ||
        (bc2 && !fut_graphics_bc2_aa2_family_approximation_enabled()) ||
        u<2 || u>12 || v<2 || v>12 || !levels || levels>largest+1 ||
        (t->valid&0x2Fu)!=0x2Fu || t->control0!=0x4003FFC0u ||
        (t->filter!=0x02063F1Bu && t->filter!=0x02062000u) ||
        (t->address!=0x10101u && !(bc1 && t->address==0x10303u)) ||
        (t->offset&(bc1?7u:15u))) return 0;
    uint64_t span=0;
    for(unsigned level=0;level<levels;++level) {
        unsigned w=(1u<<u)>>level,h=(1u<<v)>>level;
        span+=(uint64_t)(((w?w:1u)+3u)/4u)*(((h?h:1u)+3u)/4u)*(bc1?8u:16u);
    }
    return span<=0x04000000u && t->offset<=0x04000000u-span;
}

/* Caller has already validated stage0's established projective family. An
 * enabled secondary resource may remain bound after its shader mode becomes
 * zero; the original mode determines whether the fragment program samples it. */
static int projective_secondary_stages(const FutGraphicsKelvinState *k)
{
    unsigned secondary=0;
    if(!(k->fragment.valid&2u) || k->fragment.stage_program&~0xFFFFFu ||
        k->draw.dither!=1 || k->depth_test!=1 || k->stencil_test ||
        k->draw.depth_write!=1 || k->compress_depth!=1 || k->control0!=0x00100001u)
        return 0;
    if((k->fragment.stage_program&31u)!=1u || !(k->textures[0].control0&0x40000000u)) return 0;
    for(unsigned stage=1;stage<4;++stage) {
        unsigned mode=(k->fragment.stage_program>>(stage*5u))&31u;
        if(k->textures[stage].control0&0x40000000u) {
            if((mode!=0u && mode!=1u) || !projective_compressed_stage(&k->textures[stage])) return 0;
            secondary|=1u<<stage;
        } else if(mode) return 0;
    }
    return secondary!=0;
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
    const FutGraphicsTextureState *t0=&k->textures[0];
    unsigned texture_u=(t0->format>>20)&15u,texture_v=(t0->format>>24)&15u;
    int family_dimensions=texture_u>=2 && texture_u<=12 && texture_v>=2 && texture_v<=12;
    int family_common_baseline=t0->valid==0x3BFu && t0->control0==0x4003FFC0u &&
        t0->filter==0x02062000u && k->draw.dither==1;
    int family_baseline=family_common_baseline && t0->control1==0x14000000u &&
        t0->image_rect==0x050001E0u;
    /* Swizzled/compressed texture dimensions come from FORMAT; LC_IMAGE is
     * retained separately. Both observed register pairs keep all other gates. */
    int family_lc_retained_baseline=family_common_baseline &&
        t0->control1==0x05000000u && t0->image_rect==0x028001E0u;
    unsigned u=(t0->format>>20)&15u,v=(t0->format>>24)&15u;
    unsigned levels=(t0->format>>16)&15u,largest=u>v?u:v;
    int bc1_header=(t0->format&~0x0FFF0000u)==0x00000C29u;
    int projective_bc1=bc1_header && u>=2 && u<=12 && v>=2 && v<=12 &&
        levels && levels<=largest+1 && fut_graphics_dxt1_noise_approximation_enabled();
    int argb_header=(t0->format&~0x0FFF0000u)==0x00000629u;
    int projective_argb=argb_header && u<=12 && v<=12 && levels && levels<=largest+1;
    int bc2_projective_header=(t0->format&~0x0FFF0000u)==0x00000E29u && (levels>1 || t0->filter==0x02063F1Bu);
    int projective_bc2=bc2_projective_header && u>=2 && u<=12 && v>=2 && v<=12 &&
        levels && levels<=largest+1 && fut_graphics_bc2_aa2_family_approximation_enabled();
    int p8_projective_header=(t0->format&~0x0FFF0000u)==0x00000B29u &&
        (levels>1 || t0->filter==0x02063F1Bu ||
         (levels==1 && t0->filter==0x02062000u &&
          (t0->address==0x10101u || t0->address==0x10303u)));
    int projective_p8=p8_projective_header && u>=2 && u<=12 && v>=2 && v<=12 && levels && levels<=largest+1 &&
        fut_graphics_p8_aa2_family_approximation_enabled() && !(t0->palette&0x3Fu) &&
        t0->palette<=0x04000000u-1024u;
    int projective_bc3=t0->format==0x09A30F29u;
    unsigned projective_span=projective_bc3?688128u:86016u;
    if(projective_bc1 || projective_bc2 || projective_argb || projective_p8) {
        projective_span=0;
        for(unsigned level=0;level<levels;++level) {
            unsigned w=(1u<<u)>>level,h=(1u<<v)>>level;
            projective_span+=(projective_argb || projective_p8)?(w?w:1u)*(h?h:1u)*(projective_p8?1u:4u):
                ((w?w:1u)+3u)/4u*(((h?h:1u)+3u)/4u)*(projective_bc1?8u:16u);
        }
    }
    int argb_unbiased_single=projective_argb && levels==1 && t0->filter==0x02062000u;
    int observed_projective_texture=fut_graphics_projective_aa2_approximation_enabled() &&
        (projective_argb || projective_bc1 || projective_bc2 || projective_bc3 || projective_p8) && t0->valid==0x3BFu && t0->control0==0x4003FFC0u &&
        t0->control1==0x14000000u && t0->image_rect==0x050001E0u &&
        (t0->filter==0x02063F1Bu || ((projective_bc1 || projective_bc2 || projective_bc3 ||
            ((projective_p8 || projective_argb) && levels==1)) && t0->filter==0x02062000u)) && (t0->address==0x10101u ||
            ((projective_bc1 || ((projective_argb || projective_p8) && levels==1 && t0->filter==0x02062000u)) && t0->address==0x10303u) ||
            (argb_unbiased_single && t0->address==0x30303u)) &&
        !(t0->offset&((projective_bc3 || projective_bc2)?15u:projective_bc1?7u:3u)) && projective_span<=0x04000000u && t0->offset<=0x04000000u-projective_span && k->draw.dither==1;
    int bc2_format=(t0->format&~0x0FF00000u)==0x00010E29u;
    int untextured=fut_graphics_untextured_aa2_approximation_enabled() &&
        !fut_graphics_software_draw_ready(s) &&
        surface->logical_width==640 && surface->logical_height==480 &&
        t0->control0==0 && (k->fragment.valid&2u) &&
        k->fragment.stage_program==0 && k->draw.dither==1 &&
        k->depth_test==1 && k->draw.depth_write==1 && k->compress_depth==1 &&
        k->stencil_test<=1 && k->control0==0x00100001u;
    for(unsigned stage=1;stage<4;++stage)
        if(k->textures[stage].control0) untextured=0;
    /* No shader texture stage and all controls disabled: retained resource
     * descriptors are not sampled. Stencil remains enabled when requested;
     * the normal pipeline validates and applies its function/masks/ops.
     * Actual shader/draw validation is separate. */
    if(fut_graphics_untextured_aa2_approximation_enabled() &&
        !fut_graphics_software_draw_ready(s) && t0->valid &&
        t0->control0==0 && !untextured) return 0;
    if(!untextured && (argb_header || p8_projective_header || bc2_projective_header || (bc1_header && t0->filter==0x02063F1Bu) || projective_bc3) &&
        (!observed_projective_texture || fut_graphics_software_draw_ready(s)))return 0;
    int bc2_family=bc2_format && fut_graphics_bc2_aa2_family_approximation_enabled() &&
        family_dimensions &&
        !(t0->offset&15u) && t0->offset<=0x04000000u &&
        (1u<<texture_u)*(1u<<texture_v)<=0x04000000u-t0->offset &&
        (family_baseline || family_lc_retained_baseline) &&
        (t0->address==0x10303u || t0->address==0x10101u || t0->address==0x30303u);
    int p8_format=(t0->format&~0x0FF00000u)==0x00010B29u;
    /* The first two captured P8 layouts retain their independent opt-ins. */
    int p8_family=p8_format && t0->format!=0x05510B29u && t0->format!=0x05710B29u &&
        fut_graphics_p8_aa2_family_approximation_enabled() &&
        family_dimensions && (family_baseline || family_lc_retained_baseline) && k->draw.depth_write<=1 &&
        t0->address==0x30303u &&
        !(t0->offset&3u) && t0->offset<=0x04000000u &&
        (1u<<texture_u)*(1u<<texture_v)<=0x04000000u-t0->offset &&
        !(t0->palette&0x3Fu) && t0->palette<=0x04000000u-1024u;
    if(p8_format && !untextured && t0->format!=0x05510B29u && t0->format!=0x05710B29u &&
        (!(p8_family || (projective_p8 && observed_projective_texture)) || fut_graphics_software_draw_ready(s))) return 0;
    if(bc2_format && !untextured && (!(t0->control0&0x40000000u) ||
        fut_graphics_software_draw_ready(s))) return 0;
    int bc2_c9df00=t0->format==0x09A10E29u &&
        fut_graphics_bc2_c9df00_aa2_approximation_enabled() &&
        t0->offset==0x00C9DF00u && t0->valid==0x3BFu &&
        t0->control0==0x4003FFC0u && t0->control1==0x14000000u &&
        t0->image_rect==0x050001E0u && t0->address==0x10303u &&
        t0->filter==0x02062000u && k->draw.dither==1;
    /* The new scope requires the captured baseline and enabled texture too;
     * empty software controls or a disabled texture cannot bypass its gate. */
    if(t0->format==0x09A10E29u && !untextured &&
        (!(bc2_c9df00 || bc2_family || (projective_bc2 && observed_projective_texture)) || fut_graphics_software_draw_ready(s))) return 0;
    if(bc2_format && !untextured && t0->format!=0x06610E29u && t0->format!=0x09A10E29u &&
        (!(bc2_family || (projective_bc2 && observed_projective_texture)) || fut_graphics_software_draw_ready(s))) return 0;
    if(!fut_graphics_software_draw_ready(s)) {
        /* User-approved experiment, ONLY the observed ARGB8 AA2 baseline.
         * Ordinary texture/shader/Z/S validation remains mandatory. */
        if(!fut_graphics_aa2_approximation_enabled() || surface->logical_width!=640 ||
            surface->logical_height!=480 || s->register_valid!=3 ||
            s->register_words[0]!=4 || s->register_words[1]!=0x45EAD10E ||
            s->noise_valid!=1 || s->dxt1_noise!=1 ||
            s->rdi_index[0]!=0xE00050 || s->rdi_index[1]!=0xDF0008 ||
            s->rdi_value[0]!=1 || s->rdi_value[1]!=1) return 0;
        unsigned secondary_projective=k->fragment.stage_program&~31u;
        for(unsigned stage=1;stage<4;++stage)
            secondary_projective|=k->textures[stage].control0&0x40000000u;
        if(observed_projective_texture && secondary_projective)
            return projective_secondary_stages(k);
        for(unsigned stage=0;stage<4;++stage) if(k->textures[stage].control0&0x40000000u) {
            uint32_t format=k->textures[stage].format;
            if(format!=0x11229 && format!=0x1122A && format!=0x11F29 && format!=0x11F2A) {
                const FutGraphicsTextureState *t=&k->textures[stage];
                /* Separately authorized diagnostic scopes: BC1 without the
                 * unmeasured NV2A noise, or losslessly expanded P8 under the
                 * existing AA2 baseline. Only the captured states qualify. */
                int bc1=format==0x09910C29u && fut_graphics_dxt1_noise_approximation_enabled();
                int p8_32=format==0x05510B29u && fut_graphics_p8_aa2_approximation_enabled() &&
                    (t->valid&0x100u) && !(t->palette&0x3Fu);
                int p8_128x32=(format==0x05710B29u &&
                    fut_graphics_p8_128x32_aa2_approximation_enabled() &&
                    (t->valid&0x100u) && !(t->palette&0x3Fu)) || (stage==0 && p8_family);
                int bc2=(format==0x06610E29u && fut_graphics_bc2_aa2_approximation_enabled() &&
                    t->offset==0x00D1F980u && t->image_rect==0x050001E0u &&
                    t->control1==0x14000000u) || (stage==0 && (bc2_c9df00 || bc2_family));
                int projective_argb=stage==0 && observed_projective_texture;
                int yuy2_first=format==0x00012429u &&
                    fut_graphics_yuy2_aa2_approximation_enabled() &&
                    fut_graphics_yuy2_bt601_approximation_enabled() &&
                    t->offset==0x023F1580u;
                int yuy2_second=format==0x00012429u &&
                    fut_graphics_yuy2_second_aa2_approximation_enabled() &&
                    fut_graphics_yuy2_second_bt601_approximation_enabled() &&
                    t->offset==0x02487680u;
                int yuy2_family=format==0x00012429u &&
                    fut_graphics_yuy2_family_aa2_approximation_enabled() &&
                    fut_graphics_yuy2_family_bt601_approximation_enabled();
                int yuy2=yuy2_first || yuy2_second || yuy2_family;
                yuy2=yuy2 && t->image_rect==0x028001E0u &&
                    t->control1==0x05000000u && t->valid==0x3BFu;
                int depth_write_matches=(p8_family || bc2_family || argb_unbiased_single) ? k->draw.depth_write<=1 :
                    (p8_128x32 || bc2 || projective_argb) ? k->draw.depth_write==1 : !k->draw.depth_write;
                unsigned valid_mask=bc2?0x9Bu:0x2Fu;
                int texture_state_matches=projective_argb ? 1 : yuy2 ? t->control0==0x4003FFC0u &&
                    t->address==0x00030303u && t->filter==0x02062000u : bc2 ?
                    (t->valid&valid_mask)==valid_mask && t->control0==0x4003FFC0u &&
                    (t->address==0x10303u || (bc2_family &&
                        (t->address==0x10101u || t->address==0x30303u))) &&
                    t->filter==0x02062000u :
                    (t->valid&valid_mask)==valid_mask && t->control0==0x4003FFC0u &&
                    t->address==0x30303u && t->filter==0x02062000u;
                if((!bc1 && !p8_32 && !p8_128x32 && !bc2 && !yuy2 && !projective_argb) || stage!=0 || !texture_state_matches ||
                    k->depth_test!=1 || k->stencil_test ||
                    !depth_write_matches || k->compress_depth!=1 ||
                    k->control0!=(yuy2?0x10100001u:0x00100001u))
                    return 0;
                for(unsigned other=1;other<4;++other)
                    if(k->textures[other].control0&0x40000000u) return 0;
            }
        }
    }
    /* Texture translation and ordinary Z/S pipeline validation remain separate
     * mandatory checks. Dithering is still refused by the AA2 pipeline. */
    return 1;
}
