#ifndef FUT_GRAPHICS_NATIVE_CONSTANTS_H
#define FUT_GRAPHICS_NATIVE_CONSTANTS_H

#include <windows.h>
#include <d3d11.h>
#include <stdint.h>

typedef struct {
    ID3D11DeviceContext *context; /* Borrowed from the owning renderer. */
    ID3D11Buffer *buffer;
    ID3D11Query *completion;
} FutNativeConstants;

HRESULT fut_native_constants_init(FutNativeConstants *native,
    ID3D11Device *device, ID3D11DeviceContext *context);
/* Optional raw constant upload at VS b1 (the NV microcode translator's slot),
 * then wait for actual native GPU completion.
 * This operation never writes any guest memory or completion marker. */
int fut_native_constants_finish(FutNativeConstants *native, const uint32_t (*constants)[4],
    DWORD timeout_ms, HANDLE stop_event);
void fut_native_constants_destroy(FutNativeConstants *native);

#endif
