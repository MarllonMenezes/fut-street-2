#ifndef FUT_GRAPHICS_EXPERIMENT_H
#define FUT_GRAPHICS_EXPERIMENT_H
#include <stdlib.h>
#include <string.h>
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
#endif
