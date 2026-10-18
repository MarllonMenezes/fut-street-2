#include "recomp_types.h"
#include "recomp_funcs.h"

/* Keep title-specific control-flow aliases exact and evidence-backed. The
 * instruction at 0x0023A4C3 is a five-byte JMP to 0x0024B547; it changes
 * neither guest stack nor registers, so resolving the thunk to the translated
 * destination preserves the original call/return behavior. */
recomp_func_t recomp_lookup_manual(uint32_t xbox_va)
{
    switch (xbox_va) {
    case 0x0023A4C3u:
        return sub_0024B547;
    case 0x00240B00u:
        return sub_0023D3A0;
    default:
        return NULL;
    }
}
