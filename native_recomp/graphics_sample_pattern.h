#ifndef FUT_GRAPHICS_SAMPLE_PATTERN_H
#define FUT_GRAPHICS_SAMPLE_PATTERN_H
#include <stdint.h>
/* Coverage model inferred from the packed framebuffer in the public Xbox1.0
 * FBSurfaceWithCenterCorner2 golden test. Even storage columns sample the
 * logical integer corner; odd columns coincide with the single-sample center.
 * This is not a claim about fragment interpolation, resolve or dithering.
 * An expanded uniform horizontal raster grid alone does not implement it. */
typedef struct {
    float clip_shift[2];
    uint32_t storage_column, reserved;
} FutGraphicsSamplePass;
static inline int fut_graphics_aa2_coverage_pass(unsigned width,unsigned height,
    unsigned slot,FutGraphicsSamplePass *pass)
{
    if(!pass || !width || width>8192 || !height || height>16384 || slot>1) return 0;
    /* Native target is 2*width by height. A half stored-pixel X shift places
     * its even/odd centers at logical x and x+.5, respectively. Corner pass
     * additionally moves vertices down half a logical pixel. */
    *pass=(FutGraphicsSamplePass){{1.0f/(2.0f*width),slot?0.0f:-1.0f/height},slot,0};
    return 1;
}
#define FUT_AA2_COVERAGE_HLSL \
    "cbuffer SamplePass:register(b4){float2 sampleShift;uint sampleSlot;uint sampleReserved;};" \
    "float4 fut_sample_position(float4 p){p.xy+=sampleShift*p.w;return p;}" \
    "bool fut_sample_column(float x){return (uint(x)&1)==sampleSlot;}"
/* Only for batches whose vertex outputs have proved W=1. For the corner
 * storage slot, the logical pixel center is one stored X pixel and half a Y
 * pixel away. Evaluate affine attributes there before parity/clip discard;
 * derivatives in divergent discarded lanes cannot define this construction.
 * The center slot is already evaluated at the desired location. */
#define FUT_AA2_AFFINE_HLSL \
    "float4 fut_sample_center(float4 a){float2 d=sampleSlot==0?float2(1,.5):float2(0,0);" \
    "return a+ddx(a)*d.x+ddy(a)*d.y;}"
#endif
