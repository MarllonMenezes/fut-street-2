#ifndef FUT_STREET2_MCPX_DSP_ADAPTER_H
#define FUT_STREET2_MCPX_DSP_ADAPTER_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#include "dsp56300.h"

#ifdef __cplusplus
extern "C" {
#endif

#define MCPX_DSP_X_RAM_WORDS 0x1000u
#define MCPX_DSP_Y_RAM_WORDS 0x0800u
#define MCPX_DSP_P_RAM_WORDS 0x1000u
#define MCPX_DSP_BOOTSTRAP_WORDS 0x0800u

typedef struct McpxDsp McpxDsp;

typedef uint32_t (*McpxDspPeripheralRead)(void *opaque, uint32_t address);
typedef void (*McpxDspPeripheralWrite)(void *opaque, uint32_t address,
                                       uint32_t value);

typedef struct McpxDspConfig {
    void *peripheral_opaque;
    McpxDspPeripheralRead peripheral_read;
    McpxDspPeripheralWrite peripheral_write;
} McpxDspConfig;

/* Both peripheral callbacks are required; absent device behavior must not
 * silently become a zero-valued fake peripheral. */
McpxDsp *mcpx_dsp_create(const McpxDspConfig *config);
void mcpx_dsp_destroy(McpxDsp *dsp);

void mcpx_dsp_reset(McpxDsp *dsp);
bool mcpx_dsp_run(McpxDsp *dsp, uint32_t cycles);
bool mcpx_dsp_get_state(const McpxDsp *dsp, Dsp56300State *out);

uint32_t mcpx_dsp_read(McpxDsp *dsp, Dsp56300MemSpace space,
                       uint32_t address);
bool mcpx_dsp_write(McpxDsp *dsp, Dsp56300MemSpace space,
                    uint32_t address, uint32_t value);
bool mcpx_dsp_read_run(McpxDsp *dsp, Dsp56300MemSpace space,
                       uint32_t address, uint32_t *out, size_t count);
bool mcpx_dsp_write_run(McpxDsp *dsp, Dsp56300MemSpace space,
                        uint32_t address, const uint32_t *values,
                        size_t count);

/* Load the 0x800-word GP/EP bootstrap image into P memory, mask each word to
 * 24 bits, and invalidate translated code. This mirrors the observed xemu
 * bootstrap contract; it is not a claim of measured NV2A behavior. */
bool mcpx_dsp_load_bootstrap(McpxDsp *dsp, const uint32_t *words,
                             size_t word_count);

/* Isolated scratch bootstrap: read 0x800 little-endian 32-bit words from
 * two 4 KiB pages described by 8-byte SG entries (LE32 physical base first).
 * max_sge is an inclusive index, not an entry count. RAM and its table must
 * remain stable during the call. Invalid input leaves P memory unchanged.
 * This does not start the DSP, model DMA registers or acknowledge a command.
 * Layout reference: xemu 36334d95, gp_ep.c and dsp_jit.c; no hardware claim. */
bool mcpx_dsp_load_bootstrap_sg(McpxDsp *dsp, const uint8_t *physical_ram,
                               size_t ram_size, uint32_t table_address,
                               uint32_t max_sge);

typedef enum McpxDspDmaResult {
    MCPX_DSP_DMA_OK,
    MCPX_DSP_DMA_INVALID,
    MCPX_DSP_DMA_UNSUPPORTED,
    MCPX_DSP_DMA_RANGE
} McpxDspDmaResult;

/* Execute exactly one seven-word DMA node from DSP RAM, under exclusive
 * caller ownership of DSP and physical_ram. Only linear scratch buffer F,
 * formats 1 (16-bit, shifted by 8) and 6 (24-bit LSB in LE32) are supported.
 * Other controls, including interleave and offset writeback, are refused.
 * next_node is written only after success; no chain traversal, status,
 * interrupt, command ACK or device timing is synthesized. Output parameters
 * must not alias DSP/RAM. On failure both memories and next_node are unchanged.
 * Reference contract: xemu 36334d95 dsp_dma.c; actual title nodes unverified. */
McpxDspDmaResult mcpx_dsp_dma_scratch_node(
    McpxDsp *dsp, uint8_t *physical_ram, size_t ram_size,
    uint32_t table_address, uint32_t max_sge,
    uint32_t node_address, uint32_t *next_node);

#ifdef __cplusplus
}
#endif

#endif
