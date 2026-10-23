#ifndef FUT_GRAPHICS_IMMEDIATE_H
#define FUT_GRAPHICS_IMMEDIATE_H
#include "graphics_kelvin_state.h"
#define FUT_IMMEDIATE_CAPACITY 256u
/* Observed scene strip contains 2315 indices; 2560 expand to 7674 vertices.
 * Immediate-method emission remains separately capped at 256 inputs. */
#define FUT_IMMEDIATE_DRAW_CAPACITY 7680u
typedef struct {
    float vertices[FUT_IMMEDIATE_DRAW_CAPACITY][16][4];
    uint16_t defined[FUT_IMMEDIATE_DRAW_CAPACITY];
    unsigned count,primitive,pending_x,pending_position;
    int active,ready;
    uint32_t inline_words[6];
    unsigned inline_pending, input_kind; /* 1=attribute methods, 2=inline array. */
} FutGraphicsImmediate;
typedef enum {FUT_IMMEDIATE_APPLIED,FUT_IMMEDIATE_READY,FUT_IMMEDIATE_NOT_HANDLED,FUT_IMMEDIATE_REJECTED} FutImmediateStatus;
/* Observed BEGIN(TRIANGLES)/ATTR2F or INLINE_ARRAY(float4,ARGB8), and
 * BEGIN(QUADS)/SET_VERTEX4F or INLINE_ARRAY(float3,ARGB8,float2) forms.
 * Quads expand to triangle-list vertices; no rasterization or completion. */
FutImmediateStatus fut_graphics_immediate_method(FutGraphicsImmediate *batch,
    FutGraphicsKelvinState *state,uint32_t method,uint32_t value);
/* Only after actual execution/completion, not merely upload. */
int fut_graphics_immediate_consume(FutGraphicsImmediate *batch);
#endif
