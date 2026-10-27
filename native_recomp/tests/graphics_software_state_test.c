#include "graphics_software_state.h"
#include <string.h>
#include <stdio.h>
#include <stdlib.h>
static int verify_projective_stages(const FutGraphicsSoftwareState *baseline)
{
    FutGraphicsSurfacePlan surface={.width=1280,.height=480,.logical_width=640,
        .logical_height=480,.samples=2};
    FutGraphicsKelvinState base={0};
    base.anti_aliasing_control=0xFFFF0001; base.depth_test=base.compress_depth=1;
    base.draw.depth_write=base.draw.dither=1; base.control0=0x00100001;
    base.fragment.valid=2; base.fragment.stage_program=0x21;
    base.textures[0]=(FutGraphicsTextureState){.valid=0x3BF,.format=0x07720E29,
        .offset=0x01678280,.address=0x10101,.control0=0x4003FFC0,
        .control1=0x14000000,.filter=0x02063F1B,.image_rect=0x050001E0};
    base.textures[1]=(FutGraphicsTextureState){.valid=0xFE2F,.format=0x07720C29,
        .offset=0x01549B80,.address=0x10101,.control0=0x4003FFC0,.filter=0x02063F1B};
    const char *opts[]={"RECOMP_AA2_APPROXIMATION","RECOMP_PROJECTIVE_AA2_APPROXIMATION",
        "RECOMP_BC2_AA2_FAMILY_APPROXIMATION","RECOMP_DXT1_NOISE_APPROXIMATION"};
    for(unsigned i=0;i<4;++i)_putenv_s(opts[i],"1");
    if(!fut_graphics_software_aa2_draw_ready(baseline,&base,&surface))return 160;
    for(unsigned profile=1;profile<4;++profile) {
        FutGraphicsKelvinState k=base;
        if(profile&1u)k.textures[0].filter=0x02062000;
        if(profile&2u)k.textures[1].filter=0x02062000;
        if(!fut_graphics_software_aa2_draw_ready(baseline,&k,&surface))return 176;
        for(unsigned opt=0;opt<4;++opt) {
            _putenv_s(opts[opt],"0");
            if(fut_graphics_software_aa2_draw_ready(baseline,&k,&surface))return 177;
            _putenv_s(opts[opt],"1");
        }
    }
    FutGraphicsKelvinState retained=base;
    retained.fragment.stage_program=1;
    retained.textures[0].format=0x07710B29;retained.textures[0].offset=0x013DC580;
    retained.textures[0].filter=0x02062000;retained.textures[0].palette=0x016B9900;
    _putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(baseline,&retained,&surface))return 170;
    retained.fragment.stage_program=0x21;
    if(!fut_graphics_software_aa2_draw_ready(baseline,&retained,&surface))return 171;
    retained.fragment.stage_program=1;
    retained.textures[1].filter^=1;
    if(fut_graphics_software_aa2_draw_ready(baseline,&retained,&surface))return 172;
    for(unsigned i=0;i<4;++i) {
        _putenv_s(opts[i],"0");
        if(fut_graphics_software_aa2_draw_ready(baseline,&base,&surface))return 161;
        _putenv_s(opts[i],"1");
    }
    /* Exact end of each independent mip span, then swapped compression family.
     * No address, size, or secondary-stage valid-word allowlist. */
    FutGraphicsKelvinState k=base;
    k.textures[0].offset=0x04000000u-20480u;k.textures[1].offset=0x04000000u-10240u;
    if(!fut_graphics_software_aa2_draw_ready(baseline,&k,&surface))return 162;
    k=base;k.textures[0].format=0x07720C29;k.textures[1].format=0x07720E29;
    if(!fut_graphics_software_aa2_draw_ready(baseline,&k,&surface))return 163;
    for(unsigned stage=2;stage<4;++stage) {
        k.textures[stage]=k.textures[1];k.fragment.stage_program|=1u<<(stage*5u);
        if(!fut_graphics_software_aa2_draw_ready(baseline,&k,&surface))return 164;
    }
    for(unsigned profile=0;profile<4;++profile)
    for(unsigned stage=0;stage<2;++stage) for(unsigned bad=0;bad<17;++bad) {
        k=base;
        if(profile&1u)k.textures[0].filter=0x02062000;
        if(profile&2u)k.textures[1].filter=0x02062000;
        FutGraphicsTextureState *t=&k.textures[stage];
        switch(bad) {
        case 0:t->valid&=~1u;break;case 1:t->valid&=~2u;break;
        case 2:t->valid&=~4u;break;case 3:t->valid&=~8u;break;
        case 4:t->valid&=~0x20u;break;case 5:t->control0^=0x40;break;
        case 6:t->filter^=1;break;case 7:t->address=0x30303;break;
        case 8:t->format^=3;break;case 9:t->format^=0x100;break;
        case 10:t->format=0x07120C29;break;case 11:t->format=0x07D20C29;break;
        case 12:t->format&=~0xF0000u;break;case 13:t->format=0x07790C29;break;
        case 14:++t->offset;break;
        case 15:t->offset=0x04000000u-(stage?10240u:20480u)+16u;break;
        case 16:k.fragment.stage_program^=3u<<(stage*5u);break;
        }
        if(fut_graphics_software_aa2_draw_ready(baseline,&k,&surface)) {
            fprintf(stderr,"projective stage negative admitted: %u/%u\n",stage,bad);return 165;
        }
    }
    for(unsigned bad=0;bad<13;++bad) {
        k=base;FutGraphicsSoftwareState s=*baseline;
        switch(bad) {
        case 0:k.draw.dither=0;break;case 1:k.depth_test=0;break;
        case 2:k.draw.depth_write=0;break;case 3:k.stencil_test=1;break;
        case 4:k.compress_depth=0;break;case 5:k.control0^=1;break;
        case 6:k.fragment.valid=0;break;case 7:k.fragment.stage_program|=1u<<20;break;
        case 8:k.textures[2].control0=0x40000000;break;
        case 9:k.fragment.stage_program|=1u<<10;break;
        case 10:k.textures[1].control0=0;break;
        case 11:s.register_words[1]^=1;break;case 12:s.rdi_value[0]=0;break;
        }
        if(fut_graphics_software_aa2_draw_ready(&s,&k,&surface)) {
            fprintf(stderr,"projective common negative admitted: %u\n",bad);return 166;
        }
    }
    /* Same min/mag/mip filter with zero LOD bias; the independently bounded
     * chains stay intact and every existing software baseline is required. */
    const uint32_t formats[]={0x07720C29u,0x07720E29u,0x09A30F29u};
    const unsigned spans[]={10240u,20480u,688128u};
    for(unsigned family=0;family<3;++family) {
        k=base;k.fragment.stage_program=1;k.textures[1].control0=0;
        k.textures[0].format=formats[family];k.textures[0].filter=0x02062000;
        k.textures[0].offset=0x04000000u-spans[family];
        if(!fut_graphics_software_aa2_draw_ready(baseline,&k,&surface))return 178;
        for(unsigned bad=0;bad<16;++bad) {
            FutGraphicsKelvinState variant=k;FutGraphicsSoftwareState s=*baseline;
            const char *opt=NULL;
            switch(bad) {
            case 0:variant.textures[0].filter^=1;break;
            case 1:variant.textures[0].offset+=16;break;
            case 2:variant.textures[0].offset++;break;
            case 3:variant.textures[0].format=(variant.textures[0].format&~0xFF00u)|0x7F00u;break;
            case 4:variant.textures[0].format&=~0xF0000u;break;
            case 5:variant.textures[0].valid&=~2u;break;
            case 6:variant.textures[0].control1=0;break;
            case 7:variant.textures[0].image_rect=0;break;
            case 8:variant.draw.depth_write=0;break;
            case 9:variant.stencil_test=1;break;
            case 10:variant.depth_test=0;break;
            case 11:variant.compress_depth=0;break;
            case 12:s.register_words[1]^=1;break;
            case 13:opt=opts[0];break;
            case 14:opt=opts[1];break;
            case 15:opt=family==0?opts[3]:family==1?opts[2]:opts[1];break;
            }
            if(opt)_putenv_s(opt,"0");
            if(fut_graphics_software_aa2_draw_ready(&s,&variant,&surface)) {
                fprintf(stderr,"unbiased compressed variant admitted: %u/%u\n",family,bad);return 179;
            }
            if(opt)_putenv_s(opt,"1");
        }
    }
    /* Captured single-mip 32x32 BC3, clamp and read-only Z. Preserve every
     * existing baseline and require the separate projective experiment. */
    k=base;k.fragment.stage_program=1;k.textures[1].control0=0;
    k.textures[0].format=0x05510F29;k.textures[0].offset=0x04000000u-1024u;
    k.textures[0].filter=0x02062000;k.textures[0].address=0x30303;
    k.draw.depth_write=0;
    if(!fut_graphics_software_aa2_draw_ready(baseline,&k,&surface))return 180;
    k.draw.depth_write=1;
    if(!fut_graphics_software_aa2_draw_ready(baseline,&k,&surface))return 181;
    k.draw.depth_write=0;
    for(unsigned bad=0;bad<16;++bad) {
        FutGraphicsKelvinState variant=k;FutGraphicsSoftwareState s=*baseline;
        const char *opt=NULL;
        switch(bad) {
        case 0:variant.textures[0].format=0x05610F29;break;
        case 1:variant.textures[0].offset+=16;break;
        case 2:variant.textures[0].offset++;break;
        case 3:variant.textures[0].address=0x10101;break;
        case 4:variant.textures[0].filter=0x02063F1B;break;
        case 5:variant.textures[0].valid&=~2u;break;
        case 6:variant.textures[0].control1=0;break;
        case 7:variant.textures[0].image_rect=0;break;
        case 8:variant.depth_test=0;break;
        case 9:variant.stencil_test=1;break;
        case 10:variant.compress_depth=0;break;
        case 11:s.register_words[1]^=1;break;
        case 12:opt=opts[0];break;
        case 13:opt=opts[1];break;
        case 14:variant.draw.dither=0;break;
        case 15:variant.textures[0].control0=0;break;
        }
        if(opt)_putenv_s(opt,"0");
        if(fut_graphics_software_aa2_draw_ready(&s,&variant,&surface)) {
            fprintf(stderr,"single BC3 negative admitted: %u\n",bad);return 182;
        }
        if(opt)_putenv_s(opt,"1");
    }
    /* Captured single-level BC2 plus linear ARGB8 stage1. */
    k=base;k.fragment.stage_program=0x21;k.textures[0].format=0x06610E29;
    k.textures[0].filter=0x02062000;k.textures[0].address=0x10101;
    k.textures[1]=(FutGraphicsTextureState){.valid=0xFEBF,.format=0x00011229,.offset=0x03FC0000,
        .address=0x30303,.control0=0x4003FFC0,.control1=0x04000000,.filter=0x02062000,.image_rect=0x01000100};
    if(!fut_graphics_software_aa2_draw_ready(baseline,&k,&surface))return 185;
    for(unsigned bad=0;bad<17;++bad) {
        FutGraphicsKelvinState v=k;FutGraphicsSoftwareState s=*baseline;const char *opt=NULL;
        switch(bad) {
        case 0:v.textures[0].format=0x06710E29;break;case 1:v.textures[0].address=0x10303;break;
        case 2:v.textures[1].valid&=~1u;break;case 3:v.textures[1].format^=0x100;break;
        case 4:v.textures[1].offset+=4;break;case 5:v.textures[1].offset--;break;
        case 6:v.textures[1].address^=1;break;case 7:v.textures[1].control0^=0x40;break;
        case 8:v.textures[1].control1^=0x10000;break;case 9:v.textures[1].filter^=1;break;
        case 10:v.textures[1].image_rect^=1;break;case 11:v.fragment.stage_program=1;break;
        case 12:v.depth_test=0;break;case 13:v.stencil_test=1;break;
        case 14:s.register_words[1]^=1;break;case 15:opt=opts[0];break;case 16:opt=opts[2];break;
        }
        if(opt)_putenv_s(opt,"0");
        if(fut_graphics_software_aa2_draw_ready(&s,&v,&surface)) {fprintf(stderr,"BC2 linear stage negative %u\n",bad);return 186;}
        if(opt)_putenv_s(opt,"1");
    }
    return 0;
}
int main(void)
{
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_DXT1_NOISE_APPROXIMATION","0");
    _putenv_s("RECOMP_P8_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_P8_128X32_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_BC2_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_BC2_C9DF00_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","0");
    _putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_023F1580_BT601_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_023F1580_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_02487680_BT601_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_02487680_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_FAMILY_BT601_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_FAMILY_AA2_APPROXIMATION","0");
    FutGraphicsSoftwareState state = {0};
    if (!fut_graphics_software_draw_ready(&state)) return 1;
    if (!fut_graphics_software_state(&state, 0x28, 0, 0, 0) || state.dxt1_noise != 1 ||
        state.rdi_index[0] != 0xE00050 || state.rdi_index[1] != 0xDF0008 ||
        state.rdi_value[0] != 1 || state.rdi_value[1] != 1 || fut_graphics_software_draw_ready(&state)) return 2;
    if (!fut_graphics_software_state(&state, 9, 0x400094, 4, 3) || state.register_words[0] != 4 || state.register_valid != 1) return 3;
    if (!fut_graphics_software_state(&state, 9, 0x400B80, 0x45EAD10E, 3) || state.register_words[1] != 0x45EAD10E || state.register_valid != 3) return 4;
    FutGraphicsSoftwareState baseline=state;
    for(unsigned low=0;low<2;++low) for(unsigned bits=0;bits<8;++bits) {
        /* Independently enumerate the original setter's boolean truth table.
         * Decoding must not grant draw readiness or mutate retained writes. */
        state=baseline;state.register_words[0]=4|((bits&4)?8:0);
        state.register_words[1]=(low?0x45EAD10F:0x45EAD10E)|
            ((bits&1)?0x100000:0)|((bits&2)?0x8000000:0);
        FutGraphicsSoftwareState retained=state;FutGraphicsRopControls rop;
        if(!fut_graphics_software_rop_controls(&state,&rop) ||
            rop.compare_always_read!=!!(bits&1) || rop.depth_read!=!!(bits&2) ||
            rop.dont_cull_uncompressed!=!!(bits&4) || rop.baseline[0]!=4 ||
            rop.baseline[1]!=(low?0x45EAD10F:0x45EAD10E) ||
            memcmp(&retained,&state,sizeof(state)) || fut_graphics_software_draw_ready(&state)) return 27;
    }
    state=baseline;
    FutGraphicsRopControls untouched;memset(&untouched,0xA5,sizeof(untouched));
    FutGraphicsRopControls decoded=untouched;
    state.register_valid=1;
    if(fut_graphics_software_rop_controls(&state,&decoded) || memcmp(&decoded,&untouched,sizeof(decoded))) return 28;
    state=baseline;state.register_words[1]^=0x100;
    if(fut_graphics_software_rop_controls(&state,&decoded) || memcmp(&decoded,&untouched,sizeof(decoded))) return 29;
    state=baseline;
    FutGraphicsSoftwareState before = state;
    if (fut_graphics_software_state(&state, 9, 0x400094, 0, 2) ||
        fut_graphics_software_state(&state, 9, 0x400100, 0, 3) ||
        fut_graphics_software_state(&state, 6, 0, 0, 0) || memcmp(&before, &state, sizeof(state))) return 5;
    if (!fut_graphics_software_state(&state, 8, 0, 0, 0) || fut_graphics_software_draw_ready(&state)) return 6;
    FutGraphicsKelvinState kelvin={0};
    FutGraphicsSurfacePlan surface={.width=640,.height=480,.logical_width=640,.logical_height=480,.samples=1};
    kelvin.draw.depth_write=1;kelvin.compress_depth=1;
    kelvin.textures[0].control0=0x4003FFC0;kelvin.textures[0].format=0x11229;
    FutGraphicsSoftwareState empty={0};
    if(!fut_graphics_software_linear_draw_ready(&empty,&kelvin,&surface)) return 20;
    kelvin.anti_aliasing_control=0xFFFF0000;
    if(!fut_graphics_software_linear_draw_ready(&empty,&kelvin,&surface)) return 24;
    kelvin.anti_aliasing_control=0xFFFF0001;
    if(fut_graphics_software_linear_draw_ready(&empty,&kelvin,&surface)) return 25;
    kelvin.anti_aliasing_control=1;
    if(fut_graphics_software_linear_draw_ready(&empty,&kelvin,&surface)) return 26;
    kelvin.anti_aliasing_control=0;
    surface.samples=2;surface.width=1280;
    if(fut_graphics_software_linear_draw_ready(&empty,&kelvin,&surface)) return 21;
    surface.samples=1;surface.width=640;kelvin.depth_test=1;
    if(fut_graphics_software_linear_draw_ready(&empty,&kelvin,&surface)) return 22;
    kelvin.depth_test=0;kelvin.stencil_test=1;
    if(fut_graphics_software_linear_draw_ready(&empty,&kelvin,&surface)) return 23;
    kelvin.stencil_test=0;
    before=state;
    if(!fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface) || memcmp(&state,&before,sizeof(state))) return 7;
    for(unsigned noise=0;noise<2;++noise) {
        if(!fut_graphics_software_state(&state,(noise<<5)|8,0,0,0) ||
            !fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 8;
    }
    kelvin.textures[0].format=0x11F29; /* Observed A8, texture context A. */
    if(!fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 18;
    kelvin.textures[0].format=0x11F2A; /* Same linear A8 layout, context B. */
    if(!fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 19;
    kelvin.depth_test=1;
    if(fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 9;
    kelvin.depth_test=0;kelvin.stencil_test=1;
    if(fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 10;
    kelvin.stencil_test=0;surface.samples=2;surface.width=1280;
    if(fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 11;
    surface.samples=1;surface.width=640;
    surface.samples=1;kelvin.textures[0].format=0x1060D; /* DXT1/BC1, not linearARGB8. */
    if(fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 12;
    kelvin.textures[0].format=0x11229;before=state;
    state.rdi_value[1]^=1;
    if(fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 13;
    state=before;state.register_words[0]|=8;
    if(fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 14;
    state=before;state.register_words[1]^=0x100000;
    if(fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 15;
    state=before;state.register_words[1]=0x45EAD10F;
    if(!fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 16;
    state=before;
    if(!fut_graphics_software_state(&state,0x48,0,0,0) ||
        fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 17;
    state=baseline;kelvin.anti_aliasing_control=0xFFFF0001;
    surface.samples=2;surface.width=1280;kelvin.textures[0].format=0x11F29;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 30;
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 31;
    state.register_words[1]^=1;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 32;
    state=baseline;kelvin.textures[0].format=0x1060D;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 33;
    kelvin.textures[0].format=0x11F29;surface.logical_width=320;surface.width=640;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 34;
    surface.logical_width=640;surface.width=1280;
    _putenv_s("RECOMP_AA2_APPROXIMATION","true");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 35;
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    state=baseline;kelvin=(FutGraphicsKelvinState){0};
    kelvin.anti_aliasing_control=0xFFFF0001;kelvin.depth_test=1;
    kelvin.compress_depth=1;kelvin.control0=0x100001;
    kelvin.textures[0]=(FutGraphicsTextureState){.format=0x09910C29,.control0=0x4003FFC0,
        .address=0x30303,.filter=0x02062000,.valid=0x2F};
    surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,.logical_height=480,.samples=2};
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 36;
    _putenv_s("RECOMP_DXT1_NOISE_APPROXIMATION","true");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 37;
    _putenv_s("RECOMP_DXT1_NOISE_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 38;
    FutGraphicsKelvinState scoped=kelvin;
    for(unsigned i=0;i<9;++i) {
        kelvin=scoped;
        switch(i) {
        case 0:kelvin.textures[0].format^=0x100000;break;
        case 1:kelvin.textures[0].filter^=0x10000;break;
        case 2:kelvin.textures[0].address^=1;break;
        case 3:kelvin.textures[0].control0^=0x40;break;
        case 4:kelvin.draw.depth_write=1;break;
        case 5:kelvin.stencil_test=1;break;
        case 6:kelvin.control0^=1;break;
        case 7:kelvin.textures[1].control0=0x40000000;break;
        case 8:kelvin.textures[0].valid=0;break;
        }
        if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 39;
    }
    kelvin=scoped;surface.logical_width=320;surface.width=640;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 40;
    surface.logical_width=640;surface.width=1280;
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 41;
    _putenv_s("RECOMP_DXT1_NOISE_APPROXIMATION","0");
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");kelvin=scoped;
    kelvin.textures[0].format=0x05510B29;kelvin.textures[0].valid|=0x100;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 42;
    _putenv_s("RECOMP_P8_AA2_APPROXIMATION","true");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 43;
    _putenv_s("RECOMP_P8_AA2_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 44;
    kelvin.textures[0].palette=4;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 45;
    kelvin.textures[0].palette=0;kelvin.textures[0].format^=0x100000;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 46;
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");_putenv_s("RECOMP_P8_AA2_APPROXIMATION","0");
    state=baseline;kelvin=(FutGraphicsKelvinState){0};
    kelvin.anti_aliasing_control=0xFFFF0001;kelvin.depth_test=1;
    kelvin.draw.depth_write=1;kelvin.compress_depth=1;kelvin.control0=0x00100001;
    kelvin.textures[0]=(FutGraphicsTextureState){.format=0x05710B29,.control0=0x4003FFC0,
        .address=0x30303,.filter=0x02062000,.palette=0x001D5AC0,.valid=0x12F};
    surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,
        .logical_height=480,.samples=2};
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 47;
    _putenv_s("RECOMP_P8_128X32_AA2_APPROXIMATION","true");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 48;
    _putenv_s("RECOMP_P8_128X32_AA2_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 49;
    FutGraphicsKelvinState p8_128x32=kelvin;
    for(unsigned i=0;i<9;++i) {
        kelvin=p8_128x32;
        switch(i) {
        case 0:kelvin.textures[0].format^=0x00200000;break;
        case 1:kelvin.draw.depth_write=0;break;
        case 2:kelvin.textures[0].filter^=0x10000;break;
        case 3:kelvin.textures[0].address^=1;break;
        case 4:kelvin.textures[0].control0^=0x40;break;
        case 5:kelvin.textures[0].palette|=4;break;
        case 6:kelvin.textures[1].control0=0x40000000;break;
        case 7:kelvin.textures[0].valid=0;break;
        case 8:kelvin.stencil_test=1;break;
        }
        if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 50;
    }
    kelvin=p8_128x32;surface.width=640;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 51;
    surface.width=1280;surface.logical_width=320;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 52;
    surface.logical_width=640;surface.height=481;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 53;
    surface.height=480;
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 54;
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    _putenv_s("RECOMP_P8_128X32_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 55;
    state=baseline;kelvin=(FutGraphicsKelvinState){0};
    kelvin.anti_aliasing_control=0xFFFF0001;kelvin.depth_test=1;
    kelvin.draw.depth_write=1;kelvin.compress_depth=1;kelvin.control0=0x00100001;
    kelvin.textures[0]=(FutGraphicsTextureState){.offset=0x00D1F980,.format=0x06610E29,
        .address=0x10303,.control0=0x4003FFC0,.control1=0x14000000,
        .filter=0x02062000,.image_rect=0x050001E0,.valid=0x3BF};
    surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,
        .logical_height=480,.samples=2};
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 56;
    _putenv_s("RECOMP_BC2_AA2_APPROXIMATION","true");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 57;
    _putenv_s("RECOMP_BC2_AA2_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 58;
    FutGraphicsKelvinState bc2=kelvin;
    /* Correct storage dimensions do not widen the separately authorized
     * AA2 register tuple to the newly observed 1024x512 texture. */
    kelvin.textures[0].format=0x09A10E29u;
    kelvin.textures[0].offset=0x00C9DF00u;
    kelvin.draw.dither=1;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 77;
    for(unsigned i=0;i<15;++i) {
        kelvin=bc2;surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,
            .logical_width=640,.logical_height=480,.samples=2};
        switch(i) {
        case 0:kelvin.textures[0].format^=0x100000;break;
        case 1:kelvin.textures[0].offset^=16;break;
        case 2:kelvin.textures[0].image_rect^=0x10000;break;
        case 3:kelvin.textures[0].control1^=0x10000;break;
        case 4:kelvin.textures[0].filter^=0x10000;break;
        case 5:kelvin.textures[0].address^=1;break;
        case 6:kelvin.textures[0].control0^=0x40;break;
        case 7:kelvin.textures[0].valid=0;break;
        case 8:kelvin.draw.depth_write=0;break;
        case 9:kelvin.depth_test=0;break;
        case 10:kelvin.stencil_test=1;break;
        case 11:kelvin.compress_depth=0;break;
        case 12:kelvin.control0^=1;break;
        case 13:kelvin.textures[1].control0=0x40000000;break;
        case 14:surface.width=640;break;
        }
        if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 59;
    }
    FutGraphicsKelvinState bc2_lc=bc2;
    bc2_lc.draw.dither=1;
    bc2_lc.textures[0].control1=0x05000000u;
    bc2_lc.textures[0].image_rect=0x028001E0u;
    surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,
        .logical_width=640,.logical_height=480,.samples=2};
    _putenv_s("RECOMP_BC2_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&bc2_lc,&surface)) return 89;
    FutGraphicsKelvinState bc2_lc_baseline=bc2_lc;
    bc2_lc.draw.dither=0;
    if(fut_graphics_software_aa2_draw_ready(&state,&bc2_lc,&surface)) return 93;
    bc2_lc=bc2_lc_baseline;
    bc2_lc.textures[0].control1=0x14000000u;
    if(fut_graphics_software_aa2_draw_ready(&state,&bc2_lc,&surface)) return 90;
    bc2_lc=bc2_lc_baseline;bc2_lc.textures[0].image_rect=0x050001E0u;
    if(fut_graphics_software_aa2_draw_ready(&state,&bc2_lc,&surface)) return 91;
    bc2_lc=bc2_lc_baseline;_putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&bc2_lc,&surface)) return 92;
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","1");
    kelvin=bc2;surface=(FutGraphicsSurfacePlan){.width=1280,.height=481,
        .logical_width=640,.logical_height=480,.samples=2};
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 60;
    surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,
        .logical_height=480,.samples=2};
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 61;
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    _putenv_s("RECOMP_BC2_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 62;
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_P8_128X32_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_BC2_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","0");
    state=baseline;kelvin=(FutGraphicsKelvinState){0};
    kelvin.anti_aliasing_control=0xFFFF0001;kelvin.depth_test=1;
    kelvin.draw.depth_write=0;kelvin.compress_depth=1;kelvin.control0=0x10100001;
    kelvin.textures[0]=(FutGraphicsTextureState){.offset=0x023F1580,.format=0x00012429,
        .address=0x00030303,.control0=0x4003FFC0,.control1=0x05000000,
        .filter=0x02062000,.image_rect=0x028001E0,.valid=0x3BF};
    surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,
        .logical_height=480,.samples=2};
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    _putenv_s("RECOMP_YUY2_023F1580_AA2_APPROXIMATION","1");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 63;
    _putenv_s("RECOMP_YUY2_023F1580_BT601_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 64;
    FutGraphicsKelvinState yuy2=kelvin;
    for(unsigned i=0;i<15;++i) {
        kelvin=yuy2;surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,
            .logical_width=640,.logical_height=480,.samples=2};
        switch(i) {
        case 0:kelvin.textures[0].format^=1;break;
        case 1:kelvin.anti_aliasing_control=0xFFFF0000u;break;
        case 2:kelvin.textures[0].image_rect++;break;
        case 3:kelvin.textures[0].control1++;break;
        case 4:kelvin.textures[0].filter^=0x10000;break;
        case 5:kelvin.textures[0].address^=1;break;
        case 6:kelvin.textures[0].control0^=0x40;break;
        case 7:kelvin.textures[0].valid^=1;break;
        case 8:kelvin.draw.depth_write=1;break;
        case 9:kelvin.depth_test=0;break;
        case 10:kelvin.stencil_test=1;break;
        case 11:kelvin.compress_depth=0;break;
        case 12:kelvin.control0=0x00100001;break;
        case 13:kelvin.textures[1].control0=0x40000000;break;
        case 14:surface.width=640;break;
        }
        if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 65;
    }
    kelvin=yuy2;surface.width=640;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 66;
    surface.width=1280;surface.logical_width=320;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 67;
    surface.logical_width=640;kelvin.control0^=1;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 68;
    kelvin=yuy2;_putenv_s("RECOMP_YUY2_023F1580_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 69;
    _putenv_s("RECOMP_YUY2_023F1580_AA2_APPROXIMATION","1");
    _putenv_s("RECOMP_YUY2_023F1580_BT601_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 70;
    _putenv_s("RECOMP_YUY2_023F1580_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_023F1580_BT601_APPROXIMATION","0");
    kelvin=yuy2;kelvin.textures[0].offset=0x02487680;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 71;
    _putenv_s("RECOMP_YUY2_02487680_AA2_APPROXIMATION","1");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 72;
    _putenv_s("RECOMP_YUY2_02487680_BT601_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 73;
    FutGraphicsKelvinState second_yuy2=kelvin;
    kelvin=second_yuy2;kelvin.textures[0].offset=0x02487681;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 74;
    kelvin=second_yuy2;_putenv_s("RECOMP_YUY2_02487680_BT601_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 75;
    _putenv_s("RECOMP_YUY2_02487680_BT601_APPROXIMATION","1");
    kelvin=second_yuy2;_putenv_s("RECOMP_YUY2_02487680_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 76;
    _putenv_s("RECOMP_YUY2_02487680_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_02487680_BT601_APPROXIMATION","0");
    kelvin=yuy2;kelvin.textures[0].offset=0x01A63C80u;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 77;
    _putenv_s("RECOMP_YUY2_023F1580_AA2_APPROXIMATION","1");
    _putenv_s("RECOMP_YUY2_023F1580_BT601_APPROXIMATION","1");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 82;
    _putenv_s("RECOMP_YUY2_FAMILY_AA2_APPROXIMATION","1");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 83;
    _putenv_s("RECOMP_YUY2_FAMILY_BT601_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 84;
    FutGraphicsKelvinState yuy2_family=kelvin;
    for(unsigned i=0;i<15;++i) {
        kelvin=yuy2_family;surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,
            .logical_width=640,.logical_height=480,.samples=2};
        switch(i) {
        case 0:kelvin.textures[0].format^=1;break;
        case 1:kelvin.anti_aliasing_control=0xFFFF0000u;break;
        case 2:kelvin.textures[0].image_rect++;break;
        case 3:kelvin.textures[0].control1++;break;
        case 4:kelvin.textures[0].filter^=0x10000;break;
        case 5:kelvin.textures[0].address^=1;break;
        case 6:kelvin.textures[0].control0^=0x40;break;
        case 7:kelvin.textures[0].valid^=1;break;
        case 8:kelvin.draw.depth_write=1;break;
        case 9:kelvin.depth_test=0;break;
        case 10:kelvin.stencil_test=1;break;
        case 11:kelvin.compress_depth=0;break;
        case 12:kelvin.control0=0x00100001;break;
        case 13:kelvin.textures[1].control0=0x40000000;break;
        case 14:surface.width=640;break;
        }
        if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 85;
    }
    kelvin=yuy2_family;_putenv_s("RECOMP_YUY2_FAMILY_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 86;
    _putenv_s("RECOMP_YUY2_FAMILY_AA2_APPROXIMATION","1");
    kelvin=yuy2_family;_putenv_s("RECOMP_YUY2_FAMILY_BT601_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 87;
    _putenv_s("RECOMP_YUY2_FAMILY_BT601_APPROXIMATION","1");
    _putenv_s("RECOMP_YUY2_FAMILY_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_FAMILY_BT601_APPROXIMATION","0");
    state=baseline;kelvin=bc2;kelvin.draw.dither=1;
    kelvin.textures[0].format=0x09A10E29u;kelvin.textures[0].offset=0x00C9DF00u;
    surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,
        .logical_height=480,.samples=2};
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 78;
    _putenv_s("RECOMP_BC2_C9DF00_AA2_APPROXIMATION","true");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 79;
    _putenv_s("RECOMP_BC2_C9DF00_AA2_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 80;
    FutGraphicsKelvinState bc2_c9df00=kelvin;
    kelvin=bc2; /* The new flag must not admit the independently gated old draw. */
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 81;
    for(unsigned i=0;i<25;++i) {
        state=baseline;kelvin=bc2_c9df00;
        surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,
            .logical_height=480,.samples=2};
        switch(i) {
        case 0:kelvin.textures[0].format^=0x100000;break;
        case 1:kelvin.textures[0].offset+=16;break;
        case 2:kelvin.textures[0].valid^=1;break;
        case 3:kelvin.textures[0].valid|=0x400;break;
        case 4:kelvin.textures[0].control0^=0x40;break;
        case 5:kelvin.textures[0].control0=0;break;
        case 6:kelvin.textures[0].control1^=0x10000;break;
        case 7:kelvin.textures[0].image_rect^=0x10000;break;
        case 8:kelvin.textures[0].address^=1;break;
        case 9:kelvin.textures[0].filter^=0x10000;break;
        case 10:kelvin.depth_test=0;break;
        case 11:kelvin.draw.depth_write=0;break;
        case 12:kelvin.stencil_test=1;break;
        case 13:kelvin.compress_depth=0;break;
        case 14:kelvin.control0^=1;break;
        case 15:kelvin.draw.dither=0;break;
        case 16:kelvin.textures[1].control0=0x40000000;break;
        case 17:kelvin.anti_aliasing_control^=1;break;
        case 18:surface.width=640;break;
        case 19:surface.logical_width=320;break;
        case 20:surface.height++;break;
        case 21:surface.samples=1;break;
        case 22:state=(FutGraphicsSoftwareState){0};break;
        case 23:state.register_words[0]^=1;break;
        case 24:state.rdi_value[1]^=1;break;
        }
        if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) {
            fprintf(stderr,"BC2 C9DF00 negative state admitted: %u\n",i);return 82;
        }
    }
    state=baseline;kelvin=bc2_c9df00;
    surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,
        .logical_height=480,.samples=2};
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 83;
    _putenv_s("RECOMP_BC2_C9DF00_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    state=baseline;kelvin=bc2_c9df00;
    kelvin.textures[0].format=0x08810E29u;kelvin.textures[0].offset=0x00C4CD00u;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 84;
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","true");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 85;
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 86;
    FutGraphicsKelvinState bc2_family=kelvin;
    FutGraphicsKelvinState bc2_read_only=bc2_family;
    bc2_read_only.textures[0].format=0x06610E29;
    bc2_read_only.textures[0].offset=0x015A2400;
    bc2_read_only.textures[0].address=0x30303;
    bc2_read_only.draw.depth_write=0;
    if(!fut_graphics_software_aa2_draw_ready(&state,&bc2_read_only,&surface))return 176;
    bc2_read_only.textures[0].format=0x07710E29;
    if(!fut_graphics_software_aa2_draw_ready(&state,&bc2_read_only,&surface))return 177;
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&bc2_read_only,&surface))return 178;
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","1");
    FutGraphicsKelvinState bc2_wrap=bc2_family;
    bc2_wrap.textures[0].format=0x07910E29;
    bc2_wrap.textures[0].offset=0x00CFBD00;
    bc2_wrap.textures[0].address=0x10101;
    if(!fut_graphics_software_aa2_draw_ready(&state,&bc2_wrap,&surface)) return 132;
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&bc2_wrap,&surface)) return 133;
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","1");
    /* Storage data may vary without granting other raster/sampler modes. */
    kelvin.textures[0].format=0x07710E29u;kelvin.textures[0].offset=0x10000u;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 87;
    for(unsigned scope=0;scope<2;++scope) for(unsigned i=0;i<24;++i) {
        state=baseline;kelvin=scope?bc2_wrap:bc2_family;
        surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,
            .logical_height=480,.samples=2};
        switch(i) {
        case 0:kelvin.textures[0].format^=0x10000;break;
        case 1:kelvin.textures[0].format|=4;break;
        case 2:kelvin.textures[0].format^=3;break;
        case 3:kelvin.textures[0].format=0x08110E29;break;
        case 4:kelvin.textures[0].format=0x0D810E29;break;
        case 5:kelvin.textures[0].offset++;break;
        case 6:kelvin.textures[0].offset=0xFFFFFFF0;break;
        case 7:kelvin.textures[0].offset=0x03FFFFF0;break;
        case 8:kelvin.textures[0].filter^=0x10000;break;
        case 9:kelvin.textures[0].address^=1;break;
        case 10:kelvin.textures[0].control1^=0x10000;break;
        case 11:kelvin.textures[0].image_rect^=0x10000;break;
        case 12:kelvin.textures[0].valid^=1;break;
        case 13:kelvin.draw.dither=0;break;
        case 14:kelvin.draw.depth_write=2;break;
        case 15:kelvin.stencil_test=1;break;
        case 16:state=(FutGraphicsSoftwareState){0};break;
        case 17:kelvin.textures[1].control0=0x40000000;break;
        case 18:kelvin.textures[0].control0=0;break;
        case 19:surface.logical_width=320;break;
        case 20:state.register_words[0]^=1;break;
        case 21:kelvin.depth_test=0;break;
        case 22:kelvin.compress_depth=0;break;
        case 23:state.dxt1_noise=2;break;
        }
        if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) {
            fprintf(stderr,"BC2 family negative admitted: %u\n",i);return 88;
        }
    }
    state=baseline;kelvin=bc2_family;
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 89;
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","0");
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    state=baseline;kelvin=bc2_family;
    kelvin.textures[0].format=0x08810B29u;kelvin.textures[0].offset=0x001AE180u;
    kelvin.textures[0].address=0x30303u;kelvin.textures[0].palette=0x001BE180u;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 90;
    _putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","true");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 91;
    _putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 92;
    FutGraphicsKelvinState p8_family=kelvin;
    FutGraphicsKelvinState p8_retained=p8_family;
    p8_retained.textures[0].format=0x05910B29u;
    p8_retained.textures[0].offset=0x001CBB80u;
    p8_retained.textures[0].control1=0x05000000u;
    p8_retained.textures[0].image_rect=0x028001E0u;
    if(!fut_graphics_software_aa2_draw_ready(&state,&p8_retained,&surface)) return 121;
    _putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&p8_retained,&surface)) return 122;
    _putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","1");
    p8_retained.textures[0].offset+=4;
    if(!fut_graphics_software_aa2_draw_ready(&state,&p8_retained,&surface)) return 123;
    p8_retained.textures[0].offset-=4;p8_retained.textures[0].format=0x06910B29u;
    if(!fut_graphics_software_aa2_draw_ready(&state,&p8_retained,&surface)) return 124;
    p8_retained.textures[0].format=0x05910B29u;
    kelvin.textures[0].format=0x05510B29;kelvin.textures[0].offset=0x15080;
    kelvin.draw.depth_write=0;
    _putenv_s("RECOMP_P8_AA2_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 96;
    _putenv_s("RECOMP_P8_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 97;
    kelvin=p8_family;
    kelvin.textures[0].format=0x06710B29u;kelvin.textures[0].offset=0x20000;
    kelvin.textures[0].palette=0x30000;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 93;
    FutGraphicsKelvinState p8_no_zwrite=p8_family;
    p8_no_zwrite.textures[0].format=0x07710B29;
    p8_no_zwrite.textures[0].offset=0x01E6F200;
    p8_no_zwrite.draw.depth_write=0;
    if(!fut_graphics_software_aa2_draw_ready(&state,&p8_no_zwrite,&surface)) return 134;
    _putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&p8_no_zwrite,&surface)) return 135;
    _putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","1");
    for(unsigned scope=0;scope<3;++scope) for(unsigned i=0;i<24;++i) {
        state=baseline;kelvin=scope==2?p8_no_zwrite:scope?p8_retained:p8_family;
        surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,
            .logical_height=480,.samples=2};
        switch(i) {
        case 0:kelvin.textures[0].format^=0x10000;break;
        case 1:kelvin.textures[0].format|=4;break;
        case 2:kelvin.textures[0].format^=3;break;
        case 3:kelvin.textures[0].format=0x08110B29;break;
        case 4:kelvin.textures[0].format=0x0D810B29;break;
        case 5:kelvin.textures[0].offset++;break;
        case 6:kelvin.textures[0].offset=0x03FFFFFC;break;
        case 7:kelvin.textures[0].palette++;break;
        case 8:kelvin.textures[0].palette=0x03FFFFC0;break;
        case 9:kelvin.textures[0].address^=1;break;
        case 10:kelvin.textures[0].filter^=0x10000;break;
        case 11:kelvin.textures[0].control1^=0x10000;break;
        case 12:kelvin.textures[0].image_rect^=0x10000;break;
        case 13:kelvin.textures[0].valid^=0x100;break;
        case 14:kelvin.draw.dither=0;break;
        case 15:kelvin.draw.depth_write=2;break;
        case 16:kelvin.depth_test=0;break;
        case 17:kelvin.stencil_test=1;break;
        case 18:kelvin.compress_depth=0;break;
        case 19:kelvin.textures[1].control0=0x40000000;break;
        case 20:kelvin.textures[0].control0=0;break;
        case 21:state=(FutGraphicsSoftwareState){0};break;
        case 22:state.register_words[1]^=1;break;
        case 23:surface.logical_width=320;break;
        }
        if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) {
            fprintf(stderr,"P8 family negative admitted: %u\n",i);return 94;
        }
    }
    state=baseline;kelvin=p8_family;
    surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,
        .logical_height=480,.samples=2};
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 95;
    _putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","0");
    /* Retained BC2 state must not be sampled by the captured untextured
     * shader. This separate scope leaves the old disabled-BC2 negatives. */
    state=baseline;kelvin=(FutGraphicsKelvinState){0};
    surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,
        .logical_height=480,.samples=2};
    kelvin.anti_aliasing_control=0xFFFF0001;kelvin.depth_test=1;
    kelvin.draw.depth_write=1;kelvin.draw.dither=1;kelvin.compress_depth=1;
    kelvin.control0=0x00100001;kelvin.fragment.valid=2;
    kelvin.textures[0]=(FutGraphicsTextureState){.valid=0x3BF,.format=0x06A10E29,
        .offset=0x00C6CF00,.address=0x10303,.control0=0,.control1=0x14000000,
        .filter=0x02062000,.image_rect=0x050001E0};
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","0");
    _putenv_s("RECOMP_UNTEXTURED_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 98;
    _putenv_s("RECOMP_UNTEXTURED_AA2_APPROXIMATION","true");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 99;
    _putenv_s("RECOMP_UNTEXTURED_AA2_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 100;
    FutGraphicsKelvinState untextured=kelvin;
    FutGraphicsKelvinState retained_bc3=untextured;
    retained_bc3.textures[0].format=0x05510F29;
    retained_bc3.textures[0].control0=0x0003FFC0;
    if(!fut_graphics_software_aa2_draw_ready(&state,&retained_bc3,&surface))return 183;
    for(unsigned bad=0;bad<7;++bad) {
        FutGraphicsKelvinState variant=retained_bc3;FutGraphicsSoftwareState sw=state;
        const char *opt=NULL;
        switch(bad) {
        case 0:variant.textures[0].format^=0x100;break;
        case 1:variant.textures[0].control0^=1;break;
        case 2:variant.textures[0].control0|=0x40000000;break;
        case 3:variant.fragment.stage_program=1;break;
        case 4:variant.textures[1].control0=1;break;
        case 5:sw.register_words[1]^=1;break;
        case 6:opt="RECOMP_UNTEXTURED_AA2_APPROXIMATION";break;
        }
        if(opt)_putenv_s(opt,"0");
        if(fut_graphics_software_aa2_draw_ready(&sw,&variant,&surface))return 184;
        if(opt)_putenv_s(opt,"1");
    }
    kelvin.stencil_test=1;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 166;
    _putenv_s("RECOMP_UNTEXTURED_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 167;
    _putenv_s("RECOMP_UNTEXTURED_AA2_APPROXIMATION","1");
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 168;
    _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    FutGraphicsKelvinState untextured_retained=untextured;
    untextured_retained.textures[0].control1=0x05000000;
    untextured_retained.textures[0].image_rect=0x028001E0;
    if(!fut_graphics_software_aa2_draw_ready(&state,&untextured_retained,&surface)) return 125;
    _putenv_s("RECOMP_UNTEXTURED_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&untextured_retained,&surface)) return 126;
    _putenv_s("RECOMP_UNTEXTURED_AA2_APPROXIMATION","1");
    for(unsigned scope=0;scope<4;++scope) for(unsigned i=0;i<23;++i) {
        state=baseline;kelvin=(scope&1)?untextured_retained:untextured;
        kelvin.stencil_test=scope>>1;
        surface=(FutGraphicsSurfacePlan){.width=1280,.height=480,.logical_width=640,
            .logical_height=480,.samples=2};
        switch(i) {
        case 0:kelvin.textures[0].format^=0x100;break;
        case 1:kelvin.textures[0].offset+=1;break;
        case 2:kelvin.textures[0].control0=0x40000000;break;
        case 3:kelvin.textures[1].control0=1;break;
        case 4:kelvin.fragment.stage_program=1;break;
        case 5:kelvin.fragment.valid=0;break;
        case 6:surface.samples=1;surface.width=640;break;
        case 7:surface.logical_width=320;surface.width=640;break;
        case 8:kelvin.depth_test=0;break;
        case 9:kelvin.draw.depth_write=0;break;
        case 10:kelvin.compress_depth=0;break;
        case 11:kelvin.stencil_test=2;break;
        case 12:kelvin.control0^=1;break;
        case 13:kelvin.draw.dither=0;break;
        case 14:kelvin.textures[0].valid^=1;break;
        case 15:kelvin.textures[0].address^=1;break;
        case 16:kelvin.textures[0].control1^=1;break;
        case 17:kelvin.textures[0].filter^=0x10000;break;
        case 18:kelvin.textures[0].image_rect^=1;break;
        case 19:state.register_words[1]^=1;break;
        case 20:state=(FutGraphicsSoftwareState){0};break;
        case 21:state.rdi_value[1]^=1;break;
        case 22:kelvin.anti_aliasing_control=0;break;
        }
        int unused=(i==0 || i==1 || (i>=14 && i<=18));
        if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)!=unused) {
            fprintf(stderr,"Untextured variant mismatch: %u\n",i);return 101;
        }
    }
    state=baseline;kelvin=untextured;
    kelvin.textures[0].format=0x06610C29;kelvin.textures[0].valid=0x3BF;
    kelvin.textures[0].filter=0x02063F1B;kelvin.textures[0].address=0x10101;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 148;
    kelvin.fragment.stage_program=1;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 149;
    state=baseline;kelvin=untextured;
    kelvin.textures[0].format=0x06710E29;
    kelvin.textures[0].offset=0x00CF9C00;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 127;
    kelvin.textures[0].offset+=16;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 128;
    kelvin.textures[0].format=0x09A10E29;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 129;
    kelvin.textures[0].format=0x06710E29;
    kelvin.textures[0].offset=0x04000000u-8192u;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 130;
    kelvin.textures[0].offset+=16;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 131;
    kelvin=untextured;
    kelvin.textures[0]=(FutGraphicsTextureState){.valid=0x3BF,.format=0x07730629,
        .offset=0x01476300,.address=0x10101,.filter=0x02063F1B,.control0=0x4003FFC0,
        .control1=0x14000000,.image_rect=0x050001E0};
    kelvin.fragment.stage_program=1;
    _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 132;
    _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 133;
    FutGraphicsKelvinState argb_linear=kelvin;
    argb_linear.textures[0].format=0x08810629;
    argb_linear.textures[0].offset=0x0121CC00;
    argb_linear.textures[0].filter=0x02062000;
    argb_linear.textures[0].address=0x10303;
    if(!fut_graphics_software_aa2_draw_ready(&state,&argb_linear,&surface))return 167;
    argb_linear.textures[0].format=0x07810629;
    argb_linear.textures[0].address=0x10101;
    argb_linear.textures[0].offset=0x04000000u-131072u;
    if(!fut_graphics_software_aa2_draw_ready(&state,&argb_linear,&surface))return 168;
    for(unsigned bad=0;bad<10;++bad) {
        FutGraphicsKelvinState variant=argb_linear;
        switch(bad) {
        case 0:variant.textures[0].filter^=1;break;
        case 1:variant.textures[0].address=0x10301;break;
        case 2:variant.textures[0].format+=0x10000;break;
        case 3:variant.textures[0].control1=0;break;
        case 4:variant.textures[0].image_rect=0;break;
        case 5:variant.textures[0].valid&=~2u;break;
        case 6:variant.textures[0].offset+=4;break;
        case 7:variant.draw.depth_write=2;break;
        case 8:_putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","0");break;
        case 9:_putenv_s("RECOMP_AA2_APPROXIMATION","0");break;
        }
        if(fut_graphics_software_aa2_draw_ready(&state,&variant,&surface))return 169;
        _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","1");
        _putenv_s("RECOMP_AA2_APPROXIMATION","1");
    }
    FutGraphicsKelvinState argb_readonly=argb_linear;
    argb_readonly.textures[0].format=0x04410629;
    argb_readonly.textures[0].offset=0x04000000u-1024u;
    argb_readonly.textures[0].address=0x30303;
    argb_readonly.draw.depth_write=0;
    const uint32_t argb_formats[]={0x00010629u,0x04410629u,0x04710629u,0x08810629u};
    const uint32_t argb_addresses[]={0x10101u,0x10303u,0x30303u};
    for(unsigned f=0;f<4;++f)for(unsigned address=0;address<3;++address)for(unsigned write=0;write<2;++write) {
        FutGraphicsKelvinState variant=argb_readonly;
        variant.textures[0].format=argb_formats[f];variant.textures[0].address=argb_addresses[address];
        unsigned u=(argb_formats[f]>>20)&15u,v=(argb_formats[f]>>24)&15u;
        variant.textures[0].offset=0x04000000u-(1u<<(u+v+2u));variant.draw.depth_write=write;
        if(!fut_graphics_software_aa2_draw_ready(&state,&variant,&surface))return 180;
    }
    for(unsigned bad=0;bad<17;++bad) {
        FutGraphicsKelvinState variant=argb_readonly;FutGraphicsSoftwareState sw=state;
        switch(bad) {
        case 0:variant.draw.depth_write=2;break;
        case 1:variant.depth_test=0;break;
        case 2:variant.stencil_test=1;break;
        case 3:variant.compress_depth=0;break;
        case 4:variant.control0^=1;break;
        case 5:variant.textures[0].control0=0;break;
        case 6:variant.textures[0].valid&=~2u;break;
        case 7:variant.textures[0].address=0x10301;break;
        case 8:variant.textures[0].filter^=1;break;
        case 9:variant.textures[0].format=(variant.textures[0].format&~0xFF00u)|0x7F00u;break;
        case 10:variant.textures[0].format+=0x10000;break;
        case 11:variant.textures[0].offset++;break;
        case 12:variant.textures[0].offset+=4;break;
        case 13:_putenv_s("RECOMP_AA2_APPROXIMATION","0");break;
        case 14:_putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","0");break;
        case 15:sw.rdi_value[1]^=1;break;
        case 16:variant.textures[1].control0=0x40000000u;break;
        }
        if(fut_graphics_software_aa2_draw_ready(&sw,&variant,&surface)) {
            fprintf(stderr,"ARGB8 read-only depth variant admitted: %u\n",bad);return 181;
        }
        _putenv_s("RECOMP_AA2_APPROXIMATION","1");
        _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","1");
    }
    kelvin.textures[0].format=0x07830629;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 143;
    kelvin.textures[0].offset=0x04000000u-172032u+4u;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 144;
    kelvin.textures[0].offset=0x01476300;
    kelvin.textures[0].format=0x07720E29;
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 145;
    kelvin.textures[0].format=0x05610E29;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 147;
    _putenv_s("RECOMP_BC2_AA2_FAMILY_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 146;
    kelvin.textures[0].format=0x05720B29;kelvin.textures[0].palette=0x166E3C0;
    _putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 150;
    kelvin.textures[0].format=0x06610B29;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 153;
    kelvin.textures[0].format=0x07810B29;kelvin.textures[0].filter=0x02062000;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 154;
    FutGraphicsKelvinState p8_clamp=kelvin;
    p8_clamp.textures[0].format=0x05510B29;p8_clamp.textures[0].offset=0x00AD4E80;
    p8_clamp.textures[0].address=0x10303;
    if(!fut_graphics_software_aa2_draw_ready(&state,&p8_clamp,&surface))return 173;
    p8_clamp.textures[0].format=0x06710B29;
    if(!fut_graphics_software_aa2_draw_ready(&state,&p8_clamp,&surface))return 174;
    for(unsigned bad=0;bad<7;++bad) {
        FutGraphicsKelvinState variant=p8_clamp;
        switch(bad) {
        case 0:variant.textures[0].palette|=4;break;
        case 1:variant.textures[0].address=0x10301;break;
        case 2:variant.textures[0].filter^=1;break;
        case 3:variant.textures[0].format+=0x10000;break;
        case 4:variant.textures[0].offset=0x03FFFFFC;break;
        case 5:_putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","0");break;
        case 6:_putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","0");break;
        }
        if(fut_graphics_software_aa2_draw_ready(&state,&variant,&surface))return 175;
        _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","1");
        _putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","1");
    }
    _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 155;
    _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","1");
    kelvin.textures[0].filter=0x02062001;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 156;
    kelvin.textures[0].filter=0x02063F1B;
    kelvin.textures[0].palette|=4;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 151;
    kelvin.textures[0].palette&=~4u;
    _putenv_s("RECOMP_P8_AA2_FAMILY_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 152;
    kelvin.textures[0].format=0x06510C29;
    _putenv_s("RECOMP_DXT1_NOISE_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 135;
    _putenv_s("RECOMP_DXT1_NOISE_APPROXIMATION","1");
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 136;
    kelvin.textures[0].format=0x07620C29;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 137;
    kelvin.textures[0].address=0x10303;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 140;
    kelvin.textures[0].address=0x10101;
    kelvin.textures[0].format=0x07720C29;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 141;
    kelvin.textures[0].format=0x07790C29;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 142;
    kelvin.textures[0].format=0x09A30F29;
    if(!fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 138;
    kelvin.textures[0].offset=0x04000000u-688128u+16u;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 139;
    kelvin.textures[0].format=0x07730629;
    kelvin.textures[0].offset=0x04000000u-86016u+4u;
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface))return 134;
    int stage_test=verify_projective_stages(&baseline);if(stage_test)return stage_test;
    _putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","0");kelvin=untextured;
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    if(fut_graphics_software_aa2_draw_ready(&state,&kelvin,&surface)) return 102;
    _putenv_s("RECOMP_UNTEXTURED_AA2_APPROXIMATION","0");
    puts("Software controls: captured untextured draw and existing texture scopes, independent opt-ins and negative variants verified");
    return 0;
}
