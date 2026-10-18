#include "graphics_software_state.h"
#include <string.h>
#include <stdio.h>
#include <stdlib.h>
int main(void)
{
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_DXT1_NOISE_APPROXIMATION","0");
    _putenv_s("RECOMP_P8_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_P8_128X32_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_BC2_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_023F1580_BT601_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_023F1580_AA2_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_02487680_BT601_APPROXIMATION","0");
    _putenv_s("RECOMP_YUY2_02487680_AA2_APPROXIMATION","0");
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
        case 1:kelvin.textures[0].offset++;break;
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
    _putenv_s("RECOMP_AA2_APPROXIMATION","0");
    puts("Software controls: exact BC1/P8/BC2 and both YUY2 AA2 scopes, independent opt-ins and negative variants verified");
    return 0;
}
