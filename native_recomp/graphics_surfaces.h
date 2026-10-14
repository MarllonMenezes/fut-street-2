#ifndef FUT_GRAPHICS_SURFACES_H
#define FUT_GRAPHICS_SURFACES_H
#include "graphics_objects.h"

typedef struct {
    uint32_t horizontal, vertical, format, pitch, color_offset, depth_offset;
    unsigned valid;
} FutGraphicsSurfaceState;
typedef struct {
    uint32_t color_address, depth_address, color_pitch, depth_pitch;
    uint32_t color_span, depth_span, width, height, logical_width, logical_height;
    unsigned samples;
} FutGraphicsSurfacePlan;
typedef enum { FUT_SURFACE_READY, FUT_SURFACE_INCOMPLETE, FUT_SURFACE_UNSUPPORTED,
    FUT_SURFACE_INVALID_RANGE } FutGraphicsSurfaceStatus;

/* Applies resource-description state only; no allocation or completion. */
int fut_graphics_surface_state(FutGraphicsSurfaceState *state, uint32_t method, uint32_t value);
/* Current implemented layout: pitched A8R8G8B8 / integer Z24S8, including
 * expanded sample storage. This plan does not claim MSAA rasterization. */
FutGraphicsSurfaceStatus fut_graphics_surface_plan(const FutGraphicsSurfaceState *state,
    const FutGraphicsDmaView *color, const FutGraphicsDmaView *depth, FutGraphicsSurfacePlan *plan);
uint32_t fut_graphics_depth_to_native(uint32_t guest);
uint32_t fut_graphics_depth_to_guest(uint32_t native);
#endif
