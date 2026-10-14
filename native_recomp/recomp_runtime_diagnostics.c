#include "recomp_types.h"

#include <stdio.h>
#include <stdlib.h>

static uint8_t g_tv_encoder_field_pin;
static atomic_bool g_xbox_interrupts_enabled = ATOMIC_VAR_INIT(1);

uint8_t recomp_xbox_io_read8(uint16_t port)
{
    if (port == 0x80C0u) {
        /* Independent Xbox compatibility evidence models the byte read as
         * the TV encoder field-pin bit toggling on each sample. */
        g_tv_encoder_field_pin =
            (uint8_t)((g_tv_encoder_field_pin + 1u) & 1u);
        return (uint8_t)(g_tv_encoder_field_pin << 5);
    }

    static int reported;
    if (!reported) {
        fprintf(stderr, "[IO] unsupported Xbox byte read from port 0x%04X\n",
                (unsigned)port);
        reported = 1;
    }
    return 0;
}

void recomp_xbox_cli(void)
{
    /* Compatibility state only: no synthetic interrupt producer exists in
     * this no-emulation host. */
    atomic_store_explicit(&g_xbox_interrupts_enabled, 0,
                          memory_order_seq_cst);
}

void recomp_xbox_sti(void)
{
    atomic_store_explicit(&g_xbox_interrupts_enabled, 1,
                          memory_order_seq_cst);
}

uint8_t recomp_xbox_interrupts_enabled(void)
{
    return (uint8_t)atomic_load_explicit(&g_xbox_interrupts_enabled,
                                         memory_order_seq_cst);
}

void recomp_icall_fail_log(uint32_t va)
{
    fprintf(stderr, "[ICALL] unresolved guest target 0x%08X (calls=%llu)\n",
            va, (unsigned long long)g_icall_count);

    for (uint32_t i = 0; i < ICALL_TRACE_SIZE; ++i) {
        const uint32_t slot =
            (g_icall_trace_idx - ICALL_TRACE_SIZE + i) & (ICALL_TRACE_SIZE - 1);
        const uint32_t target = g_icall_trace[slot];
        if (target != 0)
            fprintf(stderr, "  recent[%u]=0x%08X\n", i, target);
    }
}

void recomp_icall_not_code_log(uint32_t va)
{
    enum { SLOT_COUNT = 16 };
    static uint32_t addresses[SLOT_COUNT];
    static uint64_t hits[SLOT_COUNT];
    static uint32_t used;

    uint32_t slot = 0;
    while (slot < used && addresses[slot] != va)
        ++slot;
    if (slot == used) {
        if (used == SLOT_COUNT)
            return;
        addresses[slot] = va;
        ++used;
    }

    const uint64_t hit_count = ++hits[slot];
    uint64_t milestone = hit_count;
    while (milestone >= 10 && milestone % 10 == 0)
        milestone /= 10;
    if (milestone != 1)
        return;

    fprintf(stderr,
            "[ICALL] target 0x%08X is not code (hits=%llu, calls=%llu)\n",
            va, (unsigned long long)hit_count,
            (unsigned long long)g_icall_count);
}

void recomp_unimpl(const char *text, uint32_t va)
{
    static uint32_t reported;
    const char *trap = getenv("RECOMP_UNIMPL_TRAP");
    const int stop = trap != NULL && trap[0] != '\0' && trap[0] != '0';

    if (reported < 50 || stop) {
        ++reported;
        fprintf(stderr,
                "[UNIMPL] reached untranslated instruction at 0x%08X: %s\n",
                va, text != NULL ? text : "(no instruction text)");
    }

    if (stop)
        abort();
}
