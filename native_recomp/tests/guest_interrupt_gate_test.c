#include "guest_interrupt_gate.h"
#include <windows.h>
#include <stdio.h>
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"interrupt gate failed: %d\n",n);return n;}}while(0)
typedef struct {HANDLE started,entered,release;int sti;} Worker;
static DWORD WINAPI attempt(void *opaque)
{
    Worker *w=opaque;SetEvent(w->started);
    if(w->sti) fut_guest_interrupt_sti();else fut_guest_interrupt_cli();
    SetEvent(w->entered);
    if(WaitForSingleObject(w->release,2000)!=WAIT_OBJECT_0) return 1;
    if(!w->sti) fut_guest_interrupt_sti();
    return 0;
}
static int blocked_worker(int sti)
{
    Worker w={CreateEventA(NULL,TRUE,FALSE,NULL),CreateEventA(NULL,TRUE,FALSE,NULL),
        CreateEventA(NULL,TRUE,FALSE,NULL),sti};
    CHECK(w.started && w.entered && w.release,10);
    CHECK(fut_guest_interrupt_try_enter(),11);
    HANDLE thread=CreateThread(NULL,0,attempt,&w,0,NULL);CHECK(thread,12);
    CHECK(WaitForSingleObject(w.started,2000)==WAIT_OBJECT_0,13);
    CHECK(WaitForSingleObject(w.entered,30)==WAIT_TIMEOUT && !fut_guest_interrupt_enabled(),14);
    /* A guest STI inside the host scope must not release its outer gate. */
    fut_guest_interrupt_cli();fut_guest_interrupt_cli();fut_guest_interrupt_sti();
    CHECK(fut_guest_interrupt_enabled() && !fut_guest_interrupt_try_enter(),15);
    CHECK(WaitForSingleObject(w.entered,30)==WAIT_TIMEOUT,16);
    /* Interrupt return restores IF even if callback left CLI active. */
    fut_guest_interrupt_cli();fut_guest_interrupt_leave();
    CHECK(WaitForSingleObject(w.entered,2000)==WAIT_OBJECT_0,17);
    if(!sti) CHECK(!fut_guest_interrupt_enabled() && !fut_guest_interrupt_try_enter(),18);
    SetEvent(w.release);
    CHECK(WaitForSingleObject(thread,2000)==WAIT_OBJECT_0,19);
    DWORD code=1;CHECK(GetExitCodeThread(thread,&code) && code==0,20);
    CHECK(fut_guest_interrupt_enabled() && fut_guest_interrupt_try_enter(),21);
    fut_guest_interrupt_leave();
    CloseHandle(thread);CloseHandle(w.started);CloseHandle(w.entered);CloseHandle(w.release);
    return 0;
}
int main(void)
{
    CHECK(fut_guest_interrupt_enabled(),1);
    fut_guest_interrupt_cli();fut_guest_interrupt_cli();
    CHECK(!fut_guest_interrupt_enabled() && !fut_guest_interrupt_try_enter(),2);
    fut_guest_interrupt_sti();CHECK(fut_guest_interrupt_enabled(),3);
    for(unsigned i=0;i<8;++i) {
        int result=blocked_worker(0);if(result) return result;
        result=blocked_worker(1);if(result) return result;
    }
    puts("CLI/STI and host scope exclusion verified; no interrupt generated");return 0;
}
