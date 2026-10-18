#include "graphics_textures.h"
#include "graphics_experiment.h"
#include <stdio.h>
#include <string.h>
#define CHECK(x,n) do { if(!(x)) { fprintf(stderr,"YUY2 capture test failed: %d\n",n); return n; } } while(0)
static uint8_t source[FUT_GRAPHICS_OBSERVED_YUY2_BYTES];
static uint8_t output[FUT_GRAPHICS_OBSERVED_YUY2_BYTES];
static uint8_t decoded[640u*480u*4u];
static unsigned reads,fail_read;
static uint32_t last_address;
static int read_memory(void *context,uint32_t address,void *data,size_t size)
{
    (void)context;++reads;
    if(fail_read || (address!=0x823F1580u && address!=0x82487680u) || size!=sizeof(source)) return 0;
    last_address=address;
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
    FutGraphicsTexturePlan plan={.width=17};
    CHECK(fut_graphics_texture_plan_observed_yuy2_approximation(&state,&dma,&plan),13);
    CHECK(plan.storage==4 && plan.width==640 && plan.height==480 && plan.pitch==1280 &&
        plan.span==sizeof(source) && plan.bytes_per_pixel==2 && plan.address==0x823F1580u,14);
    FutGraphicsTexturePlan sentinel={.width=99};
    CHECK(!_putenv_s("RECOMP_YUY2_023F1580_BT601_APPROXIMATION","0") &&
        !fut_graphics_yuy2_bt601_approximation_enabled(),15);
    CHECK(!_putenv_s("RECOMP_YUY2_023F1580_BT601_APPROXIMATION","1") &&
        fut_graphics_yuy2_bt601_approximation_enabled(),16);
    _putenv_s("RECOMP_YUY2_023F1580_BT601_APPROXIMATION","0");
    FutGraphicsTextureState bad_plan=state;bad_plan.format^=0x10000u;
    CHECK(!fut_graphics_texture_plan_observed_yuy2_approximation(&bad_plan,&dma,&sentinel) && sentinel.width==99,17);
    bad_plan=state;bad_plan.offset++;
    CHECK(!fut_graphics_texture_plan_observed_yuy2_approximation(&bad_plan,&dma,&sentinel) && sentinel.width==99,18);
    bad_plan=state;bad_plan.image_rect++;
    CHECK(!fut_graphics_texture_plan_observed_yuy2_approximation(&bad_plan,&dma,&sentinel) && sentinel.width==99,19);
    bad_plan=state;bad_plan.control0^=1;
    CHECK(!fut_graphics_texture_plan_observed_yuy2_approximation(&bad_plan,&dma,&sentinel) && sentinel.width==99,20);
    bad_plan=state;bad_plan.control1++;
    CHECK(!fut_graphics_texture_plan_observed_yuy2_approximation(&bad_plan,&dma,&sentinel) && sentinel.width==99,21);
    bad_plan=state;bad_plan.valid^=1;
    CHECK(!fut_graphics_texture_plan_observed_yuy2_approximation(&bad_plan,&dma,&sentinel) && sentinel.width==99,22);
    FutGraphicsDmaView other_dma=dma;other_dma.size--;
    CHECK(!fut_graphics_texture_plan_observed_yuy2_approximation(&state,&other_dma,&sentinel) && sentinel.width==99,23);
    other_dma=dma;other_dma.guest_address+=4;
    CHECK(!fut_graphics_texture_plan_observed_yuy2_approximation(&state,&other_dma,&sentinel) && sentinel.width==99,24);
    memset(source,0x78,sizeof(source));
    for(size_t i=0;i<sizeof(source);i+=4) {source[i+1]=0x44;source[i+3]=0xD4;}
    memset(decoded,0xA5,sizeof(decoded));
    CHECK(fut_graphics_texture_yuy2_bt601_to_bgra(source,sizeof(source),decoded,sizeof(decoded)),25);
    CHECK(decoded[0]==0 && decoded[1]==76 && decoded[2]==255 && decoded[3]==255 &&
        decoded[4]==0 && decoded[5]==76 && decoded[6]==255 && decoded[7]==255,26);
    CHECK(!fut_graphics_texture_yuy2_bt601_to_bgra(source,sizeof(source)-1,decoded,sizeof(decoded)) &&
        decoded[0]==0 && decoded[1]==76 && decoded[2]==255 && decoded[3]==255,27);
    for(unsigned i=0;i<sizeof(source);++i) source[i]=(uint8_t)(i*29u+7u);
    memset(output,0xA5,sizeof(output));reads=0;fail_read=0;
    CHECK(fut_graphics_texture_capture_observed_yuy2(read_memory,NULL,&state,&dma,output),1);
    CHECK(reads==1 && last_address==0x823F1580u && !memcmp(source,output,sizeof(source)),2);
    FutGraphicsTextureState second_capture=state;second_capture.offset=0x02487680u;
    reads=0;
    CHECK(fut_graphics_texture_capture_observed_yuy2(read_memory,NULL,&second_capture,&dma,output),28);
    CHECK(reads==1 && last_address==0x82487680u && !memcmp(source,output,sizeof(source)),29);
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
    FutGraphicsTextureState third_capture=state;third_capture.offset=0x02487684u;
    CHECK(rejected_without_read(third_capture,dma),30);
    puts("Exact LC_IMAGE captures, transactional reads, default-off opt-ins and BT.601 vector verified");
    return 0;
}
