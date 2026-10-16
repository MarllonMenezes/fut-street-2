#include "recomp_types.h"

#include <stddef.h>
#include <stdint.h>
#include <string.h>

void nv2a_pb_scan(uint32_t start_va, uint32_t end_va);
void nv2a_pb_scan_report(void);
void recomp_icall_fail_log(uint32_t va);
void recomp_icall_not_code_log(uint32_t va);
uint8_t recomp_xbox_io_read8(uint16_t port);
void recomp_xbox_cli(void);
void recomp_xbox_sti(void);
uint8_t recomp_xbox_interrupts_enabled(void);

/* The unit target does not link the generated game code. The manual lookup
 * contract is tested by address identity; the native bring-up validates the
 * real translated implementation. */
void sub_0024B547(void)
{
}

/* The standalone runtime-support test does not link the generated dispatch
 * table or the kernel bridge. Keep every indirect lookup unresolved so this
 * test exercises only the safety envelope of RECOMP_ICALL_SAFE_AT*. */
recomp_func_t recomp_lookup(uint32_t xbox_va)
{
    (void)xbox_va;
    return NULL;
}

recomp_func_t recomp_lookup_kernel(uint32_t xbox_va)
{
    (void)xbox_va;
    return NULL;
}

uint32_t g_xbox_code_lo;
uint32_t g_xbox_code_hi;
ptrdiff_t g_xbox_mem_offset;
RECOMP_TLS uint32_t g_eax;
RECOMP_TLS uint32_t g_esp;

volatile uint32_t g_icall_trace[ICALL_TRACE_SIZE];
volatile uint32_t g_icall_trace_idx;
volatile uint64_t g_icall_count;

int main(int argc, char **argv)
{
    if (argc == 2) {
        if (strcmp(argv[1], "unresolved") == 0)
            recomp_icall_fail_log(0x1234u);
        else if (strcmp(argv[1], "not-code") == 0)
            recomp_icall_not_code_log(0xFFFFFFFFu);
        else if (strcmp(argv[1], "read") == 0)
            (void)recomp_xbox_io_read8(0x1234u);
        else if (strcmp(argv[1], "write-port") == 0)
            recomp_xbox_io_write8(0x1234u, 1u);
        else if (strcmp(argv[1], "write-value") == 0)
            recomp_xbox_io_write8(0x80C0u, 2u);
        else if (strcmp(argv[1], "unimpl") == 0)
            recomp_unimpl("test untranslated instruction", 0x1234u);
        return 99; /* Each diagnostic probe must exit before reaching here. */
    }
    static const uint32_t guest_addresses[] = {
        0u,
        0x80000000u,
        0xFFFFFFFFu,
    };

    if (recomp_lookup_manual(0x0023A4C3u) != sub_0024B547)
        return 12;
    if (recomp_lookup_manual(0x0023A4C2u) != NULL ||
        recomp_lookup_manual(0x0023A4C4u) != NULL)
        return 13;

    for (size_t i = 0; i < sizeof(guest_addresses) / sizeof(guest_addresses[0]); ++i) {
        if (recomp_lookup_manual(guest_addresses[i]) != NULL)
            return 1;
        nv2a_pb_scan(guest_addresses[i], guest_addresses[i]);
    }

    nv2a_pb_scan(0x80000000u, 0xFFFFFFFFu);
    nv2a_pb_scan_report();
    recomp_icall_fail_log(0x80000000u);
    recomp_icall_not_code_log(0xFFFFFFFFu);

    /* A wild target must not reach any lookup or function pointer call, and
     * the stdcall-style safe arm must restore the pre-argument stack. */
    g_xbox_code_lo = 0x00001000u;
    g_xbox_code_hi = 0x00002000u;
    g_esp = 0x00005000u;
    g_eax = 0xAAAAAAAAu;
#define eax g_eax
    RECOMP_ICALL_SAFE_AT(0x00000020u, 0x00004FF0u, 0x0024D3B1u);
#undef eax
    if (g_esp != 0x00004FF0u || g_eax != 0u)
        return 9;

    /* A plausible code VA with no generated/kernel mapping is unresolved;
     * it must return zero and preserve the caller's pre-argument stack. */
    g_esp = 0x00006000u;
    g_eax = 0xBBBBBBBBu;
#define eax g_eax
    RECOMP_ICALL_SAFE_AT(0x00001500u, 0x00005FF0u, 0x002512A4u);
#undef eax
    if (g_esp != 0x00005FF0u || g_eax != 0u)
        return 10;

    /* Caller-cleanup sites pop only the return address on failure. */
    g_esp = 0x00007000u;
    g_eax = 0xCCCCCCCCu;
#define eax g_eax
    RECOMP_ICALL_SAFE_AT_CC(0x00000020u, 0x00006FF0u, 0x0026E75Eu);
#undef eax
    if (g_esp != 0x00007004u || g_eax != 0u)
        return 11;


    recomp_xbox_io_write8(0x80C0u, 1u);
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
