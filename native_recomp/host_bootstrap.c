#include "recomp_types.h"
#include "xbox_memory_layout.h"
#include "graphics_command_stream.h"
#include "graphics_native_backend.h"
#include "mcpx_aci_host.h"

#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <windows.h>

#define FUT_STREET_2_ENTRY_POINT 0x0024D8BEu
#define FUT_PRCB_CACHE_OFFSET 0x250u
#define FUT_BSS_CALLBACK_SLOT_0 0x00360A70u
#define FUT_BSS_CALLBACK_SLOT_1 0x00360AA8u
#define FUT_BSS_CALLBACK_SLOT_2 0x00360AB4u
#define FUT_MEMORY_CALLBACK_SLOT_0 0x0030BC94u
#define FUT_MEMORY_CALLBACK_SLOT_1 0x0030BC98u

/* These host-runtime declarations match the pinned XboxRecomp kernel.h ABI. */
void xbox_kernel_init(void);
void xbox_kernel_shutdown(void);
void xbox_kernel_bridge_init(void);
void xbox_path_init(const char *game_dir, const char *save_dir);
uint32_t xbox_GetConnectedInterrupt(uint32_t bus);
void xbox_PeekSample(const char *label);

static int read_xbe(const char *path, void **out_data, size_t *out_size)
{
    FILE *file = fopen(path, "rb");
    long file_size;
    void *data;

    if (file == NULL) {
        fprintf(stderr, "Cannot open XBE: %s\n", path);
        return 0;
    }
    if (fseek(file, 0, SEEK_END) != 0 || (file_size = ftell(file)) <= 0 ||
        fseek(file, 0, SEEK_SET) != 0) {
        fprintf(stderr, "Cannot determine XBE size: %s\n", path);
        fclose(file);
        return 0;
    }

    data = malloc((size_t)file_size);
    if (data == NULL) {
        fclose(file);
        return 0;
    }
    if (fread(data, 1, (size_t)file_size, file) != (size_t)file_size) {
        fprintf(stderr, "Cannot read complete XBE: %s\n", path);
        free(data);
        fclose(file);
        return 0;
    }

    fclose(file);
    *out_data = data;
    *out_size = (size_t)file_size;
    return 1;
}

static int environment_variable_is_set(const char *name)
{
    return getenv(name) != NULL;
}

/* Read-only bring-up check for the seven indirect sites that are not fixed
 * kernel thunks. The memory layout deliberately starts the PRCB cache and
 * these callback slots at zero; no guest instruction is called here. */
static int preflight_read32(uint32_t xbox_va, uint32_t *value)
{
    const size_t mapped_size = xbox_GetMappedSize();
    const uint8_t *memory = (const uint8_t *)xbox_GetMemoryOffset();

    if (value == NULL || (uint64_t)xbox_va + sizeof(*value) > mapped_size)
        return 0;
    *value = *(const uint32_t *)(memory + xbox_va);
    return 1;
}

static int validate_preflight_dynamic_guards(void)
{
    uint32_t prcb_va;
    uint32_t prcb_cache;
    uint32_t callback_slot_0;
    uint32_t callback_slot_1;
    uint32_t callback_slot_2;
    uint32_t memory_callback_slot_0;
    uint32_t memory_callback_slot_1;

    if (!preflight_read32(XBOX_FS_BASE + 0x20u, &prcb_va) ||
        !preflight_read32(prcb_va + FUT_PRCB_CACHE_OFFSET, &prcb_cache) ||
        !preflight_read32(FUT_BSS_CALLBACK_SLOT_0, &callback_slot_0) ||
        !preflight_read32(FUT_BSS_CALLBACK_SLOT_1, &callback_slot_1) ||
        !preflight_read32(FUT_BSS_CALLBACK_SLOT_2, &callback_slot_2) ||
        !preflight_read32(FUT_MEMORY_CALLBACK_SLOT_0, &memory_callback_slot_0) ||
        !preflight_read32(FUT_MEMORY_CALLBACK_SLOT_1, &memory_callback_slot_1)) {
        fprintf(stderr, "Cannot inspect indirect-call guard memory.\n");
        return 0;
    }
    if (prcb_cache != 0 || callback_slot_0 != 0 || callback_slot_1 != 0 ||
        callback_slot_2 != 0 || memory_callback_slot_0 != 0 ||
        memory_callback_slot_1 != 0) {
        fprintf(stderr,
                "Unexpected dynamic-call guard state: PRCB cache=0x%08X, "
                "memory callbacks=[0x%08X,0x%08X], "
                "BSS callbacks=[0x%08X,0x%08X,0x%08X].\n",
                prcb_cache, memory_callback_slot_0, memory_callback_slot_1,
                callback_slot_0, callback_slot_1, callback_slot_2);
        return 0;
    }
    printf("Dynamic-call guards OK: PRCB cache=0, memory callback slots="
           "0x%08X/0x%08X and BSS callback slots="
           "0x%08X/0x%08X/0x%08X; no guest code executed.\n",
           memory_callback_slot_0, memory_callback_slot_1, callback_slot_0,
           callback_slot_1, callback_slot_2);
    return 1;
}

