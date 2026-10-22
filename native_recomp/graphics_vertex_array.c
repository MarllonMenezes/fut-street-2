#include "graphics_vertex_array.h"
#include <string.h>

void fut_graphics_vertex_array_reset(FutGraphicsVertexArray *a)
{
    if(a) memset(a,0,sizeof(*a));
}

int fut_graphics_vertex_array_begin(FutGraphicsVertexArray *a,unsigned primitive)
{
    if(!a || primitive!=6 || a->active || a->ready) return 0;
    a->count=0;a->inline_count=0;a->input_mode=0;a->primitive=primitive;a->active=1;return 1;
}

int fut_graphics_vertex_array_begin_observed_lines(FutGraphicsVertexArray *a)
{
    if(!a || a->active || a->ready) return 0;
    a->count=0;a->inline_count=0;a->input_mode=0;a->primitive=2;a->active=1;return 1;
}

int fut_graphics_vertex_array_indices(FutGraphicsVertexArray *a,uint32_t packed)
{
    if(!a || !a->active || a->ready || a->input_mode==2 ||
        (a->primitive==2 ? a->count>30u : a->count>FUT_VERTEX_ARRAY_INDEX_CAPACITY-2)) return 0;
    a->input_mode=1;
    a->indices[a->count++]=(uint16_t)packed;
    a->indices[a->count++]=(uint16_t)(packed>>16);
    return 1;
}

int fut_graphics_vertex_array_inline(FutGraphicsVertexArray *a,uint32_t word)
{
    if(!a || !a->active || a->ready || a->input_mode==1 ||
        a->inline_count>=FUT_IMMEDIATE_CAPACITY*6u) return 0;
    a->input_mode=2;a->inline_words[a->inline_count++]=word;return 1;
}

int fut_graphics_vertex_array_index32(FutGraphicsVertexArray *a,uint32_t index)
{
    if(!a || !a->active || a->ready || a->primitive!=6 || a->input_mode==2 ||
        a->count>=FUT_VERTEX_ARRAY_INDEX_CAPACITY) return 0;
    a->input_mode=1;a->indices[a->count++]=index;return 1;
}

int fut_graphics_vertex_array_end(FutGraphicsVertexArray *a)
{
    if(!a || !a->active || a->ready) return 0;
    if(a->primitive==2) {
        if(a->input_mode!=1 || (a->count!=32u && a->count!=8u)) return 0;
        a->active=0;a->ready=1;return 1;
    }
    if(a->primitive!=6) return 0;
    if((a->input_mode==1 && a->count<3) ||
        (a->input_mode==2 && (a->inline_count%6u || a->inline_count<18u)) ||
        !a->input_mode) return 0;
    a->active=0;a->ready=1;return 1;
}

static int view_equal(const FutGraphicsDmaView *a,const FutGraphicsDmaView *b)
{
    return a && b && a->guest_address==b->guest_address && a->size==b->size;
}

