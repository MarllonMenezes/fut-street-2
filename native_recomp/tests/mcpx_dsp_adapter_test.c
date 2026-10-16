#include "../mcpx_dsp_adapter.h"

#include <assert.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

typedef struct PeripheralProbe {
    uint32_t last_read_address;
    uint32_t last_write_address;
    uint32_t last_write_value;
    unsigned read_count;
    unsigned write_count;
} PeripheralProbe;

static void stage(const char *name)
{
    fprintf(stderr, "[DSP-TEST] %s\n", name);
    fflush(stderr);
}

static uint32_t peripheral_read(void *opaque, uint32_t address)
{
    PeripheralProbe *probe = (PeripheralProbe *)opaque;
    probe->last_read_address = address;
    probe->read_count++;
    return address ^ 0xA5A50000u;
}

static void peripheral_write(void *opaque, uint32_t address, uint32_t value)
{
    PeripheralProbe *probe = (PeripheralProbe *)opaque;
    probe->last_write_address = address;
    probe->last_write_value = value;
    probe->write_count++;
}

static void test_create_requires_peripheral_service(void)
{
    McpxDspConfig config = {0};
    config.peripheral_read = peripheral_read;
    assert(mcpx_dsp_create(&config) == NULL);

    config.peripheral_read = NULL;
    config.peripheral_write = peripheral_write;
    assert(mcpx_dsp_create(&config) == NULL);
}

static McpxDsp *create_test_dsp(PeripheralProbe *probe)
{
    McpxDspConfig config = {
        .peripheral_opaque = probe,
        .peripheral_read = peripheral_read,
        .peripheral_write = peripheral_write,
    };
    McpxDsp *dsp = mcpx_dsp_create(&config);
    assert(dsp != NULL);
    return dsp;
}

static void test_memory_spaces_and_mixbuffer_alias(void)
{
    PeripheralProbe probe = {0};
    McpxDsp *dsp = create_test_dsp(&probe);
    uint32_t values[3] = {0x112233u, 0x445566u, 0x778899u};
    uint32_t actual[3] = {0};

    assert(mcpx_dsp_write(dsp, DSP56300_MEM_SPACE_X, 0x0C00u, values[0]));
    assert(mcpx_dsp_read(dsp, DSP56300_MEM_SPACE_X, 0x1400u) == values[0]);
    assert(mcpx_dsp_write(dsp, DSP56300_MEM_SPACE_X, 0x1401u, values[1]));
    assert(mcpx_dsp_read(dsp, DSP56300_MEM_SPACE_X, 0x0C01u) == values[1]);

    assert(mcpx_dsp_write_run(dsp, DSP56300_MEM_SPACE_X, 0x0FFDu,
                              values, 3));
    assert(mcpx_dsp_read_run(dsp, DSP56300_MEM_SPACE_X, 0x0FFDu,
                             actual, 3));
    assert(memcmp(values, actual, sizeof(values)) == 0);

    assert(mcpx_dsp_write(dsp, DSP56300_MEM_SPACE_Y, 0x07FFu, 0xABCDEFu));
    assert(mcpx_dsp_read(dsp, DSP56300_MEM_SPACE_Y, 0x07FFu) == 0xABCDEFu);
    assert(mcpx_dsp_write(dsp, DSP56300_MEM_SPACE_P, 0x0FFFu, 0xFEDCBAu));
    assert(mcpx_dsp_read(dsp, DSP56300_MEM_SPACE_P, 0x0FFFu) == 0xFEDCBAu);

    assert(mcpx_dsp_read(dsp, DSP56300_MEM_SPACE_X, 0x1000u) == 0);
    assert(mcpx_dsp_read(dsp, DSP56300_MEM_SPACE_Y, 0x0800u) == 0);
    assert(mcpx_dsp_read(dsp, DSP56300_MEM_SPACE_P, 0x1000u) == 0);
    assert(!mcpx_dsp_write(dsp, DSP56300_MEM_SPACE_X, 0x1000u, 1));
    assert(!mcpx_dsp_write_run(dsp, DSP56300_MEM_SPACE_X, 0x0FFFu,
                               values, 2));
    mcpx_dsp_destroy(dsp);
}

