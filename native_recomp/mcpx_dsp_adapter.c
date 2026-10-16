#include "mcpx_dsp_adapter.h"

#include <limits.h>
#include <stdlib.h>

#define MCPX_DSP_MIX_X_START 0x1400u
#define MCPX_DSP_MIX_X_END 0x1800u
#define MCPX_DSP_MIX_X_OFFSET 0x0C00u
#define MCPX_DSP_PERIPHERAL_START 0xFFFF80u
#define MCPX_DSP_PERIPHERAL_END 0x1000000u

struct McpxDsp {
    Dsp56300Jit *jit;
    void *peripheral_opaque;
    McpxDspPeripheralRead peripheral_read;
    McpxDspPeripheralWrite peripheral_write;
    uint32_t xram[MCPX_DSP_X_RAM_WORDS];
    uint32_t yram[MCPX_DSP_Y_RAM_WORDS];
    uint32_t pram[MCPX_DSP_P_RAM_WORDS];
};

static uint32_t read_peripheral(void *opaque, uint32_t address)
{
    McpxDsp *dsp = (McpxDsp *)opaque;
    return dsp->peripheral_read(dsp->peripheral_opaque, address);
}

static void write_peripheral(void *opaque, uint32_t address, uint32_t value)
{
    McpxDsp *dsp = (McpxDsp *)opaque;
    dsp->peripheral_write(dsp->peripheral_opaque, address, value);
}

static bool valid_space(Dsp56300MemSpace space)
{
    return space == DSP56300_MEM_SPACE_X ||
           space == DSP56300_MEM_SPACE_Y ||
           space == DSP56300_MEM_SPACE_P;
}

static bool span_in_region(uint32_t address, size_t count,
                           uint32_t start, uint32_t end)
{
    if (count == 0)
        return true;
    return address >= start && address < end &&
           count <= (size_t)(end - address);
}

static bool valid_span(Dsp56300MemSpace space, uint32_t address,
                       size_t count)
{
    if (count > UINT32_MAX)
        return false;
    if (count == 0)
        return true;

    switch (space) {
    case DSP56300_MEM_SPACE_X:
        return span_in_region(address, count, 0, MCPX_DSP_X_RAM_WORDS) ||
               span_in_region(address, count, MCPX_DSP_MIX_X_START,
                              MCPX_DSP_MIX_X_END) ||
               span_in_region(address, count, MCPX_DSP_PERIPHERAL_START,
                              MCPX_DSP_PERIPHERAL_END);
    case DSP56300_MEM_SPACE_Y:
        return span_in_region(address, count, 0, MCPX_DSP_Y_RAM_WORDS);
    case DSP56300_MEM_SPACE_P:
        return span_in_region(address, count, 0, MCPX_DSP_P_RAM_WORDS);
    default:
        return false;
    }
}

static bool valid_run(uint32_t address, size_t count)
{
    if (count > UINT32_MAX)
        return false;
    return (uint32_t)count <= UINT32_MAX - address;
}

McpxDsp *mcpx_dsp_create(const McpxDspConfig *config)
{
    McpxDsp *dsp;
    Dsp56300MemoryRegion x_regions[3];
    Dsp56300MemoryRegion y_regions[1];
    Dsp56300MemoryRegion p_regions[1];
    Dsp56300CreateInfo info = {0};

    if (config == NULL || config->peripheral_read == NULL ||
        config->peripheral_write == NULL)
        return NULL;

    dsp = (McpxDsp *)calloc(1, sizeof(*dsp));
    if (dsp == NULL)
        return NULL;

    dsp->peripheral_opaque = config->peripheral_opaque;
    dsp->peripheral_read = config->peripheral_read;
    dsp->peripheral_write = config->peripheral_write;

    x_regions[0] = (Dsp56300MemoryRegion){
        .start = 0,
        .end = MCPX_DSP_X_RAM_WORDS,
        .kind = DSP56300_REGION_BUFFER,
        .data.buffer = {.base = dsp->xram, .offset = 0},
    };
    x_regions[1] = (Dsp56300MemoryRegion){
        .start = MCPX_DSP_MIX_X_START,
        .end = MCPX_DSP_MIX_X_END,
        .kind = DSP56300_REGION_BUFFER,
        .data.buffer = {.base = dsp->xram, .offset = MCPX_DSP_MIX_X_OFFSET},
    };
    x_regions[2] = (Dsp56300MemoryRegion){
        .start = MCPX_DSP_PERIPHERAL_START,
        .end = MCPX_DSP_PERIPHERAL_END,
        .kind = DSP56300_REGION_CALLBACK,
        .data.callback = {
            .opaque = dsp,
            .read = read_peripheral,
            .write = write_peripheral,
        },
    };
    y_regions[0] = (Dsp56300MemoryRegion){
        .start = 0,
        .end = MCPX_DSP_Y_RAM_WORDS,
        .kind = DSP56300_REGION_BUFFER,
        .data.buffer = {.base = dsp->yram, .offset = 0},
    };
    p_regions[0] = (Dsp56300MemoryRegion){
        .start = 0,
        .end = MCPX_DSP_P_RAM_WORDS,
        .kind = DSP56300_REGION_BUFFER,
        .data.buffer = {.base = dsp->pram, .offset = 0},
    };

    info.memory_map.x_regions = x_regions;
    info.memory_map.x_count = (uint32_t)(sizeof(x_regions) / sizeof(x_regions[0]));
    info.memory_map.y_regions = y_regions;
    info.memory_map.y_count = (uint32_t)(sizeof(y_regions) / sizeof(y_regions[0]));
    info.memory_map.p_regions = p_regions;
    info.memory_map.p_count = (uint32_t)(sizeof(p_regions) / sizeof(p_regions[0]));

    dsp->jit = dsp56300_create(&info);
    if (dsp->jit == NULL) {
        free(dsp);
        return NULL;
    }
    return dsp;
}