/* Read-only snapshots of the title's D3D command/fence and graphics IRQ state.
 * DMA field offsets come from disassembly at 0x002931B0/0x00293500. Sampling
 * does not acknowledge DMA, advance a fence, or signal a guest event. */
static int snapshot_read32(uint32_t va, uint32_t *value)
{
    SIZE_T copied = 0;
    if (va > UINT32_MAX - 3u)
        return 0;
    return ReadProcessMemory(GetCurrentProcess(),
        (const void *)((uintptr_t)xbox_GetMemoryOffset() + va),
        value, sizeof(*value), &copied) && copied == sizeof(*value);
}

typedef struct {
    unsigned release_words;
} GraphicsSnapshotMethods;

static int snapshot_method(void *context, unsigned subchannel,
                           uint32_t method, uint32_t value, size_t offset)
{
    GraphicsSnapshotMethods *methods = context;
    if (method == 0)
        fprintf(stderr, "[GRAPHICS-STREAM] object binding sub=%u handle=%08X offset=%zu\n",
                subchannel, value, offset);
    if (subchannel == 0 && method == 0x1D70u) {
        ++methods->release_words;
        fprintf(stderr, "[GRAPHICS-STREAM] release word=%08X offset=%zu (decoded only)\n",
                value, offset);
    }
    return 1; /* Inspection accepted the word; no renderer work was executed. */
}

