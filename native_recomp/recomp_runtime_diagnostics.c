#include "recomp_types.h"
#include "guest_interrupt_gate.h"

#include <stdatomic.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#ifdef _WIN32
#include <windows.h>
#endif

static atomic_uint g_diagnostic_next_thread_id = ATOMIC_VAR_INIT(1u);
static _Thread_local uint32_t g_diagnostic_thread_id;

static uint32_t diagnostic_thread_id(void)
{
    if (g_diagnostic_thread_id == 0)
        g_diagnostic_thread_id = atomic_fetch_add_explicit(
            &g_diagnostic_next_thread_id, 1u, memory_order_relaxed);
    return g_diagnostic_thread_id;
}

int recomp_frontend_loading_trace_enabled(void)
{
    const char *value = getenv("RECOMP_FRONTEND_LOADING_TRACE");
    return value && strcmp(value, "1") == 0;
}

/* Only numeric entry arguments; never dereference guest pointers or alter state. */
void recomp_frontend_loading_trace(uint32_t site, uint32_t object, uint32_t stack,
    uint32_t caller, uint32_t arg0, uint32_t arg1, uint32_t arg2)
{
    static atomic_uint counts[4];
    unsigned index;
    switch (site) {
    case 0x0015F7B0u: index = 0; break;
    case 0x001606E0u: index = 1; break;
    case 0x001630B0u: index = 2; break;
    case 0x00163570u: index = 3; break;
    default: return;
    }
    if (!recomp_frontend_loading_trace_enabled()) return;
    unsigned count = atomic_load_explicit(&counts[index], memory_order_relaxed);
    do {
        if (count >= 64u) return;
    } while (!atomic_compare_exchange_weak_explicit(&counts[index], &count,
        count + 1u, memory_order_relaxed, memory_order_relaxed));
    fprintf(stderr, "[FRONTEND-LOADING-TRACE] thread=%u site=%08X sample=%u "
        "object=%08X esp=%08X return=%08X arg0=%08X arg1=%08X arg2=%08X read-only=1\n",
        diagnostic_thread_id(), site, count + 1u, object, stack, caller, arg0, arg1, arg2);
}

/* Read bounded diagnostic text through the OS: invalid guest pointers must not
 * turn optional tracing into a crash. Escape all nontrivial bytes for one-line logs. */
static const char *frontend_trace_text(uint32_t address, char text[385])
{
    text[0] = 0;
#ifdef _WIN32
    if (!g_xbox_mem_offset || address < 0x10000u || address > 0x83FFFF9Fu)
        return "unreadable";
    DWORD saved_error = GetLastError();
    unsigned used = 0;
    const char *status = "truncated";
    for (unsigned i = 0; i < 96u; ++i) {
        unsigned char value; SIZE_T read = 0;
        if (!ReadProcessMemory(GetCurrentProcess(), (const void *)XBOX_PTR(address + i),
                               &value, 1u, &read) || read != 1u) {
            status = "unreadable"; break;
        }
        if (!value) { status = "terminated"; break; }
        if (value >= 32u && value <= 126u && value != '"' && value != 92u)
            text[used++] = (char)value;
        else {
            static const char hex[] = "0123456789ABCDEF";
            text[used++] = 92; text[used++] = 'x';
            text[used++] = hex[value >> 4]; text[used++] = hex[value & 15u];
        }
    }
    text[used] = 0;
    SetLastError(saved_error);
    return status;
#else
    (void)address;
    return "unavailable";
#endif
}

static _Thread_local unsigned frontend_preload_scope;
int recomp_frontend_preload_trace_active(void)
{
    return frontend_preload_scope && recomp_frontend_loading_trace_enabled();
}

