#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <windows.h>

#include "../graphics_native_scanout_dispatch.h"
#include "../guest_interrupt_gate.h"
#include "recomp_types.h"

#define CHECK(x,n) do { if(!(x)) { fprintf(stderr,"scanout dispatch test failed: %d\n",n); return n; } } while(0)

static uint8_t *guest_mapping;
static int handler_calls,callback_calls,stack_frees;
static int hide_handler,hide_callback;
static uint32_t expected_context,expected_notification,expected_tib;
RECOMP_TLS uint32_t g_eax,g_ecx,g_edx,g_esp,g_ebx,g_esi,g_edi,g_ebp;
RECOMP_TLS uint32_t g_seh_ebp,g_fs_base;
RECOMP_TLS int g_df;
volatile uint64_t g_icall_count;
ptrdiff_t g_xbox_mem_offset;
static uint32_t *mem32(uint32_t va)
{
    return (uint32_t *)(uintptr_t)((int64_t)g_xbox_mem_offset+(int64_t)va);
}

static void fake_callback(void)
{
    if(g_fs_base!=expected_tib || g_esp<8 || *mem32(g_esp)!=0x002974CEu)
        handler_calls=-100;
    *mem32(0x003458C4u)=*mem32(0x003458C4u)+1u;
    ++callback_calls;g_esp+=4;
}

static void fake_handler(void)
{
    ++handler_calls;
    if(g_ecx!=expected_context || (g_esp&15u)!=8u ||
        *mem32(g_esp)!=0x0029769Bu || *mem32(g_esp+4)!=expected_notification)
        handler_calls=-100;
    g_esp-=4;*mem32(g_esp)=0x002974CEu;fake_callback();
    g_esp+=8;
}

recomp_func_t recomp_lookup(uint32_t va)
{
    if(va==0x002973A0u) return hide_handler?NULL:fake_handler;
    if(va==0x00191340u) return hide_callback?NULL:fake_callback;
    return NULL;
}

int xbox_worker_stack_alloc(void) { return 2; }
void xbox_worker_stack_free(int slot) { if(slot==2) ++stack_frees; }
uint32_t xbox_AllocThreadTib(void) { return 0x7000u; }

int main(void)
{
    FutNativeScanoutDispatch dispatch={.stack_slot=-1};
    uint32_t before=99,after=99;
    expected_context=0x80000100u;expected_notification=0x121u;expected_tib=0x7000;
    guest_mapping=VirtualAlloc(NULL,0x84000000u,MEM_RESERVE,PAGE_NOACCESS);
    CHECK(guest_mapping!=NULL,11);
    CHECK(VirtualAlloc(guest_mapping+0x00300000u,0x00100000u,MEM_COMMIT,PAGE_READWRITE)!=NULL,14);
    CHECK(VirtualAlloc(guest_mapping+0x00780000u,0x00400000u,MEM_COMMIT,PAGE_READWRITE)!=NULL,12);
    CHECK(VirtualAlloc(guest_mapping+0x80000000u,0x00010000u,MEM_COMMIT,PAGE_READWRITE)!=NULL,13);
    g_xbox_mem_offset=(ptrdiff_t)(intptr_t)(uintptr_t)guest_mapping;
    *mem32(expected_context+0x18C)=0x00191340u;
    *mem32(0x003458C4u)=0x12345678u;
    CHECK(fut_native_scanout_dispatch_init(&dispatch) && dispatch.stack_top!=0,1);
    g_esp=0x1234;g_ecx=0x5678;g_fs_base=0x6789;g_seh_ebp=0x9876;g_df=1;
    CHECK(fut_native_scanout_dispatch_call(&dispatch,expected_context,expected_notification,
        0x00191340u,&before,&after)==FUT_SCANOUT_DISPATCH_NO_INTERRUPT_GATE &&
        handler_calls==0,2);
    fut_guest_interrupt_cli();
    CHECK(fut_native_scanout_dispatch_call(&dispatch,expected_context,expected_notification,
        0x00191340u,&before,&after)==FUT_SCANOUT_DISPATCH_NO_INTERRUPT_GATE &&
        handler_calls==0,3);
    fut_guest_interrupt_sti();
    CHECK(fut_guest_interrupt_try_enter(),4);
    *mem32(expected_context+0x18C)=0;
    CHECK(fut_native_scanout_dispatch_call(&dispatch,expected_context,expected_notification,
        0x00191340u,&before,&after)==FUT_SCANOUT_DISPATCH_UNKNOWN_CALLBACK &&
        handler_calls==0,5);
    *mem32(expected_context+0x18C)=0x00191344u;
    CHECK(fut_native_scanout_dispatch_call(&dispatch,expected_context,expected_notification,
        0x00191340u,&before,&after)==FUT_SCANOUT_DISPATCH_UNKNOWN_CALLBACK &&
        handler_calls==0,6);
    *mem32(expected_context+0x18C)=0x00191340u;
    hide_callback=1;
    CHECK(fut_native_scanout_dispatch_call(&dispatch,expected_context,expected_notification,
        0x00191340u,&before,&after)==FUT_SCANOUT_DISPATCH_MISSING_RECOMPILED_CODE &&
        handler_calls==0,9);
    hide_callback=0;hide_handler=1;
    CHECK(fut_native_scanout_dispatch_call(&dispatch,expected_context,expected_notification,
        0x00191340u,&before,&after)==FUT_SCANOUT_DISPATCH_MISSING_RECOMPILED_CODE &&
        handler_calls==0,10);
    hide_handler=0;
    CHECK(fut_native_scanout_dispatch_call(&dispatch,expected_context,expected_notification,
        0x00191340u,&before,&after)==FUT_SCANOUT_DISPATCH_OK &&
        handler_calls==1 && callback_calls==1 && g_esp==0x1234 && g_ecx==0x5678 &&
        g_fs_base==0x6789 && g_seh_ebp==0x9876 && g_df==1 &&
        before==0x12345678u && after==0x12345679u,7);
    fut_guest_interrupt_leave();
    fut_native_scanout_dispatch_destroy(&dispatch);
    VirtualFree(guest_mapping,0,MEM_RELEASE);
    CHECK(stack_frees==1,8);
    puts("Original scanout dispatcher ABI: callback allowlist, gate required, private guest stack/TIB, cdecl cleanup, TLS restore; synthetic routines only");
    return 0;
}
