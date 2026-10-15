#include "graphics_scanout.h"

int fut_graphics_scanout_enqueue_plan(const uint32_t w[FUT_SCANOUT_CONTEXT_WORDS],
    uint32_t notification,FutGraphicsScanoutEnqueue *plan)
{
    if(!w || !plan || (notification&31u)!=1u) return 0;
    uint32_t data=notification>>5,physical=data&0xFFFFFFF0u;
    uint32_t current=w[0x1C0/4],enqueue=w[0x1CC/4],retire=w[0x1BC/4];
    unsigned slot=enqueue&1u,base=0x174/4+slot*3;
    if(physical>=0x04000000u || w[0x18C/4] || w[0x1D4/4] ||
        enqueue-retire>=2u || w[base]) return 0;
    /* Retirement would program scanout and may consume another queued slot.
     * Refuse BEFORE changing the enqueue side if that dependency is due. */
    unsigned head=0x174/4+(retire&1u)*3;
    if(w[head] && w[head+1]==current) return 0;
    uint32_t interval=data&7u,deadline=w[0x1C8/4]+interval;
    if((int32_t)(deadline-current)<=0) deadline=current+((data&8u)?0u:1u);
    if(deadline==current) return 0;
    FutGraphicsScanoutEnqueue next={slot,physical,deadline,deadline+interval,enqueue+1u};
    *plan=next;return 1;
}
