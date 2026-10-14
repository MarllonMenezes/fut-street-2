#ifndef FUT_GRAPHICS_KELVIN_STATE_H
#define FUT_GRAPHICS_KELVIN_STATE_H

#include <stdint.h>
#include "graphics_draw_state.h"

typedef struct {
    uint32_t control0, border_color, color_key, bump_env[6], texgen[4];
} FutGraphicsTextureState;
typedef struct {
    FutGraphicsDrawState draw;
    FutGraphicsTextureState textures[4];
    uint32_t constants[192][4];
    uint32_t program[136][4];
    uint8_t program_valid[136];
    unsigned program_load, program_start, program_mode;
    int program_dirty;
    float attributes[16][4];
    uint32_t clip_min, clip_max;
    uint32_t control0, line_width, anti_aliasing_control;
    unsigned depth_test, stencil_test;
    unsigned constant_load;
    int constants_dirty;
    unsigned provoking_first, edge_flag, compress_depth;
    unsigned flip_read, flip_write, flip_modulo;
    uint32_t shader_other_input, shadow_slope_threshold, clear_color, pattern_color;
    uint32_t clear_depth, clear_x, clear_y;
    unsigned clear_valid;
} FutGraphicsKelvinState;

typedef enum { FUT_KELVIN_APPLIED, FUT_KELVIN_NOT_HANDLED, FUT_KELVIN_REJECTED } FutGraphicsKelvinStatus;
FutGraphicsKelvinStatus fut_graphics_kelvin_state(FutGraphicsKelvinState *state,
    uint32_t method, uint32_t value);

#endif
