#include "recomp_types.h"
#include "kernel.h"
#include "xbox_memory_layout.h"
#include "guest_interrupt_gate.h"
#include <windows.h>
#include <stdio.h>
#include <string.h>
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"native DPC gate failed: %d\n",n);return n;}}while(0)
extern int xbox_DrainHostDpcs(void);
extern int xbox_InvokeConnectedInterrupt(uint32_t bus);
extern uint32_t xbox_GetConnectedInterrupt(uint32_t bus);
extern void xbox_kernel_bridge_init(void);
extern recomp_func_t recomp_lookup_kernel(uint32_t address);
static HANDLE started,entered,release_callback;
static uint32_t dpc,thunks,interrupt_object;
static LONG calls,callback_bad;
static unsigned isr_calls,sync_calls;
static uint32_t *word(uint32_t address)
{
    return (uint32_t*)((uintptr_t)address+xbox_GetMemoryOffset());
}
static void callback(void)
{
    if(!g_fs_base || !g_esp || word(g_esp)[1]!=dpc || word(g_esp)[2]!=0x12345678 ||
        word(g_esp)[3]!=0x11223344 || word(g_esp)[4]!=0x55667788 ||
        *(uint8_t*)word(g_fs_base+0x24)!=2 || !fut_guest_interrupt_enabled())
        InterlockedExchange(&callback_bad,1);
    InterlockedIncrement(&calls);SetEvent(entered);
    if(WaitForSingleObject(release_callback,2000)!=WAIT_OBJECT_0)
        InterlockedExchange(&callback_bad,2);
    g_esp+=20; /* synthetic __stdcall callback's ret 16 */
}
static void isr_callback(void)
{
    if(word(g_esp)[1]!=interrupt_object || word(g_esp)[2]!=0xAABBCCDD ||
        *(uint8_t*)word(g_fs_base+0x24)!=24 || fut_guest_interrupt_enabled() ||
        xbox_InvokeConnectedInterrupt(3)!=-2)
        InterlockedExchange(&callback_bad,3);
    ++isr_calls;g_eax=1;g_esp+=12;
}
static void sync_callback(void)
{
    if(word(g_esp)[1]!=0xAABBCCDD || *(uint8_t*)word(g_fs_base+0x24)!=24)
        InterlockedExchange(&callback_bad,4);
    ++sync_calls;g_eax=1;g_esp+=8;
}
recomp_func_t recomp_lookup(uint32_t address)
{
    if(address==0x12000) return isr_callback;
    if(address==0x12010) return sync_callback;
    return address==0x11000?callback:NULL;
}
static uint32_t call_bridge(unsigned slot,const uint32_t *args,unsigned count,uint32_t stack)
{
    word(stack)[0]=0;
    for(unsigned i=0;i<count;++i) word(stack)[i+1]=args[i];
    g_esp=stack;recomp_lookup_kernel(word(thunks)[slot])();return g_eax;
}
static DWORD WINAPI worker(void *unused)
{
    (void)unused;int slot=xbox_worker_stack_alloc();if(slot<0) return 1;
    g_esp=XBOX_WORKER_STACK_TOP(slot);g_fs_base=xbox_AllocThreadTib();
    if(!g_fs_base) {xbox_worker_stack_free(slot);return 2;}
    uint32_t top=g_esp;
    SetEvent(started);
    int drained=xbox_DrainHostDpcs();
    int good=drained && g_esp==top && *(uint8_t*)word(g_fs_base+0x24)==0;
    xbox_worker_stack_free(slot);return good?0:3;
}
int main(void)
{
    CHECK(!xbox_DrainHostDpcs(),1); /* no initialized guest execution context */
    _putenv_s("RECOMP_EXTERNAL_GRAPHICS_COMPLETION","1");
    SetEnvironmentVariableA("RECOMP_EXTERNAL_GRAPHICS_COMPLETION","1");
    uint32_t image[1024]={0};memcpy(image,"XBEH",4);
    image[0x104/4]=0x10000;image[0x108/4]=sizeof(image);
    image[0x10C/4]=sizeof(image); /* no code, sections, imports or game input */
    image[0x12C/4]=0x10300; /* empty synthetic TLS directory in this header */
    CHECK(xbox_MemoryLayoutInit(image,sizeof(image)),2);
    uint32_t scratch=xbox_HeapAlloc(0x4000,4096);CHECK(scratch,3);
    thunks=scratch;dpc=scratch+0x100;interrupt_object=scratch+0x180;
    uint32_t stack=scratch+0x2000,irql_output=scratch+0x300;
    word(thunks)[0]=0x80000000u|107;word(thunks)[1]=0x80000000u|119;
    word(thunks)[2]=0x80000000u|44;word(thunks)[3]=0x80000000u|109;
    word(thunks)[4]=0x80000000u|98;word(thunks)[5]=0x80000000u|153;
    xbox_kernel_set_thunk_address(thunks,6);xbox_kernel_bridge_init();
    word(irql_output)[0]=0xA5A5A5A5;
    const uint32_t vector_args[]={3,irql_output};
    CHECK(call_bridge(2,vector_args,2,stack)==0x33 && word(irql_output)[0]==0xA5A5A518,15);
    KIRQL direct_irql=0;CHECK(xbox_HalGetInterruptVector(3,&direct_irql)==0x33 && direct_irql==24,16);
    direct_irql=0xA5;CHECK(!xbox_HalGetInterruptVector(28,&direct_irql) && direct_irql==0xA5,17);
    word(interrupt_object+0x70)[0]=0xDEADBEEF;
    const uint32_t irq_init[]={interrupt_object,0x12000,0xAABBCCDD,0x33,0xA5A5A518,0,1};
    call_bridge(3,irq_init,7,stack);
    CHECK(word(interrupt_object)[2]==3 && word(interrupt_object)[3]==24 &&
        ((uint8_t*)word(interrupt_object))[0x10]==0 &&
        ((uint8_t*)word(interrupt_object))[0x11]==1 &&
        word(interrupt_object+0x70)[0]==0xDEADBEEF,18);
    const uint32_t connect[]={interrupt_object};
    CHECK(call_bridge(4,connect,1,stack)==1 && xbox_GetConnectedInterrupt(3)==interrupt_object,19);
    const uint32_t null_interrupt[]={0};CHECK(!call_bridge(4,null_interrupt,1,stack),25);
    CHECK(xbox_InvokeConnectedInterrupt(3)==-2 && !isr_calls,20);
    g_esp=stack;CHECK(fut_guest_interrupt_try_enter(),21);
    CHECK(xbox_InvokeConnectedInterrupt(3)==1 && isr_calls==1 && !callback_bad &&
        g_esp==stack && *(uint8_t*)word(g_fs_base+0x24)==0 && !fut_guest_interrupt_enabled(),22);
    CHECK(xbox_InvokeConnectedInterrupt(31)==-1,23);fut_guest_interrupt_leave();
    const uint32_t synchronize[]={interrupt_object,0x12010,0xAABBCCDD};
    CHECK(call_bridge(5,synchronize,3,stack)==1 && sync_calls==1 && !callback_bad &&
        *(uint8_t*)word(g_fs_base+0x24)==0 && fut_guest_interrupt_enabled(),24);
    const uint32_t init[]={dpc,0x11000,0x12345678};
    const uint32_t insert[]={dpc,0x11223344,0x55667788};
    call_bridge(0,init,3,stack);
    CHECK(call_bridge(1,insert,3,stack)==1 && call_bridge(1,insert,3,stack)==0,4);
    started=CreateEventA(NULL,TRUE,FALSE,NULL);entered=CreateEventA(NULL,TRUE,FALSE,NULL);
    release_callback=CreateEventA(NULL,TRUE,FALSE,NULL);CHECK(started && entered && release_callback,5);
    fut_guest_interrupt_cli();
    HANDLE thread=CreateThread(NULL,0,worker,NULL,0,NULL);CHECK(thread,6);
    CHECK(WaitForSingleObject(started,2000)==WAIT_OBJECT_0 &&
        WaitForSingleObject(entered,30)==WAIT_TIMEOUT && !calls &&
        ((uint8_t*)word(dpc))[2]==1,7);
    fut_guest_interrupt_sti();
    CHECK(WaitForSingleObject(entered,2000)==WAIT_OBJECT_0,8);
    CHECK(!fut_guest_interrupt_try_enter() && !callback_bad && calls==1,9);
    SetEvent(release_callback);
    CHECK(WaitForSingleObject(thread,2000)==WAIT_OBJECT_0,10);
    DWORD result=99;CHECK(GetExitCodeThread(thread,&result) && result==0 && !callback_bad,11);
    CHECK(!((uint8_t*)word(dpc))[2] && calls==1 && fut_guest_interrupt_enabled(),12);
    const uint32_t missing[]={dpc,0x11004,0x12345678};
    call_bridge(0,missing,3,stack);CHECK(call_bridge(1,insert,3,stack)==1,13);
    CHECK(!xbox_DrainHostDpcs() && calls==1 && fut_guest_interrupt_try_enter(),14);
    fut_guest_interrupt_leave(); /* unresolved callback is not successful drain */
    CloseHandle(thread);CloseHandle(started);CloseHandle(entered);CloseHandle(release_callback);
    xbox_MemoryLayoutShutdown();
    puts("Real kernel IRQ ABI and DPC queue: synthetic callbacks, IRQ3/vector0x33/IRQL24, private stack/TIB, IRQL2 DPC and restored ESP/IRQL; no game ISR or VBlank");
    return 0;
}