void recomp_frontend_preload_vm_trace(uint32_t pc, uint32_t opcode, uint32_t vm)
{
#ifdef _WIN32
    static atomic_uint count;
    if (!recomp_frontend_preload_trace_active()) return;
    unsigned sample = atomic_load_explicit(&count, memory_order_relaxed);
    do { if (sample >= 512u) return; }
    while (!atomic_compare_exchange_weak_explicit(&count, &sample, sample+1u,
        memory_order_relaxed, memory_order_relaxed));
    DWORD saved_error = GetLastError();
    uint32_t fields[3] = {0}, cells[3] = {0}, words[3][4] = {{0}};
    SIZE_T read = 0;
    int ok = g_xbox_mem_offset && vm >= 0x10000u && vm <= 0x83FFFFF3u &&
        ReadProcessMemory(GetCurrentProcess(), (const void *)XBOX_PTR(vm),
            fields, sizeof(fields), &read) && read == sizeof(fields) && fields[0] <= 4096u;
    if (ok) for (unsigned i = 0; i < 3u && i < fields[0]; ++i) {
        uint32_t address = fields[2] + (fields[0]-1u-i)*4u;
        if (address < fields[2] || address > 0x83FFFFFBu || address < 0x10000u ||
            !ReadProcessMemory(GetCurrentProcess(), (const void *)XBOX_PTR(address),
                &cells[i], 4, &read) || read != 4 || cells[i] < 0x10000u || cells[i] > 0x83FFFFEFu ||
            !ReadProcessMemory(GetCurrentProcess(), (const void *)XBOX_PTR(cells[i]),
                words[i], sizeof(words[i]), &read) || read != sizeof(words[i])) { ok = 0; break; }
    }
    fprintf(stderr, "[PRELOAD-VM-TRACE] sample=%u pc=%08X op=%02X vm=%08X count=%u readable=%d "
        "top=%08X/%08X/%08X/%08X next=%08X/%08X/%08X/%08X third=%08X/%08X/%08X/%08X read-only=1\n",
        sample+1u, pc, opcode, vm, fields[0], ok,
        cells[0],words[0][1],words[0][2],words[0][3],
        cells[1],words[1][1],words[1][2],words[1][3],
        cells[2],words[2][1],words[2][2],words[2][3]);
    SetLastError(saved_error);
#else
    (void)pc; (void)opcode; (void)vm;
#endif
}

/* String object layout here is the output of original 001D7A23: characters
 * follow its eight-byte header. Target/value words are raw observations. */
void recomp_frontend_member_trace(uint32_t site, uint32_t target,
    uint32_t name, uint32_t value, uint32_t result)
{
#ifdef _WIN32
    static atomic_uint counts[2];
    unsigned index;
    if (!recomp_frontend_preload_trace_active()) return;
    if (site == 0x001E6CADu) index = 0;
    else if (site == 0x001E6CC9u) index = 1;
    else return;
    unsigned sample = atomic_load_explicit(&counts[index], memory_order_relaxed);
    do { if (sample >= 16u) return; }
    while (!atomic_compare_exchange_weak_explicit(&counts[index], &sample, sample+1u,
        memory_order_relaxed, memory_order_relaxed));
    DWORD saved_error = GetLastError();
    uint32_t object[16] = {0}, cell[4] = {0};
    SIZE_T read = 0;
    int object_ok = g_xbox_mem_offset && target >= 0x10000u && target <= 0x83FFFFBFu &&
        ReadProcessMemory(GetCurrentProcess(), (const void *)XBOX_PTR(target),
            object, sizeof(object), &read) && read == sizeof(object);
    int value_ok = g_xbox_mem_offset && value >= 0x10000u && value <= 0x83FFFFEFu &&
        ReadProcessMemory(GetCurrentProcess(), (const void *)XBOX_PTR(value),
            cell, sizeof(cell), &read) && read == sizeof(cell);
    char text[385];
    const char *status = frontend_trace_text(name <= 0xFFFFFFF7u ? name+8u : 0u, text);
    fprintf(stderr, "[PRELOAD-MEMBER-TRACE] site=%08X sample=%u target=%08X name=%08X "
        "text=\"%s\" text_status=%s value=%08X result=%08X object_readable=%d value_readable=%d "
        "cell=%08X/%08X/%08X/%08X words=",
        site, sample+1u, target, name, text, status, value, result, object_ok, value_ok,
        cell[0],cell[1],cell[2],cell[3]);
    for (unsigned i=0;i<16u;++i) fprintf(stderr, "%s%08X", i?"/":"", object[i]);
    fprintf(stderr, " read-only=1\n");
    SetLastError(saved_error);
#else
    (void)site; (void)target; (void)name; (void)value; (void)result;
#endif
}

