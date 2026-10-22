#define COBJMACROS
#include "graphics_native_constants.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

HRESULT fut_native_constants_init(FutNativeConstants *native,
    ID3D11Device *device, ID3D11DeviceContext *context)
{
    memset(native, 0, sizeof(*native));
    native->context = context;
    D3D11_BUFFER_DESC desc = {0};
    desc.ByteWidth = 192 * 4 * sizeof(uint32_t);
    desc.Usage = D3D11_USAGE_DEFAULT;
    desc.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
    HRESULT hr = ID3D11Device_CreateBuffer(device, &desc, NULL, &native->buffer);
    D3D11_QUERY_DESC query = {D3D11_QUERY_EVENT, 0};
    if (SUCCEEDED(hr))
        hr = ID3D11Device_CreateQuery(device, &query, &native->completion);
    const char *high_res = getenv("RECOMP_GPU_HIGH_RES_WAIT");
    const char *profile = getenv("RECOMP_GRAPHICS_STAGE_TIMING");
    native->profile_waits = profile && !strcmp(profile, "1");
    if (SUCCEEDED(hr) && high_res && strcmp(high_res, "0")) {
        if (strcmp(high_res, "1")) hr = E_INVALIDARG;
        else {
            /* CREATE_WAITABLE_TIMER_HIGH_RESOLUTION (Win10 1803+).
             * Local timer only; does not request global timer resolution. */
            native->poll_timer = CreateWaitableTimerExW(NULL, NULL, 0x2u,
                TIMER_MODIFY_STATE | SYNCHRONIZE);
            if (!native->poll_timer) hr = HRESULT_FROM_WIN32(GetLastError());
            else fputs("[GRAPHICS] high-resolution GPU polling wait enabled; real query completion required\n", stderr);
        }
    }
    if (FAILED(hr))
        fut_native_constants_destroy(native);
    return hr;
}

int fut_native_constants_finish(FutNativeConstants *native, const uint32_t (*constants)[4],
    DWORD timeout_ms, HANDLE stop_event)
{
    if (!native || !native->buffer || !native->completion || !timeout_ms) {
        fprintf(stderr, "[GRAPHICS] constants completion unavailable: native=%p buffer=%p completion=%p timeout_ms=%lu\n",
            (void *)native, native ? (void *)native->buffer : NULL,
            native ? (void *)native->completion : NULL, (unsigned long)timeout_ms);
        return 0;
    }
    if (stop_event && WaitForSingleObject(stop_event, 0) != WAIT_TIMEOUT) {
        fputs("[GRAPHICS] constants completion interrupted before query by stop event\n", stderr);
        return 0;
    }
    if (constants) {
        ID3D11DeviceContext_UpdateSubresource(native->context,
            (ID3D11Resource *)native->buffer, 0, NULL, constants, 0, 0);
        ID3D11DeviceContext_VSSetConstantBuffers(native->context, 1, 1, &native->buffer);
    }
    ID3D11DeviceContext_End(native->context, (ID3D11Asynchronous *)native->completion);
    ID3D11DeviceContext_Flush(native->context);
    const ULONGLONG deadline = GetTickCount64() + timeout_ms;
    HRESULT last_hr = S_FALSE;
    BOOL last_complete = FALSE;
    do {
        if (stop_event && WaitForSingleObject(stop_event, 0) != WAIT_TIMEOUT) {
            fprintf(stderr, "[GRAPHICS] constants completion interrupted by stop event: last_hr=%08lX complete=%u\n",
                (unsigned long)last_hr, (unsigned)last_complete);
            return 0;
        }
        last_complete = FALSE;
        last_hr = ID3D11DeviceContext_GetData(native->context,
            (ID3D11Asynchronous *)native->completion, &last_complete, sizeof(last_complete),
            D3D11_ASYNC_GETDATA_DONOTFLUSH);
        if (last_hr == S_OK && last_complete)
            return 1;
        if (FAILED(last_hr)) {
            fprintf(stderr, "[GRAPHICS] constants completion query failed: HRESULT=%08lX\n",
                (unsigned long)last_hr);
            return 0;
        }
        LARGE_INTEGER wait_begin={0},wait_end={0},frequency={0};
        if(native->profile_waits) QueryPerformanceCounter(&wait_begin);
        if (native->poll_timer) {
            LARGE_INTEGER due; due.QuadPart = -10000; /* Relative 1ms. */
            if (!SetWaitableTimer(native->poll_timer, &due, 0, NULL, NULL, FALSE)) return 0;
            HANDLE waits[2] = {stop_event, native->poll_timer};
            const ULONGLONG now = GetTickCount64();
            const DWORD remaining = now < deadline ? (DWORD)(deadline-now) : 0;
            const DWORD result = WaitForMultipleObjects(stop_event ? 2u : 1u,
                stop_event ? waits : waits+1, FALSE, remaining);
            const DWORD timer_result = stop_event ? WAIT_OBJECT_0+1u : WAIT_OBJECT_0;
            if (result != timer_result) {
                fprintf(stderr,"[GRAPHICS] high-resolution GPU polling wait stopped: result=%lu; no completion accepted\n",result);
                return 0;
            }
        } else if (stop_event) {
            if (WaitForSingleObject(stop_event, 1) != WAIT_TIMEOUT) {
                fprintf(stderr, "[GRAPHICS] constants completion interrupted during wait: last_hr=%08lX complete=%u\n",
                    (unsigned long)last_hr, (unsigned)last_complete);
                return 0;
            }
        } else {
            Sleep(1);
        }
        if(native->profile_waits && QueryPerformanceCounter(&wait_end) &&
            QueryPerformanceFrequency(&frequency) && frequency.QuadPart>0 &&
            wait_end.QuadPart>=wait_begin.QuadPart) {
            uint64_t us=(uint64_t)(wait_end.QuadPart-wait_begin.QuadPart)*1000000u/
                (uint64_t)frequency.QuadPart;
            ++native->wait_count;native->wait_us+=us;
            if(us>native->max_wait_us) native->max_wait_us=us;
            if(native->wait_count<=8u || !(native->wait_count&(native->wait_count-1u)))
                fprintf(stderr,"[GPU-POLL-WAIT] mode=%s count=%llu mean_us=%llu max_us=%llu\n",
                    native->poll_timer?"high-resolution":"legacy",
                    (unsigned long long)native->wait_count,
                    (unsigned long long)(native->wait_us/native->wait_count),
                    (unsigned long long)native->max_wait_us);
        }
    } while (GetTickCount64() < deadline);
    fprintf(stderr, "[GRAPHICS] constants completion query timed out: timeout_ms=%lu last_hr=%08lX complete=%u\n",
        (unsigned long)timeout_ms, (unsigned long)last_hr, (unsigned)last_complete);
    return 0;
}

