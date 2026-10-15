#define COBJMACROS
#include "graphics_native_vblank.h"
#include <stdlib.h>
#include <string.h>
typedef struct {
    IDXGIOutput *output;
    DXGI_OUTPUT_DESC source;
    HANDLE stop,changed,thread;
    DWORD owner;
    CRITICAL_SECTION lock;
    HRESULT status;
    FutNativeVBlankReceipt latest;
} ClockState;
static DWORD WINAPI vblank_worker(void *opaque)
{
    ClockState *s=opaque;HRESULT status=E_ABORT;
    while(WaitForSingleObject(s->stop,0)==WAIT_TIMEOUT) {
        status=IDXGIOutput_WaitForVBlank(s->output);
        if(status!=S_OK) break;
        LARGE_INTEGER now;
        if(!QueryPerformanceCounter(&now) || now.QuadPart<=0) {status=E_FAIL;break;}
        DXGI_OUTPUT_DESC current;
        status=IDXGIOutput_GetDesc(s->output,&current);
        if(status!=S_OK) break;
        if(!current.AttachedToDesktop || current.Monitor!=s->source.Monitor ||
            !EqualRect(&current.DesktopCoordinates,&s->source.DesktopCoordinates) ||
            current.Rotation!=s->source.Rotation) {status=DXGI_ERROR_ACCESS_LOST;break;}
        if(WaitForSingleObject(s->stop,0)!=WAIT_TIMEOUT) {status=E_ABORT;break;}
        EnterCriticalSection(&s->lock);
        ++s->latest.sequence;s->latest.qpc_after=now;s->latest.monitor=current.Monitor;
        SetEvent(s->changed);LeaveCriticalSection(&s->lock);
    }
    EnterCriticalSection(&s->lock);
    /* Non-S_OK success codes are terminal too, not observations. */
    s->status=status==S_OK?E_ABORT:status;
    SetEvent(s->changed);LeaveCriticalSection(&s->lock);return 0;
}
static void destroy(ClockState *s)
{
    if(s->thread) CloseHandle(s->thread);
    if(s->changed) CloseHandle(s->changed);
    if(s->stop) CloseHandle(s->stop);
    if(s->output) IDXGIOutput_Release(s->output);
    DeleteCriticalSection(&s->lock);free(s);
}
HRESULT fut_native_vblank_start(FutNativeVBlank *clock,IDXGIOutput *output)
{
    if(!clock || !output) return E_INVALIDARG;
    if(clock->state) return E_PENDING;
    ClockState *s=calloc(1,sizeof(*s));if(!s) return E_OUTOFMEMORY;
    InitializeCriticalSection(&s->lock);s->owner=GetCurrentThreadId();s->status=S_OK;
    HRESULT hr=IDXGIOutput_GetDesc(output,&s->source);
    if(hr!=S_OK) {destroy(s);return hr;}
    if(!s->source.AttachedToDesktop || !s->source.Monitor) {destroy(s);return DXGI_ERROR_NOT_CURRENTLY_AVAILABLE;}
    s->output=output;IDXGIOutput_AddRef(output);
    s->stop=CreateEventA(NULL,TRUE,FALSE,NULL);s->changed=CreateEventA(NULL,TRUE,FALSE,NULL);
    if(!s->stop || !s->changed) {destroy(s);return E_FAIL;}
    s->thread=CreateThread(NULL,0,vblank_worker,s,0,NULL);
    if(!s->thread) {destroy(s);return E_FAIL;}
    clock->state=s;return S_OK;
}
HRESULT fut_native_vblank_close(FutNativeVBlank *clock,DWORD timeout_ms)
{
    if(!clock || !timeout_ms || timeout_ms>2000) return E_INVALIDARG;
    ClockState *s=clock->state;if(!s) return S_OK;
    if(s->owner!=GetCurrentThreadId()) return E_INVALIDARG;
    SetEvent(s->stop);
    DWORD wait=WaitForSingleObject(s->thread,timeout_ms);
    if(wait==WAIT_TIMEOUT) return HRESULT_FROM_WIN32(WAIT_TIMEOUT);
    if(wait!=WAIT_OBJECT_0) return E_FAIL;
    destroy(s);clock->state=NULL;return S_OK;
}
HRESULT fut_native_vblank_wait(FutNativeVBlank *clock,uint64_t after,DWORD timeout_ms,
    HANDLE cancel,FutNativeVBlankReceipt *receipt)
{
    if(!clock || !clock->state || !receipt || !timeout_ms || timeout_ms>2000) return E_INVALIDARG;
    ClockState *s=clock->state;if(s->owner!=GetCurrentThreadId()) return E_INVALIDARG;
    ULONGLONG deadline=GetTickCount64()+timeout_ms;
    for(;;) {
        if(cancel && WaitForSingleObject(cancel,0)!=WAIT_TIMEOUT) return E_ABORT;
        if(WaitForSingleObject(s->stop,0)!=WAIT_TIMEOUT) return E_ABORT;
        EnterCriticalSection(&s->lock);
        HRESULT status=s->status;
        if(status==S_OK && s->latest.sequence>after) {
            *receipt=s->latest;LeaveCriticalSection(&s->lock);return S_OK;
        }
        ResetEvent(s->changed);LeaveCriticalSection(&s->lock);
        if(status!=S_OK) return status;
        ULONGLONG now=GetTickCount64();
        if(now>=deadline) return HRESULT_FROM_WIN32(WAIT_TIMEOUT);
        HANDLE handles[3]={s->changed,s->stop,cancel};
        DWORD wait=WaitForMultipleObjects(cancel?3:2,handles,FALSE,(DWORD)(deadline-now));
        if(wait==WAIT_TIMEOUT) return HRESULT_FROM_WIN32(WAIT_TIMEOUT);
        if(wait==WAIT_FAILED) return E_FAIL;
    }
}
