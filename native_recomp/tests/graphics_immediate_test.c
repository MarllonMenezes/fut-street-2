#include "graphics_immediate.h"
#include "graphics_vertex_program.h"
#include <stdio.h>
#include <string.h>
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"immediate test failed: %d\n",n);return n;}}while(0)
static FutGraphicsImmediate b,before;
static FutGraphicsKelvinState s,saved;
static int rejected(uint32_t method,uint32_t value)
{
    before=b;saved=s;
    return fut_graphics_immediate_method(&b,&s,method,value)==FUT_IMMEDIATE_REJECTED &&
        !memcmp(&b,&before,sizeof(b)) && !memcmp(&s,&saved,sizeof(s));
}
static int pair(unsigned slot,uint32_t x,uint32_t y)
{
    return fut_graphics_immediate_method(&b,&s,0x1880+slot*8,x)==FUT_IMMEDIATE_APPLIED &&
        fut_graphics_immediate_method(&b,&s,0x1884+slot*8,y)==FUT_IMMEDIATE_APPLIED;
}
int main(void)
{
    /* Operand dependency decoding, including non-A input sources and FINAL.
     * Synthetic instructions only; conservative full-float4 requirements. */
    uint32_t words[2][4]={{0,0,0,1},{0,0xFFFFFFFF,0xFFFFFFFF,0xFFFFFFFF}};
    uint16_t mask=99;
    CHECK(fut_vertex_program_input_mask(words,2,&mask) && !mask,18);
    words[0][1]=(2u<<21)|(5u<<9)|0x1Bu;
    words[0][2]=(1u<<26)|(2u<<11); /* MUL A=temp0 B=input5. */
    CHECK(fut_vertex_program_input_mask(words,2,&mask) && mask==(1u<<5),19);
    words[0][1]=(3u<<21)|(7u<<9)|0x1Bu;
    words[0][2]=(1u<<26)|0x6Cu;words[0][3]=(2u<<28)|1; /* ADD A=temp0 C=input7. */
    CHECK(fut_vertex_program_input_mask(words,1,&mask) && mask==(1u<<7),20);
    words[0][1]=(2u<<25)|(15u<<9)|0x1Bu; /* ILU RCP C=input15. */
    CHECK(fut_vertex_program_input_mask(words,1,&mask) && mask==(1u<<15),21);
    words[0][1]=(4u<<21);mask=99;
    CHECK(!fut_vertex_program_input_mask(words,1,&mask) && !mask,22);
    CHECK(fut_graphics_kelvin_state(&s,0x194C,0x80402010)==FUT_KELVIN_APPLIED &&
        s.attribute_valid[3]==15,23);
    for(unsigned component=0;component<4;++component) {
        CHECK(fut_graphics_kelvin_state(&s,0x1A40+component*4,0)==FUT_KELVIN_APPLIED &&
            s.attribute_valid[4]==((1u<<(component+1))-1),24);
    }
    memset(&s,0,sizeof(s));
    CHECK(rejected(0x17FC,0) && rejected(0x17FC,4) && rejected(0x1880,0),1);
    CHECK(fut_graphics_immediate_method(&b,&s,0x1800,0)==FUT_IMMEDIATE_NOT_HANDLED,2);
    CHECK(pair(9,0x80000000,0x7FC12345) && s.attribute_valid[9]==15,3);
    CHECK(fut_graphics_immediate_method(&b,&s,0x17FC,5)==FUT_IMMEDIATE_APPLIED,4);
    CHECK(rejected(0x17FC,5) && rejected(0x1884,0) && rejected(0x1881,0),5);
    CHECK(pair(0,0x3F800000,0x40000000) && b.count==1 && b.defined[0]==0x201,6);
    uint32_t bits[4];memcpy(bits,b.vertices[0][9],sizeof(bits));
    CHECK(bits[0]==0x80000000 && bits[1]==0x7FC12345 && bits[2]==0 && bits[3]==0x3F800000,7);
    CHECK(rejected(0x17FC,0) && !fut_graphics_immediate_consume(&b),8);
    CHECK(fut_graphics_immediate_method(&b,&s,0x18C8,0)==FUT_IMMEDIATE_APPLIED &&
        rejected(0x18C8,0) && rejected(0x17FC,0),9);
    CHECK(fut_graphics_immediate_method(&b,&s,0x1880,0)==FUT_IMMEDIATE_APPLIED &&
        rejected(0x1884,0),10); /* Cannot emit with another attribute half-set. */
    CHECK(fut_graphics_immediate_method(&b,&s,0x18CC,0)==FUT_IMMEDIATE_APPLIED &&
        fut_graphics_immediate_method(&b,&s,0x1884,0)==FUT_IMMEDIATE_APPLIED,11);
    CHECK(pair(0,0,0) && b.count==3 && b.vertices[0][0][0]==1 &&
        b.vertices[1][0][0]==0 && b.vertices[0][9][3]==1,12);
    CHECK(fut_graphics_immediate_method(&b,&s,0x17FC,0)==FUT_IMMEDIATE_READY &&
        b.ready && !b.active && rejected(0x17FC,5) && rejected(0x18C8,0),13);
    CHECK(fut_graphics_immediate_consume(&b) && !b.count && !b.ready &&
        !fut_graphics_immediate_consume(&b) && s.attribute_valid[9]==15,14);
    CHECK(fut_graphics_immediate_method(&b,&s,0x17FC,5)==FUT_IMMEDIATE_APPLIED,15);
    for(unsigned i=0;i<FUT_IMMEDIATE_CAPACITY;++i) CHECK(pair(0,0,0),16);
    CHECK(b.count==FUT_IMMEDIATE_CAPACITY && rejected(0x1880,0),17);
    memset(&b,0,sizeof(b));
    CHECK(fut_graphics_immediate_method(&b,&s,0x17FC,8)==FUT_IMMEDIATE_APPLIED,25);
    for(unsigned vertex=0;vertex<4;++vertex) for(unsigned part=0;part<4;++part) {
        uint32_t value=0x3F800000u+vertex*16+part;
        CHECK(fut_graphics_immediate_method(&b,&s,0x1518+part*4,value)==FUT_IMMEDIATE_APPLIED,26);
    }
    uint32_t expected_bits=0x3F800030u;float expected_vertex3_x;
    memcpy(&expected_vertex3_x,&expected_bits,sizeof(expected_vertex3_x));
    CHECK(b.count==4 && b.defined[0]==0x201 &&
        fut_graphics_immediate_method(&b,&s,0x17FC,0)==FUT_IMMEDIATE_READY &&
        b.count==6 && b.primitive==5,27);
    CHECK(b.vertices[3][0][0]==b.vertices[0][0][0] &&
        b.vertices[4][0][0]==b.vertices[2][0][0] &&
        b.vertices[5][0][0]==expected_vertex3_x,28);
    memset(&b,0,sizeof(b));memset(&s,0,sizeof(s));
    for(unsigned i=0;i<16;++i) CHECK(fut_graphics_kelvin_state(&s,0x1760+i*4,
        i==0?0x32:i==3?0x40:i==9?0x22:2)==FUT_KELVIN_APPLIED,29);
    s.attributes[4][0]=0.25f;s.attribute_valid[4]=15;
    CHECK(rejected(0x1818,0),30);
    CHECK(fut_graphics_immediate_method(&b,&s,0x17FC,8)==FUT_IMMEDIATE_APPLIED,31);
    saved=s;
    for(unsigned v=0;v<4;++v) {
        uint32_t data[]={0x3F800000u+v,0x40000000,0x40400000,0x80402010,0,0x3F800000};
        for(unsigned j=0;j<6;++j) {
            CHECK(fut_graphics_immediate_method(&b,&s,0x1818,data[j])==FUT_IMMEDIATE_APPLIED,32);
            if(j<5) CHECK(rejected(0x17FC,0) && rejected(0x1880,0),33);
        }
    }
    CHECK(!memcmp(&saved,&s,sizeof(s)) && b.count==4 && b.defined[0]==0x219 &&
        b.vertices[0][0][3]==1 && b.vertices[0][9][2]==0 && b.vertices[0][9][3]==1 &&
        b.vertices[0][4][0]==0.25f && b.vertices[0][3][0]==64.0f/255 &&
        b.vertices[0][3][1]==32.0f/255 && b.vertices[0][3][2]==16.0f/255 &&
        b.vertices[0][3][3]==128.0f/255,34);
    CHECK(fut_graphics_immediate_method(&b,&s,0x17FC,0)==FUT_IMMEDIATE_READY && b.count==6,35);
    CHECK(!memcmp(b.vertices[3],b.vertices[0],sizeof(b.vertices[0])) &&
        !memcmp(b.vertices[4],b.vertices[2],sizeof(b.vertices[0])) && rejected(0x1818,0),36);
    CHECK(fut_graphics_immediate_consume(&b),37);
    CHECK(fut_graphics_immediate_method(&b,&s,0x17FC,8)==FUT_IMMEDIATE_APPLIED,38);
    s.vertex_array_format[9]=0x32;CHECK(rejected(0x1818,0),39);s.vertex_array_format[9]=0x22;
    for(unsigned i=0;i<FUT_IMMEDIATE_CAPACITY*6;++i)
        CHECK(fut_graphics_immediate_method(&b,&s,0x1818,0)==FUT_IMMEDIATE_APPLIED,40);
    CHECK(b.count==FUT_IMMEDIATE_CAPACITY && rejected(0x1818,0),41);
    memset(&b,0,sizeof(b));memset(&s,0,sizeof(s));
    for(unsigned i=0;i<16;++i) CHECK(fut_graphics_kelvin_state(&s,0x1760+i*4,
        i==0?0x642:i==3?0x640:0x602)==FUT_KELVIN_APPLIED,42);
    s.attributes[9][0]=0.25f;s.attribute_valid[9]=15;
    CHECK(fut_graphics_immediate_method(&b,&s,0x17FC,5)==FUT_IMMEDIATE_APPLIED,43);
    s.vertex_array_format[0]=0x632;CHECK(rejected(0x1818,0),44);s.vertex_array_format[0]=0x642;
    s.vertex_array_format[9]=0x622;CHECK(rejected(0x1818,0),45);s.vertex_array_format[9]=0x602;
    saved=s;
    for(unsigned v=0;v<6;++v) {
        float position[4]={(float)v,480.0f,3.0f,2.0f};uint32_t data[5];
        memcpy(data,position,16);data[4]=0x80402010;
        for(unsigned j=0;j<5;++j) {
            CHECK(fut_graphics_immediate_method(&b,&s,0x1818,data[j])==FUT_IMMEDIATE_APPLIED,46);
            if(j<4) CHECK(rejected(0x17FC,0) && rejected(0x1880,0),47);
        }
        CHECK(b.count==v+1 && b.vertices[v][0][0]==(float)v &&
            b.vertices[v][0][1]==480 && b.vertices[v][0][2]==3 && b.vertices[v][0][3]==2 &&
            b.vertices[v][3][0]==64.0f/255 && b.vertices[v][3][1]==32.0f/255 &&
            b.vertices[v][3][2]==16.0f/255 && b.vertices[v][3][3]==128.0f/255 &&
            b.vertices[v][9][0]==0.25f && b.defined[v]==0x209,48);
        if(v==3) CHECK(rejected(0x17FC,0),49);
    }
    CHECK(!memcmp(&s,&saved,sizeof(s)) &&
        fut_graphics_immediate_method(&b,&s,0x17FC,0)==FUT_IMMEDIATE_READY && b.count==6,50);
    CHECK(fut_graphics_immediate_consume(&b),51);
    CHECK(fut_graphics_immediate_method(&b,&s,0x17FC,5)==FUT_IMMEDIATE_APPLIED,52);
    for(unsigned i=0;i<FUT_IMMEDIATE_CAPACITY*5;++i)
        CHECK(fut_graphics_immediate_method(&b,&s,0x1818,0)==FUT_IMMEDIATE_APPLIED,53);
    CHECK(b.count==FUT_IMMEDIATE_CAPACITY && rejected(0x1818,0),54);
    puts("Immediate attributes and captured inline quads/triangles: layout, W, BGRA, defaults, masks and bounds verified");
    return 0;
}
