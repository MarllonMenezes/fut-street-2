#ifndef FUT_GRAPHICS_VERTEX_PROGRAM_H
#define FUT_GRAPHICS_VERTEX_PROGRAM_H
#include <stddef.h>
#include <stdint.h>
/* Complete float4 attributes required by the supported program. Failure
 * clears mask. Conservative when only some swizzled components are used. */
int fut_vertex_program_input_mask(const uint32_t (*words)[4],size_t count,uint16_t *mask);
/* Recompile original instruction words to native HLSL. Unsupported operands,
 * constant writes, opcodes or incomplete programs fail instead of becoming NOPs. */
int fut_vertex_program_hlsl(const uint32_t (*words)[4], size_t count,
    char *text, size_t capacity, size_t *length);
#endif
