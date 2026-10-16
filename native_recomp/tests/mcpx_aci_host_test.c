#include "../mcpx_aci_host.h"

#include <assert.h>
#include <stdint.h>
#include <stdio.h>
#include <windows.h>

int main(void)
{
    SYSTEM_INFO system_info;
    uint8_t *bar;
    McpxAciHost host;
    MEMORY_BASIC_INFORMATION memory_info;

    GetSystemInfo(&system_info);
    assert(system_info.dwPageSize >= MCPX_ACI_BAR_SIZE);
    bar = VirtualAlloc(NULL, system_info.dwPageSize,
                       MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
    assert(bar != NULL);
    assert(mcpx_aci_host_start(&host, bar));

    assert(VirtualQuery(bar, &memory_info, sizeof(memory_info)) ==
           sizeof(memory_info));
    assert(memory_info.Protect == PAGE_READONLY);
    assert(*(volatile uint32_t *)(bar + MCPX_ACI_GLOB_STA) == MCPX_ACI_S0CR);

    *(volatile uint32_t *)(bar + MCPX_ACI_GLOB_CNT) = 0x11;
    assert(*(volatile uint32_t *)(bar + MCPX_ACI_GLOB_CNT) == 0x11);
    *(volatile uint32_t *)(bar + MCPX_ACI_GLOB_CNT) = 0x16;
    assert(*(volatile uint32_t *)(bar + MCPX_ACI_GLOB_CNT) == 0x11);

    *(volatile uint32_t *)(bar + MCPX_ACI_GLOB_STA) = MCPX_ACI_S0CR | 1u;
    assert(*(volatile uint32_t *)(bar + MCPX_ACI_GLOB_STA) == MCPX_ACI_S0CR);
    *(volatile uint16_t *)(bar + 0x26) = 0x8000;
    assert(*(volatile uint16_t *)(bar + 0x26) == 0x000F);
    *(volatile uint8_t *)(bar + 0x10B) = 0x03;
    assert(*(volatile uint8_t *)(bar + 0x10B) == 0x01);

    assert(VirtualQuery(bar, &memory_info, sizeof(memory_info)) ==
           sizeof(memory_info));
    assert(memory_info.Protect == PAGE_READONLY);
    mcpx_aci_host_stop(&host);
    assert(VirtualQuery(bar, &memory_info, sizeof(memory_info)) ==
           sizeof(memory_info));
    assert(memory_info.Protect == PAGE_READWRITE);
    assert(VirtualFree(bar, 0, MEM_RELEASE));
    puts("mcpx ACI VEH trap tests passed");
    return 0;
}
