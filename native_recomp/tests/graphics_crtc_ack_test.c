#include "guest_interrupt_gate.h"
#include <stdint.h>
#include <stddef.h>
#include <stdio.h>
void recomp_xbox_pcrtc_store32(uint32_t address,uint32_t value);
static uint32_t registers[0x600144/4+1];
ptrdiff_t xbox_GetMemoryOffset(void)
{
    return (ptrdiff_t)((uintptr_t)registers-(uintptr_t)0xFD000000u);
}
#define P registers[0x600100/4]
#define E registers[0x600140/4]
#define M registers[0x100/4]
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"CRTC ack failed: %u\n",n);return n;}}while(0)
int main(void)
{
    P=3;E=1;M=0x01001100;
    recomp_xbox_pcrtc_store32(0xFD600100,0);
    CHECK(P==3 && M==0x01001100 && fut_guest_interrupt_enabled(),1);
    recomp_xbox_pcrtc_store32(0xFD600100,1);
    CHECK(P==2 && M==0x1100 && E==1,2);
    P=3;E=3;M=0x1100;
    recomp_xbox_pcrtc_store32(0xFD600100,1);
    CHECK(P==2 && M==0x01001100,3);
    fut_guest_interrupt_cli();
    recomp_xbox_pcrtc_store32(0xFD600100,0xFFFFFFFF);
    CHECK(P==0 && M==0x1100 && !fut_guest_interrupt_enabled(),4);
    fut_guest_interrupt_sti();
    P=1;E=0;M=0x01001100;
    CHECK(fut_guest_interrupt_try_enter(),5);
    recomp_xbox_pcrtc_store32(0xFD600100,0);
    CHECK(P==1 && M==0x1100 && !fut_guest_interrupt_enabled(),6);
    fut_guest_interrupt_leave();
    recomp_xbox_pcrtc_store32(0xFD000200,0x12345678);
    CHECK(registers[0x200/4]==0x12345678 && P==1 && M==0x1100,7);
    puts("PCRTC W1C host writes preserve masked pending bits, other PMC sources, RAM fallback and IF; no ISR or clock exercised");
    return 0;
}
