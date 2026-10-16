/* Isolated diagnostic: inspect a caller-supplied bootstrap up to its first
 * DMA start. No game host, scratch transfer, fabricated completion or IRQ.
 * Initial peripheral state is explicitly synthetic, not a hardware capture. */
#include "mcpx_dsp_adapter.h"
#include <stdio.h>
#include <stdlib.h>

typedef struct {
    McpxDsp *dsp;
    uint32_t dma[4]; /* NEXT, START, CONTROL, CONFIGURATION */
    unsigned events;
} Probe;

static void stop_probe(int code)
{
    fflush(NULL);
    _Exit(code);
}

static void event(Probe *p, const char *kind, uint32_t address, uint32_t value)
{
    fprintf(stderr, "[DSP-PROBE] %s address=%06X value=%06X\n", kind, address, value);
    if (++p->events > 128) stop_probe(23);
}

static int dump_node(Probe *p, const char *label)
{
    uint32_t node[7], ptr = p->dma[0], offset = ptr;
    Dsp56300MemSpace space = DSP56300_MEM_SPACE_X;
    if (ptr >= 0x1800 && ptr < 0x2000) {
        space = DSP56300_MEM_SPACE_Y; offset -= 0x1800;
    } else if (ptr >= 0x2800 && ptr < 0x3800) {
        space = DSP56300_MEM_SPACE_P; offset -= 0x2800;
    }
    fprintf(stderr, "[DSP-PROBE] %s next=%06X config=%06X\n", label, ptr, p->dma[3]);
    if (ptr >= 0x4000 || !mcpx_dsp_read_run(p->dsp, space, offset, node, 7))
        return 0;
    fprintf(stderr, "[DSP-PROBE] node next=%06X control=%06X count=%06X "
            "dsp=%06X scratch_offset=%06X scratch_base=%06X scratch_limit=%06X\n",
            node[0],node[1],node[2],node[3],node[4],node[5],node[6]);
    return 1;
}

static uint32_t peripheral_read(void *opaque, uint32_t address)
{
    Probe *p = opaque;
    if (address >= 0xFFFFD4 && address <= 0xFFFFD7) {
        uint32_t value = p->dma[address - 0xFFFFD4];
        event(p, "read", address, value);
        return value;
    }
    if (address == 0xFFFFC5) {
        event(p, "read-synthetic-initial-interrupts", address, 0);
        return 0; /* Fixture has no frame producer and no completed DMA. */
    }
    event(p, "unsupported-read", address, 0);
    dump_node(p, "prepared-node-only-no-DMA-start");
    stop_probe(21);
    return 0; /* unreachable */
}

static void peripheral_write(void *opaque, uint32_t address, uint32_t value)
{
    Probe *p = opaque;
    event(p, "write", address, value);
    if (address == 0xFFFFC5) return; /* W1C on fixture's zero interrupt state */
    if (address == 0xFFFFD4 || address == 0xFFFFD5 || address == 0xFFFFD7) {
        p->dma[address - 0xFFFFD4] = value;
        return;
    }
    if (address == 0xFFFFD6 && (value & 7) == 2) {
        p->dma[2] = (p->dma[2] & 8u) | 0x20u; /* STOP, preserving FREEZE */
        return;
    }
    if (address == 0xFFFFD6 && (value & 7) == 3) {
        p->dma[2] |= 8u;
        return;
    }
    if (address == 0xFFFFD6 && (value & 7) == 4) {
        p->dma[2] &= ~8u;
        return;
    }
    if (address == 0xFFFFD6 && (value & 7) == 1) {
        if (!dump_node(p, "first-DMA-start")) stop_probe(24);
        stop_probe(20); /* Captured request, deliberately not executed. */
    }
    fputs("[DSP-PROBE] unsupported peripheral write; stopped\n", stderr);
    dump_node(p, "prepared-node-only-no-DMA-start");
    stop_probe(22);
}

int main(int argc, char **argv)
{
    uint8_t bytes[MCPX_DSP_BOOTSTRAP_WORDS * 4];
    uint32_t program[MCPX_DSP_BOOTSTRAP_WORDS];
    Probe probe = {0};
    if (argc != 2) { fputs("usage: mcpx_dsp_probe bootstrap-le32.bin\n", stderr); return 2; }
    FILE *input = fopen(argv[1], "rb");
    if (!input) return 2;
    size_t got = fread(bytes, 1, sizeof(bytes), input);
    int extra = fgetc(input), error = ferror(input);
    fclose(input);
    if (got != sizeof(bytes) || extra != EOF || error) return 2;
    for (size_t i = 0; i < MCPX_DSP_BOOTSTRAP_WORDS; ++i) {
        const uint8_t *p = bytes + i * 4;
        program[i] = (uint32_t)p[0] | (uint32_t)p[1] << 8 |
                     (uint32_t)p[2] << 16 | (uint32_t)p[3] << 24;
    }
    McpxDspConfig config = {&probe, peripheral_read, peripheral_write};
    probe.dsp = mcpx_dsp_create(&config);
    if (!probe.dsp || !mcpx_dsp_load_bootstrap(probe.dsp, program,
                                             MCPX_DSP_BOOTSTRAP_WORDS)) return 3;
    fputs("[DSP-PROBE] isolated fixture: peripheral state zero, no frame events; "
          "limit=10000 cycles/128 events; external wall timeout required\n", stderr);
    if (!mcpx_dsp_run(probe.dsp, 10000)) return 3;
    Dsp56300State state;
    if (!mcpx_dsp_get_state(probe.dsp, &state)) return 3;
    fprintf(stderr, "[DSP-PROBE] cycle budget ended pc=%06X; no DMA start observed\n", state.pc);
    mcpx_dsp_destroy(probe.dsp);
    return 25;
}
