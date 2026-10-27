#ifndef FUT_GRAPHICS_FRAGMENT_PROGRAM_H
#define FUT_GRAPHICS_FRAGMENT_PROGRAM_H
#include <stddef.h>
#include <stdint.h>
typedef struct {
    uint32_t control, stage_program, dot_rgbmapping;
    uint32_t final_words[2], final_factor[2];
    uint32_t color_input[8], alpha_input[8], color_output[8], alpha_output[8], factors[2][8];
    unsigned valid, color_input_valid, alpha_input_valid, color_output_valid, alpha_output_valid;
    unsigned factor_valid[2], final_factor_valid;
    int dirty;
} FutGraphicsFragmentState;
typedef enum { FUT_FRAGMENT_APPLIED, FUT_FRAGMENT_NOT_HANDLED, FUT_FRAGMENT_REJECTED } FutFragmentStatus;
/* Retains configuration only. Unknown method and unaligned writes do not mutate. */
FutFragmentStatus fut_graphics_fragment_state(FutGraphicsFragmentState *state,uint32_t method,uint32_t value);
/* Compile register-combiner COLOR/ALPHA equations to a HLSL function.
 * Inputs are already processed texture-stage results, not unfiltered texels.
 * Supported: up to8 stages, multiply/dot/add, all8 input mappings, scale/bias,
 * parallel color/alpha writes, AB blue-to-alpha and direct final inputs. MUX/CD blue-to-alpha,
 * final SUM/EF pseudo registers, fog inputs and dependent texture modes fail.
 * Float32 equations are NOT proof of NV2A fixed-point/rounding equivalence.
 * Failure returns length0 and empty text; no runnable partial translation. */
int fut_fragment_program_hlsl(const FutGraphicsFragmentState *state,char *text,size_t capacity,size_t *length);
#endif
