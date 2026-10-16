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
        uint32_t guest_return = 0;
        const int stack_mapped = g_xbox_mem_offset != 0 &&
            g_esp >= 0x00010000u && g_esp <= 0x03FFFF90u;
        if (stack_mapped)
            guest_return = MEM32(g_esp);
        fprintf(stderr,
                "[ICALL] target 0x%08X is not code (calls=%llu guest_esp=%08X guest_return=%08X)\n",
                va, (unsigned long long)g_icall_count, g_esp, guest_return);
        if (stack_mapped) {
            uint32_t words[29];
            for (unsigned i = 0; i < 29; ++i)
                words[i] = MEM32(g_esp + i * 4u);
            fputs("[ICALL-STACK]", stderr);
            for (unsigned i = 0; i < 29; ++i)
                fprintf(stderr, " +%02X=%08X", i * 4u, words[i]);
            fputc('\n', stderr);
            if (words[3] == 0x00289AD9u && words[11] == 0x00286B0Cu &&
                words[19] == 0x00286BB6u && words[12] == 6u) {
                uint32_t edi = words[20], edi8 = 0, edi1c = 0, edi8c = 0;
                if (edi >= 0x00010000u && edi <= 0x03FFFFE0u) {
                    edi8 = MEM32(edi + 8u);
                    edi1c = MEM32(edi + 0x1Cu);
                }
                if (edi8 >= 0x00010000u && edi8 <= 0x03FFFFF0u)
                    edi8c = MEM32(edi8 + 0xCu);
                const uint32_t lowpage_c = MEM32(0xCu);
                const uint32_t audio_global_351834 = MEM32(0x00351834u);
                const uint32_t audio_global_351838 = MEM32(0x00351838u);
                fprintf(stderr,
                    "[ICALL-OBJECT] chain=00289AD9/00286B0C/00286BB6 edi=%08X edi8=%08X edi1c=%08X edi8c=%08X lowpage_c=%08X audio351834=%08X audio351838=%08X stack60=%08X stack64=%08X stack68=%08X stack6C=%08X stack70=%08X\n",
                    edi, edi8, edi1c, edi8c, lowpage_c, audio_global_351834,
                    audio_global_351838, words[24], words[25], words[26],
                    words[27], words[28]);
            }
        }
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