HRESULT fut_native_readback_texture(FutNativeConstants *native, ID3D11Device *device,
    const D3D11_TEXTURE2D_DESC *desc, ID3D11Texture2D **texture)
{
    if (!texture) return E_INVALIDARG;
    *texture = NULL;
    if (!native || !native->context || !device || !desc ||
        desc->Usage != D3D11_USAGE_STAGING || desc->BindFlags ||
        desc->CPUAccessFlags != D3D11_CPU_ACCESS_READ || desc->MiscFlags ||
        desc->MipLevels != 1 || desc->ArraySize != 1 ||
        desc->SampleDesc.Count != 1 || desc->SampleDesc.Quality) return E_INVALIDARG;
    ID3D11Device *owner = NULL;
    ID3D11DeviceContext_GetDevice(native->context, &owner);
    int same_device = owner == device;
    if (owner) ID3D11Device_Release(owner);
    if (!same_device) return E_INVALIDARG;
    const char *option = getenv("RECOMP_READBACK_RESOURCE_CACHE");
    /* Keep retention bounded to the existing image sizes/formats. Other
     * valid staging descriptions retain the uncached allocation behavior. */
    int cache = (!option || strcmp(option,"0")) && desc->Width && desc->Height &&
        desc->Width <= 1920 && desc->Height <= 1080 &&
        (desc->Format == DXGI_FORMAT_B8G8R8A8_UNORM || desc->Format == DXGI_FORMAT_R24G8_TYPELESS);
    if (cache) for (unsigned i=0; i<4; ++i) {
        if (native->readbacks[i].texture && !memcmp(&native->readbacks[i].desc, desc, sizeof(*desc))) {
            *texture = native->readbacks[i].texture;
            ID3D11Texture2D_AddRef(*texture);
            ++native->readback_hits;
            if (native->profile_waits && (native->readback_hits<=4 ||
                !(native->readback_hits & (native->readback_hits-1))))
                fprintf(stderr,"[READBACK-CACHE] hits=%llu creations=%llu; allocation reuse only, fresh copy required\n",
                    (unsigned long long)native->readback_hits, (unsigned long long)native->readback_creations);
            return S_OK;
        }
    }
    HRESULT hr = ID3D11Device_CreateTexture2D(device, desc, NULL, texture);
    if (FAILED(hr)) return hr;
    ++native->readback_creations;
    if (cache) {
        unsigned slot = native->readback_next;
        if (native->readbacks[slot].texture) ID3D11Texture2D_Release(native->readbacks[slot].texture);
        native->readbacks[slot].desc = *desc;
        native->readbacks[slot].texture = *texture;
        ID3D11Texture2D_AddRef(*texture);
        native->readback_next = (slot+1u)%4u;
    }
    return S_OK;
}

void fut_native_constants_destroy(FutNativeConstants *native)
{
    for (unsigned i=0; i<4; ++i)
        if (native->readbacks[i].texture) ID3D11Texture2D_Release(native->readbacks[i].texture);
    if (native->poll_timer) CloseHandle(native->poll_timer);
    if (native->completion) ID3D11Query_Release(native->completion);
    if (native->buffer) ID3D11Buffer_Release(native->buffer);
    memset(native, 0, sizeof(*native));
}
