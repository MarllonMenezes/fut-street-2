#include "graphics_software_state.h"
#include <string.h>
#include <stdio.h>
int main(void)
{
    FutGraphicsSoftwareState state = {0};
    if (!fut_graphics_software_draw_ready(&state)) return 1;
    if (!fut_graphics_software_state(&state, 0x28, 0, 0, 0) || state.sample_alpha != 1 ||
        state.rdi_index[0] != 0xE00050 || state.rdi_index[1] != 0xDF0008 ||
        state.rdi_value[0] != 1 || state.rdi_value[1] != 1 || fut_graphics_software_draw_ready(&state)) return 2;
    if (!fut_graphics_software_state(&state, 9, 0x400094, 4, 3) || state.register_words[0] != 4 || state.register_valid != 1) return 3;
    if (!fut_graphics_software_state(&state, 9, 0x400B80, 0x45EAD10E, 3) || state.register_words[1] != 0x45EAD10E || state.register_valid != 3) return 4;
    FutGraphicsSoftwareState before = state;
    if (fut_graphics_software_state(&state, 9, 0x400094, 0, 2) ||
        fut_graphics_software_state(&state, 9, 0x400100, 0, 3) ||
        fut_graphics_software_state(&state, 6, 0, 0, 0) || memcmp(&before, &state, sizeof(state))) return 5;
    if (!fut_graphics_software_state(&state, 8, 0, 0, 0) || fut_graphics_software_draw_ready(&state)) return 6;
    puts("Original software selectors 8/9 retain exact state; unsupported effects keep draws gated");
    return 0;
}
