#include "recomp_types.h"

#include <stdint.h>

void nv2a_pb_scan(uint32_t start_va, uint32_t end_va);
void nv2a_pb_scan_report(void);
void recomp_icall_fail_log(uint32_t va);
void recomp_icall_not_code_log(uint32_t va);

volatile uint32_t g_icall_trace[ICALL_TRACE_SIZE];
volatile uint32_t g_icall_trace_idx;
volatile uint64_t g_icall_count;

int main(void)
{
    static const uint32_t guest_addresses[] = {
        0u,
        0x80000000u,
        0xFFFFFFFFu,
    };

    for (size_t i = 0; i < sizeof(guest_addresses) / sizeof(guest_addresses[0]); ++i) {
        if (recomp_lookup_manual(guest_addresses[i]) != NULL)
            return 1;
        nv2a_pb_scan(guest_addresses[i], guest_addresses[i]);
    }

    nv2a_pb_scan(0x80000000u, 0xFFFFFFFFu);
    nv2a_pb_scan_report();
    recomp_icall_fail_log(0x80000000u);
    recomp_icall_not_code_log(0xFFFFFFFFu);
    return 0;
}
