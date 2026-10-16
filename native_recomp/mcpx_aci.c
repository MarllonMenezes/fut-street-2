#include "mcpx_aci.h"

#include <string.h>

enum {
    AC97_GC_WR = 1u << 2,
    AC97_GC_CR = 1u << 1,
    AC97_GC_VALID_MASK = (1u << 6) - 1,
    AC97_GS_RCS = 1u << 15,
    AC97_GS_S1R1 = 1u << 11,
    AC97_GS_S0R1 = 1u << 10,
    AC97_GS_GSCI = 1u,
    AC97_GS_WCLEAR_MASK = AC97_GS_RCS | AC97_GS_S1R1 |
                          AC97_GS_S0R1 | AC97_GS_GSCI,
    AC97_GS_RO_MASK = (1u << 14) | (1u << 13) | (1u << 12) |
                      (1u << 9) | MCPX_ACI_S0CR | (1u << 7) |
                      (1u << 6) | (1u << 5) | (1u << 4) |
                      (1u << 3) | (1u << 2) | (1u << 1),
    AC97_GS_VALID_MASK = (1u << 18) - 1,
    AC97_POWERDOWN = 0x26,
    AC97_VENDOR_ID1 = 0x7C,
    AC97_VENDOR_ID2 = 0x7E,
    AC97_EXT_AUDIO_ID = 0x28,
};

static uint16_t load_u16(const volatile uint8_t *p)
{
    return (uint16_t)p[0] | (uint16_t)((uint16_t)p[1] << 8);
}

static uint32_t load_u32(const volatile uint8_t *p)
{
    return (uint32_t)p[0] | ((uint32_t)p[1] << 8) |
           ((uint32_t)p[2] << 16) | ((uint32_t)p[3] << 24);
}

static void store_u16(volatile uint8_t *p, uint16_t value)
{
    p[0] = (uint8_t)value;
    p[1] = (uint8_t)(value >> 8);
}

static void store_u32(volatile uint8_t *p, uint32_t value)
{
    p[0] = (uint8_t)value;
    p[1] = (uint8_t)(value >> 8);
    p[2] = (uint8_t)(value >> 16);
    p[3] = (uint8_t)(value >> 24);
}

static void sync_mixer(McpxAci *aci, uint32_t index)
{
    if (index < 128)
        store_u16(aci->bar + index * 2, aci->mixer[index]);
}

static void reset_mixer(McpxAci *aci)
{
    memset(aci->mixer, 0, sizeof(aci->mixer));
    aci->mixer[AC97_POWERDOWN / 2] = 0x000F;
    aci->mixer[AC97_VENDOR_ID1 / 2] = 0x8384;
    aci->mixer[AC97_VENDOR_ID2 / 2] = 0x7600;
    aci->mixer[AC97_EXT_AUDIO_ID / 2] = 0x0809;
    aci->mixer[0x2A / 2] = 0x0009;
    aci->mixer[0x2C / 2] = 0xBB80;
    aci->mixer[0x2E / 2] = 0xBB80;
    aci->mixer[0x30 / 2] = 0xBB80;
    aci->mixer[0x32 / 2] = 0xBB80;
    aci->mixer[0x34 / 2] = 0xBB80;
    for (uint32_t i = 0; i < 128; ++i)
        sync_mixer(aci, i);
}

bool mcpx_aci_init(McpxAci *aci, volatile uint8_t *bar, size_t bar_size)
{
    if (aci == NULL || bar == NULL || bar_size < MCPX_ACI_BAR_SIZE)
        return false;
    memset(aci, 0, sizeof(*aci));
    aci->bar = bar;
    aci->bar_size = bar_size;
    aci->initialized = true;
    mcpx_aci_reset(aci);
    return true;
}

