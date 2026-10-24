#include "graphics_kelvin_state.h"
#include <stdio.h>
#include <string.h>

int main(void)
{
    FutGraphicsKelvinState state = {0};
    const unsigned offset_slots[] = {0, 3, 9};
    const uint32_t offset_values[] = {0x00166574u, 0x00166580u, 0x00166584u};
    FutGraphicsKelvinState observed_offsets = {0};
    uint16_t expected_offset_mask = 0;
    for (unsigned i=0;i<sizeof(offset_slots)/sizeof(offset_slots[0]);++i) {
        unsigned slot = offset_slots[i];
        expected_offset_mask |= (uint16_t)(1u << slot);
        if (fut_graphics_kelvin_state(&observed_offsets, 0x1720u + slot * 4u, offset_values[i]) != FUT_KELVIN_APPLIED ||
            observed_offsets.vertex_array_offset[slot] != offset_values[i] ||
            observed_offsets.vertex_array_offset_valid != expected_offset_mask) return 68;
    }
    FutGraphicsKelvinState observed_offsets_before = observed_offsets;
    if (fut_graphics_kelvin_state(&observed_offsets, 0x171Cu, 0) != FUT_KELVIN_NOT_HANDLED ||
        fut_graphics_kelvin_state(&observed_offsets, 0x1760u, 0x635u) != FUT_KELVIN_APPLIED ||
        observed_offsets.vertex_array_offset_valid != expected_offset_mask ||
        fut_graphics_kelvin_state(&observed_offsets, 0x1721u, 0x00166574u) != FUT_KELVIN_REJECTED ||
        observed_offsets.vertex_array_offset_valid != expected_offset_mask ||
        memcmp(&observed_offsets_before.vertex_array_offset,
            &observed_offsets.vertex_array_offset, sizeof(observed_offsets.vertex_array_offset)) ||
        memcmp(&observed_offsets_before.vertex_array_offset_valid,
            &observed_offsets.vertex_array_offset_valid, sizeof(observed_offsets.vertex_array_offset_valid))) return 69;
    const uint32_t eye_vector[3] = {0, 0, 0};
    for (unsigned i = 0; i < 3; ++i) {
        if (fut_graphics_kelvin_state(&state, 0x181Cu + i * 4u, eye_vector[i]) != FUT_KELVIN_APPLIED ||
            state.eye_vector[i] != eye_vector[i] ||
            state.eye_vector_valid != (uint8_t)((1u << (i + 1u)) - 1u)) return 64;
    }
    FutGraphicsKelvinState eye_vector_before = state;
    if (fut_graphics_kelvin_state(&state, 0x1818u, 0) != FUT_KELVIN_NOT_HANDLED ||
        fut_graphics_kelvin_state(&state, 0x1828u, 0) != FUT_KELVIN_NOT_HANDLED ||
        fut_graphics_kelvin_state(&state, 0x181Du, 0) != FUT_KELVIN_REJECTED ||
        memcmp(&eye_vector_before, &state, sizeof(state))) return 65;
    for (unsigned i=0;i<16;++i) {
        uint32_t format=i==0?0x32u:i==3?0x40u:i==9?0x22u:2u;
        if(fut_graphics_kelvin_state(&state,0x1760+i*4,format)!=FUT_KELVIN_APPLIED ||
            state.vertex_array_format[i]!=format ||
            state.vertex_array_format_valid!=(uint16_t)((1u<<(i+1))-1u)) return 62;
    }
    FutGraphicsKelvinState observed_array_formats = {0};
    for (unsigned i=0;i<16;++i) {
        uint32_t format=i==0?0x635u:0x602u;
        if(fut_graphics_kelvin_state(&observed_array_formats,0x1760+i*4,format)!=FUT_KELVIN_APPLIED ||
            observed_array_formats.vertex_array_format[i]!=format ||
            observed_array_formats.vertex_array_format_valid!=(uint16_t)((1u<<(i+1))-1u)) return 66;
    }
    const uint32_t captured_formats[16]={0x632u,0x602u,0x602u,0x640u,
        0x602u,0x602u,0x602u,0x602u,0x602u,0x622u,0x602u,0x602u,
        0x602u,0x602u,0x602u,0x602u};
    FutGraphicsKelvinState captured_array_formats={0};
    for(unsigned i=0;i<16;++i)
        if(fut_graphics_kelvin_state(&captured_array_formats,0x1760+i*4,captured_formats[i])!=FUT_KELVIN_APPLIED ||
            captured_array_formats.vertex_array_format[i]!=captured_formats[i] ||
            captured_array_formats.vertex_array_format_valid!=(uint16_t)((1u<<(i+1))-1u)) return 70;
    FutGraphicsKelvinState captured_formats_before=captured_array_formats;
    for(unsigned i=0;i<16;++i) {
        uint32_t format=i==0?0xC32u:i==1?0x822u:i==2?0x440u:0xC02u;
        if(fut_graphics_kelvin_state(&state,0x1760+i*4,format)!=FUT_KELVIN_APPLIED ||
            state.vertex_array_format[i]!=format) return 72;
    }
    const uint32_t captured_near_misses[]={0x631u,0x633u,0x641u,0x63Fu,0x621u,0x623u};
    for(unsigned i=0;i<sizeof(captured_near_misses)/sizeof(captured_near_misses[0]);++i)
        if(fut_graphics_kelvin_state(&captured_array_formats,0x1760,captured_near_misses[i])!=FUT_KELVIN_REJECTED ||
            memcmp(&captured_formats_before,&captured_array_formats,sizeof(captured_array_formats))) return 71;
    FutGraphicsKelvinState observed_formats_before = observed_array_formats;
    const uint32_t unobserved_formats[] = {0x634u, 0x636u, 0x603u};
    for (unsigned i=0;i<sizeof(unobserved_formats)/sizeof(unobserved_formats[0]);++i)
        if(fut_graphics_kelvin_state(&observed_array_formats,0x1760,unobserved_formats[i])!=FUT_KELVIN_REJECTED ||
            memcmp(&observed_formats_before,&observed_array_formats,sizeof(observed_array_formats))) return 67;
    FutGraphicsKelvinState format_before=state;
    const uint32_t strided_formats[]={0x10032,0x1032,0xC40,0xC16,0x415};
    for(unsigned i=0;i<sizeof(strided_formats)/sizeof(strided_formats[0]);++i)
        if(fut_graphics_kelvin_state(&state,0x1760,strided_formats[i])!=FUT_KELVIN_APPLIED ||
            state.vertex_array_format[0]!=strided_formats[i])return 73;
    format_before=state;
    const uint32_t bad_formats[]={0,0x12,0x42,0x132,0xA32,0x340,0xFFFFFFFFu};
    for(unsigned i=0;i<sizeof(bad_formats)/sizeof(bad_formats[0]);++i)
        if(fut_graphics_kelvin_state(&state,0x1760,bad_formats[i])!=FUT_KELVIN_REJECTED ||
            memcmp(&format_before,&state,sizeof(state))) return 63;
    if (fut_graphics_kelvin_state(&state, 0x17F8, 0) != FUT_KELVIN_APPLIED ||
        state.shader_clip_plane_mode != 0) return 52;
    if (fut_graphics_kelvin_state(&state, 0x17F8, 0xA5A55A5Au) != FUT_KELVIN_APPLIED ||
        state.shader_clip_plane_mode != 0xA5A55A5Au) return 53;
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
    if (fut_graphics_kelvin_state(&state, 0x1710, 0) != FUT_KELVIN_APPLIED ||
        memcmp(&before, &state, sizeof(state))) return 60;
    if (fut_graphics_kelvin_state(&state, 0x1710, 1) != FUT_KELVIN_REJECTED ||
        memcmp(&before, &state, sizeof(state)) ||
        fut_graphics_kelvin_state(&state, 0x1710, 0xFFFFFFFFu) != FUT_KELVIN_REJECTED ||
        memcmp(&before, &state, sizeof(state))) return 61;
    if (fut_graphics_kelvin_state(&state, 0xB90, 0) != FUT_KELVIN_REJECTED ||
        memcmp(&before, &state, sizeof(state))) return 11;
    const uint32_t rejected[][2] = {{0x9FC,2},{0x128,0},{0x1EA4,192},{0x1E68,0},{0x16BC,2}};
    for (unsigned i = 0; i < sizeof(rejected)/sizeof(rejected[0]); ++i)
        if (fut_graphics_kelvin_state(&state, rejected[i][0], rejected[i][1]) != FUT_KELVIN_REJECTED ||
            memcmp(&before, &state, sizeof(state))) return 12;
    if (fut_graphics_kelvin_state(&state, 0x1FFC, 0) != FUT_KELVIN_NOT_HANDLED ||
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
    for(unsigned stage=0;stage<4;++stage) {
        const uint32_t offsets[]={0,4,8,0x10,0x14,0x1C,0x20};
        for(unsigned i=0;i<7;++i)
            if(fut_graphics_kelvin_state(&state,0x1B00+stage*64+offsets[i],0x80000000+stage*16+i)!=FUT_KELVIN_APPLIED) return 34;
        const FutGraphicsTextureState *t=&state.textures[stage];
        if(t->offset!=0x80000000+stage*16 || t->format!=0x80000001+stage*16 ||
            t->address!=0x80000002+stage*16 || t->control1!=0x80000003+stage*16 ||
            t->filter!=0x80000004+stage*16 || t->image_rect!=0x80000005+stage*16 ||
            t->palette!=0x80000006+stage*16 || (t->valid & 0x1B7)!=0x1B7) return 35;
    }
    before=state;
    if(fut_graphics_kelvin_state(&state,0x1B18,0)!=FUT_KELVIN_NOT_HANDLED ||
        fut_graphics_kelvin_state(&state,0x1B01,0)!=FUT_KELVIN_REJECTED || memcmp(&before,&state,sizeof(state))) return 36;
    if(fut_graphics_kelvin_state(&state,0x318,1)!=FUT_KELVIN_APPLIED || !state.point_params_enable ||
        fut_graphics_kelvin_state(&state,0x31C,1)!=FUT_KELVIN_APPLIED || !state.point_smooth ||
        fut_graphics_kelvin_state(&state,0x43C,8)!=FUT_KELVIN_APPLIED || state.point_size!=8) return 37;
    before=state;
    if(fut_graphics_kelvin_state(&state,0x318,2)!=FUT_KELVIN_REJECTED ||
        fut_graphics_kelvin_state(&state,0x31C,2)!=FUT_KELVIN_REJECTED || memcmp(&before,&state,sizeof(state))) return 38;
    FutGraphicsKelvinState matrices={0};
    const unsigned matrix_methods[]={0x480,0x580,0x6C0};
    const unsigned context_bases[]={0x08,0x0C,0x44};
    for(unsigned family=0;family<3;++family) for(unsigned matrix=0;matrix<4;++matrix)
        for(unsigned word=0;word<16;++word) {
            uint32_t value=0x7FC00000u+family*256+matrix*16+word;
            unsigned row=context_bases[family]+matrix*8+word/4;
            if(fut_graphics_kelvin_state(&matrices,matrix_methods[family]+matrix*64+word*4,value)!=FUT_KELVIN_APPLIED ||
                matrices.constants[row][word%4]!=value || !(matrices.constant_valid[row]&(1u<<(word%4)))) return 39;
        }
    for(unsigned matrix=0;matrix<4;++matrix) for(unsigned row=0;row<4;++row)
        if(matrices.constant_valid[0x40+matrix*8+row] || matrices.constants[0x40+matrix*8+row][0]) return 40;
    for(unsigned family=0;family<2;++family) for(unsigned word=0;word<16;++word) {
        unsigned base=family?0:4,method=family?0x680:0x440;
        uint32_t value=0x80000000u+family*16+word;
        if(fut_graphics_kelvin_state(&matrices,method+word*4,value)!=FUT_KELVIN_APPLIED ||
            matrices.constants[base+word/4][word%4]!=value) return 41;
    }
    before=matrices;
    if(fut_graphics_kelvin_state(&matrices,0x7C0,0)!=FUT_KELVIN_NOT_HANDLED ||
        fut_graphics_kelvin_state(&matrices,0x481,0)!=FUT_KELVIN_REJECTED || memcmp(&before,&matrices,sizeof(matrices))) return 42;
    const unsigned enables[]={0x2A4,0x314,0x3B8,0x420,0x424,0x428,0x42C};
    for(unsigned i=0;i<sizeof(enables)/sizeof(enables[0]);++i) {
        if(fut_graphics_kelvin_state(&matrices,enables[i],1)!=FUT_KELVIN_APPLIED) return 43;
        before=matrices;
        if(fut_graphics_kelvin_state(&matrices,enables[i],2)!=FUT_KELVIN_REJECTED || memcmp(&before,&matrices,sizeof(matrices))) return 44;
    }
    if(!matrices.fog_enable || !matrices.lighting_enable || !matrices.specular_enable ||
        !matrices.texture_matrix_enable[0] || !matrices.texture_matrix_enable[3] ||
        fut_graphics_kelvin_state(&matrices,0x294,0x20001)!=FUT_KELVIN_APPLIED || matrices.light_control!=0x20001 ||
        fut_graphics_kelvin_state(&matrices,0x3BC,0xFFFF)!=FUT_KELVIN_APPLIED || matrices.light_enable_mask!=0xFFFF) return 45;
    before=matrices;
    if(fut_graphics_kelvin_state(&matrices,0x294,2)!=FUT_KELVIN_REJECTED ||
        fut_graphics_kelvin_state(&matrices,0x3BC,0x10000)!=FUT_KELVIN_REJECTED || memcmp(&before,&matrices,sizeof(matrices))) return 46;
    FutGraphicsKelvinState flip={0};
    if(fut_graphics_flip_available(&flip)!=-1 || fut_graphics_kelvin_state(&flip,0x12C,0)!=FUT_KELVIN_REJECTED) return 47;
    fut_graphics_kelvin_state(&flip,0x120,0);fut_graphics_kelvin_state(&flip,0x124,0);fut_graphics_kelvin_state(&flip,0x128,3);
    if(fut_graphics_flip_available(&flip)!=0) return 48;
    for(unsigned i=1;i<=3;++i)
        if(fut_graphics_kelvin_state(&flip,0x12C,0)!=FUT_KELVIN_APPLIED || flip.flip_write!=i%3 || flip.flip_read!=0 ||
            fut_graphics_flip_available(&flip)!=(i<3)) return 49;
    before=flip;
    if(fut_graphics_kelvin_state(&flip,0x12C,1)!=FUT_KELVIN_REJECTED || memcmp(&before,&flip,sizeof(flip))) return 50;
    fut_graphics_kelvin_state(&flip,0x124,7);before=flip;
    if(fut_graphics_flip_available(&flip)!=-1 || fut_graphics_kelvin_state(&flip,0x12C,0)!=FUT_KELVIN_REJECTED ||
        memcmp(&before,&flip,sizeof(flip))) return 51;
    puts("Native state slots, matrix context gaps, raw validity, flip free-slot protocol and rejection OK");
    return 0;
}
