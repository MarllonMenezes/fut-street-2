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

void fut_native_constants_destroy(FutNativeConstants *native)
{
    if (native->poll_timer) CloseHandle(native->poll_timer);
    if (native->completion) ID3D11Query_Release(native->completion);
    if (native->buffer) ID3D11Buffer_Release(native->buffer);
    memset(native, 0, sizeof(*native));
}
