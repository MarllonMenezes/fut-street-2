#include <stdint.h>
#include <stddef.h>
#include <stdlib.h>
#include <string.h>

ptrdiff_t g_xbox_mem_offset;
void fut_native_pcm_configure(uint32_t format);
int fut_native_pcm_position(uint32_t ring, uint32_t frames, uint32_t rate, uint32_t *position);
void fut_native_pcm_submit(uint32_t ring, uint32_t filled);

int main(int argc, char **argv)
{
    if (argc != 2) return 2;
    if (!strcmp(argv[1], "off")) {
        _putenv("RECOMP_NATIVE_PCM_OUTPUT=0");
        uint32_t untouched = 0x12345678u;
        fut_native_pcm_configure(0);
        if (fut_native_pcm_position(0, 0, 0, &untouched) || untouched != 0x12345678u) return 1;
        fut_native_pcm_submit(0, 0);
        return 0;
    }
    if (!strcmp(argv[1], "invalid-option")) {
        _putenv("RECOMP_NATIVE_PCM_OUTPUT=yes");
        fut_native_pcm_configure(0);
        return 1;
    }
    _putenv("RECOMP_NATIVE_PCM_OUTPUT=1");
    if (!strcmp(argv[1], "invalid-pointer")) {
        fut_native_pcm_configure(0);
        return 1;
    }
    uint16_t format[9] = {1, 6, 0xBB80, 0, 0xCA00, 8, 12, 16, 0};
    g_xbox_mem_offset = (ptrdiff_t)(uintptr_t)format - 0x10000;
    if (!strcmp(argv[1], "invalid-format")) {
        format[1] = 2;
        fut_native_pcm_configure(0x10000u);
        return 1;
    }
    if (!strcmp(argv[1], "valid-format")) {
        fut_native_pcm_configure(0x10000u);
        return 0;
    }
    return 2;
}
