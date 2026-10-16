#ifndef FUT_GRAPHICS_SCANOUT_H
#define FUT_GRAPHICS_SCANOUT_H
#include <stdint.h>
/* Snapshot of the title's registered D3D8 scanout context, not MMIO. */
#define FUT_SCANOUT_CONTEXT_WORDS 119u
typedef struct {
    unsigned slot;
    uint32_t physical_address,deadline,next_deadline,next_enqueue;
} FutGraphicsScanoutEnqueue;
/* Plan only the asynchronous/deferred branch for no callback or the one
 * explicitly supported original callback, 00191340. Unknown callbacks and
 * entries the original handler would immediately retire remain unsupported.
 * Failure leaves output unchanged. This does NOT complete a flip/VBlank. */
int fut_graphics_scanout_enqueue_plan(const uint32_t words[FUT_SCANOUT_CONTEXT_WORDS],
    uint32_t notification,FutGraphicsScanoutEnqueue *plan);
/* One outstanding host presentation only. Pure validation: never retires a
 * guest entry, invents a VBlank or changes a flip index. */
int fut_graphics_scanout_retirement_matches(const uint32_t words[FUT_SCANOUT_CONTEXT_WORDS],
    const FutGraphicsScanoutEnqueue *plan,int presentation_received,
    uint32_t scanout_physical,uint32_t expected_scanout,uint32_t disabled_before,
    uint32_t title_physical,uint32_t increment);
#endif
