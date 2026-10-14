#include "graphics_kelvin_state.h"
#include <stdio.h>
#include <string.h>

int main(void)
{
    FutGraphicsKelvinState state = {0};
    if (fut_graphics_kelvin_state(&state, 0x9FC, 1) != FUT_KELVIN_APPLIED || !state.provoking_first) return 1;
    if (fut_graphics_kelvin_state(&state, 0x9FC, 0) != FUT_KELVIN_APPLIED || state.provoking_first) return 2;
    /* The four texgen planes have gaps occupied by texture matrices; they
     * must not be flattened to one contiguous range in the constants. */
    for (unsigned stage = 0; stage < 4; ++stage) {
        for (unsigned word = 0; word < 16; ++word) {
            uint32_t value = 0xA0000000u + stage * 16 + word;
            if (fut_graphics_kelvin_state(&state, 0x840 + stage * 64 + word * 4, value) != FUT_KELVIN_APPLIED ||
                state.constants[0x40 + stage * 8 + word / 4][word % 4] != value) return 3;
        }
        for (unsigned word = 0; word < 16; ++word)
            if (state.constants[0x44 + stage * 8 + word / 4][word % 4]) return 4;
    }
    if (fut_graphics_kelvin_state(&state, 0xA5C, 0x7FC01234u) != FUT_KELVIN_APPLIED ||
        state.constants[0x38][3] != 0x7FC01234u ||
        fut_graphics_kelvin_state(&state, 0x9D8, 0x80000000u) != FUT_KELVIN_APPLIED ||
        state.constants[0x39][2] != 0x80000000u) return 5;
    if (fut_graphics_kelvin_state(&state, 0x1EA4, 60) != FUT_KELVIN_APPLIED) return 6;
    for (unsigned i = 0; i < 12; ++i) {
        if (fut_graphics_kelvin_state(&state, 0xB80 + i * 4, 100 + i) != FUT_KELVIN_APPLIED ||
            state.constants[60 + i / 4][i % 4] != 100 + i) return 7;
    }
    if (state.constant_load != 63) return 8;
    if (fut_graphics_kelvin_state(&state, 0x1EA4, 191) != FUT_KELVIN_APPLIED) return 9;
    for (unsigned i = 0; i < 4; ++i)
        if (fut_graphics_kelvin_state(&state, 0xB80 + i * 4, i) != FUT_KELVIN_APPLIED) return 10;
    FutGraphicsKelvinState before = state;
    if (fut_graphics_kelvin_state(&state, 0xB90, 0) != FUT_KELVIN_REJECTED ||
        memcmp(&before, &state, sizeof(state))) return 11;
    const uint32_t rejected[][2] = {{0x9FC,2},{0x128,0},{0x1EA4,192},{0x1E68,0},{0x16BC,2}};
    for (unsigned i = 0; i < sizeof(rejected)/sizeof(rejected[0]); ++i)
        if (fut_graphics_kelvin_state(&state, rejected[i][0], rejected[i][1]) != FUT_KELVIN_REJECTED ||
            memcmp(&before, &state, sizeof(state))) return 12;
    if (fut_graphics_kelvin_state(&state, 0x314, 0) != FUT_KELVIN_NOT_HANDLED ||
        memcmp(&before, &state, sizeof(state))) return 13;
    if (fut_graphics_kelvin_state(&state, 0x194C, 0xFF804000u) != FUT_KELVIN_APPLIED ||
        state.attributes[3][0] != 0 || state.attributes[3][1] != 64.0f/255.0f ||
        state.attributes[3][2] != 128.0f/255.0f || state.attributes[3][3] != 1) return 14;
    uint32_t attribute_bits = 0;
    if (fut_graphics_kelvin_state(&state, 0x1A1C, 0x80000000u) != FUT_KELVIN_APPLIED) return 15;
    memcpy(&attribute_bits, &state.attributes[1][3], sizeof(attribute_bits));
    if (attribute_bits != 0x80000000u) return 16;
    if (fut_graphics_kelvin_state(&state, 0x1E9C, 135) != FUT_KELVIN_APPLIED) return 17;
    for (unsigned i = 0; i < 4; ++i)
        if (fut_graphics_kelvin_state(&state, 0xB00+i*4, i+1) != FUT_KELVIN_APPLIED ||
            state.program[135][i] != i+1) return 18;
    if (state.program_load != 136 || state.program_valid[135] != 15 || !state.program_dirty) return 19;
    before = state;
    const uint32_t invalid_new[][2] = {{0xB10,0},{0x1E9C,136},{0x1EA0,136},
        {0x1E98,1},{0x1E94,3},{0x1940,0},{0x1A00,0},{0x100,1}};
    for (unsigned i = 0; i < sizeof(invalid_new)/sizeof(invalid_new[0]); ++i)
        if (fut_graphics_kelvin_state(&state, invalid_new[i][0], invalid_new[i][1]) != FUT_KELVIN_REJECTED ||
            memcmp(&before, &state, sizeof(state))) return 20;
    if (fut_graphics_kelvin_state(&state, 0x100, 0) != FUT_KELVIN_APPLIED ||
        memcmp(&before, &state, sizeof(state))) return 21;
    if (fut_graphics_kelvin_state(&state, 0xA20, 0x7FC01234u) != FUT_KELVIN_APPLIED ||
        state.constants[0x3B][0] != 0x7FC01234u ||
        fut_graphics_kelvin_state(&state, 0xAF0, 0x80000000u) != FUT_KELVIN_APPLIED ||
        state.constants[0x3A][0] != 0x80000000u) return 22;
    if (fut_graphics_kelvin_state(&state, 0x30C, 1) != FUT_KELVIN_APPLIED || !state.depth_test ||
        fut_graphics_kelvin_state(&state, 0x32C, 1) != FUT_KELVIN_APPLIED || !state.stencil_test ||
        fut_graphics_kelvin_state(&state, 0x290, 0x110001) != FUT_KELVIN_APPLIED || state.control0 != 0x110001 ||
        fut_graphics_kelvin_state(&state, 0x380, 8) != FUT_KELVIN_APPLIED || state.line_width != 8 ||
        fut_graphics_kelvin_state(&state, 0x1D7C, 0xFFFF0001) != FUT_KELVIN_APPLIED ||
        state.anti_aliasing_control != 0xFFFF0001) return 23;
    before = state;
    if (fut_graphics_kelvin_state(&state, 0x30C, 2) != FUT_KELVIN_REJECTED ||
        fut_graphics_kelvin_state(&state, 0x32C, 2) != FUT_KELVIN_REJECTED ||
        memcmp(&before, &state, sizeof(state))) return 24;
    if (fut_graphics_kelvin_state(&state, 0x2C0, 0x00020001) != FUT_KELVIN_APPLIED ||
        fut_graphics_kelvin_state(&state, 0x2E0, 0x00040003) != FUT_KELVIN_APPLIED) return 25;
    for (unsigned i = 0; i < 8; ++i)
        if (state.draw.window_x[i] != 0x00020001 || state.draw.window_y[i] != 0x00040003) return 26;
    if (fut_graphics_kelvin_state(&state, 0x2CC, 0x00060005) != FUT_KELVIN_APPLIED ||
        state.draw.window_x[2] != 0x00020001 || state.draw.window_x[3] != 0x00060005 ||
        state.draw.window_x[7] != 0x00060005 || !fut_graphics_window_contains(&state.draw, 2, 4) ||
        !fut_graphics_window_contains(&state.draw, 6, 3) || fut_graphics_window_contains(&state.draw, 3, 3)) return 27;
    if (fut_graphics_kelvin_state(&state, 0x2B4, 1) != FUT_KELVIN_APPLIED ||
        fut_graphics_window_contains(&state.draw, 2, 4) || !fut_graphics_window_contains(&state.draw, 3, 3)) return 28;
    before = state;
    const uint32_t invalid_draw[][2] = {{0x2B4,2},{0x354,0x208},{0x344,0x999},{0x370,0x1E04},
        {0x39C,0x406},{0x38C,0x1B03},{0x3A0,0x902},{0x1D84,4},{0x1D78,0x20},{0x300,2}};
    for (unsigned i = 0; i < sizeof(invalid_draw)/sizeof(invalid_draw[0]); ++i)
        if (fut_graphics_kelvin_state(&state, invalid_draw[i][0], invalid_draw[i][1]) != FUT_KELVIN_REJECTED ||
            memcmp(&before, &state, sizeof(state))) return 29;
    for (unsigned stage=0;stage<4;++stage) {
        if (fut_graphics_kelvin_state(&state,0x1B0C+stage*64,0)!=FUT_KELVIN_APPLIED ||
            fut_graphics_kelvin_state(&state,0xAE0+stage*4,0x12340000+stage)!=FUT_KELVIN_APPLIED ||
            state.textures[stage].color_key!=0x12340000+stage) return 30;
        for (unsigned word=0;word<6;++word)
            if (fut_graphics_kelvin_state(&state,0x1B28+stage*64+word*4,0x80000000+word)!=FUT_KELVIN_APPLIED ||
                state.textures[stage].bump_env[word]!=0x80000000+word) return 31;
        for (unsigned word=0;word<4;++word)
            if (fut_graphics_kelvin_state(&state,0x3C0+stage*16+word*4,0)!=FUT_KELVIN_APPLIED) return 32;
    }
    before=state;
    if (fut_graphics_kelvin_state(&state,0x3C0,1)!=FUT_KELVIN_REJECTED ||
        fut_graphics_kelvin_state(&state,0x3C8,0x2402)!=FUT_KELVIN_REJECTED ||
        fut_graphics_kelvin_state(&state,0x3CC,0x8511)!=FUT_KELVIN_REJECTED || memcmp(&before,&state,sizeof(state))) return 33;
    puts("Native initial-state slots, bit preservation, load sequencing and rejection OK");
    return 0;
}
