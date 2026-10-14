#ifndef FUT_GRAPHICS_SOFTWARE_STATE_H
#define FUT_GRAPHICS_SOFTWARE_STATE_H
#include <stdint.h>
typedef struct {
    uint32_t sample_alpha, rdi_index[2], rdi_value[2], register_words[2];
    unsigned sample_valid, register_valid;
} FutGraphicsSoftwareState;
/* Retain the exact state-only effects established from selectors 8/9 of
 * the original software dispatcher. This is NOT proof of RDI rasterization
 * equivalence. Draws must remain gated until those effects are translated.
 * Unknown selectors/targets never mutate state or acknowledge a callback. */
int fut_graphics_software_state(FutGraphicsSoftwareState *state, uint32_t notification,
    uint32_t clear_depth, uint32_t clear_color, unsigned clear_valid);
int fut_graphics_software_draw_ready(const FutGraphicsSoftwareState *state);
#endif
