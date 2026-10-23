#include "recomp_types.h"

#include <stddef.h>
#include <stdint.h>
#include <string.h>
#ifdef _WIN32
#include <windows.h>
#endif

void nv2a_pb_scan(uint32_t start_va, uint32_t end_va);
void nv2a_pb_scan_report(void);
void recomp_icall_fail_log(uint32_t va);
void recomp_icall_not_code_log(uint32_t va);
uint8_t recomp_xbox_io_read8(uint16_t port);
void recomp_xbox_cli(void);
void recomp_xbox_sti(void);
uint8_t recomp_xbox_interrupts_enabled(void);
int fut_audio_effects_bypass(uint32_t, uint32_t, volatile uint32_t *);
unsigned fut_audio_effects_bypass_limit(void);
int recomp_frontend_preload_trace_active(void);
void recomp_frontend_preload_vm_trace(uint32_t,uint32_t,uint32_t);
void recomp_frontend_member_trace(uint32_t,uint32_t,uint32_t,uint32_t,uint32_t);
void recomp_frontend_command_trace(uint32_t,uint32_t,uint32_t,uint32_t);
void recomp_frontend_loading_trace(uint32_t,uint32_t,uint32_t,uint32_t,uint32_t,uint32_t,uint32_t);

/* The unit target does not link the generated game code. The manual lookup
 * contract is tested by address identity; the native bring-up validates the
 * real translated implementation. */
void sub_0024B547(void)
{
}
void sub_0023D3A0(void)
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
#ifdef _WIN32
        if (strcmp(argv[1], "frontend-member") == 0) {
            uint32_t memory[64] = {0}, saved[64];
            memory[0]=0x12345678u; memory[1]=0x1A040191u;
            memory[16]=0xCAFEBABEu; memory[17]=0x0A000077u; memory[18]=1u;
            memcpy((char *)memory+136, "_visible", 9);
            memcpy(saved,memory,sizeof(memory));
            g_xbox_mem_offset=(ptrdiff_t)(uintptr_t)memory-(ptrdiff_t)0x10000u;
            g_eax=123; g_esp=456;
            recomp_frontend_member_trace(0x001E6CADu,0x10000u,0x10080u,0x10040u,7u);
            recomp_frontend_command_trace(0x00160840u,0,0,0);
            void *guard=VirtualAlloc(NULL,4096,MEM_COMMIT|MEM_RESERVE,PAGE_NOACCESS);
            if (!guard) return 93;
            g_xbox_mem_offset=(ptrdiff_t)(uintptr_t)guard-(ptrdiff_t)0x10000u;
            recomp_frontend_member_trace(0x001E6CADu,0x10000u,0xFFFFFFFFu,0x10000u,7u);
            VirtualFree(guard,0,MEM_RELEASE);
            g_xbox_mem_offset=(ptrdiff_t)(uintptr_t)memory-(ptrdiff_t)0x10000u;
            recomp_frontend_member_trace(0x001E6CC9u,0,0xFFFFFFFFu,0xFFFFFFFFu,7u);
            SetLastError(0x1234u);
            for (unsigned i=0;i<24u;++i) {
                recomp_frontend_member_trace(0x001E6CADu,0x10000u,0x10080u,0x10040u,7u);
                recomp_frontend_member_trace(0x001E6CC9u,0x10000u,0x10080u,0x10040u,7u);
                recomp_frontend_member_trace(0xDEADBEEFu,0x10000u,0x10080u,0x10040u,7u);
            }
            DWORD error=GetLastError();
            recomp_frontend_command_trace(0x00160859u,0,0,0);
            recomp_frontend_member_trace(0x001E6CC9u,0x10000u,0x10080u,0x10040u,7u);
            int ok=error==0x1234u && g_eax==123 && g_esp==456 &&
                !recomp_frontend_preload_trace_active() && !memcmp(saved,memory,sizeof(memory));
            g_xbox_mem_offset=0;
            return ok ? 0 : 94;
        }
        if (strcmp(argv[1], "frontend-text-boundaries") == 0) {
            void *guard = VirtualAlloc(NULL, 4096, MEM_COMMIT|MEM_RESERVE, PAGE_NOACCESS);
            if (!guard) return 96;
            g_xbox_mem_offset = (ptrdiff_t)(uintptr_t)guard - (ptrdiff_t)0x10000u;
            SetLastError(0x1234u);
            recomp_frontend_command_trace(0x00160DF0u, 0x10000u, 0u, 0u);
            DWORD error = GetLastError();
            VirtualFree(guard, 0, MEM_RELEASE);
            if (error != 0x1234u) return 97;
            char bytes[96]; memset(bytes, 1, sizeof(bytes));
            g_xbox_mem_offset = (ptrdiff_t)(uintptr_t)bytes - (ptrdiff_t)0x10000u;
            recomp_frontend_command_trace(0x00160E2Eu, 0x10000u, 0u, 0u);
            memset(bytes, 'X', sizeof(bytes)); bytes[95] = 0;
            recomp_frontend_command_trace(0x00160ED0u, 0x10000u, 0u, 0u);
            g_xbox_mem_offset = 0;
            return 0;
        }
