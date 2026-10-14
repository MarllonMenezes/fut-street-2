#ifndef FUT_GRAPHICS_DRAW_STATE_H
#define FUT_GRAPHICS_DRAW_STATE_H
#include <stdint.h>
typedef struct {
    uint32_t window_x[8], window_y[8], window_exclusive;
    uint32_t alpha_test, alpha_func, alpha_ref, blend_enable, blend_src, blend_dst, blend_op, blend_color;
    uint32_t cull_enable, cull_face, front_face, front_mode, back_mode, shade_mode;
    uint32_t depth_func, depth_write, stencil_func, stencil_ref, stencil_read, stencil_write, stencil_op[3];
    uint32_t color_mask, dither, offset_enable[3], offset_factor, offset_bias;
    uint32_t line_smooth, polygon_smooth, normalization, skin_mode, stipple, two_side, logic_op;
    uint32_t z_control, occlusion_control, swath_width, fog_color, shadow_compare, combiner_factor[2][8];
    uint32_t valid; int dirty;
} FutGraphicsDrawState;
typedef enum { FUT_DRAW_STATE_APPLIED, FUT_DRAW_STATE_NOT_HANDLED, FUT_DRAW_STATE_REJECTED } FutGraphicsDrawStatus;
FutGraphicsDrawStatus fut_graphics_draw_state(FutGraphicsDrawState *state, uint32_t method, uint32_t value);
int fut_graphics_draw_ready(const FutGraphicsDrawState *state);
/* Inclusive 12-bit windows, union in inclusive mode and its complement in
 * exclusive mode. Coordinates are logical pixels, not host/MSAA samples. */
int fut_graphics_window_contains(const FutGraphicsDrawState *state, unsigned x, unsigned y);
#endif