static void test_peripheral_callback_routing(void)
{
    PeripheralProbe probe = {0};
    McpxDsp *dsp = create_test_dsp(&probe);
    const uint32_t address = 0xFFFFA5u;
    const uint32_t value = 0x12345678u;

    assert(mcpx_dsp_read(dsp, DSP56300_MEM_SPACE_X, address) ==
           ((address ^ 0xA5A50000u) & 0x00FFFFFFu));
    assert(probe.read_count == 1);
    assert(probe.last_read_address == address);

    assert(mcpx_dsp_write(dsp, DSP56300_MEM_SPACE_X, address, value));
    assert(probe.write_count == 1);
    assert(probe.last_write_address == address);
    assert(probe.last_write_value == (value & 0x00FFFFFFu));
    mcpx_dsp_destroy(dsp);
}

static void test_bootstrap_mask_and_execution(void)
{
    PeripheralProbe probe = {0};
    McpxDsp *dsp = create_test_dsp(&probe);
    uint32_t program[MCPX_DSP_BOOTSTRAP_WORDS];
    Dsp56300State state = {0};
    size_t i;

    for (i = 0; i < MCPX_DSP_BOOTSTRAP_WORDS; ++i)
        program[i] = 0xAB000000u | (uint32_t)i;

    assert(!mcpx_dsp_load_bootstrap(dsp, program,
                                    MCPX_DSP_BOOTSTRAP_WORDS - 1));
    assert(mcpx_dsp_load_bootstrap(dsp, program,
                                   MCPX_DSP_BOOTSTRAP_WORDS));
    for (i = 0; i < MCPX_DSP_BOOTSTRAP_WORDS; ++i)
        assert(mcpx_dsp_read(dsp, DSP56300_MEM_SPACE_P, (uint32_t)i) == i);

    assert(mcpx_dsp_write(dsp, DSP56300_MEM_SPACE_P, 0, 0x0C0000u));
    stage("run bootstrap test program begin");
    assert(mcpx_dsp_run(dsp, 32));
    stage("run bootstrap test program end");
    assert(mcpx_dsp_get_state(dsp, &state));
    assert(state.pc == 0);

    memset(program, 0, sizeof(program));
    assert(mcpx_dsp_load_bootstrap(dsp, program,
                                   MCPX_DSP_BOOTSTRAP_WORDS));
    assert(mcpx_dsp_read(dsp, DSP56300_MEM_SPACE_P, 0) == 0);
    stage("run zero test program begin");
    assert(mcpx_dsp_run(dsp, 8));
    stage("run zero test program end");
    assert(mcpx_dsp_get_state(dsp, &state));
    assert(state.pc != 0);

    stage("reject oversized cycle budget begin");
    assert(!mcpx_dsp_run(dsp, UINT32_MAX));
    stage("reject oversized cycle budget end");
    mcpx_dsp_reset(dsp);
    assert(mcpx_dsp_read(dsp, DSP56300_MEM_SPACE_P, 0) == 0);
    mcpx_dsp_destroy(dsp);
}

static void test_bulk_argument_validation(void)
{
    PeripheralProbe probe = {0};
    McpxDsp *dsp = create_test_dsp(&probe);
    uint32_t value = 0;

    assert(mcpx_dsp_read_run(dsp, DSP56300_MEM_SPACE_X, 0, NULL, 0));
    assert(!mcpx_dsp_read_run(dsp, DSP56300_MEM_SPACE_X, 0, NULL, 1));
    assert(!mcpx_dsp_write_run(dsp, DSP56300_MEM_SPACE_Y, 0, NULL, 1));
    assert(!mcpx_dsp_read_run(dsp, (Dsp56300MemSpace)99, 0, &value, 1));
    assert(!mcpx_dsp_write(dsp, (Dsp56300MemSpace)99, 0, 1));
    assert(!mcpx_dsp_read_run(dsp, DSP56300_MEM_SPACE_X, UINT32_MAX,
                              &value, 1));
    mcpx_dsp_destroy(dsp);
}

static void put_le32(uint8_t *bytes, uint32_t value)
{
    for (unsigned i = 0; i < 4; ++i)
        bytes[i] = (uint8_t)(value >> (i * 8));
}

