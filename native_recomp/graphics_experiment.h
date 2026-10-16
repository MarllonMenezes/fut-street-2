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
#endif
