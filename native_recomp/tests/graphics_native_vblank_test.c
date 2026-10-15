#define COBJMACROS
#include "graphics_native_vblank.h"
#include <stdio.h>
#include <string.h>
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"native VBlank source failed: %d\n",n);return n;}}while(0)
typedef struct {
    IDXGIOutput iface;LONG refs;HANDLE entered,release;int fail,changed;
} FakeOutput;
static ULONG STDMETHODCALLTYPE fake_addref(IDXGIOutput *p) {return (ULONG)InterlockedIncrement(&((FakeOutput*)p)->refs);}
static ULONG STDMETHODCALLTYPE fake_release(IDXGIOutput *p) {return (ULONG)InterlockedDecrement(&((FakeOutput*)p)->refs);}
static HRESULT STDMETHODCALLTYPE fake_desc(IDXGIOutput *p,DXGI_OUTPUT_DESC *desc)
{
    memset(desc,0,sizeof(*desc));desc->AttachedToDesktop=TRUE;
    desc->Monitor=(HMONITOR)(uintptr_t)(1+((FakeOutput*)p)->changed);desc->DesktopCoordinates=(RECT){0,0,1,1};
    desc->Rotation=DXGI_MODE_ROTATION_IDENTITY;return S_OK;
}
static HRESULT STDMETHODCALLTYPE fake_wait(IDXGIOutput *p)
{
    FakeOutput *f=(FakeOutput*)p;SetEvent(f->entered);
    if(f->fail) return E_FAIL;
    return WaitForSingleObject(f->release,2000)==WAIT_OBJECT_0?S_OK:E_ABORT;
}
static IDXGIOutputVtbl methods={.AddRef=fake_addref,.Release=fake_release,
    .GetDesc=fake_desc,.WaitForVBlank=fake_wait};
