#include "graphics_software_state.h"
int fut_graphics_software_state(FutGraphicsSoftwareState *s, uint32_t notification,
    uint32_t clear_depth, uint32_t clear_color, unsigned clear_valid)
{
    if (!s) return 0;
    unsigned selector = notification & 31;
    if (selector == 8) {
        uint32_t data = notification >> 5;
        s->sample_alpha = data; s->sample_valid = 1;
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
    return s && !s->sample_valid && !s->register_valid;
}