static DWORD WINAPI graphics_snapshot_thread(void *stop_event)
{
    for (unsigned sample = 0; sample < 12; ++sample) {
        uint32_t device = 0, start = 0, end = 0, submitted = 0;
        uint32_t consumer_ptr = 0, consumed = 0, channel = 0, put = 0, get = 0;
        uint32_t write_ptr = 0, fence_tag = 0, fence_end = 0;
        uint32_t pfifo_put = 0, pfifo_get = 0;
        uint32_t interrupt = xbox_GetConnectedInterrupt(3), irq_context = 0;
        uint32_t blank = 0, completed = 0, scheduled = 0, retired = 0;
        uint32_t pcrtc_pending = 0, pcrtc_enable = 0, pmc_pending = 0, pmc_enable = 0;
        uint32_t crtc_start = 0;
        uint32_t icall_2a90c0 = 0, icall_2a9200 = 0;
        uint32_t icall_2a9204 = 0, icall_2a9208 = 0;
        uint32_t wait_object = 0, wait_header = 0, wait_signal = 0;
        if (WaitForSingleObject((HANDLE)stop_event, 1000) != WAIT_TIMEOUT)
            break;
        /* Optional read-only samples, including device handshakes while a
         * translated thread spins without making another kernel call. */
        xbox_PeekSample("GRAPHICS-PEEK");
        if (!snapshot_read32(0x0029E968u, &device) || !device ||
            device > UINT32_MAX - 0x1C28u) {
            fprintf(stderr, "[GRAPHICS-SNAPSHOT] device not initialized\n");
            continue;
        }
        snapshot_read32(0x002A90C0u, &icall_2a90c0);
        snapshot_read32(0x002A9200u, &icall_2a9200);
        snapshot_read32(0x002A9204u, &icall_2a9204);
        snapshot_read32(0x002A9208u, &icall_2a9208);
        wait_object = device + 0x1DBCu;
        snapshot_read32(wait_object, &wait_header);
        snapshot_read32(wait_object + 4u, &wait_signal);
        fprintf(stderr,
            "[GRAPHICS-CALLBACKS] sample=%u slot_2A90C0=%08X "
            "slot_2A9200=%08X slot_2A9204=%08X slot_2A9208=%08X "
            "wait_object=%08X header=%08X signal=%08X (read-only)\n",
            sample, icall_2a90c0, icall_2a9200, icall_2a9204, icall_2a9208,
            wait_object, wait_header, wait_signal);
        snapshot_read32(device, &write_ptr);
        snapshot_read32(device + 0x24u, &start);
        snapshot_read32(device + 0x28u, &end);
        snapshot_read32(device + 0x2Cu, &submitted);
        snapshot_read32(device + 0x30u, &consumer_ptr);
        const int consumer_valid = consumer_ptr && snapshot_read32(consumer_ptr, &consumed);
        snapshot_read32(device + 0x1C20u, &channel);
        if (channel && channel <= UINT32_MAX - 0x44u) {
            snapshot_read32(channel + 0x40u, &put);
            snapshot_read32(channel + 0x44u, &get);
        }
        /* Descriptor selected for fence 11 by the observed init call. */
        snapshot_read32(device + 0x8Cu, &fence_tag);
        snapshot_read32(device + 0x90u, &fence_end);
        snapshot_read32(0xFD003240u, &pfifo_put);
        snapshot_read32(0xFD003244u, &pfifo_get);
        if (interrupt && interrupt <= UINT32_MAX - 8u &&
            snapshot_read32(interrupt + 4u, &irq_context) && irq_context &&
            irq_context <= UINT32_MAX - 0x1C7u) {
            snapshot_read32(irq_context + 0x1B8u, &blank);
            snapshot_read32(irq_context + 0x1C0u, &completed);
            snapshot_read32(irq_context + 0x1C4u, &scheduled);
            snapshot_read32(irq_context + 0x1BCu, &retired);
        } else {
            irq_context = 0;
        }
        snapshot_read32(0xFD600100u, &pcrtc_pending);
        snapshot_read32(0xFD600140u, &pcrtc_enable);
        snapshot_read32(0xFD000100u, &pmc_pending);
        snapshot_read32(0xFD000140u, &pmc_enable);
        snapshot_read32(0xFD600800u, &crtc_start);
        fprintf(stderr,
            "[GRAPHICS-SNAPSHOT] sample=%u device=%08X write=%08X ring=[%08X,%08X) "
            "submitted=%08X consumer_ptr=%08X consumed=%08X valid=%d "
            "channel=%08X put=%08X get=%08X pfifo_put=%08X pfifo_get=%08X "
            "fence11_tag=%08X fence11_end=%08X irq=%08X context=%08X blank=%u "
            "completed=%u scheduled=%u retired=%u pcrtc=%08X/%08X pmc=%08X/%08X "
            "crtc_start=%08X (read-only)\n",
            sample, device, write_ptr, start, end, submitted, consumer_ptr,
            consumed, consumer_valid, channel, put, get, pfifo_put, pfifo_get,
            fence_tag, fence_end, interrupt, irq_context, blank, completed,
            scheduled, retired, pcrtc_pending, pcrtc_enable, pmc_pending,
            pmc_enable, crtc_start);
        const uint32_t submitted_end = put | 0x80000000u;
        if (sample == 0 && start && submitted_end > start &&
            submitted_end <= end && submitted_end - start <= 65536u) {
            uint8_t bytes[65536];
            SIZE_T copied = 0;
            if (ReadProcessMemory(GetCurrentProcess(),
                    (const void *)((uintptr_t)xbox_GetMemoryOffset() + start),
                    bytes, submitted_end - start, &copied) && copied == submitted_end - start) {
                HANDLE dump = CreateFileA("graphics-pushbuffer.bin", GENERIC_WRITE,
                    0, NULL, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);
                if (dump != INVALID_HANDLE_VALUE) {
                    DWORD written = 0;
                    if (WriteFile(dump, bytes, (DWORD)copied, &written, NULL) &&
                        written == copied)
                        fprintf(stderr, "[GRAPHICS-SNAPSHOT] captured %lu bytes from %08X\n",
                                written, start);
                    CloseHandle(dump);
                }
                FutGraphicsDecodeResult decoded;
                GraphicsSnapshotMethods methods = {0};
                const FutGraphicsDecodeStatus status = fut_graphics_decode(
                    bytes, copied, snapshot_method, &methods, &decoded);
                fprintf(stderr, "[GRAPHICS-STREAM] status=%d bytes=%zu packets=%zu "
                        "methods=%zu release_words=%u blocked_offset=%zu "
                        "blocked_header=%08X execution=none\n",
                        status, decoded.decoded_bytes, decoded.packets,
                        decoded.methods, methods.release_words,
                        decoded.blocked_offset, decoded.blocked_header);
            }
        }
        fflush(stderr);
    }
    return 0;
}

