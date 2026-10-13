#include "recomp_types.h"

/* XboxRecomp's generated output has no manual-override registry by default.
 * Until title-specific overrides are independently justified and added, make
 * that absence explicit instead of leaving the runtime's lookup unresolved. */
recomp_func_t recomp_lookup_manual(uint32_t xbox_va)
{
    (void)xbox_va;
    return NULL;
}
