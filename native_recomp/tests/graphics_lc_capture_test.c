#include "graphics_textures.h"
#include <stdio.h>
#include <string.h>
#define CHECK(x,n) do { if(!(x)) { fprintf(stderr,"YUY2 capture test failed: %d\n",n); return n; } } while(0)
static uint8_t source[FUT_GRAPHICS_OBSERVED_YUY2_BYTES];
static uint8_t output[FUT_GRAPHICS_OBSERVED_YUY2_BYTES];
static unsigned reads,fail_read;
static int read_memory(void *context,uint32_t address,void *data,size_t size)
{
    (void)context;++reads;
    if(fail_read || address!=0x823F1580u || size!=sizeof(source)) return 0;
    memcpy(data,source,size);return 1;
}
static int rejected_without_read(FutGraphicsTextureState state,FutGraphicsDmaView dma)
{
    memset(output,0xA5,sizeof(output));reads=0;
    return !fut_graphics_texture_capture_observed_yuy2(read_memory,NULL,&state,&dma,output) &&
        !reads && output[0]==0xA5 && output[sizeof(output)-1]==0xA5;
}
int main(void)
{
    FutGraphicsTextureState state={.format=0x00012429u,.offset=0x023F1580u,
        .image_rect=0x028001E0u,.control0=0x4003FFC0u,.control1=0x05000000u,.valid=0x3BFu};
    FutGraphicsDmaView dma={0x80000000u,0x04000000u};
    for(unsigned i=0;i<sizeof(source);++i) source[i]=(uint8_t)(i*29u+7u);
    memset(output,0xA5,sizeof(output));reads=0;fail_read=0;
    CHECK(fut_graphics_texture_capture_observed_yuy2(read_memory,NULL,&state,&dma,output),1);
    CHECK(reads==1 && !memcmp(source,output,sizeof(source)),2);
    memset(output,0xA5,sizeof(output));reads=0;fail_read=1;
    CHECK(!fut_graphics_texture_capture_observed_yuy2(read_memory,NULL,&state,&dma,output),3);
    CHECK(reads==1 && output[0]==0xA5 && output[sizeof(output)-1]==0xA5,4);
    fail_read=0;
    FutGraphicsTextureState bad=state;bad.format^=0x10000u;CHECK(rejected_without_read(bad,dma),5);
    bad=state;bad.offset++;CHECK(rejected_without_read(bad,dma),6);
    bad=state;bad.image_rect++;CHECK(rejected_without_read(bad,dma),7);
    bad=state;bad.control0^=1;CHECK(rejected_without_read(bad,dma),8);
    bad=state;bad.control1++;CHECK(rejected_without_read(bad,dma),9);
    bad=state;bad.valid^=1;CHECK(rejected_without_read(bad,dma),10);
    FutGraphicsDmaView short_dma=dma;short_dma.size--;
    CHECK(rejected_without_read(state,short_dma),11);
    FutGraphicsDmaView other=dma;other.guest_address+=4;
    CHECK(rejected_without_read(state,other),12);
    puts("Exact LC_IMAGE YUY2 capture scope and transactional read verified; no decoding claim");
    return 0;
}
