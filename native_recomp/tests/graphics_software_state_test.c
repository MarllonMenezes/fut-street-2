#include "graphics_software_state.h"
#include <string.h>
#include <stdio.h>
int main(void)
{
    FutGraphicsSoftwareState state = {0};
    if (!fut_graphics_software_draw_ready(&state)) return 1;
    if (!fut_graphics_software_state(&state, 0x28, 0, 0, 0) || state.dxt1_noise != 1 ||
        state.rdi_index[0] != 0xE00050 || state.rdi_index[1] != 0xDF0008 ||
        state.rdi_value[0] != 1 || state.rdi_value[1] != 1 || fut_graphics_software_draw_ready(&state)) return 2;
    if (!fut_graphics_software_state(&state, 9, 0x400094, 4, 3) || state.register_words[0] != 4 || state.register_valid != 1) return 3;
    if (!fut_graphics_software_state(&state, 9, 0x400B80, 0x45EAD10E, 3) || state.register_words[1] != 0x45EAD10E || state.register_valid != 3) return 4;
    FutGraphicsSoftwareState before = state;
    if (fut_graphics_software_state(&state, 9, 0x400094, 0, 2) ||
        fut_graphics_software_state(&state, 9, 0x400100, 0, 3) ||
        fut_graphics_software_state(&state, 6, 0, 0, 0) || memcmp(&before, &state, sizeof(state))) return 5;
    if (!fut_graphics_software_state(&state, 8, 0, 0, 0) || fut_graphics_software_draw_ready(&state)) return 6;
    FutGraphicsKelvinState kelvin={0};
    FutGraphicsSurfacePlan surface={.width=640,.height=480,.logical_width=640,.logical_height=480,.samples=1};
    kelvin.draw.depth_write=1;kelvin.compress_depth=1;
    kelvin.textures[0].control0=0x4003FFC0;kelvin.textures[0].format=0x11229;
    before=state;
    if(!fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface) || memcmp(&state,&before,sizeof(state))) return 7;
    for(unsigned noise=0;noise<2;++noise) {
        if(!fut_graphics_software_state(&state,(noise<<5)|8,0,0,0) ||
            !fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 8;
    }
    kelvin.depth_test=1;
    if(fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 9;
    kelvin.depth_test=0;kelvin.stencil_test=1;
    if(fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 10;
    kelvin.stencil_test=0;surface.samples=2;
    if(fut_graphics_software_linear_draw_ready(&state,&kelvin,&surface)) return 11;
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
    puts("Selectors8/9: DXT1-noise identification and no-depth linearARGB8 domain; BC1, active depth/stencil and unknown RDI/register values refused");
    return 0;
}
