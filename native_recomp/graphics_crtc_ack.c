#include "guest_interrupt_gate.h"
#include <stddef.h>
#include <stdint.h>
#include <stdatomic.h>
#include <stdio.h>
extern ptrdiff_t xbox_GetMemoryOffset(void);

/* Host service for translated 32-bit PCRTC acknowledge writes. This is not
 * an interrupt source or a generic NV2A register implementation. The future
 * producer must use the same state gate, including its pending/enable writes. */
void recomp_xbox_pcrtc_store32(uint32_t address,uint32_t value)
{
    uintptr_t base=(uintptr_t)xbox_GetMemoryOffset();
    if(address!=0xFD600100u) {
        *(uint32_t*)(base+(uintptr_t)address)=value;
        return;
    }
    fut_guest_interrupt_lock_state();
    volatile uint32_t *pending=(volatile uint32_t*)(base+0xFD600100u);
    volatile uint32_t *enable=(volatile uint32_t*)(base+0xFD600140u);
    volatile uint32_t *master=(volatile uint32_t*)(base+0xFD000100u);
    uint32_t before=*pending,after=before&~value;
    uint32_t next_master=*master&~0x01000000u;
    if(after&*enable) next_master|=0x01000000u;
    *pending=after;*master=next_master;
    fut_guest_interrupt_unlock_state();
    static atomic_flag reported=ATOMIC_FLAG_INIT;
    if(!atomic_flag_test_and_set(&reported))
        fprintf(stderr,"[GRAPHICS] translated PCRTC W1C: write=%08X pending=%08X->%08X; no VBlank generated\n",
            value,before,after);
}
