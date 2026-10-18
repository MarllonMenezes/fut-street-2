#include "graphics_vertex_array.h"
#include <stdint.h>
#include <string.h>
static uint8_t memory[128];
static int read_memory(void *ctx,uint32_t address,void *data,size_t size)
{
    (void)ctx;
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
    return 0;
}
