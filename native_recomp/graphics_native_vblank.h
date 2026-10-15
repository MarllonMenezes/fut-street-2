#ifndef FUT_GRAPHICS_NATIVE_VBLANK_H
#define FUT_GRAPHICS_NATIVE_VBLANK_H
#include <dxgi.h>
#include <stdint.h>
typedef struct {void *state;} FutNativeVBlank;
typedef struct {
    uint64_t sequence; /* observed successful API waits, not physical refresh count */
    LARGE_INTEGER qpc_after; /* thread wake-up time, not exact raster/guest time */
    HMONITOR monitor;
} FutNativeVBlankReceipt;
/* Explicit output. Single controlling thread. No guest state/interrupt mutation. */
HRESULT fut_native_vblank_start(FutNativeVBlank *clock,IDXGIOutput *output);
/* Latest observation newer than 'after'; skips remain visible in sequence.
 * Failure/cancel/timeout leaves receipt untouched. Finite 1..2000 ms. */
HRESULT fut_native_vblank_wait(FutNativeVBlank *clock,uint64_t after,DWORD timeout_ms,
    HANDLE cancel,FutNativeVBlankReceipt *receipt);
/* On timeout, retain clock and worker handle: retry close on the SAME instance.
 * Never free a state/output still used by a blocked driver call. */
HRESULT fut_native_vblank_close(FutNativeVBlank *clock,DWORD timeout_ms);
#endif
