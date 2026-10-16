#include "mcpx_aci_host.h"

#if defined(_WIN32)
#include <windows.h>
#include <stdio.h>

static McpxAciHost *g_active_host;
static _Thread_local int s_single_stepping;
static _Thread_local uint32_t s_fault_offset;

static LONG CALLBACK mcpx_aci_veh(PEXCEPTION_POINTERS ep)
{
    DWORD code = ep->ExceptionRecord->ExceptionCode;
    DWORD old_protect;
    uintptr_t fault;

    if (g_active_host == NULL || !g_active_host->armed)
        return EXCEPTION_CONTINUE_SEARCH;

    if (code == EXCEPTION_SINGLE_STEP && s_single_stepping) {
        s_single_stepping = 0;
        if (!mcpx_aci_write_committed(&g_active_host->device,
                                      s_fault_offset))
            return EXCEPTION_CONTINUE_SEARCH;
        if (!VirtualProtect(g_active_host->bar_page,
                            g_active_host->page_size, PAGE_READONLY,
                            &old_protect))
            return EXCEPTION_CONTINUE_SEARCH;
        ep->ContextRecord->EFlags &= ~0x100u;
        return EXCEPTION_CONTINUE_EXECUTION;
    }

    if (code != EXCEPTION_ACCESS_VIOLATION ||
        ep->ExceptionRecord->NumberParameters < 2 ||
        ep->ExceptionRecord->ExceptionInformation[0] != 1)
        return EXCEPTION_CONTINUE_SEARCH;

    fault = (uintptr_t)ep->ExceptionRecord->ExceptionInformation[1];
    if (fault < (uintptr_t)g_active_host->bar_page ||
        fault >= (uintptr_t)g_active_host->bar_page + MCPX_ACI_BAR_SIZE)
        return EXCEPTION_CONTINUE_SEARCH;

    s_fault_offset = (uint32_t)(fault - (uintptr_t)g_active_host->bar_page);
    if (!VirtualProtect(g_active_host->bar_page, g_active_host->page_size,
                        PAGE_READWRITE, &old_protect))
        return EXCEPTION_CONTINUE_SEARCH;
    s_single_stepping = 1;
    ep->ContextRecord->EFlags |= 0x100u;
    return EXCEPTION_CONTINUE_EXECUTION;
}

int mcpx_aci_host_start(McpxAciHost *host, volatile uint8_t *bar)
{
    SYSTEM_INFO system_info;
    DWORD old_protect;

    if (host == NULL || bar == NULL || g_active_host != NULL)
        return 0;
    GetSystemInfo(&system_info);
    if (system_info.dwPageSize < MCPX_ACI_BAR_SIZE ||
        (((uintptr_t)bar) % system_info.dwPageSize) != 0)
        return 0;
    ZeroMemory(host, sizeof(*host));
    if (!mcpx_aci_init(&host->device, bar, MCPX_ACI_BAR_SIZE))
        return 0;
    host->bar_page = (void *)bar;
    host->page_size = system_info.dwPageSize;
    host->veh_handle = AddVectoredExceptionHandler(1, mcpx_aci_veh);
    if (host->veh_handle == NULL)
        return 0;
    if (!VirtualProtect(host->bar_page, host->page_size, PAGE_READONLY,
                        &old_protect)) {
        RemoveVectoredExceptionHandler(host->veh_handle);
        ZeroMemory(host, sizeof(*host));
        return 0;
    }
    host->armed = 1;
    g_active_host = host;
    fprintf(stderr,
            "  MCPX ACI: modeled NAM/NABM at 0x%p (xemu AC'97 semantics; "
            "not NV2A-validated)\n", (void *)bar);
    return 1;
}

void mcpx_aci_host_stop(McpxAciHost *host)
{
    DWORD old_protect;
    if (host == NULL || !host->armed)
        return;
    host->armed = 0;
    if (g_active_host == host)
        g_active_host = NULL;
    VirtualProtect(host->bar_page, host->page_size, PAGE_READWRITE,
                   &old_protect);
    if (host->veh_handle != NULL)
        RemoveVectoredExceptionHandler(host->veh_handle);
    ZeroMemory(host, sizeof(*host));
}
#endif
