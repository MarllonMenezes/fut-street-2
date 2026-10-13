#define RECOMP_GENERATED_CODE
#include "recomp_types.h"

int main(void)
{
    RecompXmm a = {0};
    RecompXmm b = {0};
    RecompXmm r;
    RecompMmx ma = {0};
    RecompMmx mb = {0};
    RecompMmx mr;
    int i;

    a.w[0] = 32767; a.w[1] = -32768; a.w[2] = -1; a.w[3] = 0;
    a.w[4] = 7; a.w[5] = -7; a.w[6] = 1234; a.w[7] = -1234;
    b.w[0] = 1; b.w[1] = -1; b.w[2] = 1; b.w[3] = -1;
    b.w[4] = 32767; b.w[5] = -32768; b.w[6] = -1234; b.w[7] = 1234;

    r = XMM_PADDW(a, b);
    if (r.w[0] != -32768 || r.w[1] != 32767 || r.w[2] != 0 ||
        r.w[3] != -1 || r.w[4] != -32762 || r.w[5] != 32761 ||
        r.w[6] != 0 || r.w[7] != 0)
        return 1;

    r = XMM_PSUBW(a, b);
    if (r.w[0] != 32766 || r.w[1] != -32767 || r.w[2] != -2 ||
        r.w[3] != 1 || r.w[4] != -32760 || r.w[5] != 32761 ||
        r.w[6] != 2468 || r.w[7] != -2468)
        return 2;

    for (i = 0; i < 8; ++i) {
        a.uw[i] = 0xFFFFu;
        b.uw[i] = (uint16_t)(i == 0 ? 0xFFFFu : (uint16_t)(i * 0x1111u));
    }
    r = XMM_PMULHUW(a, b);
    if (r.uw[0] != 0xFFFEu || r.uw[1] != 0x1110u ||
        r.uw[2] != 0x2221u || r.uw[3] != 0x3332u ||
        r.uw[4] != 0x4443u || r.uw[5] != 0x5554u ||
        r.uw[6] != 0x6665u || r.uw[7] != 0x7776u)
        return 3;

    a.w[0] = -32768; a.w[1] = -255; a.w[2] = -3; a.w[3] = -1;
    a.w[4] = 0; a.w[5] = 3; a.w[6] = 255; a.w[7] = 32767;
    r = XMM_PSRAW(a, 1);
    if (r.w[0] != -16384 || r.w[1] != -128 || r.w[2] != -2 ||
        r.w[3] != -1 || r.w[4] != 0 || r.w[5] != 1 ||
        r.w[6] != 127 || r.w[7] != 16383)
        return 4;
    r = XMM_PSRAW(a, 64);
    if (r.w[0] != -1 || r.w[1] != -1 || r.w[2] != -1 ||
        r.w[3] != -1 || r.w[4] != 0 || r.w[5] != 0 ||
        r.w[6] != 0 || r.w[7] != 0)
        return 5;

    for (i = 0; i < 4; ++i) {
        ma.uw[i] = 0xFFFFu;
        mb.uw[i] = (uint16_t)(i * 0x1111u);
    }
    mr = MMX_PMULHUW(ma, mb);
    if (mr.uw[0] != 0 || mr.uw[1] != 0x1110u ||
        mr.uw[2] != 0x2221u || mr.uw[3] != 0x3332u)
        return 6;

    return 0;
}
