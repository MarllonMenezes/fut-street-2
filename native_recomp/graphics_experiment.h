#ifndef FUT_GRAPHICS_EXPERIMENT_H
#define FUT_GRAPHICS_EXPERIMENT_H
#include <stdlib.h>
#include <string.h>
/* Observed projective ARGB8 mip draw; separate from unit-W AA2 interpolation. */
static inline int fut_graphics_projective_aa2_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_PROJECTIVE_AA2_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Captured untextured AA2 draw after the intro; retained descriptors are
 * not sampled. Separate opt-in from BC2 decoding and from the AA2 model. */
static inline int fut_graphics_untextured_aa2_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_UNTEXTURED_AA2_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Explicitly authorized 2026-10-15. Default is strict; an inherited arbitrary
 * string must not silently enable the approximation. */
static inline int fut_graphics_aa2_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_AA2_APPROXIMATION");
    return value && !strcmp(value,"1");
}
static inline int fut_graphics_quincunx_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_QUINCUNX_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Authorized separately on 2026-10-17, only at the exact captured BC1 gate. */
static inline int fut_graphics_dxt1_noise_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_DXT1_NOISE_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Authorized 2026-10-17 for the captured 32x32 palette texture in AA2. */
static inline int fut_graphics_p8_aa2_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_P8_AA2_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Separately authorized 2026-10-17 for only the captured P8 128x32 AA2 draw
 * with depth test/write enabled. All draw-state fields remain checked. */
static inline int fut_graphics_p8_128x32_aa2_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_P8_128X32_AA2_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Authorized 2026-10-18 for only the captured BC2/DXT23 register tuple.
 * FORMAT encodes 64x64; retained IMAGE_RECT/pitch are still scope gates. */
static inline int fut_graphics_bc2_aa2_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_BC2_AA2_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Separately authorized 2026-10-18: exact 1024x512 BC2 tuple at 0x00C9DF00. */
static inline int fut_graphics_bc2_c9df00_aa2_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_BC2_C9DF00_AA2_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Authorized 2026-10-18 for continued bring-up: one-mip BC2 textures under
 * the observed AA2/depth/sampler baseline. Other state remains fail-fast. */
static inline int fut_graphics_bc2_aa2_family_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_BC2_AA2_FAMILY_APPROXIMATION");
    return value && !strcmp(value,"1");
}
static inline int fut_graphics_p8_aa2_family_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_P8_AA2_FAMILY_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Authorized 2026-10-18 for one captured 16-segment AA2 line-list packet. */
static inline int fut_graphics_lines_16segment_aa2_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_LINES_16SEGMENT_AA2_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Authorized under the 2026-10-18 continuation, only captured offset1688B4. */
static inline int fut_graphics_lines_4segment_aa2_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_LINES_4SEGMENT_AA2_APPROXIMATION");
    return value && !strcmp(value,"1");
}
static inline int fut_graphics_lines_aa2_count_enabled(unsigned count)
{
    return (count==32 && fut_graphics_lines_16segment_aa2_approximation_enabled()) ||
        (count==8 && fut_graphics_lines_4segment_aa2_approximation_enabled());
}
/* Authorized 2026-10-18 for only the captured LC_IMAGE YUY2 640x480 draw.
 * BT.601 is a host approximation; it is not measured NV2A fetch behavior. */
static inline int fut_graphics_yuy2_bt601_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_YUY2_023F1580_BT601_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Separately authorized 2026-10-18 for only the captured YUY2 AA2 draw. */
static inline int fut_graphics_yuy2_aa2_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_YUY2_023F1580_AA2_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Separately authorized 2026-10-18 for only the second captured draw at
 * offset 0x02487680. These are independent, default-off opt-ins. */
static inline int fut_graphics_yuy2_second_bt601_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_YUY2_02487680_BT601_APPROXIMATION");
    return value && !strcmp(value,"1");
}
static inline int fut_graphics_yuy2_second_aa2_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_YUY2_02487680_AA2_APPROXIMATION");
    return value && !strcmp(value,"1");
}
/* Authorized for the observed YUY2 640x480 layout across bounded DMA offsets.
 * BT.601 conversion and AA2 admission remain separate default-off choices. */
static inline int fut_graphics_yuy2_family_bt601_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_YUY2_FAMILY_BT601_APPROXIMATION");
    return value && !strcmp(value,"1");
}
static inline int fut_graphics_yuy2_family_aa2_approximation_enabled(void)
{
    const char *value=getenv("RECOMP_YUY2_FAMILY_AA2_APPROXIMATION");
    return value && !strcmp(value,"1");
}
#endif
