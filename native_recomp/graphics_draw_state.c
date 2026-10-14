#include "graphics_draw_state.h"
static int blend_factor(uint32_t v)
{
    return v <= 1 || (v >= 0x300 && v <= 0x308) || (v >= 0x8001 && v <= 0x8004);
}
static int stencil_operation(uint32_t v)
{
    return v == 0 || (v >= 0x1E00 && v <= 0x1E03) || v == 0x150A || v == 0x8507 || v == 0x8508;
}
FutGraphicsDrawStatus fut_graphics_draw_state(FutGraphicsDrawState *s, uint32_t method, uint32_t value)
{
    if (!s || (method & 3)) return FUT_DRAW_STATE_REJECTED;
    uint32_t *destination = 0; unsigned bit = 32;
    if ((method >= 0x2C0 && method < 0x2E0) || (method >= 0x2E0 && method < 0x300)) {
        unsigned start = (method & 0x1F) / 4;
        uint32_t *windows = method < 0x2E0 ? s->window_x : s->window_y;
        /* A write also initializes all later slots, not only its own slot. */
        for (unsigned i = start; i < 8; ++i) windows[i] = value & 0x0FFF0FFFu;
        if (!start) s->valid |= 1u << (method < 0x2E0 ? 18 : 19);
        s->dirty = 1; return FUT_DRAW_STATE_APPLIED;
    }
    if (method >= 0xA60 && method < 0xAA0) {
        unsigned slot = (method - 0xA60) / 4;
        s->combiner_factor[slot / 8][slot % 8] = value;
        return FUT_DRAW_STATE_APPLIED;
    }
#define FIELD(name) destination = &s->name
#define BOOLEAN(name) if (value > 1) return FUT_DRAW_STATE_REJECTED; FIELD(name)
#define COMPARE(name) if (value < 0x200 || value > 0x207) return FUT_DRAW_STATE_REJECTED; FIELD(name)
    switch (method) {
    case 0x2B4: BOOLEAN(window_exclusive); bit = 17; break;
    case 0x2A8: FIELD(fog_color); break;
    case 0x300: BOOLEAN(alpha_test); break;
    case 0x304: BOOLEAN(blend_enable); break;
    case 0x308: BOOLEAN(cull_enable); break;
    case 0x310: BOOLEAN(dither); break;
    case 0x320: BOOLEAN(line_smooth); break;
    case 0x324: BOOLEAN(polygon_smooth); break;
    case 0x328: if (value > 6) return FUT_DRAW_STATE_REJECTED; FIELD(skin_mode); break;
    case 0x330: BOOLEAN(offset_enable[0]); break;
    case 0x334: BOOLEAN(offset_enable[1]); break;
    case 0x338: BOOLEAN(offset_enable[2]); break;
    case 0x33C: COMPARE(alpha_func); break;
    case 0x340: FIELD(alpha_ref); break;
    case 0x344: if (!blend_factor(value)) return FUT_DRAW_STATE_REJECTED; FIELD(blend_src); bit = 13; break;
    case 0x348: if (!blend_factor(value)) return FUT_DRAW_STATE_REJECTED; FIELD(blend_dst); bit = 14; break;
    case 0x34C: FIELD(blend_color); break;
    case 0x350:
        if (value != 0x8006 && value != 0x8007 && value != 0x8008 && value != 0x800A && value != 0x800B)
            return FUT_DRAW_STATE_REJECTED;
        FIELD(blend_op); bit = 15; break;
    case 0x354: COMPARE(depth_func); bit = 4; break;
    case 0x358: FIELD(color_mask); bit = 16; break;
    case 0x35C: BOOLEAN(depth_write); bit = 5; break;
    case 0x360: FIELD(stencil_write); bit = 8; break;
    case 0x364: COMPARE(stencil_func); bit = 9; break;
    case 0x368: FIELD(stencil_ref); bit = 6; break;
    case 0x36C: FIELD(stencil_read); bit = 7; break;
    case 0x370: case 0x374: case 0x378:
        if (!stencil_operation(value)) return FUT_DRAW_STATE_REJECTED;
        destination = &s->stencil_op[(method - 0x370) / 4]; bit = 10 + (method - 0x370) / 4; break;
    case 0x37C:
        if (value != 0x1D00 && value != 0x1D01) return FUT_DRAW_STATE_REJECTED;
        FIELD(shade_mode); break;
    case 0x384: FIELD(offset_factor); break;
    case 0x388: FIELD(offset_bias); break;
    case 0x38C: case 0x390:
        if (value < 0x1B00 || value > 0x1B02) return FUT_DRAW_STATE_REJECTED;
        destination = method == 0x38C ? &s->front_mode : &s->back_mode; bit = method == 0x38C ? 2 : 3; break;
    case 0x39C:
        if (value != 0x404 && value != 0x405 && value != 0x408) return FUT_DRAW_STATE_REJECTED;
        FIELD(cull_face); bit = 0; break;
    case 0x3A0:
        if (value != 0x900 && value != 0x901) return FUT_DRAW_STATE_REJECTED;
        FIELD(front_face); bit = 1; break;
    case 0x3A4: BOOLEAN(normalization); break;
    case 0x9F8: FIELD(swath_width); break; /* Native scheduling is not an NV raster swath. */
    case 0x147C: BOOLEAN(stipple); break;
    case 0x17BC: BOOLEAN(logic_op); break;
    case 0x17C4: BOOLEAN(two_side); break;
    case 0x1D78:
        if (((value >> 4) & 15) > 1) return FUT_DRAW_STATE_REJECTED;
        FIELD(z_control); break;
    case 0x1D84:
        if (value > 3) return FUT_DRAW_STATE_REJECTED;
        FIELD(occlusion_control); break; /* Retained depth/stencil optimization controls. */
    case 0x1E6C:
        if (value > 7) return FUT_DRAW_STATE_REJECTED;
        FIELD(shadow_compare); break;
    default: return FUT_DRAW_STATE_NOT_HANDLED;
    }
    *destination = value;
    if (bit < 32) s->valid |= 1u << bit;
    s->dirty = 1;
    return FUT_DRAW_STATE_APPLIED;
#undef FIELD
#undef BOOLEAN
#undef COMPARE
}
int fut_graphics_draw_ready(const FutGraphicsDrawState *s) { return s && s->valid == 0xFFFFFu; }
int fut_graphics_window_contains(const FutGraphicsDrawState *s, unsigned x, unsigned y)
{
    if (!s) return 0;
    int contained = 0;
    for (unsigned i = 0; i < 8; ++i)
        if (x >= (s->window_x[i] & 0xFFF) && x <= ((s->window_x[i] >> 16) & 0xFFF) &&
            y >= (s->window_y[i] & 0xFFF) && y <= ((s->window_y[i] >> 16) & 0xFFF)) contained = 1;
    return s->window_exclusive ? !contained : contained;
}
