#ifndef FUT_GRAPHICS_SOFTWARE_STATE_H
#define FUT_GRAPHICS_SOFTWARE_STATE_H
#include <stdint.h>
#include "graphics_kelvin_state.h"
#include "graphics_surfaces.h"
typedef struct {
    uint32_t dxt1_noise, rdi_index[2], rdi_value[2], register_words[2];
    unsigned noise_valid, register_valid;
} FutGraphicsSoftwareState;
typedef struct {
    unsigned compare_always_read, depth_read, dont_cull_uncompressed;
    uint32_t baseline[2];
} FutGraphicsRopControls;
/* Decode only the observed baseline plus three independently traced toggles.
 * Requires BOTH writes; no invented defaults. Identification is not proof of
 * raster equivalence and does not admit these controls at a draw. */
int fut_graphics_software_rop_controls(const FutGraphicsSoftwareState *state,
    FutGraphicsRopControls *controls);
/* Retain selectors8/9 of the original software dispatcher, including all raw
 * writes. This is not generic RDI execution. The context-free gate remains
 * strict; use the separate domain-aware predicate only at a validated draw.
 * Unknown selectors/targets never mutate state or acknowledge a callback. */
int fut_graphics_software_state(FutGraphicsSoftwareState *state, uint32_t notification,
    uint32_t clear_depth, uint32_t clear_color, unsigned clear_valid);
int fut_graphics_software_draw_ready(const FutGraphicsSoftwareState *state);
/* Retain exact observed DXT1-noise and selector-9 PGRAPH register writes.
 * Their effects are not generally modeled; the restricted exemption is for
 * linearARGB8/A8 texture contexts on single-sample surfaces with depth AND
 * stencil tests disabled. This does not identify the PGRAPH registers as
 * Zcull/ROP-Z or claim NV2A equivalence. BC1 noise and unknown values fail.
 * See XboxDev/nxdk14d5ee97 lib/pbkit/pbkit.c PB_SETNOISE and initialization.
 * Packed XDK5849 index159 is DXT1NOISEENABLE, NOT normalized Cxbx SAMPLEALPHA. */
int fut_graphics_software_linear_draw_ready(const FutGraphicsSoftwareState *state,
    const FutGraphicsKelvinState *kelvin,const FutGraphicsSurfacePlan *surface);
/* Strict AA2 path normally requires no retained software controls. Explicit
 * RECOMP_AA2_APPROXIMATION=1 also admits only the observed 640x480 baseline
 * under the user-authorized experiment; unknown raw values remain refused.
 * A separate RECOMP_DXT1_NOISE_APPROXIMATION=1 admits only the captured
 * 512x512 stage-0 BC1 state without modeling NV2A noise (2026-10-17).
 * Other texture stages must be disabled in that additional experiment. */
int fut_graphics_software_aa2_draw_ready(const FutGraphicsSoftwareState *state,
    const FutGraphicsKelvinState *kelvin,const FutGraphicsSurfacePlan *surface);
#endif
