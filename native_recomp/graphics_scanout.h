#ifndef FUT_GRAPHICS_SCANOUT_H
#define FUT_GRAPHICS_SCANOUT_H
#include <stdint.h>
/* Snapshot of the title's registered D3D8 scanout context, not MMIO. */
#define FUT_SCANOUT_CONTEXT_WORDS 119u
typedef struct {
    unsigned slot;
    uint32_t physical_address,deadline,next_deadline,next_enqueue;
} FutGraphicsScanoutEnqueue;
/* Plan only the asynchronous/deferred branch of software notification1.
 * No callbacks, timestamp extrapolation or immediately due scanout supported.
 * Failure leaves output unchanged. This does NOT complete a flip/VBlank. */
int fut_graphics_scanout_enqueue_plan(const uint32_t words[FUT_SCANOUT_CONTEXT_WORDS],
    uint32_t notification,FutGraphicsScanoutEnqueue *plan);
#endif
