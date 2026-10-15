#include "nv2a_ack_ownership.h"
#include <stdio.h>
int main(void)
{
    const uint32_t offsets[]={0x000100,0x600100,0x001100,0x002100,0x400100,0x100410};
    for(unsigned i=0;i<sizeof(offsets)/sizeof(offsets[0]);++i) {
        uint32_t mask=i==5?0x10000u:0xFFFFFFFFu;
        if(xbox_Nv2aLegacyAckMask(offsets[i],mask,0)!=mask) return 1;
        uint32_t native=xbox_Nv2aLegacyAckMask(offsets[i],mask,1);
        if(native!=(i<2?0:mask)) return 2;
        uint32_t pending=0x01000001u;
        pending&=~native;
        if(i<2 && pending!=0x01000001u) return 3;
        if(xbox_Nv2aLegacyAckMask(offsets[i],0,1)) return 4;
    }
    if(xbox_Nv2aLegacyAckMask(0x600104,0xFFFFFFFFu,1)!=0xFFFFFFFFu) return 5;
    puts("Native PMC/PCRTC ownership masks preserve pending bits; no W1C or ISR exercised");return 0;
}
