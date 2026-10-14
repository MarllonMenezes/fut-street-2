#include "graphics_surfaces.h"
#include <string.h>

int fut_graphics_surface_state(FutGraphicsSurfaceState *state, uint32_t method, uint32_t value)
{
    if (!state || method < 0x200 || method > 0x214 || (method & 3)) return 0;
    unsigned slot = (method - 0x200) / 4;
    switch (slot) {
    case 0: state->horizontal = value; break;
    case 1: state->vertical = value; break;
    case 2: state->format = value; break;
    case 3: state->pitch = value; break;
    case 4: state->color_offset = value; break;
    case 5: state->depth_offset = value; break;
    }
    state->valid |= 1u << slot;
    return 1;
}
static int region(const FutGraphicsDmaView *view, uint32_t offset, uint32_t pitch,
    uint32_t width, uint32_t height, uint32_t *address, uint32_t *span)
{
    uint64_t row = (uint64_t)width * 4;
    uint64_t bytes = (uint64_t)(height - 1) * pitch + row;
    if ((view->guest_address & 0xFC000000u) != 0x80000000u || ((view->guest_address | offset) & 3) ||
        !view->size || view->size > 0x04000000u - (view->guest_address & 0x03FFFFFFu) ||
        pitch < row || (pitch & 3) || offset > view->size || bytes > view->size - offset) return 0;
    *address = view->guest_address + offset; *span = (uint32_t)bytes;
    return 1;
}
FutGraphicsSurfaceStatus fut_graphics_surface_plan(const FutGraphicsSurfaceState *state,
    const FutGraphicsDmaView *color, const FutGraphicsDmaView *depth, FutGraphicsSurfacePlan *plan)
{
    if (!state || !color || !depth || !plan) return FUT_SURFACE_INVALID_RANGE;
    if (state->valid != 63) return FUT_SURFACE_INCOMPLETE;
    unsigned color_format = state->format & 15, zeta = (state->format >> 4) & 15;
    unsigned type = (state->format >> 8) & 15, aa = (state->format >> 12) & 15;
    if (color_format != 8 || zeta != 2 || type != 1 || aa > 2 ||
        (state->horizontal & 0xFFFF) || (state->vertical & 0xFFFF)) return FUT_SURFACE_UNSUPPORTED;
    FutGraphicsSurfacePlan result = {0};
    result.logical_width = state->horizontal >> 16;
    result.logical_height = state->vertical >> 16;
    result.width = result.logical_width * (aa ? 2 : 1);
    result.height = result.logical_height * (aa == 2 ? 2 : 1);
    result.samples = 1u << aa;
    result.color_pitch = state->pitch & 0xFFFF;
    result.depth_pitch = state->pitch >> 16;
    /* D3D11 texture dimension limit; bounds remain validated against DMA. */
    if (!result.width || !result.height || result.width > 16384 || result.height > 16384 ||
        !region(color, state->color_offset, result.color_pitch, result.width, result.height,
            &result.color_address, &result.color_span) ||
        !region(depth, state->depth_offset, result.depth_pitch, result.width, result.height,
            &result.depth_address, &result.depth_span)) return FUT_SURFACE_INVALID_RANGE;
    uint64_t color_end = (uint64_t)result.color_address + result.color_span;
    uint64_t depth_end = (uint64_t)result.depth_address + result.depth_span;
    if (result.color_address < depth_end && result.depth_address < color_end)
        return FUT_SURFACE_UNSUPPORTED; /* Aliased attachments require coherency handling. */
    *plan = result;
    return FUT_SURFACE_READY;
}
uint32_t fut_graphics_depth_to_native(uint32_t guest) { return (guest >> 8) | (guest << 24); }
uint32_t fut_graphics_depth_to_guest(uint32_t native) { return (native << 8) | (native >> 24); }