int fut_graphics_vertex_array_build(const FutGraphicsVertexArray *a,
    FutGraphicsImmediate *out,FutGraphicsMemoryRead read,void *ctx,
    const FutGraphicsDmaView *view_a,const FutGraphicsDmaView *view_b,
    const uint32_t formats[16],const uint32_t offsets[16],
    const float attributes[16][4],const uint8_t attribute_valid[16],uint16_t required)
{
    FutGraphicsImmediate next;
    uint64_t draw_count;
    if(!a || !out || !read || !view_equal(view_a,view_b) || !formats || !offsets ||
        !attributes || !attribute_valid || a->active || !a->ready || a->primitive!=6 || a->input_mode!=1 || a->count<3 ||
        a->count>FUT_VERTEX_ARRAY_INDEX_CAPACITY ||
        formats[0]!=0x635u ||
        view_a->guest_address>UINT32_MAX-view_a->size) return 0;
    for(unsigned slot=1;slot<16;++slot)
        if(formats[slot]!=0x602u || (required&(1u<<slot) && attribute_valid[slot]!=15)) return 0;
    if((required&1u)==0) return 0;
    draw_count=(uint64_t)(a->count-2)*3;
    if(draw_count>FUT_IMMEDIATE_DRAW_CAPACITY || draw_count%3) return 0;
    memset(&next,0,sizeof(next));
    for(unsigned tri=0;tri<a->count-2;++tri) {
        unsigned order[3];
        if(tri&1u) { order[0]=tri+1;order[1]=tri;order[2]=tri+2; }
        else { order[0]=tri;order[1]=tri+1;order[2]=tri+2; }
        for(unsigned corner=0;corner<3;++corner) {
            unsigned dst=tri*3+corner;uint64_t rel=(uint64_t)offsets[0]+(uint64_t)a->indices[order[corner]]*6u;
            uint8_t raw[6];
            if(rel>view_a->size || view_a->size-rel<sizeof(raw) ||
                rel>UINT32_MAX-view_a->guest_address ||
                !read(ctx,view_a->guest_address+(uint32_t)rel,raw,sizeof(raw))) return 0;
            memcpy(next.vertices[dst],attributes,sizeof(next.vertices[dst]));
            int16_t component[3];memcpy(component,raw,sizeof(component));
            next.vertices[dst][0][0]=(float)component[0];
            next.vertices[dst][0][1]=(float)component[1];
            next.vertices[dst][0][2]=(float)component[2];
            next.vertices[dst][0][3]=1.0f;
            unsigned defined=0;
            for(unsigned slot=0;slot<16;++slot) if(attribute_valid[slot]==15) defined|=1u<<slot;
            next.defined[dst]=(uint16_t)(defined|1u);
        }
    }
    next.count=(unsigned)draw_count;next.primitive=5;next.ready=1;next.input_kind=3;
    *out=next;
    return 1;
}

int fut_graphics_vertex_array_build_inline(const FutGraphicsVertexArray *a,
    FutGraphicsImmediate *out,const uint32_t formats[16],
    const float attributes[16][4],const uint8_t attribute_valid[16],uint16_t required)
{
    FutGraphicsImmediate next;
    if(!a || !out || !formats || !attributes || !attribute_valid || a->active || !a->ready ||
        a->input_mode!=2 || a->inline_count<18u || a->inline_count%6u) return 0;
    int legacy_profile=1, captured_stride6_profile=1;
    for(unsigned slot=0;slot<16;++slot) {
        uint32_t expected=slot==0?0x32u:slot==3?0x40u:slot==9?0x22u:0x02u;
        if(formats[slot]!=expected) legacy_profile=0;
        if(formats[slot]!=(expected|0x600u)) captured_stride6_profile=0;
    }
    if(!legacy_profile && !captured_stride6_profile) return 0;
    unsigned vertices=a->inline_count/6u;
    uint64_t draw_count=(uint64_t)(vertices-2)*3u;
    if(draw_count>FUT_IMMEDIATE_DRAW_CAPACITY) return 0;
    memset(&next,0,sizeof(next));
    for(unsigned tri=0;tri<vertices-2;++tri) {
        unsigned order[3];
        if(tri&1u) {order[0]=tri+1;order[1]=tri;order[2]=tri+2;}
        else {order[0]=tri;order[1]=tri+1;order[2]=tri+2;}
        for(unsigned corner=0;corner<3;++corner) {
            unsigned src=order[corner]*6u,dst=tri*3u+corner;
            float (*v)[4]=next.vertices[dst];
            memcpy(v,attributes,sizeof(next.vertices[dst]));
            memcpy(v[0],a->inline_words+src,3u*sizeof(uint32_t));v[0][3]=1.0f;
            uint32_t color=a->inline_words[src+3u];
            v[3][0]=(float)((color>>16)&255u)/255.0f;
            v[3][1]=(float)((color>>8)&255u)/255.0f;
            v[3][2]=(float)(color&255u)/255.0f;
            v[3][3]=(float)(color>>24)/255.0f;
            memcpy(v[9],a->inline_words+src+4u,2u*sizeof(uint32_t));v[9][2]=0.0f;v[9][3]=1.0f;
            unsigned defined=0x209u;
            for(unsigned slot=0;slot<16;++slot) if(attribute_valid[slot]==15) defined|=1u<<slot;
            if((defined&required)!=required) return 0;
            next.defined[dst]=(uint16_t)defined;
        }
    }
    next.count=(unsigned)draw_count;next.primitive=5;next.ready=1;next.input_kind=3;
    *out=next;return 1;
}