void recomp_frontend_command_trace(uint32_t site, uint32_t name,
                                   uint32_t detail, uint32_t result)
{
    static atomic_uint counts[10], song_count;
    unsigned index;
    switch (site) {
    case 0x00160DF0u: index = 0; break;
    case 0x00160E2Eu: index = 1; break;
    case 0x00160ED0u: index = 2; break;
    case 0x00160840u: index = 3; break;
    case 0x00160859u: index = 4; break;
    case 0x001D3F13u: index = 5; break;
    case 0x001D3F2Bu: index = 6; break;
    case 0x00160330u: index = 7; break;
    case 0x00160429u: index = 8; break;
    case 0x00160433u: index = 9; break;
    default: return;
    }
    if (!recomp_frontend_loading_trace_enabled()) return;
    if (site == 0x00160840u) ++frontend_preload_scope;
    if (site == 0x00160859u && frontend_preload_scope) --frontend_preload_scope;
    unsigned count = atomic_load_explicit(&counts[index], memory_order_relaxed);
    if (count >= 64u) return;
#ifdef _WIN32
    DWORD saved_error = GetLastError();
#endif
    char text[385];
    const char *status = frontend_trace_text(name, text);
    /* Preserve the same total budget while keeping a recurring music query
     * from hiding later original navigation and input commands. */
    if (site == 0x00160ED0u && strcmp(status,"terminated")==0 &&
        strcmp(text,"eatrax?sub=currentsong")==0) {
        unsigned songs=atomic_load_explicit(&song_count,memory_order_relaxed);
        do { if(songs>=2u) goto trace_done; }
        while(!atomic_compare_exchange_weak_explicit(&song_count,&songs,songs+1u,
            memory_order_relaxed,memory_order_relaxed));
    }
    do { if(count>=64u) goto trace_done; }
    while(!atomic_compare_exchange_weak_explicit(&counts[index],&count,count+1u,
        memory_order_relaxed,memory_order_relaxed));
    fprintf(stderr, "[FRONTEND-COMMAND-TRACE] site=%08X sample=%u name=%08X "
        "text=\"%s\" text_status=%s detail=%08X result=%08X read-only=1\n",
        site, count + 1u, name, text, status, detail, result);
trace_done: ;
#ifdef _WIN32
    SetLastError(saved_error);
#endif
}

/* User-authorized boot exploration only. This fabricates bounded completions;
 * it implements neither DSP execution nor audio. Called by a separate,
 * explicitly selected generated-source overlay in the submitting thread. */
unsigned fut_audio_effects_bypass_limit(void)
{
    const char *option = getenv("RECOMP_AUDIO_EFFECTS_BYPASS_MAX_COMPLETIONS");
    if (!option) return 4096u;
    if (!option[0]) return 0;
    for (const char *p = option; *p; ++p)
        if (*p < '0' || *p > '9') return 0;
    unsigned long value = strtoul(option, NULL, 10);
    return value >= 1u && value <= 16384u ? (unsigned)value : 0u;
}

int fut_audio_effects_bypass(uint32_t site, uint32_t address,
                             volatile uint32_t *command)
{
    static unsigned applied;
    const char *option = getenv("RECOMP_AUDIO_EFFECTS_BYPASS");
    if (!option || strcmp(option, "0") == 0)
        return 0;
    const unsigned limit = fut_audio_effects_bypass_limit();
    if (strcmp(option, "1") != 0 || !limit || applied >= limit ||
        site != 0x002872FFu || address != 0x83360810u ||
        (uintptr_t)command != (uintptr_t)XBOX_PTR(address) ||
        !g_xbox_mem_offset || *command != 3u) {
        fprintf(stderr, "[EXPERIMENTAL-AUDIO-BYPASS] refused scope/configuration "
                "site=%08X address=%08X applied=%u; NOT_READY\n",
                site, address, applied);
        fflush(NULL);
        _Exit(3);
    }
    *command = 0;
    ++applied;
    fprintf(stderr, "[EXPERIMENTAL-AUDIO-BYPASS] thread=%u site=%08X address=%08X "
            "before=00000003 after=00000000 simulated-completion=1 "
            "application=%u/%u DSP-executed=0 audio-validated=0 NOT_READY\n",
            diagnostic_thread_id(), site, address, applied, limit);
    fflush(stderr);
    return 1;
}

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

/* Read-only instrumentation for the original STRM callback path. Event
 * counters and log limits are local to this trace, independent of the global
 * generated-function trace budget. */
enum {
    STREAM_TRACE_EVENT_COUNT = 17,
    STREAM_TRACE_SUMMARY_INTERVAL = 256,
    STREAM_TRACE_SUMMARY_LIMIT = 4096
};

static const char *const g_stream_trace_event_names[STREAM_TRACE_EVENT_COUNT] = {
    "invalid", "register", "remove-enter", "remove-found", "remove-miss",
    "caller-enter", "caller-return", "dispatch-enter", "dispatch-next",
    "dispatch-return", "dispatch-exit", "callback-enter", "callback-node",
    "callback-3988d", "process-39bd7-enter", "process-39bd7-return",
    "callback-exit"
};

static atomic_uint_fast64_t g_stream_trace_event_counts[STREAM_TRACE_EVENT_COUNT];
static atomic_uint_fast64_t g_stream_trace_total;
static atomic_uint_fast64_t g_stream_trace_detail_count;
static atomic_uint_fast64_t g_stream_trace_summary_count;
static atomic_flag g_stream_trace_init_lock = ATOMIC_FLAG_INIT;
static atomic_int g_stream_trace_initialized;
static atomic_int g_stream_trace_enabled;
static atomic_uint g_stream_trace_per_event_limit;

