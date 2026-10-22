#include "graphics_vertex_array.h"
#include <stdint.h>
#include <string.h>
static uint8_t memory[128];
static unsigned fail_read_at,read_calls;
static uint32_t relocated_base;
static int read_memory(void *ctx,uint32_t address,void *data,size_t size)
{
    (void)ctx;
    ++read_calls;
    if(fail_read_at && read_calls==fail_read_at) return 0;
    if(relocated_base && address>=relocated_base && address-relocated_base<96 &&
        size<=96-(address-relocated_base)) {
        memcpy(data,memory+(address-relocated_base),size);return 1;
    }
    if(address>=0x801688B4u && address-0x801688B4u<24 &&
        size<=24-(address-0x801688B4u)) {
        memcpy(data,memory+(address-0x801688B4u),size);return 1;
    }
    if(address>=0x80163D24u && address-0x80163D24u<96 &&
        size<=96-(address-0x80163D24u)) {
        memcpy(data,memory+(address-0x80163D24u),size);return 1;
    }
    if(address>=0x801689D4u && address-0x801689D4u<=sizeof(memory) &&
        size<=sizeof(memory)-(address-0x801689D4u)) {
        memcpy(data,memory+(address-0x801689D4u),size);return 1;
    }
    if(address<0x1000u || address-0x1000u>sizeof(memory) || size>sizeof(memory)-(address-0x1000u)) return 0;
    memcpy(data,memory+(address-0x1000u),size);return 1;
}
static int check(int ok,int line){return ok?0:line;}
#define CHECK(x) do { int e=check((x),__LINE__);if(e)return e; } while(0)
int main(void)
{
    FutGraphicsVertexArray a={0};FutGraphicsImmediate out={0},before;
    FutGraphicsDmaView va={0x1000,25},vb={0x1000,25},different={0x1000,23};
    uint32_t formats[16],offsets[16]={0};float attrs[16][4]={0};uint8_t valid[16]={0};
    int16_t positions[4][3]={{10,20,30},{40,50,60},{70,80,90},{100,110,120}};
    memcpy(memory,positions,sizeof(positions));memset(formats,0,sizeof(formats));
    formats[0]=0x635;for(unsigned i=1;i<16;++i)formats[i]=0x602;offsets[0]=0;
    CHECK(fut_graphics_vertex_array_begin(&a,6));CHECK(!fut_graphics_vertex_array_begin(&a,5));
    CHECK(fut_graphics_vertex_array_indices(&a,0x00010000));
    CHECK(fut_graphics_vertex_array_indices(&a,0x00030002));
    CHECK(a.count==4 && a.indices[0]==0 && a.indices[1]==1 && a.indices[2]==2 && a.indices[3]==3);
    CHECK(fut_graphics_vertex_array_end(&a));memset(&out,0xA5,sizeof(out));before=out;
    FutGraphicsDmaView short_view={0x1000,23};
    CHECK(!fut_graphics_vertex_array_build(&a,&out,read_memory,NULL,&short_view,&short_view,formats,offsets,attrs,valid,1));
    CHECK(memcmp(&out,&before,sizeof(out))==0);
    CHECK(!fut_graphics_vertex_array_build(&a,&out,read_memory,NULL,&va,&different,formats,offsets,attrs,valid,1));
    CHECK(fut_graphics_vertex_array_build(&a,&out,read_memory,NULL,&va,&vb,formats,offsets,attrs,valid,1));
    CHECK(out.ready && out.primitive==5 && out.count==6);
    CHECK(out.vertices[0][0][0]==10 && out.vertices[0][0][1]==20 && out.vertices[0][0][2]==30);
    CHECK(out.vertices[1][0][0]==40 && out.vertices[2][0][0]==70);
    CHECK(out.vertices[3][0][0]==70 && out.vertices[4][0][0]==40 && out.vertices[5][0][0]==100);
    before=out;formats[1]=0x603;
    CHECK(!fut_graphics_vertex_array_build(&a,&out,read_memory,NULL,&va,&vb,formats,offsets,attrs,valid,1));
    CHECK(memcmp(&out,&before,sizeof(out))==0);formats[1]=0x602;
    formats[0]=0x634;CHECK(!fut_graphics_vertex_array_build(&a,&out,read_memory,NULL,&va,&vb,formats,offsets,attrs,valid,1));
    formats[0]=0x635;offsets[0]=19;
    CHECK(!fut_graphics_vertex_array_build(&a,&out,read_memory,NULL,&va,&vb,formats,offsets,attrs,valid,1));
    offsets[0]=0;formats[4]=0x602;CHECK(!fut_graphics_vertex_array_build(&a,&out,read_memory,NULL,&va,&vb,formats,offsets,attrs,valid,1u|16u));
    CHECK(fut_graphics_vertex_array_consume(&a));CHECK(!a.ready && !a.active && a.count==0);

    uint32_t inline_formats[16];for(unsigned i=0;i<16;++i)inline_formats[i]=i==0?0x32u:i==3?0x40u:i==9?0x22u:2u;
    CHECK(fut_graphics_vertex_array_begin(&a,6));
    float inline_vertices[4][6]={{-1,0,0,0,0,0},{0,1,0,0,1,0},{1,0,0,0,1,1},{0,-1,0,0,0,1}};
    uint32_t inline_words[24];
    for(unsigned vertex=0;vertex<4;++vertex) {
        memcpy(inline_words+vertex*6,inline_vertices[vertex],3*sizeof(float));
        inline_words[vertex*6+3]=0xFF804020u+vertex;
        memcpy(inline_words+vertex*6+4,inline_vertices[vertex]+4,2*sizeof(float));
    }
    for(unsigned i=0;i<24;++i)CHECK(fut_graphics_vertex_array_inline(&a,inline_words[i]));
    CHECK(fut_graphics_vertex_array_end(&a));
    CHECK(fut_graphics_vertex_array_build_inline(&a,&out,inline_formats,attrs,valid,0x201));
    CHECK(out.count==6 && out.primitive==5 && out.defined[0]==0x209u);
    CHECK(out.vertices[0][0][0]==-1 && out.vertices[1][0][0]==0 && out.vertices[2][0][0]==1);
    CHECK(out.vertices[3][0][0]==1 && out.vertices[4][0][0]==0 && out.vertices[5][0][0]==0);
    CHECK(out.vertices[0][3][0]==128.0f/255.0f && out.vertices[0][9][1]==0.0f);
    FutGraphicsImmediate legacy_inline=out;
    for(unsigned i=0;i<16;++i) inline_formats[i]|=0x600u;
    CHECK(fut_graphics_vertex_array_build_inline(&a,&out,inline_formats,attrs,valid,0x201));
    CHECK(memcmp(&out,&legacy_inline,sizeof(out))==0);
    FutGraphicsImmediate inline_before=out;inline_formats[3]=0x740u;
    CHECK(!fut_graphics_vertex_array_build_inline(&a,&out,inline_formats,attrs,valid,0x201));
    CHECK(memcmp(&out,&inline_before,sizeof(out))==0);

    FutGraphicsDmaView captured_view={0x80000000u,0x04000000u};
    FutGraphicsDmaView captured_short={0x80000000u,0x00168A33u};
    uint8_t captured[96],captured_before[96];
    for(unsigned i=0;i<sizeof(memory);++i) memory[i]=(uint8_t)(i*17u+3u);
    memset(captured,0xA5,sizeof(captured));
    fail_read_at=0;read_calls=0;
    CHECK(fut_graphics_vertex_array_capture_observed_lines(read_memory,NULL,
        &captured_view,&captured_view,0x635u,0x001689D4u,captured));
    CHECK(read_calls==16 && memcmp(captured,memory,96)==0);
    memcpy(captured_before,captured,sizeof(captured));
    CHECK(!fut_graphics_vertex_array_capture_observed_lines(read_memory,NULL,
        &captured_view,&different,0x635u,0x001689D4u,captured));
    CHECK(!fut_graphics_vertex_array_capture_observed_lines(read_memory,NULL,
        &captured_short,&captured_short,0x635u,0x001689D4u,captured));
    CHECK(!fut_graphics_vertex_array_capture_observed_lines(read_memory,NULL,
        &captured_view,&captured_view,0x632u,0x001689D4u,captured));
    CHECK(!fut_graphics_vertex_array_capture_observed_lines(read_memory,NULL,
        &captured_view,&captured_view,0x635u,0x001689D0u,captured));
    FutGraphicsDmaView wrong_base={0x80000004u,0x04000000u};
    CHECK(!fut_graphics_vertex_array_capture_observed_lines(read_memory,NULL,
        &wrong_base,&wrong_base,0x635u,0x001689D4u,captured));
    fail_read_at=7;read_calls=0;
    CHECK(!fut_graphics_vertex_array_capture_observed_lines(read_memory,NULL,
        &captured_view,&captured_view,0x635u,0x001689D4u,captured));
    CHECK(memcmp(captured,captured_before,sizeof(captured))==0);

    /* Exact captured LINELIST packet: 32 indices expand to 32 endpoints. */
    int16_t line_positions[16][3];
    for(unsigned i=0;i<16;++i) {line_positions[i][0]=(int16_t)(i*10);line_positions[i][1]=(int16_t)(i*20);line_positions[i][2]=0;}
    memcpy(memory,line_positions,sizeof(line_positions));
    uint32_t line_formats[16];uint32_t line_offsets[16]={0};
    for(unsigned i=0;i<16;++i) line_formats[i]=i?0x602u:0x635u;
    valid[0]=15;line_offsets[0]=0x001689D4u;fut_graphics_vertex_array_reset(&a);
    CHECK(fut_graphics_vertex_array_begin_observed_lines(&a));
    CHECK(!fut_graphics_vertex_array_begin_observed_lines(&a));
    static const uint16_t sequence[32]={0,1,1,2,2,3,3,4,4,5,5,6,6,7,7,8,
        8,9,9,10,10,11,11,12,12,13,13,14,14,15,15,0};
    for(unsigned i=0;i<32;i+=2)
        CHECK(fut_graphics_vertex_array_indices(&a,sequence[i]|((uint32_t)sequence[i+1]<<16)));
    CHECK(!fut_graphics_vertex_array_indices(&a,0));
    CHECK(fut_graphics_vertex_array_end(&a));
    memset(&out,0xA5,sizeof(out));before=out;fail_read_at=0;read_calls=0;
    CHECK(fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(out.ready && out.primitive==2 && out.input_kind==4 && out.count==32);
    CHECK(out.vertices[0][0][0]==0 && out.vertices[1][0][0]==10 &&
        out.vertices[30][0][0]==150 && out.vertices[31][0][0]==0);
    FutGraphicsImmediate line_before=out;
    relocated_base=0x801641A4u;line_offsets[0]=0x001641A4u;
    CHECK(fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(!memcmp(&out,&line_before,sizeof(out)));
    relocated_base=0x83FFFFA0u;line_offsets[0]=0x03FFFFA0u;
    CHECK(fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(!memcmp(&out,&line_before,sizeof(out)));
    line_offsets[0]+=4;
    CHECK(!fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(!memcmp(&out,&line_before,sizeof(out)));
    line_offsets[0]=0x001641A5;read_calls=0;
    CHECK(!fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1)&&!read_calls);
    CHECK(!memcmp(&out,&line_before,sizeof(out)));
    relocated_base=0;
    line_offsets[0]=0x00163D24u;
    CHECK(fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(!memcmp(&out,&line_before,sizeof(out)));
    int16_t signed_position[3]={-32768,-7,32767};
    memcpy(memory,signed_position,sizeof(signed_position));
    CHECK(fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(out.vertices[0][0][0]==-32768 && out.vertices[0][0][1]==-7 &&
        out.vertices[0][0][2]==32767 && out.vertices[31][0][0]==-32768);
    memcpy(memory,line_positions,sizeof(line_positions));out=line_before;
    for(unsigned i=0;i<2;++i) {
        line_offsets[0]=i?0x00163D26u:0x00163D22u;
        CHECK(!fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
            &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
        CHECK(!memcmp(&out,&line_before,sizeof(out)));
    }
    line_offsets[0]=0x00163D24u;fail_read_at=3;read_calls=0;
    CHECK(!fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(!memcmp(&out,&line_before,sizeof(out)));
    fail_read_at=0;line_offsets[0]=0x001689D4u;
    a.indices[5]=4;
    CHECK(!fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(memcmp(&out,&line_before,sizeof(out))==0);a.indices[5]=sequence[5];
    line_formats[1]=0x603u;
    CHECK(!fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));line_formats[1]=0x602u;
    line_offsets[0]=0;
    CHECK(!fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));line_offsets[0]=0x001689D4u;
    fail_read_at=3;read_calls=0;
    CHECK(!fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(memcmp(&out,&line_before,sizeof(out))==0);
    fail_read_at=0;
    fut_graphics_vertex_array_reset(&a);
    CHECK(fut_graphics_vertex_array_begin_observed_lines(&a));
    const uint32_t rectangle[4]={0x00010000u,0x00020001u,0x00030002u,0x00000003u};
    for(unsigned i=0;i<4;++i) CHECK(fut_graphics_vertex_array_indices(&a,rectangle[i]));
    CHECK(fut_graphics_vertex_array_end(&a));
    line_offsets[0]=0x001688B4u;
    CHECK(fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(out.count==8 && out.vertices[6][0][0]==30 && out.vertices[7][0][0]==0);
    line_before=out;
    line_offsets[0]=0x00163D24u;
    CHECK(!fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(!memcmp(&out,&line_before,sizeof(out)));line_offsets[0]=0x001688B4u;
    a.indices[7]=1;
    CHECK(!fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(!memcmp(&out,&line_before,sizeof(out)));a.indices[7]=0;
    line_offsets[0]=0x001689D4u;
    CHECK(!fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    line_offsets[0]=0x001688B4u;fail_read_at=4;read_calls=0;
    CHECK(!fut_graphics_vertex_array_build_observed_lines(&a,&out,read_memory,NULL,
        &captured_view,&captured_view,line_formats,line_offsets,attrs,valid,1));
    CHECK(!memcmp(&out,&line_before,sizeof(out)));fail_read_at=0;
    for(unsigned n=0;n<=32;n+=2) if(n!=8 && n!=32) {
        a.active=1;a.ready=0;a.count=n;
        CHECK(!fut_graphics_vertex_array_end(&a));
    }
    /* Large profile strip: keep degenerate indices and parity exactly as
     * supplied, including beyond the old 256-index diagnostic limit. */
    memcpy(memory,positions,sizeof(positions));
    for(unsigned length=912;length<=1024;length+=112) {
        fut_graphics_vertex_array_reset(&a);CHECK(fut_graphics_vertex_array_begin(&a,6));
        for(unsigned i=0;i<length;i+=2)
            CHECK(fut_graphics_vertex_array_indices(&a,(i%4)|(((i+1)%4)<<16)));
        if(length==1024) CHECK(!fut_graphics_vertex_array_indices(&a,0));
        CHECK(fut_graphics_vertex_array_end(&a));
        CHECK(fut_graphics_vertex_array_build(&a,&out,read_memory,NULL,&va,&vb,
            formats,offsets,attrs,valid,1));
        CHECK(out.count==(length-2)*3 && out.ready && out.primitive==5);
        for(unsigned tri=0;tri<length-2;++tri) {
            unsigned first=tri+(tri&1u),second=tri+!(tri&1u);
            CHECK(out.vertices[tri*3][0][0]==positions[first%4][0] &&
                out.vertices[tri*3+1][0][0]==positions[second%4][0] &&
                out.vertices[tri*3+2][0][0]==positions[(tri+2)%4][0]);
        }
        before=out;a.indices[length-1]=65535;
        CHECK(!fut_graphics_vertex_array_build(&a,&out,read_memory,NULL,&va,&vb,
            formats,offsets,attrs,valid,1));
        CHECK(!memcmp(&out,&before,sizeof(out)));
    }
    return 0;
}
