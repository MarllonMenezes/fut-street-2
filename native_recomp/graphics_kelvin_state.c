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
    if (method >= 0x181Cu && method <= 0x1824u) {
        /* NV097_SET_EYE_VECTOR: retain its three raw component registers.
         * This records guest state; native fixed-function lighting is still
         * gated independently and no rendered effect is implied here. */
        unsigned component = (method - 0x181Cu) / 4u;
        state->eye_vector[component] = value;
        state->eye_vector_valid |= (uint8_t)(1u << component);
        return FUT_KELVIN_APPLIED;
    }
    if (method >= 0x1720u && method < 0x1760u) {
        unsigned slot = (method - 0x1720u) / 4u;
        state->vertex_array_offset[slot] = value;
        state->vertex_array_offset_valid |= (uint16_t)(1u << slot);
        return FUT_KELVIN_APPLIED;
    }
    if (method >= 0x1760u && method < 0x17A0u) {
        /* NV20 VTXBUF_FMT: retain only format words observed in the title.
         * This records descriptors; fetch, conversion and drawing remain gated. */
        if (value != 0x2u && value != 0x22u && value != 0x32u && value != 0x40u &&
            value != 0x635u && value != 0x602u && value != 0x632u &&
            value != 0x640u && value != 0x622u && value != 0x642u &&
            value != 0xC32u && value != 0x822u && value != 0x440u && value != 0xC02u)
            return FUT_KELVIN_REJECTED;
        unsigned slot = (method - 0x1760u) / 4u;
        state->vertex_array_format[slot] = value;
        state->vertex_array_format_valid |= (uint16_t)(1u << slot);
        return FUT_KELVIN_APPLIED;
    }
    FutFragmentStatus fragment=fut_graphics_fragment_state(&state->fragment,method,value);
    if(fragment==FUT_FRAGMENT_APPLIED) {
        if(method>=0xA60 && method<0xAA0) fut_graphics_draw_state(&state->draw,method,value);
        return FUT_KELVIN_APPLIED;
    }
    if(fragment==FUT_FRAGMENT_REJECTED) return FUT_KELVIN_REJECTED;
    FutGraphicsDrawStatus draw = fut_graphics_draw_state(&state->draw, method, value);
    if (draw == FUT_DRAW_STATE_APPLIED) return FUT_KELVIN_APPLIED;
    if (draw == FUT_DRAW_STATE_REJECTED) return FUT_KELVIN_REJECTED;
    /* Texture description/FF texgen state is retained for the translated
     * fragment program; accepting state is not executing a texture sample. */
    if (method >= 0x3C0 && method < 0x400) {
        unsigned slot = (method - 0x3C0) / 4;
        if (value != 0 && value != 0x2400 && value != 0x2401 && value != 0x2402 && value != 0x8511 && value != 0x8512)
            return FUT_KELVIN_REJECTED;
        if ((value == 0x2402 && slot % 4 >= 2) || ((value == 0x8511 || value == 0x8512) && slot % 4 == 3))
            return FUT_KELVIN_REJECTED;
        state->textures[slot / 4].texgen[slot % 4] = value;
        return FUT_KELVIN_APPLIED;
    }
    if (method >= 0xAE0 && method < 0xAF0) {
        state->textures[(method - 0xAE0) / 4].color_key = value;
        return FUT_KELVIN_APPLIED;
    }
    if (method >= 0x1B00 && method < 0x1C00) {
        unsigned stage = (method - 0x1B00) / 64, local = method & 63;
        if (local == 0) state->textures[stage].offset = value;
        else if (local == 4) state->textures[stage].format = value;
        else if (local == 8) state->textures[stage].address = value;
        else if (local == 0xC) state->textures[stage].control0 = value;
        else if (local == 0x10) state->textures[stage].control1 = value;
        else if (local == 0x14) state->textures[stage].filter = value;
        else if (local == 0x1C) state->textures[stage].image_rect = value;
        else if (local == 0x20) state->textures[stage].palette = value;
        else if (local == 0x24) state->textures[stage].border_color = value;
        else if (local >= 0x28 && local <= 0x3C) state->textures[stage].bump_env[(local - 0x28) / 4] = value;
        else return FUT_KELVIN_NOT_HANDLED;
        state->textures[stage].valid |= 1u << (local / 4);
        return FUT_KELVIN_APPLIED;
    }
    unsigned row = 0, component = 0;
    int constant = 0;
    if (method >= 0x1940u && method < 0x1980u) {
        unsigned slot = (method - 0x1940u) / 4;
        if (!slot) return FUT_KELVIN_REJECTED; /* Would emit an inline vertex. */
        for (unsigned i = 0; i < 4; ++i)
            state->attributes[slot][i] = (float)((value >> (i * 8)) & 255u) / 255.0f;
        state->attribute_valid[slot]=15;
        return FUT_KELVIN_APPLIED;
    }
    if (method >= 0x1A00u && method < 0x1B00u) {
        unsigned slot = (method - 0x1A00u) / 4;
        if (slot < 4) return FUT_KELVIN_REJECTED;
        memcpy(&state->attributes[slot / 4][slot % 4], &value, sizeof(value));
        state->attribute_valid[slot/4]|=1u<<(slot%4);
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
    if (method >= 0x440u && method < 0x480u) {
        unsigned slot=(method-0x440u)/4;
        row=0x04+slot/4; component=slot%4; constant=1;
    } else if (method >= 0x480u && method < 0x680u) {
        unsigned inverse=method>=0x580u;
        unsigned slot=(method-(inverse?0x580u:0x480u))/4;
        row=(inverse?0x0Cu:0x08u)+(slot/16)*8+(slot%16)/4;
        component=slot%4; constant=1;
    } else if (method >= 0x680u && method < 0x6C0u) {
        unsigned slot=(method-0x680u)/4;
        row=slot/4; component=slot%4; constant=1;
    } else if (method >= 0x6C0u && method < 0x7C0u) {
        unsigned slot=(method-0x6C0u)/4;
        row=0x44+(slot/16)*8+(slot%16)/4;
        component=slot%4; constant=1;
    } else if (method >= 0xA20u && method <= 0xA2Cu) {
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
        state->constant_valid[row] |= 1u<<component;
        state->constants_dirty = 1;
        return FUT_KELVIN_APPLIED;
    }
    switch (method) {
    case 0x100:
        if (value) return FUT_KELVIN_REJECTED; /* Nonzero NOP is a software-method notification. */
        break;
    case 0x394: state->clip_min = value; break;
    case 0x398: state->clip_max = value; break;
    case 0x290: state->control0 = value; break;
    case 0x294:
        if(value & ~0x00030001u) return FUT_KELVIN_REJECTED;
        state->light_control=value; break;
    case 0x2A4:
        if(value>1) return FUT_KELVIN_REJECTED;
        state->fog_enable=value; state->draw.dirty=1; break;
    case 0x314:
        if(value>1) return FUT_KELVIN_REJECTED;
        state->lighting_enable=value; state->draw.dirty=1; break;
    case 0x3B8:
        if(value>1) return FUT_KELVIN_REJECTED;
        state->specular_enable=value; state->draw.dirty=1; break;
    case 0x3BC:
        if(value & ~0xFFFFu) return FUT_KELVIN_REJECTED;
        state->light_enable_mask=value; state->draw.dirty=1; break;
    case 0x420:case 0x424:case 0x428:case 0x42C:
        if(value>1) return FUT_KELVIN_REJECTED;
        state->texture_matrix_enable[(method-0x420)/4]=value; state->draw.dirty=1; break;
    case 0x30C:
        if (value > 1) return FUT_KELVIN_REJECTED;
        state->depth_test = value; state->draw.dirty = 1; break;
    case 0x32C:
        if (value > 1) return FUT_KELVIN_REJECTED;
        state->stencil_test = value; state->draw.dirty = 1; break;
    case 0x380: state->line_width = value; break;
    case 0x318:
        if (value > 1) return FUT_KELVIN_REJECTED;
        state->point_params_enable=value; state->draw.dirty=1; break;
    case 0x31C:
        if (value > 1) return FUT_KELVIN_REJECTED;
        state->point_smooth=value; state->draw.dirty=1; break;
    case 0x43C: state->point_size=value; state->draw.dirty=1; break;
    case 0x1D7C: state->anti_aliasing_control = value; break;
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
    case 0x1710:
        /* NV20 VTXBUF_VALIDATE: Kelvin accepts zero and resets the index
         * fetch bookkeeping (envytools pgraph_idx_unkf8). This backend has
         * no guest array/index fetch cache; immediate vertices are already
         * materialized. Array draws remain unsupported, not validated here. */
        if (value) return FUT_KELVIN_REJECTED;
        break;
    case 0x17F8:
        /* Retain the exact NV097 mode. Fragment-program mode 5 remains
         * rejected by the native shader compiler until clipping is modeled. */
        state->shader_clip_plane_mode = value; break;
    case 0x1E68:
        /* Finite slope filtering needs a native shader implementation. */
        if (value != 0x7F800000u) return FUT_KELVIN_REJECTED;
        state->shadow_slope_threshold = value; break;
    case 0x1EA4:
        if (value >= 192) return FUT_KELVIN_REJECTED;
        state->constant_load = value; break;
    case 0x120:
        if (value > 7) return FUT_KELVIN_REJECTED;
        state->flip_read = value; state->flip_valid|=1; break;
    case 0x124:
        if (value > 7) return FUT_KELVIN_REJECTED;
        state->flip_write = value; state->flip_valid|=2; break;
    case 0x128:
        if (value == 0 || value > 7) return FUT_KELVIN_REJECTED;
        state->flip_modulo = value; state->flip_valid|=4; break;
    case 0x12C:
        if(value || fut_graphics_flip_available(state)<0) return FUT_KELVIN_REJECTED;
        state->flip_write=(state->flip_write+1)%state->flip_modulo; break;
    case 0x1D90:
        state->clear_color = value; state->clear_valid |= 2; break;
    case 0x1D8C: state->clear_depth = value; state->clear_valid |= 1; break;
    case 0x1D98: state->clear_x = value & 0x0FFF0FFFu; state->clear_valid |= 4; break;
    case 0x1D9C: state->clear_y = value & 0x0FFF0FFFu; state->clear_valid |= 8; break;
    default:
        return FUT_KELVIN_NOT_HANDLED;
    }
    return FUT_KELVIN_APPLIED;
}

int fut_graphics_flip_available(const FutGraphicsKelvinState *state)
{
    if(!state || state->flip_valid!=7 || !state->flip_modulo || state->flip_modulo>7 ||
        state->flip_read>=state->flip_modulo || state->flip_write>=state->flip_modulo) return -1;
    return state->flip_read!=state->flip_write;
}
