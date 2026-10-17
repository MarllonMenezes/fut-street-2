#define COBJMACROS
#include "graphics_native_display.h"
#include <string.h>
#include <stdlib.h>

static LRESULT CALLBACK display_proc(HWND window,UINT message,WPARAM w,LPARAM l)
{
    if(message==WM_CLOSE) {DestroyWindow(window);return 0;}
    return DefWindowProcA(window,message,w,l);
}
static HRESULT pump(FutNativeDisplay *d)
{
    if(!d || !d->window || !d->swap || d->owner!=GetCurrentThreadId()) return E_INVALIDARG;
    MSG message;
    for(unsigned i=0;i<64 && PeekMessageA(&message,d->window,0,0,PM_REMOVE);++i) {
        if(message.message==WM_QUIT) return E_ABORT;
        TranslateMessage(&message);DispatchMessageA(&message);
    }
    return IsWindow(d->window)?S_OK:E_ABORT;
}
void fut_native_display_close(FutNativeDisplay *d)
{
    if(!d || (d->owner && d->owner!=GetCurrentThreadId())) return;
    if(d->swap) IDXGISwapChain2_Release(d->swap);
    if(d->latency) CloseHandle(d->latency);
    if(d->window && IsWindow(d->window)) DestroyWindow(d->window);
    memset(d,0,sizeof(*d));
}
HRESULT fut_native_display_open(FutNativeDisplay *d,ID3D11Device *device,
    unsigned width,unsigned height,int visible)
{
    if(!d || !device || !width || !height || width>16384 || height>16384 ||
        (visible!=0 && visible!=1)) return E_INVALIDARG;
    if(d->window || d->swap || d->latency) return E_PENDING;
    FutNativeDisplay next={0};next.owner=GetCurrentThreadId();
    HINSTANCE instance=GetModuleHandleA(NULL);
    WNDCLASSA wc={0};wc.lpfnWndProc=display_proc;wc.hInstance=instance;
    wc.lpszClassName="FutStreet2NativeDisplay";wc.hCursor=LoadCursorA(NULL,IDC_ARROW);
    if(!RegisterClassA(&wc) && GetLastError()!=ERROR_CLASS_ALREADY_EXISTS) return HRESULT_FROM_WIN32(GetLastError());
    DWORD style=WS_OVERLAPPED|WS_CAPTION|WS_SYSMENU|WS_MINIMIZEBOX;
    RECT rect={0,0,(LONG)width,(LONG)height};
    if(!AdjustWindowRect(&rect,style,FALSE)) return HRESULT_FROM_WIN32(GetLastError());
    const char *origin=getenv("RECOMP_NATIVE_DISPLAY_ORIGIN");
    next.window=CreateWindowExA(0,wc.lpszClassName,"Fut Street 2 - native display",
        style,CW_USEDEFAULT,CW_USEDEFAULT,rect.right-rect.left,rect.bottom-rect.top,
        NULL,NULL,instance,NULL);
    if(!next.window) return HRESULT_FROM_WIN32(GetLastError());
    IDXGIDevice *dxgi=NULL;IDXGIAdapter *adapter=NULL;IDXGIFactory2 *factory=NULL;
    IDXGISwapChain1 *swap=NULL;
    HRESULT hr=ID3D11Device_QueryInterface(device,&IID_IDXGIDevice,(void**)&dxgi);
    if(SUCCEEDED(hr)) hr=IDXGIDevice_GetAdapter(dxgi,&adapter);
    if(SUCCEEDED(hr)) hr=IDXGIAdapter_GetParent(adapter,&IID_IDXGIFactory2,(void**)&factory);
    DXGI_SWAP_CHAIN_DESC1 desc={0};desc.Width=width;desc.Height=height;
    desc.Format=DXGI_FORMAT_B8G8R8A8_UNORM;desc.SampleDesc.Count=1;desc.BufferCount=2;
    desc.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT;desc.SwapEffect=DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL;
    desc.AlphaMode=DXGI_ALPHA_MODE_IGNORE;desc.Flags=DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT;
    if(SUCCEEDED(hr)) hr=IDXGIFactory2_CreateSwapChainForHwnd(factory,(IUnknown*)device,
        next.window,&desc,NULL,NULL,&swap);
    if(SUCCEEDED(hr)) hr=IDXGIFactory2_MakeWindowAssociation(factory,next.window,DXGI_MWA_NO_ALT_ENTER);
    if(SUCCEEDED(hr)) hr=IDXGISwapChain1_QueryInterface(swap,&IID_IDXGISwapChain2,(void**)&next.swap);
    if(SUCCEEDED(hr)) hr=IDXGISwapChain2_SetMaximumFrameLatency(next.swap,1);
    if(SUCCEEDED(hr)) {
        next.latency=IDXGISwapChain2_GetFrameLatencyWaitableObject(next.swap);
        if(!next.latency) hr=E_FAIL;
    }
    if(swap) IDXGISwapChain1_Release(swap);
    if(factory) IDXGIFactory2_Release(factory);
    if(adapter) IDXGIAdapter_Release(adapter);
    if(dxgi) IDXGIDevice_Release(dxgi);
    if(FAILED(hr)) {fut_native_display_close(&next);return hr;}
    next.width=width;next.height=height;*d=next;
    if(visible) {
        ShowWindow(d->window,SW_SHOWNOACTIVATE);
        if(origin && origin[0]=='1' && !SetWindowPos(d->window,HWND_TOP,0,0,0,0,
                SWP_NOACTIVATE|SWP_NOSIZE|SWP_SHOWWINDOW)) {
            DWORD error=GetLastError();
            fut_native_display_close(d);
            return error?HRESULT_FROM_WIN32(error):E_FAIL;
        }
    }
    return S_OK;
}
HRESULT fut_native_display_stage(FutNativeDisplay *d,ID3D11DeviceContext *context,
    ID3D11Texture2D *source,DWORD timeout_ms,HANDLE stop)
{
    if(!d || !context || !source || !timeout_ms || timeout_ms>2000) return E_INVALIDARG;
    /* Deferred command recording is not execution of the backbuffer copy. */
    if(ID3D11DeviceContext_GetType(context)!=D3D11_DEVICE_CONTEXT_IMMEDIATE) return E_NOTIMPL;
    HRESULT hr=pump(d);if(FAILED(hr)) return hr;
    if(d->staged || d->pending) return E_PENDING;
    D3D11_TEXTURE2D_DESC desc;ID3D11Texture2D_GetDesc(source,&desc);
    if(desc.Width!=d->width || desc.Height!=d->height || desc.Format!=DXGI_FORMAT_B8G8R8A8_UNORM ||
        desc.SampleDesc.Count!=1 || desc.MipLevels!=1 || desc.ArraySize!=1) return E_NOTIMPL;
    ID3D11Device *src_device=NULL,*ctx_device=NULL,*swap_device=NULL;
    ID3D11Texture2D_GetDevice(source,&src_device);ID3D11DeviceContext_GetDevice(context,&ctx_device);
    hr=IDXGISwapChain2_GetDevice(d->swap,&IID_ID3D11Device,(void**)&swap_device);
    int same=SUCCEEDED(hr) && src_device==ctx_device && src_device==swap_device;
    if(src_device) ID3D11Device_Release(src_device);
    if(ctx_device) ID3D11Device_Release(ctx_device);
    if(swap_device) ID3D11Device_Release(swap_device);
    if(!same) return E_INVALIDARG;
    ULONGLONG deadline=GetTickCount64()+timeout_ms;
    for(;;) {
        if(stop && WaitForSingleObject(stop,0)!=WAIT_TIMEOUT) return E_ABORT;
        hr=pump(d);if(FAILED(hr)) return hr;
        if(d->latency_ready) break;
        DWORD wait=WaitForSingleObject(d->latency,0);
        if(wait==WAIT_OBJECT_0) {d->latency_ready=1;break;}
        if(wait!=WAIT_TIMEOUT) return E_FAIL;
        if(GetTickCount64()>=deadline) return HRESULT_FROM_WIN32(WAIT_TIMEOUT);
        Sleep(1);
    }
    ID3D11Texture2D *back=NULL;
    hr=IDXGISwapChain2_GetBuffer(d->swap,0,&IID_ID3D11Texture2D,(void**)&back);
    if(FAILED(hr)) return hr;
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)back,(ID3D11Resource*)source);
    ID3D11Texture2D_Release(back);d->staged=1;d->latency_ready=0;return S_OK;
}
HRESULT fut_native_display_submit(FutNativeDisplay *d,unsigned interval)
{
    HRESULT hr=pump(d);if(FAILED(hr)) return hr;
    if(interval<1 || interval>4) return E_INVALIDARG;
    if(d->pending) return E_PENDING;
    if(!d->staged) return E_INVALIDARG;
    hr=IDXGISwapChain2_Present(d->swap,interval,DXGI_PRESENT_DO_NOT_WAIT);
    if(hr!=S_OK) return hr;
    d->staged=0;d->pending=1;d->count_valid=0;
    /* Keep pending even if counting fails: a submitted frame must not replay. */
    hr=IDXGISwapChain2_GetLastPresentCount(d->swap,&d->target_present);
    if(hr==S_OK) d->count_valid=1;
    return hr;
}
HRESULT fut_native_display_poll(FutNativeDisplay *d,FutNativeDisplayReceipt *receipt)
{
    if(!receipt) return E_INVALIDARG;
    HRESULT hr=pump(d);if(FAILED(hr)) return hr;
    if(!d->pending) return E_INVALIDARG;
    DXGI_FRAME_STATISTICS stats={0};hr=IDXGISwapChain2_GetFrameStatistics(d->swap,&stats);
    if(hr!=S_OK && hr!=DXGI_ERROR_FRAME_STATISTICS_DISJOINT) return hr;
    FutNativeDisplayReceipt result={0};
    if(hr==S_OK && d->count_valid && stats.PresentCount==d->target_present && stats.SyncQPCTime.QuadPart) {
        result.present_count=stats.PresentCount;result.present_refresh=stats.PresentRefreshCount;
        result.sync_refresh=stats.SyncRefreshCount;result.sync_qpc=stats.SyncQPCTime;
    } else {
        /* DXGI documents this event as finishing presentation of the previous
         * frame. Stage consumed initial readiness; only one present is pending.
         * Stale statistics are not replaced with invented refresh counters. */
        UINT submitted=0;
        hr=IDXGISwapChain2_GetLastPresentCount(d->swap,&submitted);
        if(hr!=S_OK) return hr;
        if(!d->count_valid || submitted!=d->target_present) return S_FALSE;
        if(!d->latency_ready) {
            DWORD wait=WaitForSingleObject(d->latency,0);
            if(wait==WAIT_TIMEOUT) return S_FALSE;
            if(wait!=WAIT_OBJECT_0) return E_FAIL;
            d->latency_ready=1; /* Transfer the consumed readiness to next stage. */
        }
        result.present_count=submitted;result.source=1;
    }
    if(!QueryPerformanceCounter(&result.observed_qpc) || result.observed_qpc.QuadPart<=0) return E_FAIL;
    *receipt=result;d->pending=0;return S_OK;
}
