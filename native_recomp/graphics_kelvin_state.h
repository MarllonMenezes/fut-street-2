#ifndef FUT_GRAPHICS_KELVIN_STATE_H
#define FUT_GRAPHICS_KELVIN_STATE_H

#include <stdint.h>
#include "graphics_draw_state.h"
#include "graphics_fragment_program.h"

typedef struct {
    uint32_t control0, border_color, color_key, bump_env[6], texgen[4];
    uint32_t offset, format, address, control1, filter, image_rect, palette;
    unsigned valid; /* One bit per 4-byte method slot within each 64-byte stage. */
} FutGraphicsTextureState;
typedef struct {
    FutGraphicsDrawState draw;
    FutGraphicsFragmentState fragment;
    FutGraphicsTextureState textures[4];
    uint32_t constants[192][4];
    uint8_t constant_valid[192];
    uint32_t program[136][4];
    uint8_t program_valid[136];
    unsigned program_load, program_start, program_mode;
    int program_dirty;
    float attributes[16][4];
    uint32_t clip_min, clip_max;
    uint32_t control0, line_width, anti_aliasing_control;
    unsigned depth_test, stencil_test;
    unsigned point_params_enable, point_smooth;
    unsigned fog_enable, lighting_enable, specular_enable;
    unsigned texture_matrix_enable[4];
    uint32_t light_control, light_enable_mask;
    uint32_t point_size;
    unsigned constant_load;
    int constants_dirty;
    unsigned provoking_first, edge_flag, compress_depth;
    unsigned flip_read, flip_write, flip_modulo;
    unsigned flip_valid;
    uint32_t shader_other_input, shadow_slope_threshold, clear_color, pattern_color;
    uint32_t clear_depth, clear_x, clear_y;
    unsigned clear_valid;
} FutGraphicsKelvinState;

typedef enum { FUT_KELVIN_APPLIED, FUT_KELVIN_NOT_HANDLED, FUT_KELVIN_REJECTED } FutGraphicsKelvinStatus;
FutGraphicsKelvinStatus fut_graphics_kelvin_state(FutGraphicsKelvinState *state,
    uint32_t method, uint32_t value);
/* -1=incomplete/invalid counters,0=write slot still being read,1=free slot.
 * No read-index advance, present, guest marker or synthetic VBlank. */
int fut_graphics_flip_available(const FutGraphicsKelvinState *state);

#endif
