#include "graphics_scanout.h"
#include <stdio.h>
#include <string.h>
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"scanout plan test failed: %d\n",n);return n;}}while(0)
int main(void)
{
    uint32_t w[FUT_SCANOUT_CONTEXT_WORDS]={0},before[FUT_SCANOUT_CONTEXT_WORDS];
    FutGraphicsScanoutEnqueue plan={0},saved;
    CHECK(fut_graphics_scanout_enqueue_plan(w,(0x123401u<<5)|1,&plan) &&
        plan.slot==0 && plan.physical_address==0x123400 && plan.deadline==1 &&
        plan.next_deadline==2 && plan.next_enqueue==1,1);
    saved=plan;memcpy(before,w,sizeof(w));
    CHECK(!fut_graphics_scanout_enqueue_plan(w,(0x123408u<<5)|1,&plan) &&
        !memcmp(&plan,&saved,sizeof(plan)) && !memcmp(w,before,sizeof(w)),2); /* due now */
    CHECK(!fut_graphics_scanout_enqueue_plan(w,2,&plan),3);
    CHECK(!fut_graphics_scanout_enqueue_plan(w,(0x04000001u<<5)|1,&plan),4);
    w[0x18C/4]=1;CHECK(!fut_graphics_scanout_enqueue_plan(w,0x61,&plan),5);
    w[0x18C/4]=0;
    CHECK(fut_graphics_scanout_enqueue_plan(w,0x61,&plan) && plan.deadline==3,31);
    w[0x18C/4]=0x00191340u;
    w[0x1D4/4]=1;w[0x1D8/4]=0x12345678;
    CHECK(fut_graphics_scanout_enqueue_plan(w,0x61,&plan) &&
        plan.slot==0 && plan.deadline==3,6);
    w[0x1D4/4]=0;w[0x1D8/4]=0;
    w[0x1CC/4]=2;CHECK(!fut_graphics_scanout_enqueue_plan(w,0x61,&plan),7);w[0x1CC/4]=0;
    w[0x174/4]=1;CHECK(!fut_graphics_scanout_enqueue_plan(w,0x61,&plan),8);w[0x174/4]=0;
    w[0x1C0/4]=0xFFFFFFFF;w[0x1C8/4]=0xFFFFFFFF;w[0x1CC/4]=0xFFFFFFFF;w[0x1BC/4]=0xFFFFFFFF;
    CHECK(fut_graphics_scanout_enqueue_plan(w,0x61,&plan) && plan.slot==1 &&
        plan.deadline==2 && plan.next_deadline==5 && plan.next_enqueue==0,9);
    w[0x180/4]=1;w[0x184/4]=w[0x1C0/4];
    CHECK(!fut_graphics_scanout_enqueue_plan(w,0x61,&plan),10);
    memset(w,0,sizeof(w));
    w[0x18C/4]=0x00191340u;
    CHECK(fut_graphics_scanout_enqueue_plan(w,1,&plan) && plan.deadline==1,11); /* retirement follows receipt */
    CHECK(fut_graphics_scanout_enqueue_plan(w,(7u<<5)|1,&plan) && plan.deadline==7 && plan.next_deadline==14,12);
    w[0x1C0/4]=5;w[0x1C8/4]=4;
    CHECK(fut_graphics_scanout_enqueue_plan(w,(3u<<5)|1,&plan) && plan.deadline==7 && plan.next_deadline==10,13);
    CHECK(!fut_graphics_scanout_enqueue_plan(w,(9u<<5)|1,&plan),14);
    w[0x18C/4]=0x00191340u;w[0x1C0/4]=0;w[0x1C8/4]=0;
    CHECK(fut_graphics_scanout_enqueue_plan(w,0x21,&plan) && plan.deadline==1,32);
    CHECK(!fut_graphics_scanout_enqueue_plan(NULL,1,&plan) &&
        !fut_graphics_scanout_enqueue_plan(w,1,NULL),15);
    memset(w,0,sizeof(w));
    plan=(FutGraphicsScanoutEnqueue){0,0x2F88000,12,13,1};
    w[0]=0xFD000000;w[0x1CC/4]=w[0x1BC/4]=1;
    w[0x178/4]=12;w[0x17C/4]=plan.physical_address;
#define MATCH(received,physical,title,increment) fut_graphics_scanout_retirement_matches(w,&plan,received,physical,0,0,title,increment)
    CHECK(MATCH(1,0x2F88000,0x2F88000,2),16);
    CHECK(!MATCH(0,0x2F88000,0x2F88000,2),17);
    CHECK(!MATCH(1,0x2F88000,0x2F88000,0),18);
    CHECK(!MATCH(1,0x2F88004,0x2F88000,2) && !MATCH(1,0x2F88000,0x2F88004,2),19);
    w[0x1BC/4]=0;CHECK(!MATCH(1,0x2F88000,0x2F88000,2),20);w[0x1BC/4]=1;
    w[0x174/4]=1;CHECK(!MATCH(1,0x2F88000,0x2F88000,2),21);w[0x174/4]=0;
    w[0x1CC/4]=2;CHECK(!MATCH(1,0x2F88000,0x2F88000,2),22);w[0x1CC/4]=1;
    w[0x178/4]=11;CHECK(!MATCH(1,0x2F88000,0x2F88000,2),23);w[0x178/4]=12;
    w[0x1B8/4]=2;CHECK(!MATCH(1,0x2F88000,0x2F88000,2),24);w[0x1B8/4]=0;
    plan.slot=1;CHECK(!MATCH(1,0x2F88000,0x2F88000,2),25);
    plan.next_enqueue=0;plan.physical_address=0;plan.deadline=0;
    w[0x1CC/4]=w[0x1BC/4]=0;
    CHECK(MATCH(1,0,0,2),26); /* uint32 ordinal wrap */
    CHECK(!fut_graphics_scanout_retirement_matches(NULL,&plan,1,0,0,0,0,2) &&
        !fut_graphics_scanout_retirement_matches(w,NULL,1,0,0,0,0,2),27);
    w[0x1B8/4]=1;
    CHECK(fut_graphics_scanout_retirement_matches(w,&plan,1,0x1234,0x1234,1,0,2),28);
    CHECK(!fut_graphics_scanout_retirement_matches(w,&plan,1,0x1235,0x1234,1,0,2),29);
    w[0x1B8/4]=0;
    CHECK(fut_graphics_scanout_retirement_matches(w,&plan,1,0x1234,0x1234,1,0,2),30);
#undef MATCH
    puts("Deferred scanout planning: callback-free and exact callback domains, interval/address, counter wrap, full and immediately-due refusal; no presentation");
    return 0;
}