typedef struct {FutNativeVBlank *clock;HRESULT result;} Foreign;
static DWORD WINAPI wrong_owner(void *opaque)
{
    Foreign *f=opaque;f->result=fut_native_vblank_close(f->clock,10);return 0;
}
static int test_lifecycle(void)
{
    FakeOutput f={0};f.iface.lpVtbl=&methods;f.refs=1;
    f.entered=CreateEventA(NULL,TRUE,FALSE,NULL);f.release=CreateEventA(NULL,TRUE,FALSE,NULL);
    HANDLE cancel=CreateEventA(NULL,TRUE,TRUE,NULL);CHECK(f.entered && f.release && cancel,1);
    FutNativeVBlank clock={0};FutNativeVBlankReceipt r,saved;
    memset(&r,0xA5,sizeof(r));memcpy(&saved,&r,sizeof(r));
    CHECK(fut_native_vblank_start(&clock,NULL)==E_INVALIDARG && !clock.state,2);
    CHECK(fut_native_vblank_start(&clock,&f.iface)==S_OK && f.refs==2,3);
    CHECK(WaitForSingleObject(f.entered,2000)==WAIT_OBJECT_0,4);
    void *instance=clock.state;
    CHECK(fut_native_vblank_start(&clock,&f.iface)==E_PENDING && clock.state==instance,5);
    CHECK(fut_native_vblank_wait(&clock,0,10,cancel,&r)==E_ABORT && !memcmp(&r,&saved,sizeof(r)),6);
    ResetEvent(cancel);
    CHECK(fut_native_vblank_wait(&clock,0,10,cancel,&r)==HRESULT_FROM_WIN32(WAIT_TIMEOUT) &&
        !memcmp(&r,&saved,sizeof(r)),7);
    Foreign foreign={&clock,S_OK};HANDLE thread=CreateThread(NULL,0,wrong_owner,&foreign,0,NULL);
    CHECK(thread && WaitForSingleObject(thread,2000)==WAIT_OBJECT_0 && foreign.result==E_INVALIDARG,8);
    CloseHandle(thread);
    CHECK(fut_native_vblank_close(&clock,10)==HRESULT_FROM_WIN32(WAIT_TIMEOUT) &&
        clock.state==instance && f.refs==2,9);
    CHECK(fut_native_vblank_wait(&clock,0,10,NULL,&r)==E_ABORT && !memcmp(&r,&saved,sizeof(r)),10);
    SetEvent(f.release);
    CHECK(fut_native_vblank_close(&clock,2000)==S_OK && !clock.state && f.refs==1,11);
    f.fail=1;ResetEvent(f.entered);
    CHECK(fut_native_vblank_start(&clock,&f.iface)==S_OK,12);
    CHECK(fut_native_vblank_wait(&clock,0,2000,NULL,&r)==E_FAIL && !memcmp(&r,&saved,sizeof(r)),13);
    CHECK(fut_native_vblank_close(&clock,2000)==S_OK && f.refs==1,14);
    f.fail=0;ResetEvent(f.entered);ResetEvent(f.release);
    CHECK(fut_native_vblank_start(&clock,&f.iface)==S_OK &&
        WaitForSingleObject(f.entered,2000)==WAIT_OBJECT_0,15);
    f.changed=1;SetEvent(f.release);
    CHECK(fut_native_vblank_wait(&clock,0,2000,NULL,&r)==DXGI_ERROR_ACCESS_LOST &&
        !memcmp(&r,&saved,sizeof(r)),16);
    CHECK(fut_native_vblank_close(&clock,2000)==S_OK && f.refs==1,17);
    CloseHandle(cancel);CloseHandle(f.entered);CloseHandle(f.release);return 0;
}
int main(void)
{
    int result=test_lifecycle();if(result) return result;
    IDXGIFactory1 *factory=NULL;
    CHECK(CreateDXGIFactory1(&IID_IDXGIFactory1,(void**)&factory)==S_OK,20);
    unsigned tested=0;
    for(UINT ai=0;ai<16;++ai) {
        IDXGIAdapter1 *adapter=NULL;HRESULT hr=IDXGIFactory1_EnumAdapters1(factory,ai,&adapter);
        if(hr==DXGI_ERROR_NOT_FOUND) break;CHECK(hr==S_OK,21);
        for(UINT oi=0;oi<16;++oi) {
            IDXGIOutput *output=NULL;hr=IDXGIAdapter1_EnumOutputs(adapter,oi,&output);
            if(hr==DXGI_ERROR_NOT_FOUND) break;CHECK(hr==S_OK,22);
            DXGI_OUTPUT_DESC desc;CHECK(IDXGIOutput_GetDesc(output,&desc)==S_OK,23);
            if(desc.AttachedToDesktop) {
                FutNativeVBlank clock={0};FutNativeVBlankReceipt first,second;
                CHECK(fut_native_vblank_start(&clock,output)==S_OK,24);
                CHECK(fut_native_vblank_wait(&clock,0,2000,NULL,&first)==S_OK,25);
                CHECK(fut_native_vblank_wait(&clock,first.sequence,2000,NULL,&second)==S_OK,26);
                CHECK(second.sequence>first.sequence && second.qpc_after.QuadPart>first.qpc_after.QuadPart &&
                    first.monitor==desc.Monitor && second.monitor==desc.Monitor,27);
                CHECK(fut_native_vblank_close(&clock,2000)==S_OK && !clock.state,28);
                printf("DXGI output %ls: observed waits=%llu->%llu wakeup_delta_qpc=%lld; not guest VBlank\n",
                    desc.DeviceName,(unsigned long long)first.sequence,(unsigned long long)second.sequence,
                    (long long)(second.qpc_after.QuadPart-first.qpc_after.QuadPart));
                ++tested;
            }
            IDXGIOutput_Release(output);
        }
        IDXGIAdapter1_Release(adapter);
    }
    IDXGIFactory1_Release(factory);
    if(!tested) {puts("No desktop output; native timing NOT validated");return 77;}
    printf("Native output sources=%u; lifecycle/cancel/timeout verified; no guest IRQ generated\n",tested);
    return 0;
}
