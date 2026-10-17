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
#endif