static void test_scattered_bootstrap(void)
{
    PeripheralProbe probe = {0};
    McpxDsp *dsp = create_test_dsp(&probe);
    uint8_t ram[0x5000] = {0};
    uint32_t before[MCPX_DSP_BOOTSTRAP_WORDS], after[MCPX_DSP_BOOTSTRAP_WORDS];
    Dsp56300State state = {0};
    /* Deliberately reversed, noncontiguous pages; first ends at RAM's end. */
    put_le32(ram + 0x100, 0x4000);
    put_le32(ram + 0x108, 0x2000);
    for (size_t i = 0; i < MCPX_DSP_BOOTSTRAP_WORDS; ++i) {
        size_t offset = (i < 1024 ? 0x4000 : 0x2000) + (i % 1024) * 4;
        put_le32(ram + offset, 0xAB000000u | (uint32_t)(i * 257));
    }
    assert(mcpx_dsp_write(dsp, DSP56300_MEM_SPACE_P,
                          MCPX_DSP_BOOTSTRAP_WORDS, 0xFEDCBA));
    assert(mcpx_dsp_load_bootstrap_sg(dsp, ram, sizeof(ram), 0x100, 1));
    assert(mcpx_dsp_read_run(dsp, DSP56300_MEM_SPACE_P, 0, before,
                             MCPX_DSP_BOOTSTRAP_WORDS));
    for (size_t i = 0; i < MCPX_DSP_BOOTSTRAP_WORDS; ++i)
        assert(before[i] == i * 257);

    /* A bad second page must not partially replace the first half of P. */
    put_le32(ram + 0x4000, 0x654321);
    put_le32(ram + 0x108, 0x4001);
    assert(!mcpx_dsp_load_bootstrap_sg(dsp, ram, sizeof(ram), 0x100, 1));
    put_le32(ram + 0x108, 0xFFFFF001u);
    assert(!mcpx_dsp_load_bootstrap_sg(dsp, ram, sizeof(ram), 0x100, 1));
    put_le32(ram + 0x108, 0x2000);
    assert(!mcpx_dsp_load_bootstrap_sg(dsp, ram, sizeof(ram), 0x100, 0));
    assert(!mcpx_dsp_load_bootstrap_sg(dsp, ram, sizeof(ram), 0x4FF1, 1));
    assert(!mcpx_dsp_load_bootstrap_sg(dsp, ram, sizeof(ram), UINT32_MAX, 1));
    assert(!mcpx_dsp_load_bootstrap_sg(dsp, NULL, sizeof(ram), 0x100, 1));
    assert(!mcpx_dsp_load_bootstrap_sg(dsp, ram, 0, 0x100, 1));
    assert(mcpx_dsp_read_run(dsp, DSP56300_MEM_SPACE_P, 0, after,
                             MCPX_DSP_BOOTSTRAP_WORDS));
    assert(memcmp(before, after, sizeof(before)) == 0);

    put_le32(ram + 0x4000, 0xAB0C0000u); /* synthetic jmp $0 */
    assert(mcpx_dsp_load_bootstrap_sg(dsp, ram, sizeof(ram), 0x100, 1));
    assert(mcpx_dsp_run(dsp, 16));
    assert(mcpx_dsp_get_state(dsp, &state));
    assert(state.pc == 0);
    /* Replacing that compiled branch must invalidate the cached program. */
    memset(ram + 0x4000, 0, 4096);
    memset(ram + 0x2000, 0, 4096);
    assert(mcpx_dsp_load_bootstrap_sg(dsp, ram, sizeof(ram), 0x100, 1));
    assert(mcpx_dsp_run(dsp, 8));
    assert(mcpx_dsp_get_state(dsp, &state));
    assert(state.pc != 0);
    assert(mcpx_dsp_read(dsp, DSP56300_MEM_SPACE_P,
                         MCPX_DSP_BOOTSTRAP_WORDS) == 0xFEDCBA);
    assert(probe.read_count == 0 && probe.write_count == 0);
    mcpx_dsp_destroy(dsp);
}

