#ifndef FUT_GRAPHICS_NATIVE_DISPLAY_H
#define FUT_GRAPHICS_NATIVE_DISPLAY_H
#include <d3d11.h>
#include <dxgi1_3.h>
typedef struct {
    HWND window;
    IDXGISwapChain2 *swap;
    HANDLE latency;
    DWORD owner;
    unsigned width,height,staged,pending,count_valid,latency_ready;
    UINT target_present;
} FutNativeDisplay;
typedef struct {
    UINT present_count,present_refresh,sync_refresh;
    /* DXGI synchronization reference, not an exact per-frame presentation
     * timestamp and not a guest clock. Keep the refresh counters alongside it. */
    LARGE_INTEGER sync_qpc;
    unsigned source; /* 0=matching frame statistics, 1=DXGI latency event. */
    LARGE_INTEGER observed_qpc; /* CPU observation time; never a scanout timestamp. */
} FutNativeDisplayReceipt;
/* RECOMP_NATIVE_DISPLAY_ORIGIN=1 opts visible windows into position (0,0)
 * for controlled bring-up; otherwise Windows chooses the initial placement.
 * Single owning thread; windowed flip-sequential BGRA8 only. Failure does
 * not replace an existing instance. No guest queue/flip/interrupt mutations. */
HRESULT fut_native_display_open(FutNativeDisplay *display,ID3D11Device *device,
    unsigned width,unsigned height,int visible);
void fut_native_display_close(FutNativeDisplay *display);
/* Immediate context only. Wait on DXGI latency with messages/cancel/finite timeout, then copy matching
 * single-sample BGRA8 source. Only one staged or submitted image is allowed. */
HRESULT fut_native_display_stage(FutNativeDisplay *display,ID3D11DeviceContext *context,
    ID3D11Texture2D *source,DWORD timeout_ms,HANDLE stop);
/* S_OK means queued, NOT a receipt. Occlusion/still-drawing do not publish a
 * submission. After a queued frame, no replay is allowed until receipt/close. */
HRESULT fut_native_display_submit(FutNativeDisplay *display,unsigned interval);
/* S_OK for matching statistics or a new DXGI frame-latency event for the
 * sole pending present ID. Event receipts leave refresh/sync_qpc zero.
 * S_FALSE means pending;
 * output untouched unless confirmed. No mapping to guest VBlank is asserted. */
HRESULT fut_native_display_poll(FutNativeDisplay *display,FutNativeDisplayReceipt *receipt);
#endif
