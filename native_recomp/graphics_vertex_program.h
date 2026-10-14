#ifndef FUT_GRAPHICS_VERTEX_PROGRAM_H
#define FUT_GRAPHICS_VERTEX_PROGRAM_H
#include <stddef.h>
#include <stdint.h>
/* Recompile original instruction words to native HLSL. Unsupported operands,
 * constant writes, opcodes or incomplete programs fail instead of becoming NOPs. */
int fut_vertex_program_hlsl(const uint32_t (*words)[4], size_t count,
    char *text, size_t capacity, size_t *length);
#endif
