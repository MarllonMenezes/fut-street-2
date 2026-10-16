#ifndef FUT_STREET2_MCPX_ACI_H
#define FUT_STREET2_MCPX_ACI_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

enum {
    MCPX_ACI_BAR_SIZE = 0x1000,
    MCPX_ACI_NAM_SIZE = 0x0100,
    MCPX_ACI_NABM_BASE = 0x0100,
    MCPX_ACI_GLOB_CNT = 0x012C,
    MCPX_ACI_GLOB_STA = 0x0130,
    MCPX_ACI_S0CR = 0x00000100,
};

typedef struct McpxAci {
    volatile uint8_t *bar;
    size_t bar_size;
    uint16_t mixer[128];
    uint32_t glob_cnt;
    uint32_t glob_sta;
    bool initialized;
} McpxAci;

/* ACI layout and reset defaults follow the LGPL xemu MCPX ACI/AC'97 model.
 * This is software-model compatibility, not a claim of silicon fidelity. */
bool mcpx_aci_init(McpxAci *aci, volatile uint8_t *bar, size_t bar_size);
void mcpx_aci_reset(McpxAci *aci);
uint32_t mcpx_aci_read(const McpxAci *aci, uint32_t offset, unsigned size);

/* Call after a host single-step has committed a guest store to the BAR page.
 * The handler supplies the effective BAR offset; supported observed register
 * widths are inferred from the register class and alignment. */
bool mcpx_aci_write_committed(McpxAci *aci, uint32_t offset);

#endif
