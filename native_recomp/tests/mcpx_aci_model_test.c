#include "../mcpx_aci.h"

#include <assert.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

static void store_u16(uint8_t *p, uint16_t value)
{
    p[0] = (uint8_t)value;
    p[1] = (uint8_t)(value >> 8);
}

static void store_u32(uint8_t *p, uint32_t value)
{
    p[0] = (uint8_t)value;
    p[1] = (uint8_t)(value >> 8);
    p[2] = (uint8_t)(value >> 16);
    p[3] = (uint8_t)(value >> 24);
}

int main(void)
{
    uint8_t bar[MCPX_ACI_BAR_SIZE];
    McpxAci aci;
    memset(bar, 0xA5, sizeof(bar));
    assert(!mcpx_aci_init(NULL, bar, sizeof(bar)));
    assert(!mcpx_aci_init(&aci, bar, MCPX_ACI_BAR_SIZE - 1));
    assert(mcpx_aci_init(&aci, bar, sizeof(bar)));

    assert(mcpx_aci_read(&aci, MCPX_ACI_GLOB_CNT, 4) == 0);
    assert(mcpx_aci_read(&aci, MCPX_ACI_GLOB_STA, 4) == MCPX_ACI_S0CR);
    assert(mcpx_aci_read(&aci, MCPX_ACI_GLOB_STA + 4, 4) == 0);
    assert(mcpx_aci_read(&aci, 0xFEC, 4) == 0);

    store_u32(bar + MCPX_ACI_GLOB_CNT, 0x11);
    assert(mcpx_aci_write_committed(&aci, MCPX_ACI_GLOB_CNT));
    assert(mcpx_aci_read(&aci, MCPX_ACI_GLOB_CNT, 4) == 0x11);
    store_u32(bar + MCPX_ACI_GLOB_CNT, 0x16); /* CR and WR are commands. */
    assert(mcpx_aci_write_committed(&aci, MCPX_ACI_GLOB_CNT));
    assert(mcpx_aci_read(&aci, MCPX_ACI_GLOB_CNT, 4) == 0x11);

    store_u32(bar + MCPX_ACI_GLOB_STA, MCPX_ACI_S0CR | 1u);
    assert(mcpx_aci_write_committed(&aci, MCPX_ACI_GLOB_STA));
    assert(mcpx_aci_read(&aci, MCPX_ACI_GLOB_STA, 4) == MCPX_ACI_S0CR);

    assert(mcpx_aci_read(&aci, 0x02, 2) == 0);
    store_u16(bar + 0x02, 0x1234);
    assert(mcpx_aci_write_committed(&aci, 0x02));
    assert(mcpx_aci_read(&aci, 0x02, 2) == 0x1234);

    assert(mcpx_aci_read(&aci, 0x26, 2) == 0x000F);
    store_u16(bar + 0x26, 0x8000);
    assert(mcpx_aci_write_committed(&aci, 0x26));
    assert(mcpx_aci_read(&aci, 0x26, 2) == 0x000F);
    assert(mcpx_aci_read(&aci, 0x7C, 2) == 0x8384);
    store_u16(bar + 0x7C, 0);
    assert(mcpx_aci_write_committed(&aci, 0x7C));
    assert(mcpx_aci_read(&aci, 0x7C, 2) == 0x8384);

    bar[0x10B] = 0x03;
    assert(mcpx_aci_write_committed(&aci, 0x10B));
    assert(bar[0x10B] == 0x01);
    assert(!mcpx_aci_write_committed(&aci, MCPX_ACI_BAR_SIZE));
    assert(mcpx_aci_read(&aci, MCPX_ACI_BAR_SIZE, 1) == 0);

    mcpx_aci_reset(&aci);
    assert(mcpx_aci_read(&aci, MCPX_ACI_GLOB_CNT, 4) == 0);
    assert(mcpx_aci_read(&aci, MCPX_ACI_GLOB_STA, 4) == MCPX_ACI_S0CR);
    puts("mcpx ACI model tests passed");
    return 0;
}