void mcpx_aci_reset(McpxAci *aci)
{
    if (aci == NULL || !aci->initialized)
        return;
    for (uint32_t i = 0; i < MCPX_ACI_BAR_SIZE; ++i)
        aci->bar[i] = 0;
    aci->glob_cnt = 0;
    aci->glob_sta = 0;
    store_u32(aci->bar + MCPX_ACI_GLOB_CNT, aci->glob_cnt);
    store_u32(aci->bar + MCPX_ACI_GLOB_STA,
              aci->glob_sta | MCPX_ACI_S0CR);
    reset_mixer(aci);
}

uint32_t mcpx_aci_read(const McpxAci *aci, uint32_t offset, unsigned size)
{
    if (aci == NULL || !aci->initialized || size == 0 || size > 4 ||
        offset > aci->bar_size || size > aci->bar_size - offset)
        return 0;
    if (size == 4 && offset == MCPX_ACI_GLOB_CNT)
        return aci->glob_cnt;
    if (size == 4 && offset == MCPX_ACI_GLOB_STA)
        return aci->glob_sta | MCPX_ACI_S0CR;
    if (offset >= MCPX_ACI_NABM_BASE && offset < MCPX_ACI_NABM_BASE + 0x80 &&
        offset >= MCPX_ACI_GLOB_STA + 4)
        return 0;
    if (offset < MCPX_ACI_NAM_SIZE && size == 2 && (offset & 1u) == 0)
        return aci->mixer[offset / 2];
    if (size == 1)
        return aci->bar[offset];
    if (size == 2)
        return load_u16(aci->bar + offset);
    return load_u32(aci->bar + offset);
}

bool mcpx_aci_write_committed(McpxAci *aci, uint32_t offset)
{
    uint32_t value;
    uint32_t index;

    if (aci == NULL || !aci->initialized || offset >= MCPX_ACI_BAR_SIZE)
        return false;

    if (offset == MCPX_ACI_GLOB_CNT) {
        value = load_u32(aci->bar + offset);
        /* xemu's generic AC'97 model currently leaves WR/CR requests
         * uncommitted (TODO); match its observable GLOB_CNT readback. */
        if ((value & (AC97_GC_WR | AC97_GC_CR)) == 0)
            aci->glob_cnt = value & AC97_GC_VALID_MASK;
        store_u32(aci->bar + offset, aci->glob_cnt);
        return true;
    }

    if (offset == MCPX_ACI_GLOB_STA) {
        value = load_u32(aci->bar + offset);
        aci->glob_sta &= ~(value & AC97_GS_WCLEAR_MASK);
        aci->glob_sta |= value & ~(AC97_GS_WCLEAR_MASK | AC97_GS_RO_MASK) &
                         AC97_GS_VALID_MASK;
        store_u32(aci->bar + offset,
                  aci->glob_sta | MCPX_ACI_S0CR);
        return true;
    }

    if (offset < MCPX_ACI_NAM_SIZE && (offset & 1u) == 0) {
        index = offset / 2;
        value = load_u16(aci->bar + offset);
        if (index == AC97_POWERDOWN / 2) {
            value = (value & ~0x800Fu) | (aci->mixer[index] & 0x000Fu);
        } else if (offset == AC97_VENDOR_ID1 ||
                   offset == AC97_VENDOR_ID2 ||
                   offset == AC97_EXT_AUDIO_ID) {
            /* These identify/describe the codec and are read-only. */
            value = aci->mixer[index];
        }
        aci->mixer[index] = (uint16_t)value;
        sync_mixer(aci, index);
        return true;
    }

    if (offset >= MCPX_ACI_NABM_BASE + 0x0B &&
        offset < MCPX_ACI_NABM_BASE + 0x80 &&
        ((offset - (MCPX_ACI_NABM_BASE + 0x0B)) & 0x0F) == 0) {
        aci->bar[offset] &= (uint8_t)~0x02u; /* Bus-master RR self-clears. */
        return true;
    }

    if (offset >= MCPX_ACI_NABM_BASE + 0x30 &&
        offset < MCPX_ACI_NABM_BASE + 0x80)
        return true; /* Unimplemented/reserved global registers read as zero. */
    return true;
}
