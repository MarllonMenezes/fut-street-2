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
/* Raster-space variant: VS b3=float4(logicalWidth,logicalHeight,depthMax,0).
 * TEXCOORD6 retains raw screen output for mandatory finite/W-domain probing.
 * Implements XY truncation to1/16 and inverse D3D viewport. No MSAA proof. */
int fut_vertex_program_hlsl_screen(const uint32_t (*words)[4],size_t count,
    char *text,size_t capacity,size_t *length);
#endif
