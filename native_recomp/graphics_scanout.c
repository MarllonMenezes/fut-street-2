#include "graphics_scanout.h"

int fut_graphics_scanout_retirement_matches(const uint32_t w[FUT_SCANOUT_CONTEXT_WORDS],
    const FutGraphicsScanoutEnqueue *p,int received,uint32_t physical,
    uint32_t expected_scanout,uint32_t disabled_before,uint32_t title_physical,uint32_t increment)
{
    if(!w || !p || received!=1 || p->slot>1 || p->physical_address>=0x04000000u ||
        (p->physical_address&15u) ||
        p->slot!=((p->next_enqueue-1u)&1u)) return 0;
    unsigned base=0x174/4+p->slot*3;
    /* sub_002960BB explicitly skips PCRTC_START while +1B8 is nonzero.
     * Admit the observed boolean domain. When initially disabled, the register
     * may remain unchanged or be programmed if enabled before retirement.
     * Receipt/queue/physical/increment checks remain mandatory either way. */
    return disabled_before<=1 && w[0]==0xFD000000u && w[0x1B8/4]<=1 &&
        w[0x1CC/4]==p->next_enqueue && w[0x1BC/4]==p->next_enqueue &&
        !w[base] && w[base+1]==p->deadline && w[base+2]==p->physical_address &&
        (physical==p->physical_address || (disabled_before && physical==expected_scanout)) &&
        title_physical==p->physical_address && (increment&2u);
}

int fut_graphics_scanout_enqueue_plan(const uint32_t w[FUT_SCANOUT_CONTEXT_WORDS],
    uint32_t notification,FutGraphicsScanoutEnqueue *plan)
{
    if(!w || !plan || (notification&31u)!=1u ||
        (w[0x18C/4] && w[0x18C/4]!=0x00191340u)) return 0;
    uint32_t data=notification>>5,physical=data&0xFFFFFFF0u;
    uint32_t current=w[0x1C0/4],enqueue=w[0x1CC/4],retire=w[0x1BC/4];
    unsigned slot=enqueue&1u,base=0x174/4+slot*3;
    /* The original ISR at sub_002970A0 stores the elapsed performance-counter
     * delta/current sample at +0x1D4/+0x1D8. A nonzero delta is normal after
     * the first tick; it is not a busy flag for the deferred scanout queue. */
    if(physical>=0x04000000u ||
        enqueue-retire>=2u || w[base]) return 0;
    /* Retirement would program scanout and may consume another queued slot.
     * Refuse BEFORE changing the enqueue side if that dependency is due. */
    unsigned head=0x174/4+(retire&1u)*3;
    if(w[head] && w[head+1]==current) return 0;
    uint32_t interval=data&7u,deadline=w[0x1C8/4]+interval;
    if((int32_t)(deadline-current)<=0) deadline=current+((data&8u)?0u:1u);
    if((data&8u) && (int32_t)(deadline-current)<=0) return 0;
    /* Callback dispatch is deferred until after the native DXGI receipt;
     * sub_00296FC0 may then retire the newly queued current+1 entry inline. */
    if(!w[0x18C/4] && (int32_t)(deadline-current)<=0) return 0;
    FutGraphicsScanoutEnqueue next={slot,physical,deadline,deadline+interval,enqueue+1u};
    *plan=next;return 1;
}
