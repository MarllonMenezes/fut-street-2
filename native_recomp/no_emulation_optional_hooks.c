#include <stdint.h>

/* XboxRecomp's optional pushbuffer survey can call nv2a_pb_exec_method when
 * RECOMP_PB_EXEC is set. This no-emulation build excludes both the survey and
 * executor sources; these disabled hooks satisfy the kernel-layout references
 * without reading guest memory, interpreting commands, or changing GPU state. */
void nv2a_pb_scan(uint32_t start_va, uint32_t end_va)
{
    (void)start_va;
    (void)end_va;
}

void nv2a_pb_scan_report(void)
{
}