int main(int argc, char **argv)
{
    void *xbe_data = NULL;
    size_t xbe_size = 0;
    recomp_func_t entry;
    int memory_ready = 0;
    int kernel_ready = 0;
    int result = 1;
    HANDLE snapshot_stop = NULL;
    HANDLE snapshot_thread = NULL;
    McpxAciHost aci_host;
    int aci_host_ready = 0;

    if (argc == 2 && strcmp(argv[1], "--help") == 0) {
        puts("Fut Street 2 static recompilation host");
        puts("Usage: fut-street-2-recompiled --preflight <default.xbe>");
        puts("Usage: fut-street-2-recompiled --preflight <default.xbe> <game-data-directory> <save-directory>");
        puts("Usage: fut-street-2-recompiled --launch <default.xbe> <game-data-directory> <save-directory>");
        puts("This build excludes the XboxRecomp NV2A/APU emulators.");
        return 0;
    }
    /* The optional survey hooks in this build do not execute GPU commands.
     * They must never authorize DMA completion in the shared runtime. */
    if (_putenv_s("RECOMP_EXTERNAL_GRAPHICS_COMPLETION", "1") != 0 ||
        !SetEnvironmentVariableA("RECOMP_EXTERNAL_GRAPHICS_COMPLETION", "1")) {
        fprintf(stderr, "Cannot select native graphics completion ownership.\n");
        return 1;
    }
    if ((argc == 3 || argc == 5) && strcmp(argv[1], "--preflight") == 0) {
        const int runtime_preflight = argc == 5;

        if (environment_variable_is_set("RECOMP_AC97_READY") ||
            environment_variable_is_set("RECOMP_AC97_RESET_ONLY") ||
            environment_variable_is_set("RECOMP_PB_EXEC") ||
            environment_variable_is_set("RECOMP_VBLANK")) {
            fprintf(stderr,
                    "Refusing emulator or synthetic-hardware opt-in in the "
                    "no-emulation preflight.\n");
            return 2;
        }
        if (runtime_preflight &&
            (_putenv_s("RECOMP_TITLE_KEVENTS", "1") != 0 ||
             !SetEnvironmentVariableA("RECOMP_TITLE_KEVENTS", "1"))) {
            fprintf(stderr, "Cannot enable title-built event compatibility.\n");
            return 1;
        }
        if (!read_xbe(argv[2], &xbe_data, &xbe_size))
            return 1;

        if (!xbox_MemoryLayoutInit(xbe_data, xbe_size)) {
            fprintf(stderr, "Xbox memory layout initialization failed.\n");
            goto cleanup;
        }
        memory_ready = 1;

        if (!validate_preflight_dynamic_guards())
            goto cleanup;

        if (runtime_preflight) {
            xbox_kernel_init();
            kernel_ready = 1;
            xbox_path_init(argv[3], argv[4]);
            xbox_kernel_bridge_init();
        }

        if (!recomp_dispatch_init())
            fprintf(stderr, "Dispatch acceleration unavailable; using lookup fallback.\n");

        entry = recomp_lookup(FUT_STREET_2_ENTRY_POINT);
        if (entry == NULL) {
            fprintf(stderr, "No translated entry point at 0x%08X.\n",
                    FUT_STREET_2_ENTRY_POINT);
            goto cleanup;
        }

        if (runtime_preflight) {
            puts("Runtime preflight OK: XBE memory, kernel bridge, dispatch and "
                 "translated entry point resolved; no guest code executed.");
        } else {
            puts("Static preflight OK: XBE memory layout initialized and translated "
                 "entry point resolved; no guest code executed.");
        }
        result = 0;
        goto cleanup;
    }
    if (argc != 5 || strcmp(argv[1], "--launch") != 0) {
        fprintf(stderr,
                "Refusing implicit launch. Use --help, --preflight <default.xbe> "
                "[<game-data-directory> <save-directory>], or explicitly provide "
                "--launch <default.xbe> <game-data-directory> <save-directory>.\n");
        return 2;
    }
    {
        const char *bypass = getenv("RECOMP_AUDIO_EFFECTS_BYPASS");
        if (bypass && strcmp(bypass, "0") != 0) {
            if (strcmp(bypass, "1") != 0) {
                fprintf(stderr, "Invalid RECOMP_AUDIO_EFFECTS_BYPASS; use 0 or 1.\n");
                return 2;
            }
#ifndef FUT_AUDIO_EFFECTS_BYPASS_HOOK
            fprintf(stderr, "Audio bypass requested but this build has no overlay hook.\n");
            return 2;
#else
            fprintf(stderr, "[EXPERIMENTAL-AUDIO-BYPASS] armed: at most 4096 known "
                    "effects submissions; simulated completions; NOT_READY\n");
#endif
        }
    }
    if (environment_variable_is_set("RECOMP_AC97_READY") ||
        environment_variable_is_set("RECOMP_AC97_RESET_ONLY") ||
        environment_variable_is_set("RECOMP_PB_EXEC") ||
        environment_variable_is_set("RECOMP_VBLANK")) {
        fprintf(stderr,
                "Refusing emulator or synthetic-hardware opt-in variables in the "
                "no-emulation build.\n");
        return 2;
    }
    /* This title constructs dispatcher events directly in guest memory. */
    if (_putenv_s("RECOMP_TITLE_KEVENTS", "1") != 0 ||
        !SetEnvironmentVariableA("RECOMP_TITLE_KEVENTS", "1")) {
        fprintf(stderr, "Cannot enable title-built event compatibility.\n");
        return 1;
    }
    if (!read_xbe(argv[2], &xbe_data, &xbe_size))
        return 1;

    if (!xbox_MemoryLayoutInit(xbe_data, xbe_size)) {
        fprintf(stderr, "Xbox memory layout initialization failed.\n");
        goto cleanup;
    }
    memory_ready = 1;

    {
        volatile uint8_t *aci_bar = (volatile uint8_t *)(uintptr_t)(
            (uintptr_t)0xFEC00000u + (uintptr_t)xbox_GetMemoryOffset());
        if (!mcpx_aci_host_start(&aci_host, aci_bar)) {
            fprintf(stderr, "Cannot initialise modeled MCPX ACI service.\n");
            goto cleanup;
        }
        aci_host_ready = 1;
    }

    if (!validate_preflight_dynamic_guards())
        goto cleanup;

    xbox_kernel_init();
    kernel_ready = 1;
    /* Require caller-owned storage so guest writes never default into the
     * user's XboxRecomp profile. Bring-up callers should pass an isolated dir. */
    xbox_path_init(argv[3], argv[4]);
    xbox_kernel_bridge_init();

    if (!recomp_dispatch_init())
        fprintf(stderr, "Dispatch acceleration unavailable; using lookup fallback.\n");

    g_esp = XBOX_STACK_TOP;
    entry = recomp_lookup(FUT_STREET_2_ENTRY_POINT);
    if (entry == NULL) {
        fprintf(stderr, "No translated entry point at 0x%08X.\n",
                FUT_STREET_2_ENTRY_POINT);
        goto cleanup;
    }

    fprintf(stderr,
            "Calling translated entry 0x%08X in experimental bring-up; "
            "this is not a validated game runtime.\n",
            FUT_STREET_2_ENTRY_POINT);
    if (environment_variable_is_set("RECOMP_GRAPHICS_SNAPSHOT")) {
        snapshot_stop = CreateEventA(NULL, TRUE, FALSE, NULL);
        if (snapshot_stop)
            snapshot_thread = CreateThread(NULL, 0, graphics_snapshot_thread,
                                           snapshot_stop, 0, NULL);
        if (!snapshot_stop || !snapshot_thread) {
            fprintf(stderr, "Cannot start read-only graphics snapshot.\n");
            goto cleanup;
        }
    }
    if (!fut_graphics_native_start()) {
        fprintf(stderr, "Cannot start native graphics backend.\n");
        goto cleanup;
    }
    entry();
    result = 0;

cleanup:
    if (aci_host_ready)
        mcpx_aci_host_stop(&aci_host);
    fut_graphics_native_stop();
    if (snapshot_thread) {
        SetEvent(snapshot_stop);
        WaitForSingleObject(snapshot_thread, INFINITE);
        CloseHandle(snapshot_thread);
    }
    if (snapshot_stop)
        CloseHandle(snapshot_stop);
    if (kernel_ready)
        xbox_kernel_shutdown();
    if (memory_ready)
        xbox_MemoryLayoutShutdown();
    free(xbe_data);
    return result;
}