int fut_graphics_vertex_array_capture_observed_lines(FutGraphicsMemoryRead read,
    void *ctx,const FutGraphicsDmaView *view_a,const FutGraphicsDmaView *view_b,
    uint32_t format,uint32_t offset,uint8_t output[96])
{
    uint8_t captured[96];
    const uint32_t address=0x80000000u+0x001689D4u;
    if(!read || !view_equal(view_a,view_b) || !output || format!=0x635u ||
        offset!=0x001689D4u || view_a->guest_address!=0x80000000u ||
        view_a->size!=0x04000000u || offset>view_a->size ||
        sizeof(captured)>view_a->size-offset || address<view_a->guest_address ||
        address-view_a->guest_address!=offset) return 0;
    for(unsigned i=0;i<16;++i)
        if(!read(ctx,address+i*6u,captured+i*6u,6u)) return 0;
    memcpy(output,captured,sizeof(captured));
    return 1;
}

int fut_graphics_vertex_array_build_observed_lines(const FutGraphicsVertexArray *a,
    FutGraphicsImmediate *out,FutGraphicsMemoryRead read,void *ctx,
    const FutGraphicsDmaView *view_a,const FutGraphicsDmaView *view_b,
    const uint32_t formats[16],const uint32_t offsets[16],
    const float attributes[16][4],const uint8_t attribute_valid[16],uint16_t required)
{
    static const uint16_t expected[32]={0,1,1,2,2,3,3,4,4,5,5,6,6,7,7,8,
        8,9,9,10,10,11,11,12,12,13,13,14,14,15,15,0};
    static const uint16_t rectangle[8]={0,1,1,2,2,3,3,0};
    FutGraphicsImmediate next;
    if(!a || !out || !read || !view_equal(view_a,view_b) || !formats || !offsets ||
        !attributes || !attribute_valid || a->active || !a->ready || a->primitive!=2 ||
        a->input_mode!=1 || (a->count!=32u && a->count!=8u) || a->inline_count || formats[0]!=0x635u ||
        (a->count==8u ? offsets[0]!=0x001688B4u : (offsets[0]&3u)!=0u) || required!=1u ||
        view_a->guest_address!=0x80000000u || view_a->size!=0x04000000u ||
        view_a->guest_address>UINT32_MAX-view_a->size || attribute_valid[0]!=15) return 0;
    for(unsigned i=0;i<a->count;++i) if(a->indices[i]!=(a->count==8u?rectangle[i]:expected[i])) return 0;
    for(unsigned slot=1;slot<16;++slot) if(formats[slot]!=0x602u) return 0;
    memset(&next,0,sizeof(next));
    for(unsigned i=0;i<a->count;++i) {
        uint8_t raw[6];uint64_t rel=(uint64_t)offsets[0]+(uint64_t)a->indices[i]*6u;
        if(rel>view_a->size || view_a->size-rel<sizeof(raw) ||
            rel>UINT32_MAX-view_a->guest_address ||
            !read(ctx,view_a->guest_address+(uint32_t)rel,raw,sizeof(raw))) return 0;
        memcpy(next.vertices[i],attributes,sizeof(next.vertices[i]));
        int16_t component[3];memcpy(component,raw,sizeof(component));
        next.vertices[i][0][0]=(float)component[0];
        next.vertices[i][0][1]=(float)component[1];
        next.vertices[i][0][2]=(float)component[2];
        next.vertices[i][0][3]=1.0f;
        next.defined[i]=1u;
        for(unsigned slot=1;slot<16;++slot) if(attribute_valid[slot]==15)
            next.defined[i]|=(uint16_t)(1u<<slot);
    }
    next.count=a->count;next.primitive=2;next.ready=1;next.input_kind=4;
    *out=next;
    return 1;
}

int fut_graphics_vertex_array_consume(FutGraphicsVertexArray *a)
{
    if(!a || a->active || !a->ready) return 0;
    fut_graphics_vertex_array_reset(a);return 1;
}