#endif
        if (strcmp(argv[1], "frontend-command") == 0) {
            char text[] = "boot\n\"\\command";
            g_xbox_mem_offset = (ptrdiff_t)(uintptr_t)text - (ptrdiff_t)0x10000u;
            ptrdiff_t saved_offset = g_xbox_mem_offset;
            g_eax = 123u; g_esp = 456u;
            for (unsigned i = 0; i < 70; ++i) {
                recomp_frontend_command_trace(0x00160DF0u, 0x10000u, 4u, 5u);
                recomp_frontend_command_trace(0x00160E2Eu, 0xFFFFFFFFu, 4u, 0u);
                recomp_frontend_command_trace(0x00160ED0u, 0u, 0u, 0u);
                recomp_frontend_command_trace(0xDEADBEEFu, 0x10000u, 0u, 0u);
            }
            int ok = g_eax == 123u && g_esp == 456u &&
                g_xbox_mem_offset == saved_offset && strcmp(text, "boot\n\"\\command") == 0;
            g_xbox_mem_offset = 0;
            return ok ? 0 : 98;
        }
        if (strcmp(argv[1], "frontend-input") == 0 || strcmp(argv[1], "frontend-song") == 0) {
            char memory[128]={0};
            strcpy(memory,"eatrax?sub=currentsong");strcpy(memory+64,"_level0.feInputEvent");
            g_xbox_mem_offset=(ptrdiff_t)(uintptr_t)memory-(ptrdiff_t)0x10000u;
            g_eax=123;g_esp=456;
#ifdef _WIN32
            SetLastError(0x1234u);
#endif
            if(strcmp(argv[1],"frontend-song")==0){
                for(unsigned i=0;i<100;++i) recomp_frontend_command_trace(0x00160ED0u,0x10000u,0,0);
                for(unsigned i=0;i<70;++i) recomp_frontend_command_trace(0x00160ED0u,0x10040u,0,0);
            }else for(unsigned i=0;i<70;++i){
                recomp_frontend_command_trace(0x00160330u,0x10040u,0x2D7DF0u,0);
                recomp_frontend_command_trace(0x00160429u,0x10040u,0x2D7DF0u,1);
                recomp_frontend_command_trace(0x00160433u,0x10040u,0x2D7DF0u,1);
            }
            int ok=g_eax==123 && g_esp==456 &&
                strcmp(memory,"eatrax?sub=currentsong")==0 && strcmp(memory+64,"_level0.feInputEvent")==0;
#ifdef _WIN32
            ok=ok && GetLastError()==0x1234u;
#endif
            g_xbox_mem_offset=0;return ok?0:99;
        }
        if (strcmp(argv[1], "frontend-vm") == 0) {
            uint32_t memory[] = {1,0,0x1000Cu,0x10010u,0,0x02000000u,0x11223344u,0x55667788u};
            g_xbox_mem_offset = (ptrdiff_t)(uintptr_t)memory - (ptrdiff_t)0x10000u;
            g_eax = 123; g_esp = 456;
            recomp_frontend_preload_vm_trace(1,2,0x10000u);
            if (recomp_frontend_preload_trace_active()) return 91;
            recomp_frontend_command_trace(0x00160840u,0,0,0);
            for (unsigned i=0;i<600u;++i) recomp_frontend_preload_vm_trace(1,2,0x10000u);
            recomp_frontend_command_trace(0x00160859u,0,0,0);
            for (unsigned i=0;i<10u;++i) recomp_frontend_preload_vm_trace(1,2,0x10000u);
            int ok = !recomp_frontend_preload_trace_active() && g_eax==123u && g_esp==456u &&
                memory[0]==1 && memory[6]==0x11223344u;
            g_xbox_mem_offset=0;
            return ok ? 0 : 92;
        }
        if (strcmp(argv[1], "frontend-trace") == 0) {
            const uint32_t sites[] = {0x0015F7B0u,0x001606E0u,0x001630B0u,0x00163570u,0xDEADBEEFu};
            g_eax = 123u; g_esp = 456u;
            for (unsigned i = 0; i < 70; ++i)
                for (unsigned j = 0; j < 5; ++j)
                    recomp_frontend_loading_trace(sites[j], 1, 2, 3, 4, 5, 6);
            return g_eax == 123u && g_esp == 456u && g_xbox_mem_offset == 0 ? 0 : 98;
        }
        if (strncmp(argv[1], "audio-", 6) == 0) {
            if (strcmp(argv[1], "audio-limit-upper") == 0)
                return fut_audio_effects_bypass_limit() == 16384u ? 0 : 97;
            uint32_t words[] = {0xAAAAAAAAu, 3u, 0xBBBBBBBBu};
            g_xbox_mem_offset = (ptrdiff_t)(uintptr_t)&words[1] - (ptrdiff_t)0x83360810u;
            uint32_t site = 0x002872FFu, address = 0x83360810u;
            if (strcmp(argv[1], "audio-site") == 0) ++site;
            if (strcmp(argv[1], "audio-address") == 0) ++address;
            if (strcmp(argv[1], "audio-value") == 0) words[1] = 1;
            if (strcmp(argv[1], "audio-pointer") == 0) {
                fut_audio_effects_bypass(site, address, NULL);
                return 98;
            }
            int applied = fut_audio_effects_bypass(site, address, &words[1]);
            if (strcmp(argv[1], "audio-repeat") == 0) {
                words[1] = 3;
                return fut_audio_effects_bypass(site, address, &words[1]) == 1 &&
                    words[1] == 0 && words[0] == 0xAAAAAAAAu &&
                    words[2] == 0xBBBBBBBBu ? 0 : 98;
            }
            if (strcmp(argv[1], "audio-budget") == 0) {
                for (unsigned i = 1; i < 4097; ++i) {
                    words[1] = 3;
                    fut_audio_effects_bypass(site, address, &words[1]);
                }
                return 98;
            }
            int expect = strcmp(argv[1], "audio-on") == 0;
            return applied == expect && words[1] == (expect ? 0u : 3u) &&
                words[0] == 0xAAAAAAAAu && words[2] == 0xBBBBBBBBu ? 0 : 97;
        }
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
    if (recomp_lookup_manual(0x00240B00u) != sub_0023D3A0)
        return 14;
    if (recomp_lookup_manual(0x0023A4C2u) != NULL ||
        recomp_lookup_manual(0x0023A4C4u) != NULL ||
        recomp_lookup_manual(0x00240AFFu) != NULL ||
        recomp_lookup_manual(0x00240B01u) != NULL)
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