static void stream_trace_initialize(void)
{
    if (atomic_load_explicit(&g_stream_trace_initialized, memory_order_acquire))
        return;

    while (atomic_flag_test_and_set_explicit(&g_stream_trace_init_lock,
                                              memory_order_acquire)) {
    }
    if (!atomic_load_explicit(&g_stream_trace_initialized, memory_order_relaxed)) {
        const char *enabled = getenv("RECOMP_STREAM_CALLBACK_TRACE");
        const char *limit_text = getenv("RECOMP_STREAM_CALLBACK_TRACE_LIMIT");
        unsigned limit = 128;
        int trace_enabled = enabled != NULL && strcmp(enabled, "1") == 0;

        if (limit_text != NULL && limit_text[0] != '\0') {
            char *end = NULL;
            const unsigned long parsed = strtoul(limit_text, &end, 10);
            if (end == limit_text || *end != '\0' || parsed == 0 ||
                parsed > 10000ul) {
                trace_enabled = 0;
                fprintf(stderr, "[STRM-CB-TRACE] invalid local limit; trace disabled\n");
            } else {
                limit = (unsigned)parsed;
            }
        }

        atomic_store_explicit(&g_stream_trace_per_event_limit, limit,
                              memory_order_relaxed);
        atomic_store_explicit(&g_stream_trace_enabled, trace_enabled,
                              memory_order_relaxed);
        atomic_store_explicit(&g_stream_trace_initialized, 1,
                              memory_order_release);
        if (trace_enabled) {
            fprintf(stderr,
                    "[STRM-CB-TRACE] enabled per-event-limit=%u summary-interval=%u global-trace-budget-independent=1 read-only=1 NOT_READY\n",
                    limit, STREAM_TRACE_SUMMARY_INTERVAL);
            fflush(stderr);
        }
    }
    atomic_flag_clear_explicit(&g_stream_trace_init_lock, memory_order_release);
}

static void stream_trace_write_summary(uint64_t total)
{
    const uint64_t summary_number = atomic_fetch_add_explicit(
        &g_stream_trace_summary_count, 1, memory_order_relaxed) + 1;
    if (summary_number > STREAM_TRACE_SUMMARY_LIMIT)
        return;

    fprintf(stderr,
        "[STRM-CB-SUMMARY] total=%llu reg=%llu rm_enter=%llu rm_found=%llu rm_miss=%llu caller_in=%llu caller_out=%llu disp_in=%llu disp_next=%llu disp_out=%llu disp_exit=%llu cb_in=%llu node=%llu ret3988=%llu process_in=%llu process_out=%llu cb_out=%llu\n",
        (unsigned long long)total,
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[1], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[2], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[3], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[4], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[5], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[6], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[7], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[8], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[9], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[10], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[11], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[12], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[13], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[14], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[15], memory_order_relaxed),
        (unsigned long long)atomic_load_explicit(&g_stream_trace_event_counts[16], memory_order_relaxed));
    fflush(stderr);
}

void recomp_stream_callback_trace(uint32_t event, uint32_t a, uint32_t b,
                                  uint32_t c, uint32_t d, uint32_t e,
                                  uint32_t f)
{
    if (event == 0 || event >= STREAM_TRACE_EVENT_COUNT)
        return;

    stream_trace_initialize();
    if (!atomic_load_explicit(&g_stream_trace_enabled, memory_order_relaxed))
        return;

    const uint64_t event_number = atomic_fetch_add_explicit(
        &g_stream_trace_event_counts[event], 1, memory_order_relaxed) + 1;
    const uint64_t total = atomic_fetch_add_explicit(
        &g_stream_trace_total, 1, memory_order_relaxed) + 1;
    const unsigned per_event_limit = atomic_load_explicit(
        &g_stream_trace_per_event_limit, memory_order_relaxed);

    if (event_number <= per_event_limit) {
        const uint64_t detail_number = atomic_fetch_add_explicit(
            &g_stream_trace_detail_count, 1, memory_order_relaxed) + 1;
        fprintf(stderr,
                "[STRM-CB] thread=%u event=%s event_seq=%llu total=%llu a=%08X b=%08X c=%08X d=%08X e=%08X f=%08X detail=%llu\n",
                diagnostic_thread_id(),
                g_stream_trace_event_names[event],
                (unsigned long long)event_number,
                (unsigned long long)total, a, b, c, d, e, f,
                (unsigned long long)detail_number);
        fflush(stderr);
    }

    if ((total % STREAM_TRACE_SUMMARY_INTERVAL) == 0)
        stream_trace_write_summary(total);
}
