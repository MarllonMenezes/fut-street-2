#define COBJMACROS
#include "graphics_native_constants.h"
#include <string.h>

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
    if (FAILED(hr))
        fut_native_constants_destroy(native);
    return hr;
}

int fut_native_constants_finish(FutNativeConstants *native, const uint32_t (*constants)[4],
    DWORD timeout_ms, HANDLE stop_event)
{
    if (!native || !native->buffer || !native->completion || !timeout_ms)
        return 0;
    if (stop_event && WaitForSingleObject(stop_event, 0) != WAIT_TIMEOUT)
        return 0;
    if (constants) {
        ID3D11DeviceContext_UpdateSubresource(native->context,
            (ID3D11Resource *)native->buffer, 0, NULL, constants, 0, 0);
        ID3D11DeviceContext_VSSetConstantBuffers(native->context, 1, 1, &native->buffer);
    }
    ID3D11DeviceContext_End(native->context, (ID3D11Asynchronous *)native->completion);
    ID3D11DeviceContext_Flush(native->context);
    const ULONGLONG deadline = GetTickCount64() + timeout_ms;
    do {
        if (stop_event && WaitForSingleObject(stop_event, 0) != WAIT_TIMEOUT)
            return 0;
        BOOL complete = FALSE;
        HRESULT hr = ID3D11DeviceContext_GetData(native->context,
            (ID3D11Asynchronous *)native->completion, &complete, sizeof(complete),
            D3D11_ASYNC_GETDATA_DONOTFLUSH);
        if (hr == S_OK && complete)
            return 1;
        if (FAILED(hr))
            return 0;
        if (stop_event) {
            if (WaitForSingleObject(stop_event, 1) != WAIT_TIMEOUT)
                return 0;
        } else {
            Sleep(1);
        }
    } while (GetTickCount64() < deadline);
    return 0;
}

void fut_native_constants_destroy(FutNativeConstants *native)
{
    if (native->completion) ID3D11Query_Release(native->completion);
    if (native->buffer) ID3D11Buffer_Release(native->buffer);
    memset(native, 0, sizeof(*native));
}
