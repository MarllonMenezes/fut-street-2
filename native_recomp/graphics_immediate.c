#include "graphics_immediate.h"
#include <string.h>
FutImmediateStatus fut_graphics_immediate_method(FutGraphicsImmediate *b,
    FutGraphicsKelvinState *s,uint32_t method,uint32_t value)
{
    if(!b || !s || (method&3)) return FUT_IMMEDIATE_REJECTED;
    if(method==0x17FC) {
        if(value) {
            if((value!=5 && value!=8) || b->active || b->ready || b->pending_x || b->pending_position) return FUT_IMMEDIATE_REJECTED;
            b->active=1;b->count=0;b->primitive=value;return FUT_IMMEDIATE_APPLIED;
        }
        if(!b->active || b->pending_x || b->pending_position || !b->count ||
            (b->primitive==5 && b->count%3) || (b->primitive==8 && b->count%4)) return FUT_IMMEDIATE_REJECTED;
        if(b->primitive==8) {
            unsigned quads=b->count/4;
            for(unsigned q=quads;q-- > 0;) {
                unsigned src=q*4,dst=q*6;
                float vertex[4][16][4];uint16_t defined[4];
                memcpy(vertex,b->vertices+src,sizeof(vertex));
                memcpy(defined,b->defined+src,sizeof(defined));
                memcpy(b->vertices[dst+0],vertex[0],sizeof(vertex[0]));
                memcpy(b->vertices[dst+1],vertex[1],sizeof(vertex[1]));
                memcpy(b->vertices[dst+2],vertex[2],sizeof(vertex[2]));
                memcpy(b->vertices[dst+3],vertex[0],sizeof(vertex[0]));
                memcpy(b->vertices[dst+4],vertex[2],sizeof(vertex[2]));
                memcpy(b->vertices[dst+5],vertex[3],sizeof(vertex[3]));
                b->defined[dst+0]=defined[0];b->defined[dst+1]=defined[1];
                b->defined[dst+2]=defined[2];b->defined[dst+3]=defined[0];
                b->defined[dst+4]=defined[2];b->defined[dst+5]=defined[3];
            }
            b->count=quads*6;b->primitive=5;
        }
        b->active=0;b->ready=1;return FUT_IMMEDIATE_READY;
    }
    if(method>=0x1518 && method<=0x1524) {
        unsigned part=(method-0x1518)/4;
        if(!b->active || b->primitive!=8 || b->pending_x ||
            part!=b->pending_position || b->count>=FUT_IMMEDIATE_CAPACITY) return FUT_IMMEDIATE_REJECTED;
        memcpy(&s->attributes[0][part],&value,sizeof(value));
        s->attribute_valid[0]|=(uint8_t)(1u<<part);
        b->pending_position=part+1;
        if(part==3) {
            unsigned defined=0;
            for(unsigned i=0;i<16;++i) if(s->attribute_valid[i]==15) defined|=1u<<i;
            memcpy(b->vertices[b->count],s->attributes,sizeof(s->attributes));
            b->defined[b->count++]=(uint16_t)defined;
            b->pending_position=0;
        }
        return FUT_IMMEDIATE_APPLIED;
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