void mcpx_dsp_destroy(McpxDsp *dsp)
{
    if (dsp == NULL)
        return;
    dsp56300_destroy(dsp->jit);
    free(dsp);
}

void mcpx_dsp_reset(McpxDsp *dsp)
{
    if (dsp != NULL)
        dsp56300_reset(dsp->jit);
}

bool mcpx_dsp_run(McpxDsp *dsp, uint32_t cycles)
{
    if (dsp == NULL || cycles > INT32_MAX)
        return false;
    dsp56300_run(dsp->jit, (int32_t)cycles);
    return true;
}

bool mcpx_dsp_get_state(const McpxDsp *dsp, Dsp56300State *out)
{
    if (dsp == NULL || out == NULL)
        return false;
    dsp56300_get_state(dsp->jit, out);
    return true;
}

uint32_t mcpx_dsp_read(McpxDsp *dsp, Dsp56300MemSpace space,
                       uint32_t address)
{
    if (dsp == NULL || !valid_space(space))
        return 0;
    return dsp56300_read_memory(dsp->jit, space, address);
}

bool mcpx_dsp_write(McpxDsp *dsp, Dsp56300MemSpace space,
                    uint32_t address, uint32_t value)
{
    if (dsp == NULL || !valid_space(space) ||
        !valid_span(space, address, 1))
        return false;
    dsp56300_write_memory(dsp->jit, space, address, value);
    return true;
}

bool mcpx_dsp_read_run(McpxDsp *dsp, Dsp56300MemSpace space,
                       uint32_t address, uint32_t *out, size_t count)
{
    if (dsp == NULL || !valid_space(space) ||
        (count != 0 && out == NULL) || !valid_run(address, count) ||
        !valid_span(space, address, count))
        return false;
    if (count == 0)
        return true;
    dsp56300_read_memory_run(dsp->jit, space, address, out, (uint32_t)count);
    return true;
}

bool mcpx_dsp_write_run(McpxDsp *dsp, Dsp56300MemSpace space,
                        uint32_t address, const uint32_t *values,
                        size_t count)
{
    if (dsp == NULL || !valid_space(space) ||
        (count != 0 && values == NULL) || !valid_run(address, count) ||
        !valid_span(space, address, count))
        return false;
    if (count == 0)
        return true;
    dsp56300_write_memory_run(dsp->jit, space, address, values,
                              (uint32_t)count);
    return true;
}

bool mcpx_dsp_load_bootstrap(McpxDsp *dsp, const uint32_t *words,
                             size_t word_count)
{
    size_t i;

    if (dsp == NULL || words == NULL ||
        word_count != MCPX_DSP_BOOTSTRAP_WORDS)
        return false;

    for (i = 0; i < word_count; ++i)
        dsp->pram[i] = words[i] & 0x00FFFFFFu;

    dsp56300_invalidate_cache(dsp->jit);
    return true;
}

static uint32_t read_le32(const uint8_t *bytes)
{
    return (uint32_t)bytes[0] | ((uint32_t)bytes[1] << 8) |
           ((uint32_t)bytes[2] << 16) | ((uint32_t)bytes[3] << 24);
}

/* Original implementation of the narrow scratch layout contract. All input
 * checks precede the existing bootstrap commit and cache invalidation. */
bool mcpx_dsp_load_bootstrap_sg(McpxDsp *dsp, const uint8_t *physical_ram,
                               size_t ram_size, uint32_t table_address,
                               uint32_t max_sge)
{
    uint32_t words[MCPX_DSP_BOOTSTRAP_WORDS];
    uint32_t pages[2];
    if (dsp == NULL || physical_ram == NULL || max_sge < 1u ||
        table_address > UINT32_MAX - 15u ||
        (size_t)table_address > ram_size ||
        ram_size - (size_t)table_address < 16u)
        return false;

    for (size_t page = 0; page < 2; ++page) {
        pages[page] = read_le32(physical_ram + table_address + page * 8u);
        if (pages[page] > UINT32_MAX - 4095u ||
            (size_t)pages[page] > ram_size ||
            ram_size - (size_t)pages[page] < 4096u)
            return false;
    }
    for (size_t i = 0; i < MCPX_DSP_BOOTSTRAP_WORDS; ++i) {
        const size_t byte_offset = i * 4u;
        words[i] = read_le32(physical_ram + pages[byte_offset / 4096u] +
                            byte_offset % 4096u);
    }
    return mcpx_dsp_load_bootstrap(dsp, words, MCPX_DSP_BOOTSTRAP_WORDS);
}
