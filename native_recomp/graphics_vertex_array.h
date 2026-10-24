#ifndef FUT_GRAPHICS_VERTEX_ARRAY_H
#define FUT_GRAPHICS_VERTEX_ARRAY_H
#include "graphics_immediate.h"
#include "graphics_objects.h"

#define FUT_VERTEX_ARRAY_INDEX_CAPACITY 4096u
typedef struct {
    uint32_t indices[FUT_VERTEX_ARRAY_INDEX_CAPACITY];
    uint32_t inline_words[FUT_IMMEDIATE_CAPACITY*6u];
    unsigned count, inline_count, input_mode, active, ready, primitive;
} FutGraphicsVertexArray;

void fut_graphics_vertex_array_reset(FutGraphicsVertexArray *array);
int fut_graphics_vertex_array_begin(FutGraphicsVertexArray *array, unsigned primitive);
int fut_graphics_vertex_array_begin_observed_lines(FutGraphicsVertexArray *array);
int fut_graphics_vertex_array_indices(FutGraphicsVertexArray *array, uint32_t packed);
int fut_graphics_vertex_array_index32(FutGraphicsVertexArray *array, uint32_t index);
int fut_graphics_vertex_array_inline(FutGraphicsVertexArray *array, uint32_t word);
int fut_graphics_vertex_array_end(FutGraphicsVertexArray *array);
/* Transactionally builds the existing triangle-list input. Only the observed
 * S16x3/stride-6 position profile or observed tightly packed float3/float2,
 * ARGB8 and S16 scalar descriptors in their original slots are admitted.
 * Success defines only [0,count) vertex/mask entries and batch metadata;
 * inactive capacity is untouched. Failure preserves the entire output. */
int fut_graphics_vertex_array_build(const FutGraphicsVertexArray *array,
    FutGraphicsImmediate *output, FutGraphicsMemoryRead read, void *memory_context,
    const FutGraphicsDmaView *view_a, const FutGraphicsDmaView *view_b,
    const uint32_t formats[16], const uint32_t offsets[16],
    const float attributes[16][4], const uint8_t attribute_valid[16],
    uint16_t required_mask);
/* Observed INLINE_ARRAY tuple: float3 position, ARGB8 color, float2 UV.
 * Expands triangle-strip winding into the existing native triangle-list input. */
int fut_graphics_vertex_array_build_inline(const FutGraphicsVertexArray *array,
    FutGraphicsImmediate *output, const uint32_t formats[16],
    const float attributes[16][4], const uint8_t attribute_valid[16],
    uint16_t required_mask);
/* Expands captured 16/4-segment line lists with exact indices and bounded
 * S16x3 fetches. The 4-segment profile retains its captured fixed offset. */
int fut_graphics_vertex_array_build_observed_lines(const FutGraphicsVertexArray *array,
    FutGraphicsImmediate *output, FutGraphicsMemoryRead read, void *memory_context,
    const FutGraphicsDmaView *view_a, const FutGraphicsDmaView *view_b,
    const uint32_t formats[16], const uint32_t offsets[16],
    const float attributes[16][4], const uint8_t attribute_valid[16],
    uint16_t required_mask);
/* Read-only capture for the single observed LINES packet. This does not
 * assemble or submit a primitive; it only snapshots its 16 S32K positions. */
int fut_graphics_vertex_array_capture_observed_lines(FutGraphicsMemoryRead read,
    void *memory_context, const FutGraphicsDmaView *view_a,
    const FutGraphicsDmaView *view_b, uint32_t format, uint32_t offset,
    uint8_t output[96]);
int fut_graphics_vertex_array_consume(FutGraphicsVertexArray *array);
#endif
