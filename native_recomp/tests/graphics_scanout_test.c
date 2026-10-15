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
    w[0x18C/4]=1;CHECK(!fut_graphics_scanout_enqueue_plan(w,0x21,&plan),5);w[0x18C/4]=0;
    w[0x1D4/4]=1;CHECK(!fut_graphics_scanout_enqueue_plan(w,0x21,&plan),6);w[0x1D4/4]=0;
    w[0x1CC/4]=2;CHECK(!fut_graphics_scanout_enqueue_plan(w,0x21,&plan),7);w[0x1CC/4]=0;
    w[0x174/4]=1;CHECK(!fut_graphics_scanout_enqueue_plan(w,0x21,&plan),8);w[0x174/4]=0;
    w[0x1C0/4]=0xFFFFFFFF;w[0x1C8/4]=0xFFFFFFFF;w[0x1CC/4]=0xFFFFFFFF;w[0x1BC/4]=0xFFFFFFFF;
    CHECK(fut_graphics_scanout_enqueue_plan(w,0x21,&plan) && plan.slot==1 &&
        plan.deadline==0 && plan.next_deadline==1 && plan.next_enqueue==0,9);
    w[0x180/4]=1;w[0x184/4]=w[0x1C0/4];
    CHECK(!fut_graphics_scanout_enqueue_plan(w,0x21,&plan),10);
    memset(w,0,sizeof(w));
    CHECK(fut_graphics_scanout_enqueue_plan(w,1,&plan) && plan.deadline==1 && plan.next_deadline==1,11);
    CHECK(fut_graphics_scanout_enqueue_plan(w,(7u<<5)|1,&plan) && plan.deadline==7 && plan.next_deadline==14,12);
    w[0x1C0/4]=5;w[0x1C8/4]=1;
    CHECK(fut_graphics_scanout_enqueue_plan(w,0x21,&plan) && plan.deadline==6 && plan.next_deadline==7,13);
    CHECK(!fut_graphics_scanout_enqueue_plan(w,(9u<<5)|1,&plan),14);
    CHECK(!fut_graphics_scanout_enqueue_plan(NULL,1,&plan) &&
        !fut_graphics_scanout_enqueue_plan(w,1,NULL),15);
    puts("Deferred scanout planning: interval/address, counter wrap, full/due/callback/time refusal; no presentation");
    return 0;
}
