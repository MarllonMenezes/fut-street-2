#ifndef FUT_GRAPHICS_SOFTWARE_STATE_H
#define FUT_GRAPHICS_SOFTWARE_STATE_H
#include <stdint.h>
#include "graphics_kelvin_state.h"
#include "graphics_surfaces.h"
typedef struct {
    uint32_t dxt1_noise, rdi_index[2], rdi_value[2], register_words[2];
    unsigned noise_valid, register_valid;
} FutGraphicsSoftwareState;
/* Retain selectors8/9 of the original software dispatcher, including all raw
 * writes. This is not generic RDI execution. The context-free gate remains
 * strict; use the separate domain-aware predicate only at a validated draw.
 * Unknown selectors/targets never mutate state or acknowledge a callback. */
int fut_graphics_software_state(FutGraphicsSoftwareState *state, uint32_t notification,
    uint32_t clear_depth, uint32_t clear_color, unsigned clear_valid);
int fut_graphics_software_draw_ready(const FutGraphicsSoftwareState *state);
/* Exact known DXT1-noise/Zcull/ROP-Z controls are outside the effects of this
 * domain only: linearARGB8 textures, single sample, depth AND stencil disabled.
 * A retained depth-write mask/compression hint cannot enable disabled depth.
 * Does NOT implement BC1 noise or general Zcull/ROP-Z; unknown values fail.
 * See XboxDev/nxdk14d5ee97 lib/pbkit/pbkit.c PB_SETNOISE and init controls.
 * Packed XDK5849 index159 is DXT1NOISEENABLE, NOT normalized Cxbx SAMPLEALPHA. */
int fut_graphics_software_linear_draw_ready(const FutGraphicsSoftwareState *state,
    const FutGraphicsKelvinState *kelvin,const FutGraphicsSurfacePlan *surface);
#endif
