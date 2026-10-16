#include "graphics_native_scanout_dispatch.h"

#include <stddef.h>
#include <stdint.h>
#include <string.h>

#include "guest_interrupt_gate.h"
#include "recomp_types.h"
#include "xbox_memory_layout.h"

#define FUT_SCANOUT_HANDLER_VA 0x002973A0u
#define FUT_SCANOUT_CALLBACK_VA 0x00191340u
#define FUT_SCANOUT_CALLER_RETURN_VA 0x0029769Bu

typedef struct {
    uint32_t eax, ecx, edx, esp, ebx, esi, edi, ebp;
    uint32_t seh_ebp, fs_base;
    int df;
} FutRecompRegisterSnapshot;

static void snapshot_registers(FutRecompRegisterSnapshot *s)
{
    s->eax=g_eax;s->ecx=g_ecx;s->edx=g_edx;s->esp=g_esp;
    s->ebx=g_ebx;s->esi=g_esi;s->edi=g_edi;s->ebp=g_ebp;
    s->seh_ebp=g_seh_ebp;s->fs_base=g_fs_base;s->df=g_df;
}

static void restore_registers(const FutRecompRegisterSnapshot *s)
{
    g_eax=s->eax;g_ecx=s->ecx;g_edx=s->edx;g_esp=s->esp;
    g_ebx=s->ebx;g_esi=s->esi;g_edi=s->edi;g_ebp=s->ebp;
    g_seh_ebp=s->seh_ebp;g_fs_base=s->fs_base;g_df=s->df;
}

static uint32_t *guest_u32(uint32_t va)
{
    if (va>UINT32_MAX-4u) return NULL;
    return (uint32_t *)(uintptr_t)((int64_t)g_xbox_mem_offset+(int64_t)va);
}

int fut_native_scanout_dispatch_init(FutNativeScanoutDispatch *dispatch)
{
    if(!dispatch) return 0;
    if(dispatch->stack_slot>=0 && dispatch->tib) return 1;
    dispatch->stack_slot=xbox_worker_stack_alloc();
    if(dispatch->stack_slot<0) return 0;
    dispatch->stack_top=XBOX_WORKER_STACK_TOP(dispatch->stack_slot);
    dispatch->tib=xbox_AllocThreadTib();
    if(!dispatch->tib) {
        xbox_worker_stack_free(dispatch->stack_slot);
        dispatch->stack_slot=-1;dispatch->stack_top=0;
        return 0;
    }
    return 1;
}

void fut_native_scanout_dispatch_destroy(FutNativeScanoutDispatch *dispatch)
{
    if(!dispatch) return;
    if(dispatch->tib && dispatch->stack_slot>=0) xbox_worker_stack_free(dispatch->stack_slot);
    dispatch->stack_slot=-1;dispatch->stack_top=0;dispatch->tib=0;
}

FutNativeScanoutDispatchResult fut_native_scanout_dispatch_preflight(
    FutNativeScanoutDispatch *dispatch,uint32_t context,uint32_t notification,
    uint32_t expected_callback)
{
    uint32_t *callback_word;
    if(!dispatch || dispatch->stack_slot<0 || !dispatch->tib || !dispatch->stack_top ||
        !context || context>UINT32_MAX-0x190u || (notification&31u)!=1u ||
        expected_callback!=FUT_SCANOUT_CALLBACK_VA)
        return FUT_SCANOUT_DISPATCH_NO_CONTEXT;
    if(!fut_guest_interrupt_scope_active()) return FUT_SCANOUT_DISPATCH_NO_INTERRUPT_GATE;
    callback_word=guest_u32(context+0x18Cu);
    if(!callback_word || *callback_word!=expected_callback)
        return FUT_SCANOUT_DISPATCH_UNKNOWN_CALLBACK;
    if(!guest_u32(0x003458C4u) || !recomp_lookup(FUT_SCANOUT_HANDLER_VA) ||
        !recomp_lookup(FUT_SCANOUT_CALLBACK_VA))
        return FUT_SCANOUT_DISPATCH_MISSING_RECOMPILED_CODE;
    return FUT_SCANOUT_DISPATCH_OK;
}

FutNativeScanoutDispatchResult fut_native_scanout_dispatch_call(
    FutNativeScanoutDispatch *dispatch,uint32_t context,uint32_t notification,
    uint32_t expected_callback,uint32_t *callback_tsc_before,uint32_t *callback_tsc_after)
{
    uint32_t *stack,*tsc_word,tsc_before,tsc_after;
    recomp_func_t handler,callback;
    FutRecompRegisterSnapshot saved;
    uint32_t entry_esp;

    if(!dispatch || dispatch->stack_slot<0 || !dispatch->tib ||
        !dispatch->stack_top || !context || context>UINT32_MAX-0x190u ||
        (notification&31u)!=1u || expected_callback!=FUT_SCANOUT_CALLBACK_VA)
        return FUT_SCANOUT_DISPATCH_NO_CONTEXT;
    if(!fut_guest_interrupt_scope_active()) return FUT_SCANOUT_DISPATCH_NO_INTERRUPT_GATE;

    FutNativeScanoutDispatchResult preflight=fut_native_scanout_dispatch_preflight(
        dispatch,context,notification,expected_callback);
    if(preflight!=FUT_SCANOUT_DISPATCH_OK) return preflight;
    tsc_word=guest_u32(0x003458C4u);
    if(!tsc_word) return FUT_SCANOUT_DISPATCH_NO_CONTEXT;
    tsc_before=*tsc_word;
    handler=recomp_lookup(FUT_SCANOUT_HANDLER_VA);
    callback=recomp_lookup(FUT_SCANOUT_CALLBACK_VA);
    if(!handler || !callback) return FUT_SCANOUT_DISPATCH_MISSING_RECOMPILED_CODE;

    entry_esp=dispatch->stack_top-8u;
    stack=guest_u32(entry_esp);
    if(!stack) return FUT_SCANOUT_DISPATCH_BAD_GUEST_STACK;
    snapshot_registers(&saved);
    stack[0]=FUT_SCANOUT_CALLER_RETURN_VA;
    stack[1]=notification;
    g_fs_base=dispatch->tib;
    g_esp=entry_esp;g_ecx=context;
    g_eax=g_edx=g_ebx=g_esi=g_edi=g_ebp=0;
    g_seh_ebp=entry_esp;g_df=0;
    handler();
    tsc_after=*tsc_word;
    int stack_ok=g_esp==dispatch->stack_top;
    restore_registers(&saved);
    if(!stack_ok) return FUT_SCANOUT_DISPATCH_BAD_GUEST_STACK;
    if(tsc_before==tsc_after) return FUT_SCANOUT_DISPATCH_CALLBACK_NOT_OBSERVED;
    if(callback_tsc_before) *callback_tsc_before=tsc_before;
    if(callback_tsc_after) *callback_tsc_after=tsc_after;
    return FUT_SCANOUT_DISPATCH_OK;
}
