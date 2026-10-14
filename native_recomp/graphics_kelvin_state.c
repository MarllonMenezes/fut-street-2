#include "graphics_kelvin_state.h"
#include <string.h>

/* Native shader constants retain the original 192 float4 slots as raw bits.
 * Fixed-function eye/fog/texgen data use the same context slot layout as the
 * title's vertex processing pipeline. This module applies state, not draws. */
FutGraphicsKelvinStatus fut_graphics_kelvin_state(FutGraphicsKelvinState *state,
    uint32_t method, uint32_t value)
{
    if (!state || (method & 3u))
        return FUT_KELVIN_REJECTED;
    unsigned row = 0, component = 0;
    int constant = 0;
    if (method >= 0x1940u && method < 0x1980u) {
        unsigned slot = (method - 0x1940u) / 4;
        if (!slot) return FUT_KELVIN_REJECTED; /* Would emit an inline vertex. */
        for (unsigned i = 0; i < 4; ++i)
            state->attributes[slot][i] = (float)((value >> (i * 8)) & 255u) / 255.0f;
        return FUT_KELVIN_APPLIED;
    }
    if (method >= 0x1A00u && method < 0x1B00u) {
        unsigned slot = (method - 0x1A00u) / 4;
        if (slot < 4) return FUT_KELVIN_REJECTED;
        memcpy(&state->attributes[slot / 4][slot % 4], &value, sizeof(value));
        return FUT_KELVIN_APPLIED;
    }
    if (method >= 0xB00u && method < 0xB80u) {
        unsigned component = ((method - 0xB00u) / 4) % 4;
        if (state->program_load >= 136) return FUT_KELVIN_REJECTED;
        state->program[state->program_load][component] = value;
        state->program_valid[state->program_load] |= 1u << component;
        state->program_dirty = 1;
        if (component == 3) ++state->program_load;
        return FUT_KELVIN_APPLIED;
    }
    if (method >= 0xA20u && method <= 0xA2Cu) {
        row = 0x3B; component = (method - 0xA20u) / 4; constant = 1;
    } else if (method >= 0xAF0u && method <= 0xAFCu) {
        row = 0x3A; component = (method - 0xAF0u) / 4; constant = 1;
    } else if (method >= 0xA50u && method <= 0xA5Cu) {
        row = 0x38; component = (method - 0xA50u) / 4; constant = 1;
    } else if (method >= 0x9D0u && method <= 0x9DCu) {
        row = 0x39; component = (method - 0x9D0u) / 4; constant = 1;
    } else if (method >= 0x840u && method < 0x940u) {
        unsigned slot = (method - 0x840u) / 4;
        row = 0x40 + (slot / 16) * 8 + (slot % 16) / 4;
        component = slot % 4; constant = 1;
    } else if (method >= 0xB80u && method < 0xC00u) {
        if (state->constant_load >= 192)
            return FUT_KELVIN_REJECTED;
        row = state->constant_load; component = ((method - 0xB80u) / 4) % 4;
        constant = 1;
        if (component == 3) ++state->constant_load;
    }
    if (constant) {
        state->constants[row][component] = value;
        state->constants_dirty = 1;
        return FUT_KELVIN_APPLIED;
    }
    switch (method) {
    case 0x100:
        if (value) return FUT_KELVIN_REJECTED; /* Nonzero NOP is a software-method notification. */
        break;
    case 0x394: state->clip_min = value; break;
    case 0x398: state->clip_max = value; break;
    case 0x1E94:
        if ((value & 3u) > 2 || value > 7) return FUT_KELVIN_REJECTED;
        state->program_mode = value; state->program_dirty = 1; break;
    case 0x1E98:
        if (value) return FUT_KELVIN_REJECTED; /* Writable shader contexts need a separate backend. */
        break;
    case 0x1E9C:
        if (value >= 136) return FUT_KELVIN_REJECTED;
        state->program_load = value; break;
    case 0x1EA0:
        if (value >= 136) return FUT_KELVIN_REJECTED;
        state->program_start = value; state->program_dirty = 1; break;
    case 0x9FC: /* 1=first provoking vertex, 0=last. */
        if (value > 1) return FUT_KELVIN_REJECTED;
        state->provoking_first = value; break;
    case 0x16BC:
        if (value > 1) return FUT_KELVIN_REJECTED;
        state->edge_flag = value; break;
    case 0x1D80: /* Compression is a storage hint for native depth surfaces. */
        if (value > 1) return FUT_KELVIN_REJECTED;
        state->compress_depth = value; break;
    case 0x1E78:
        state->shader_other_input = value & 0x0FFFF000u; break;
    case 0x1E68:
        /* Finite slope filtering needs a native shader implementation. */
        if (value != 0x7F800000u) return FUT_KELVIN_REJECTED;
        state->shadow_slope_threshold = value; break;
    case 0x1EA4:
        if (value >= 192) return FUT_KELVIN_REJECTED;
        state->constant_load = value; break;
    case 0x120:
        if (value > 7) return FUT_KELVIN_REJECTED;
        state->flip_read = value; break;
    case 0x124:
        if (value > 7) return FUT_KELVIN_REJECTED;
        state->flip_write = value; break;
    case 0x128:
        if (value == 0 || value > 7) return FUT_KELVIN_REJECTED;
        state->flip_modulo = value; break;
    case 0x1D90:
        state->clear_color = value; break;
    default:
        return FUT_KELVIN_NOT_HANDLED;
    }
    return FUT_KELVIN_APPLIED;
}
