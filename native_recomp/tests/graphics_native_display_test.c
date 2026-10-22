#define COBJMACROS
#include "graphics_native_display.h"
#include "graphics_native_vblank.h"
#include "graphics_native_constants.h"
#include "native_input.h"
#include <stdio.h>
#include <string.h>
#define CHECK(x,n) do {if(!(x)){fprintf(stderr,"native display test failed: %d\n",n);return n;}}while(0)
static int unavailable_environment(HRESULT hr)
{
    return hr==DXGI_ERROR_UNSUPPORTED || hr==DXGI_ERROR_NOT_CURRENTLY_AVAILABLE ||
        hr==DXGI_ERROR_SESSION_DISCONNECTED || hr==DXGI_STATUS_OCCLUDED;
}
typedef struct {FutNativeDisplay *display;HRESULT result;} ForeignThread;
static int valid_receipt(const FutNativeDisplayReceipt *r)
{
    if(r->observed_qpc.QuadPart<=0) return 0;
    if(r->source==0) return r->sync_qpc.QuadPart>0;
    return r->source==1 && !r->sync_qpc.QuadPart && !r->present_refresh && !r->sync_refresh;
}
static DWORD WINAPI foreign_thread(void *opaque)
{
    ForeignThread *f=opaque;FutNativeDisplayReceipt receipt;
    f->result=fut_native_display_poll(f->display,&receipt);
    fut_native_display_close(f->display); /* Wrong thread must not destroy it. */
    return 0;
}
static HRESULT submit_and_receive(FutNativeDisplay *d,FutNativeDisplayReceipt *receipt)
{
    ULONGLONG deadline=GetTickCount64()+2000;HRESULT hr;
    do {hr=fut_native_display_submit(d,1);if(hr==DXGI_ERROR_WAS_STILL_DRAWING) Sleep(1);}
        while(hr==DXGI_ERROR_WAS_STILL_DRAWING && GetTickCount64()<deadline);
    if(hr!=S_OK) return hr;
    deadline=GetTickCount64()+2000;
    do {hr=fut_native_display_poll(d,receipt);if(hr==S_FALSE) Sleep(1);}
        while(hr==S_FALSE && GetTickCount64()<deadline);
    return hr;
}
static int verify_backbuffer(FutNativeDisplay *d,ID3D11Device *device,
    ID3D11DeviceContext *context,FutNativeConstants *completion,const uint32_t *expected)
{
    ID3D11Texture2D *back=NULL,*staging=NULL;
    CHECK(SUCCEEDED(IDXGISwapChain2_GetBuffer(d->swap,0,&IID_ID3D11Texture2D,(void**)&back)),30);
    D3D11_TEXTURE2D_DESC desc;ID3D11Texture2D_GetDesc(back,&desc);
    desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;desc.MiscFlags=0;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&desc,NULL,&staging)),31);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)staging,(ID3D11Resource*)back);
    CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),32);
    D3D11_MAPPED_SUBRESOURCE mapped;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)staging,0,D3D11_MAP_READ,0,&mapped)),33);
    int good=1;
    for(unsigned y=0;y<64;++y) if(memcmp((uint8_t*)mapped.pData+y*mapped.RowPitch,expected+y*128,128*4)) good=0;
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)staging,0);
    ID3D11Texture2D_Release(staging);ID3D11Texture2D_Release(back);
    return good?0:34;
}
int main(int argc,char **argv)
{
    ID3D11Device *device=NULL;ID3D11DeviceContext *context=NULL;D3D_FEATURE_LEVEL level;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_HARDWARE,NULL,0,NULL,0,
        D3D11_SDK_VERSION,&device,&level,&context)),1);
    FutNativeConstants completion;
    CHECK(SUCCEEDED(fut_native_constants_init(&completion,device,context)),2);
    FutNativeDisplay d={0};
    CHECK(fut_native_display_open(&d,device,0,64,1)==E_INVALIDARG && !d.window,3);
    HRESULT hr=fut_native_display_open(&d,device,128,64,1);
    if(unavailable_environment(hr)) {fprintf(stderr,"Display unavailable HRESULT=%08lX; no presentation validation\n",hr);
        fut_native_constants_destroy(&completion);ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);return 77;}
    CHECK(hr==S_OK,35);
    if(argc==2 && !strcmp(argv[1],"--native-input")){
        FutNativeInputSample sample;
        CHECK(fut_native_input_enabled(),60);
        SendMessageA(d.window,WM_KEYDOWN,VK_RETURN,0);
        CHECK(fut_native_input_sample(0,&sample)==ERROR_SUCCESS && sample.state[4]==0x10,61);
        SendMessageA(d.window,WM_KEYUP,VK_RETURN,0);
        CHECK(fut_native_input_sample(0,&sample)==ERROR_SUCCESS && !sample.state[4],62);
        SendMessageA(d.window,WM_KEYDOWN,VK_RETURN,0);
        SendMessageA(d.window,WM_KILLFOCUS,0,0);
        CHECK(fut_native_input_sample(0,&sample)==ERROR_SUCCESS && !sample.state[4],63);
        fut_native_display_close(&d);fut_native_constants_destroy(&completion);
        ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);return 0;
    }
    /* First presentation on a fresh immediate context must work without a
     * diagnostic Map/GetData forcing submission of the backbuffer copy. */
    uint32_t first_pixels[128*64];
    for(unsigned i=0;i<128*64;++i) first_pixels[i]=0xFF000000u;
    D3D11_TEXTURE2D_DESC first_desc={0};
    first_desc.Width=128;first_desc.Height=64;
    first_desc.MipLevels=first_desc.ArraySize=first_desc.SampleDesc.Count=1;
    first_desc.Format=DXGI_FORMAT_B8G8R8A8_UNORM;
    first_desc.Usage=D3D11_USAGE_IMMUTABLE;first_desc.BindFlags=D3D11_BIND_SHADER_RESOURCE;
    D3D11_SUBRESOURCE_DATA first_data={first_pixels,128*4,0};
    ID3D11Texture2D *first_texture=NULL;
    CHECK(ID3D11Device_CreateTexture2D(device,&first_desc,&first_data,&first_texture)==S_OK,52);
    CHECK(fut_native_display_stage(&d,context,first_texture,2000,NULL)==S_OK,53);
    FutNativeDisplayReceipt first_receipt={0};
    CHECK(submit_and_receive(&d,&first_receipt)==S_OK && first_receipt.present_count==1 &&
        valid_receipt(&first_receipt) && !d.pending,54);
    ID3D11Texture2D_Release(first_texture);
    IDXGIOutput *timing_output=NULL;FutNativeVBlank clock={0};FutNativeVBlankReceipt edge;
    CHECK(IDXGISwapChain2_GetContainingOutput(d.swap,&timing_output)==S_OK,49);
    CHECK(fut_native_vblank_start(&clock,timing_output)==S_OK &&
        fut_native_vblank_wait(&clock,0,2000,NULL,&edge)==S_OK &&
        edge.monitor==MonitorFromWindow(d.window,MONITOR_DEFAULTTONULL),50);
    CHECK(fut_native_vblank_close(&clock,2000)==S_OK,51);
    IDXGIOutput_Release(timing_output);
    puts("Native output wait confirmed on presenter monitor before first Present; no guest VBlank");
    HWND original=d.window;
    CHECK(fut_native_display_open(&d,device,128,64,1)==E_PENDING && d.window==original,4);
    uint32_t pixels[128*64];
    for(unsigned y=0;y<64;++y) for(unsigned x=0;x<128;++x)
        pixels[y*128+x]=0xFF000000u|(x*2u<<16)|(y*4u<<8)|(x^y);
    D3D11_TEXTURE2D_DESC desc={0};desc.Width=128;desc.Height=64;
    desc.MipLevels=desc.ArraySize=desc.SampleDesc.Count=1;desc.Format=DXGI_FORMAT_B8G8R8A8_UNORM;
    desc.Usage=D3D11_USAGE_IMMUTABLE;desc.BindFlags=D3D11_BIND_SHADER_RESOURCE;
    D3D11_SUBRESOURCE_DATA data={pixels,128*4,0};ID3D11Texture2D *source=NULL,*wrong=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&desc,&data,&source)),5);
    desc.Width=64;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&desc,&data,&wrong)),6);
    CHECK(fut_native_display_stage(&d,context,wrong,2000,NULL)==E_NOTIMPL && !d.staged,7);
    ID3D11DeviceContext *deferred=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateDeferredContext(device,0,&deferred)),44);
    CHECK(fut_native_display_stage(&d,deferred,source,2000,NULL)==E_NOTIMPL && !d.staged,45);
    ID3D11DeviceContext_Release(deferred);
    ID3D11Device *other_device=NULL;ID3D11DeviceContext *other_context=NULL;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,
        D3D11_SDK_VERSION,&other_device,&level,&other_context)),46);
    ID3D11Texture2D *other_source=NULL;desc.Width=128;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(other_device,&desc,&data,&other_source)),47);
    CHECK(fut_native_display_stage(&d,other_context,other_source,2000,NULL)==E_INVALIDARG && !d.staged,48);
    ID3D11Texture2D_Release(other_source);ID3D11DeviceContext_Release(other_context);ID3D11Device_Release(other_device);
    HANDLE stop=CreateEventA(NULL,TRUE,TRUE,NULL);CHECK(stop,8);
    CHECK(fut_native_display_stage(&d,context,source,2000,stop)==E_ABORT && !d.staged,9);
    ResetEvent(stop);
    CHECK(SUCCEEDED(fut_native_display_stage(&d,context,source,2000,stop)) && d.staged && !d.pending,10);
    CHECK(verify_backbuffer(&d,device,context,&completion,pixels)==0,11);
    CHECK(fut_native_display_stage(&d,context,source,2000,stop)==E_PENDING,12);
    CHECK(fut_native_display_submit(&d,0)==E_INVALIDARG && d.staged && !d.pending,13);
    FutNativeDisplayReceipt receipt,saved;memset(&receipt,0xA5,sizeof(receipt));memcpy(&saved,&receipt,sizeof(saved));
    CHECK(fut_native_display_poll(&d,&receipt)==E_INVALIDARG && !memcmp(&receipt,&saved,sizeof(saved)),14);
    ForeignThread foreign={&d,S_OK};HANDLE thread=CreateThread(NULL,0,foreign_thread,&foreign,0,NULL);
    CHECK(thread && WaitForSingleObject(thread,2000)==WAIT_OBJECT_0 &&
        foreign.result==E_INVALIDARG && d.window==original && IsWindow(original),36);
    CloseHandle(thread);
    ULONGLONG deadline=GetTickCount64()+2000;
    do {hr=fut_native_display_submit(&d,1);if(hr==DXGI_ERROR_WAS_STILL_DRAWING) Sleep(1);}
        while(hr==DXGI_ERROR_WAS_STILL_DRAWING && GetTickCount64()<deadline);
    int unavailable=unavailable_environment(hr);
    if(!unavailable) {
        CHECK(hr==S_OK && d.pending && !d.staged && d.count_valid,15);
        CHECK(fut_native_display_submit(&d,1)==E_PENDING &&
            fut_native_display_stage(&d,context,source,2000,stop)==E_PENDING,16);
        deadline=GetTickCount64()+2000;
        do {
            hr=fut_native_display_poll(&d,&receipt);
            if(hr!=S_OK) CHECK(!memcmp(&receipt,&saved,sizeof(saved)) && d.pending,17);
            if(hr==S_FALSE) Sleep(1);
        } while(hr==S_FALSE && GetTickCount64()<deadline);
        if(unavailable_environment(hr)) {fprintf(stderr,"DXGI receipt unavailable HRESULT=%08lX; pending=%u, no receipt fabricated\n",hr,d.pending);unavailable=1;}
        else {
            CHECK(hr==S_OK,37); /* A timeout or unexpected API error is NOT a skip. */
            CHECK(!d.pending && receipt.present_count==d.target_present && valid_receipt(&receipt),18);
            printf("DXGI synthetic receipt: present=%u present_refresh=%u sync_refresh=%u qpc=%lld\n",
                receipt.present_count,receipt.present_refresh,receipt.sync_refresh,(long long)receipt.sync_qpc.QuadPart);
        }
    }
    if(!unavailable) {
        UINT first_present=receipt.present_count;LARGE_INTEGER first_qpc=receipt.observed_qpc;
        memcpy(&saved,&receipt,sizeof(saved));d.pending=1;d.target_present=first_present+1;
        CHECK(fut_native_display_poll(&d,&receipt)==S_FALSE && d.pending &&
            !memcmp(&receipt,&saved,sizeof(saved)),38); /* Old receipt is not a new frame. */
        d.target_present=first_present;d.count_valid=0;
        CHECK(fut_native_display_poll(&d,&receipt)==S_FALSE && d.pending &&
            !memcmp(&receipt,&saved,sizeof(saved)),55); /* A signal without an ID is insufficient. */
        d.count_valid=1;
        d.pending=0;d.target_present=first_present;
        for(unsigned i=0;i<128*64;++i) pixels[i]^=0x00FFFFFF;
        ID3D11Texture2D *second=NULL;desc.Width=128;
        CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(device,&desc,&data,&second)),39);
        CHECK(SUCCEEDED(fut_native_display_stage(&d,context,second,2000,stop)) &&
            verify_backbuffer(&d,device,context,&completion,pixels)==0,40);
        hr=submit_and_receive(&d,&receipt);
        if(unavailable_environment(hr)) unavailable=1;
        else {
            CHECK(hr==S_OK && receipt.present_count==first_present+1 &&
                valid_receipt(&receipt) && receipt.observed_qpc.QuadPart>=first_qpc.QuadPart && !d.pending,41);
            printf("DXGI second synthetic receipt: present=%u present_refresh=%u sync_refresh=%u qpc=%lld\n",
                receipt.present_count,receipt.present_refresh,receipt.sync_refresh,(long long)receipt.sync_qpc.QuadPart);
        }
        ID3D11Texture2D_Release(second);
        if(!unavailable) {
            CHECK(SUCCEEDED(fut_native_display_stage(&d,context,source,2000,stop)),42);
            deadline=GetTickCount64()+2000;
            do {hr=fut_native_display_submit(&d,1);if(hr==DXGI_ERROR_WAS_STILL_DRAWING) Sleep(1);}
                while(hr==DXGI_ERROR_WAS_STILL_DRAWING && GetTickCount64()<deadline);
            if(unavailable_environment(hr)) unavailable=1;
            else CHECK(hr==S_OK && d.pending,43);
        }
    }
    if(unavailable) fprintf(stderr,"Host-only pixel copy verified; presentation receipt NOT validated\n");
    SendMessageA(d.window,WM_CLOSE,0,0);
    memcpy(&saved,&receipt,sizeof(saved));unsigned pending_before_close=d.pending;
    CHECK(fut_native_display_poll(&d,&receipt)==E_ABORT && d.pending==pending_before_close &&
        !memcmp(&receipt,&saved,sizeof(saved)),19);
    fut_native_display_close(&d);CHECK(!d.window && !d.swap && !d.latency,20);
    CloseHandle(stop);ID3D11Texture2D_Release(wrong);ID3D11Texture2D_Release(source);
    fut_native_constants_destroy(&completion);ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    if(unavailable) return 77;
    puts("Native DXGI window: synthetic BGRA8 pixels, one-frame ownership and matching presentation receipt; not a game frame or guest VBlank");
    return 0;
}
