#include "recomp_types.h"

#include <stdint.h>

void nv2a_pb_scan(uint32_t start_va, uint32_t end_va);
void nv2a_pb_scan_report(void);
void recomp_icall_fail_log(uint32_t va);
void recomp_icall_not_code_log(uint32_t va);
uint8_t recomp_xbox_io_read8(uint16_t port);
void recomp_xbox_cli(void);
void recomp_xbox_sti(void);
uint8_t recomp_xbox_interrupts_enabled(void);

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

    if (recomp_xbox_io_read8(0x80C0u) != 0x20u)
        return 4;
    if (recomp_xbox_io_read8(0x80C0u) != 0x00u)
        return 5;
    if (recomp_xbox_interrupts_enabled() != 1u)
        return 6;
    recomp_xbox_cli();
    if (recomp_xbox_interrupts_enabled() != 0u)
        return 7;
    recomp_xbox_sti();
    if (recomp_xbox_interrupts_enabled() != 1u)
        return 8;
    return 0;
}
