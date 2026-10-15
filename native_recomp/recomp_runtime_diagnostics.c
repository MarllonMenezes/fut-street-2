#include "recomp_types.h"
#include "guest_interrupt_gate.h"

#include <stdio.h>
#include <stdlib.h>

static uint8_t g_tv_encoder_field_pin;

static void stop_on_runtime_gap(void)
{
    const char *trap = getenv("RECOMP_DIAGNOSTIC_TRAP");
    if (trap != NULL && trap[0] != '\0' && trap[0] != '0') {
        fflush(NULL);
        /* A diagnostic exit avoids Windows crash dialogs and does not let
         * unresolved dispatch synthesize a return value in bring-up runs. */
        _Exit(3);
    }
}

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
    stop_on_runtime_gap();
    return 0;
}

void recomp_xbox_io_write8(uint16_t port, uint8_t value)
{
    if (port == 0x80C0u && value == 1u) {
        /* The observed title init writes 1 to GPIO offset 0. xemu's
         * xbox_pm_gpio_write applies no effect there (only offset 0x16
         * changes aspect ratio). Narrow compatibility, not hardware proof. */
        fprintf(stderr, "[IO] TV-encoder init write port=0x80C0 value=0x01 accepted (compatibility)\n");
        return;
    }
    fprintf(stderr, "[IO] unsupported Xbox byte write port=0x%04X value=0x%02X\n",
            (unsigned)port, (unsigned)value);
    /* No defined fallback for an unknown device write. */
    fflush(NULL);
    _Exit(3);
}

void recomp_xbox_cli(void)
{
    fut_guest_interrupt_cli();
}

void recomp_xbox_sti(void)
{
    fut_guest_interrupt_sti();
}

uint8_t recomp_xbox_interrupts_enabled(void)
{
    return fut_guest_interrupt_enabled();
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
    stop_on_runtime_gap();
}

void recomp_icall_not_code_log(uint32_t va)
{
    enum { SLOT_COUNT = 16 };
    static uint32_t addresses[SLOT_COUNT];
    static uint64_t hits[SLOT_COUNT];
    static uint32_t used;

    /* Always report the first fatal target, even if throttling is exhausted. */
    const char *trap = getenv("RECOMP_DIAGNOSTIC_TRAP");
    if (trap != NULL && trap[0] != '\0' && trap[0] != '0') {
        fprintf(stderr, "[ICALL] target 0x%08X is not code (calls=%llu)\n",
                va, (unsigned long long)g_icall_count);
        stop_on_runtime_gap();
    }

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

    stop_on_runtime_gap();
    if (stop)
        abort();
}
