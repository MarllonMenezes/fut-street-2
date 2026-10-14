#include "graphics_immediate.h"
#include <string.h>
FutImmediateStatus fut_graphics_immediate_method(FutGraphicsImmediate *b,
    FutGraphicsKelvinState *s,uint32_t method,uint32_t value)
{
    if(!b || !s || (method&3)) return FUT_IMMEDIATE_REJECTED;
    if(method==0x17FC) {
        if(value) {
            if(value!=5 || b->active || b->ready || b->pending_x) return FUT_IMMEDIATE_REJECTED;
            b->active=1;b->count=0;b->primitive=value;return FUT_IMMEDIATE_APPLIED;
        }
        if(!b->active || b->pending_x || !b->count || b->count%3) return FUT_IMMEDIATE_REJECTED;
        b->active=0;b->ready=1;return FUT_IMMEDIATE_READY;
    }
    if(method<0x1880 || method>=0x1900) return FUT_IMMEDIATE_NOT_HANDLED;
    if(b->ready) return FUT_IMMEDIATE_REJECTED;
    unsigned slot=(method-0x1880)/8,part=((method-0x1880)/4)%2,bit=1u<<slot;
    if((!slot && (!b->active || b->count>=FUT_IMMEDIATE_CAPACITY)) ||
        (!part && (b->pending_x&bit)) || (part && !(b->pending_x&bit)) ||
        (!slot && part && b->pending_x!=1)) return FUT_IMMEDIATE_REJECTED;
    memcpy(&s->attributes[slot][part],&value,4);
    s->attributes[slot][2]=0;s->attributes[slot][3]=1;
    s->attribute_valid[slot]|=(uint8_t)((1u<<part)|12u);
    if(!part) b->pending_x|=bit; else b->pending_x&=~bit;
    if(!slot && part) {
        unsigned defined=0;
        for(unsigned i=0;i<16;++i) if(s->attribute_valid[i]==15) defined|=1u<<i;
        memcpy(b->vertices[b->count],s->attributes,sizeof(s->attributes));
        b->defined[b->count++]=(uint16_t)defined;
    }
    return FUT_IMMEDIATE_APPLIED;
}
int fut_graphics_immediate_consume(FutGraphicsImmediate *b)
{
    if(!b || b->active || !b->ready) return 0;
    b->ready=0;b->count=0;b->primitive=0;return 1;
}
