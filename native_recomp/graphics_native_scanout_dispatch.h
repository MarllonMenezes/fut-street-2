#ifndef FUT_GRAPHICS_NATIVE_SCANOUT_DISPATCH_H
#define FUT_GRAPHICS_NATIVE_SCANOUT_DISPATCH_H

#include <stdint.h>

typedef struct {
    int stack_slot;
    uint32_t stack_top;
    uint32_t tib;
} FutNativeScanoutDispatch;

typedef enum {
    FUT_SCANOUT_DISPATCH_OK = 1,
    FUT_SCANOUT_DISPATCH_NO_CONTEXT = 0,
    FUT_SCANOUT_DISPATCH_UNKNOWN_CALLBACK = -1,
    FUT_SCANOUT_DISPATCH_MISSING_RECOMPILED_CODE = -2,
    FUT_SCANOUT_DISPATCH_NO_INTERRUPT_GATE = -3,
    FUT_SCANOUT_DISPATCH_BAD_GUEST_STACK = -4,
    FUT_SCANOUT_DISPATCH_CALLBACK_NOT_OBSERVED = -5
} FutNativeScanoutDispatchResult;

int fut_native_scanout_dispatch_init(FutNativeScanoutDispatch *dispatch);
void fut_native_scanout_dispatch_destroy(FutNativeScanoutDispatch *dispatch);
FutNativeScanoutDispatchResult fut_native_scanout_dispatch_preflight(
    FutNativeScanoutDispatch *dispatch, uint32_t context, uint32_t notification,
    uint32_t expected_callback);
FutNativeScanoutDispatchResult fut_native_scanout_dispatch_call(
    FutNativeScanoutDispatch *dispatch, uint32_t context, uint32_t notification,
    uint32_t expected_callback, uint32_t *callback_tsc_before,
    uint32_t *callback_tsc_after);

#endif
