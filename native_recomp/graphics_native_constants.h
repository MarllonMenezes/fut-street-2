#ifndef FUT_GRAPHICS_NATIVE_CONSTANTS_H
#define FUT_GRAPHICS_NATIVE_CONSTANTS_H

#include <windows.h>
#include <d3d11.h>
#include <stdint.h>

typedef struct {
    ID3D11DeviceContext *context; /* Borrowed from the owning renderer. */
    ID3D11Buffer *buffer;
    ID3D11Query *completion;
    HANDLE poll_timer; /* Optional process-local high-resolution polling wait. */
    int profile_waits;
    uint64_t wait_count, wait_us, max_wait_us;
    struct { D3D11_TEXTURE2D_DESC desc; ID3D11Texture2D *texture; } readbacks[4];
    unsigned readback_next;
    uint64_t readback_hits, readback_creations;
} FutNativeConstants;

HRESULT fut_native_constants_init(FutNativeConstants *native,
    ID3D11Device *device, ID3D11DeviceContext *context);
/* Optional raw constant upload at VS b1 (the NV microcode translator's slot),
 * then wait for actual native GPU completion.
 * This operation never writes any guest memory or completion marker. */
int fut_native_constants_finish(FutNativeConstants *native, const uint32_t (*constants)[4],
    DWORD timeout_ms, HANDLE stop_event);
/* Returns one owned reference; cached allocations never cache pixel contents.
 * The caller must issue a fresh copy and prove GPU completion before reading. */
HRESULT fut_native_readback_texture(FutNativeConstants *native, ID3D11Device *device,
    const D3D11_TEXTURE2D_DESC *desc, ID3D11Texture2D **texture);
void fut_native_constants_destroy(FutNativeConstants *native);

#endif
