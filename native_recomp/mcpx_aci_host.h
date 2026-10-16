#ifndef FUT_STREET2_MCPX_ACI_HOST_H
#define FUT_STREET2_MCPX_ACI_HOST_H

#include "mcpx_aci.h"

#if defined(_WIN32)
typedef struct McpxAciHost {
    McpxAci device;
    void *bar_page;
    void *veh_handle;
    size_t page_size;
    int armed;
} McpxAciHost;

int mcpx_aci_host_start(McpxAciHost *host, volatile uint8_t *bar);
void mcpx_aci_host_stop(McpxAciHost *host);
#endif

#endif