static McpxDspDmaResult transfer_node(McpxDsp *dsp, uint8_t *ram,
                                      const uint32_t node[7], uint32_t *next)
{
    assert(mcpx_dsp_write_run(dsp, DSP56300_MEM_SPACE_X, 0x20, node, 7));
    return mcpx_dsp_dma_scratch_node(dsp, ram, 0x6000, 0x100, 1, 0x20, next);
}

static void test_dma_scratch_node(void)
{
    PeripheralProbe probe = {0};
    McpxDsp *dsp = create_test_dsp(&probe);
    uint8_t ram[0x6000] = {0}, saved[0x6000];
    uint32_t next = 0xDEADBEEFu, actual[3];
    const uint32_t values[3] = {0x123456, 0xABCDEF, 0xFEDCBA};
    /* One terminal node, format 6, DSP -> scratch, crossing the page boundary. */
    uint32_t node[7] = {0x4000, 0x19E2, 3, 0x200, 4094, 0, 0};
    const uint8_t packed[12] = {0x56,0x34,0x12,0,0xEF,0xCD,0xAB,0,
                                0xBA,0xDC,0xFE,0};
    put_le32(ram + 0x100, 0x4000);
    put_le32(ram + 0x108, 0x2000);
    assert(mcpx_dsp_write_run(dsp, DSP56300_MEM_SPACE_X, 0x200, values, 3));
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_OK);
    assert(next == 0x4000);
    assert(memcmp(ram + 0x4FFE, packed, 2) == 0);
    assert(memcmp(ram + 0x2000, packed + 2, 10) == 0);
    assert(ram[0x4FFD] == 0 && ram[0x200A] == 0);

    /* Reverse into Y, discarding the padding byte of each LE32 word. */
    node[1] &= ~2u; node[3] = 0x1800;
    ram[0x2001] = 0xAA;
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_OK);
    assert(mcpx_dsp_read_run(dsp, DSP56300_MEM_SPACE_Y, 0, actual, 3));
    assert(memcmp(actual, values, sizeof(values)) == 0);

    /* Format 1 uses the upper 16 bits of a DSP word, with no sign extension. */
    node[1] = 0x5E2; node[3] = 0x200; node[4] = 4095;
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_OK);
    assert(ram[0x4FFF] == 0x34 && ram[0x2000] == 0x12);
    assert(ram[0x2001] == 0xCD && ram[0x2002] == 0xAB);
    assert(ram[0x2003] == 0xDC && ram[0x2004] == 0xFE);
    node[1] &= ~2u; node[3] = 0x1400;
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_OK);
    assert(mcpx_dsp_read_run(dsp, DSP56300_MEM_SPACE_X, 0xC00, actual, 3));
    for (size_t i = 0; i < 3; ++i) assert(actual[i] == (values[i] & 0xFFFF00));

    /* A late mapping failure must not commit even the first page. */
    node[1] = 0x19E2; node[3] = 0x200; node[4] = 4094;
    put_le32(ram + 0x108, sizeof(ram));
    memcpy(saved, ram, sizeof(ram)); next = 0xDEADBEEFu;
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_RANGE);
    assert(memcmp(saved, ram, sizeof(ram)) == 0 && next == 0xDEADBEEFu);
    node[1] &= ~2u;
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_RANGE);
    assert(mcpx_dsp_read_run(dsp, DSP56300_MEM_SPACE_X, 0x200, actual, 3));
    assert(memcmp(actual, values, sizeof(values)) == 0);
    put_le32(ram + 0x108, 0x2000);

    const uint32_t rejected_controls[] = {0x19E1,0x19F0,0x19E0|0x4000,
                                          0x9E0,0xDE0,0x1800};
    memcpy(saved, ram, sizeof(ram));
    for (size_t i = 0; i < sizeof(rejected_controls)/sizeof(*rejected_controls); ++i) {
        node[1] = rejected_controls[i];
        assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_UNSUPPORTED);
        assert(memcmp(saved, ram, sizeof(ram)) == 0 && next == 0xDEADBEEFu);
    }
    node[1] = 0x19E2; node[3] = 0xFFF;
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_RANGE);
    node[3] = 0x200; node[2] = 0;
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_INVALID);
    node[2] = 4097;
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_INVALID);
    node[2] = 3;
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_OK);
    assert(mcpx_dsp_dma_scratch_node(dsp, ram, sizeof(ram), 0x100, 0,
                                     0x20, &next) == MCPX_DSP_DMA_RANGE);
    assert(mcpx_dsp_dma_scratch_node(dsp, ram, sizeof(ram), UINT32_MAX, 1,
                                     0x20, &next) == MCPX_DSP_DMA_RANGE);
    assert(mcpx_dsp_dma_scratch_node(dsp, ram, sizeof(ram), 0x100, 1,
                                     0x1000, &next) == MCPX_DSP_DMA_RANGE);

    /* Self-links are returned to the caller; this operation never follows them. */
    node[0] = 0x20;
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_OK && next == 0x20);

    /* First chunk overwrites the second SG entry. Both mappings must already
     * be snapshotted, or the last word would follow a corrupted page base. */
    const uint32_t overlap_values[4] = {0x111111,0x222222,0x333333,0x444444};
    assert(mcpx_dsp_write_run(dsp, DSP56300_MEM_SPACE_X, 0x200, overlap_values, 4));
    put_le32(ram + 0x4FF4, 0x4000);
    put_le32(ram + 0x4FFC, 0x2000);
    node[0] = 0x4000; node[2] = 4; node[4] = 0xFF4;
    assert(mcpx_dsp_write_run(dsp, DSP56300_MEM_SPACE_X, 0x20, node, 7));
    assert(mcpx_dsp_dma_scratch_node(dsp, ram, sizeof(ram), 0x4FF4, 1,
                                     0x20, &next) == MCPX_DSP_DMA_OK);
    assert(ram[0x4FF4] == 0x11 && ram[0x4FFC] == 0x33);
    assert(ram[0x2000] == 0x44 && ram[0x2001] == 0x44 &&
           ram[0x2002] == 0x44 && ram[0x2003] == 0);

    /* Physical base wrap and next-node flags outside the contract fail closed. */
    put_le32(ram + 0x100, 0xFFFFF800u);
    next = 0xDEADBEEFu;
    memcpy(saved, ram, sizeof(ram));
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_RANGE);
    assert(memcmp(saved, ram, sizeof(ram)) == 0 && next == 0xDEADBEEFu);
    put_le32(ram + 0x100, 0x4000);
    node[0] = 0x8000;
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_INVALID);

    /* DMA into executable P memory must invalidate an already compiled branch. */
    assert(mcpx_dsp_write(dsp, DSP56300_MEM_SPACE_P, 0, 0x0C0000));
    assert(mcpx_dsp_run(dsp, 16));
    Dsp56300State state = {0};
    assert(mcpx_dsp_get_state(dsp, &state) && state.pc == 0);
    put_le32(ram + 0x4000, 0); /* NOP replacing jmp $0 */
    node[0] = 0x4000; node[1] = 0x19E0; node[2] = 1;
    node[3] = 0x2800; node[4] = 0;
    assert(transfer_node(dsp, ram, node, &next) == MCPX_DSP_DMA_OK);
    assert(mcpx_dsp_run(dsp, 8));
    assert(mcpx_dsp_get_state(dsp, &state) && state.pc != 0);
    assert(probe.read_count == 0 && probe.write_count == 0);
    mcpx_dsp_destroy(dsp);
}

int main(void)
{
    stage("create callback validation begin");
    test_create_requires_peripheral_service();
    stage("create callback validation end");
    stage("memory maps and mixbuffer alias begin");
    test_memory_spaces_and_mixbuffer_alias();
    stage("memory maps and mixbuffer alias end");
    stage("peripheral callback routing begin");
    test_peripheral_callback_routing();
    stage("peripheral callback routing end");
    stage("bootstrap masking and execution begin");
    test_bootstrap_mask_and_execution();
    stage("bootstrap masking and execution end");
    stage("bulk argument validation begin");
    test_bulk_argument_validation();
    stage("bulk argument validation end");
    stage("scattered bootstrap begin");
    test_scattered_bootstrap();
    stage("scattered bootstrap end");
    stage("isolated scratch DMA begin");
    test_dma_scratch_node();
    stage("isolated scratch DMA end");
    puts("mcpx DSP adapter tests passed");
    return 0;
}
