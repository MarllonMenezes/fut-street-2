#ifndef FUT_GRAPHICS_IMMEDIATE_H
#define FUT_GRAPHICS_IMMEDIATE_H
#include "graphics_kelvin_state.h"
#define FUT_IMMEDIATE_CAPACITY 256u
typedef struct {
    float vertices[FUT_IMMEDIATE_CAPACITY][16][4];
    uint16_t defined[FUT_IMMEDIATE_CAPACITY];
    unsigned count,primitive,pending_x;
    int active,ready;
} FutGraphicsImmediate;
typedef enum {FUT_IMMEDIATE_APPLIED,FUT_IMMEDIATE_READY,FUT_IMMEDIATE_NOT_HANDLED,FUT_IMMEDIATE_REJECTED} FutImmediateStatus;
/* Observed BEGIN(TRIANGLES)/ATTR2F/END form. No rasterization or completion.
 * X/Y pairs are required; missing Z/W of completed2F are exactly0/1.
 * Attribute0.Y emits. Rejection preserves batch and current attributes. */
FutImmediateStatus fut_graphics_immediate_method(FutGraphicsImmediate *batch,
    FutGraphicsKelvinState *state,uint32_t method,uint32_t value);
/* Only after actual execution/completion, not merely upload. */
int fut_graphics_immediate_consume(FutGraphicsImmediate *batch);
#endif
