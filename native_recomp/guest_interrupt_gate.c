#include "guest_interrupt_gate.h"
#include <windows.h>
#include <stdatomic.h>
#include <stdio.h>
#include <stdlib.h>

static INIT_ONCE once=INIT_ONCE_STATIC_INIT;
static CRITICAL_SECTION gate;
static atomic_bool enabled=ATOMIC_VAR_INIT(1);
static _Thread_local unsigned cli_owned,host_owned;

static BOOL CALLBACK initialize(PINIT_ONCE init,PVOID parameter,PVOID *context)
{
    (void)init;(void)parameter;(void)context;
    InitializeCriticalSection(&gate);return TRUE;
}
static void ready(void)
{
    if(!InitOnceExecuteOnce(&once,initialize,NULL,NULL)) {
        fputs("[INTERRUPT] cannot initialize serialization gate\n",stderr);_Exit(3);
    }
}
void fut_guest_interrupt_cli(void)
{
    ready();
    if(!cli_owned) {EnterCriticalSection(&gate);cli_owned=1;}
    atomic_store_explicit(&enabled,0,memory_order_seq_cst);
}
void fut_guest_interrupt_sti(void)
{
    ready();
    /* Unpaired STI cannot enable interrupts inside another thread's CLI. */
    if(!cli_owned) EnterCriticalSection(&gate);
    atomic_store_explicit(&enabled,1,memory_order_seq_cst);
    cli_owned=0;LeaveCriticalSection(&gate);
}
uint8_t fut_guest_interrupt_enabled(void)
{
    return (uint8_t)atomic_load_explicit(&enabled,memory_order_seq_cst);
}
int fut_guest_interrupt_try_enter(void)
{
    ready();
    if(host_owned || !TryEnterCriticalSection(&gate)) return 0;
    if(!fut_guest_interrupt_enabled()) {LeaveCriticalSection(&gate);return 0;}
    host_owned=1;atomic_store_explicit(&enabled,0,memory_order_seq_cst);
    return 1;
}
void fut_guest_interrupt_leave(void)
{
    if(!host_owned) {
        fputs("[INTERRUPT] unowned interrupt scope exit\n",stderr);_Exit(3);
    }
    /* Model restoration at host interrupt return, not an extra guest STI.
     * A CLI executed by the callback acquired one recursive lock level. */
    if(cli_owned) {cli_owned=0;LeaveCriticalSection(&gate);}
    atomic_store_explicit(&enabled,1,memory_order_seq_cst);
    host_owned=0;LeaveCriticalSection(&gate);
}
