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
    puts("mcpx DSP adapter tests passed");
    return 0;
}
