#define COBJMACROS
#include "graphics_native_constants.h"
#include "graphics_kelvin_state.h"
#include <stdio.h>
#include <string.h>

int main(void)
{
    ID3D11Device *device = NULL;
    ID3D11DeviceContext *context = NULL;
    D3D_FEATURE_LEVEL level;
    if (FAILED(D3D11CreateDevice(NULL, D3D_DRIVER_TYPE_WARP, NULL, 0, NULL, 0,
        D3D11_SDK_VERSION, &device, &level, &context))) return 1;
    FutNativeConstants native;
    if (FAILED(fut_native_constants_init(&native, device, context))) return 2;
    FutGraphicsKelvinState state = {0};
    fut_graphics_kelvin_state(&state, 0x1EA4, 60);
    const uint32_t bits[] = {0x7FC01234u, 0x80000000u, 0x3F000000u, 0xFF800000u};
    for (unsigned i = 0; i < 4; ++i) fut_graphics_kelvin_state(&state, 0xB80 + i * 4, bits[i]);
    const unsigned bases[]={0x440,0x480,0x580,0x680,0x6C0};
    const unsigned counts[]={16,64,64,16,64};
    for(unsigned family=0;family<5;++family) for(unsigned i=0;i<counts[family];++i) {
        uint32_t word=bits[(i+family)%4] ^ ((i&15)<<8);
        if(fut_graphics_kelvin_state(&state,bases[family]+i*4,word)!=FUT_KELVIN_APPLIED) return 10;
    }
    if (!fut_native_constants_finish(&native, state.constants, 2000, NULL)) return 3;
    ID3D11Buffer *bound = NULL;
    ID3D11DeviceContext_VSGetConstantBuffers(context, 1, 1, &bound);
    if (bound != native.buffer) return 9;
    ID3D11Buffer_Release(bound);
    D3D11_BUFFER_DESC desc = {0};
    ID3D11Buffer_GetDesc(native.buffer, &desc);
    desc.Usage = D3D11_USAGE_STAGING; desc.BindFlags = 0; desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
    ID3D11Buffer *staging = NULL;
    if (FAILED(ID3D11Device_CreateBuffer(device, &desc, NULL, &staging))) return 4;
    ID3D11DeviceContext_CopyResource(context, (ID3D11Resource *)staging, (ID3D11Resource *)native.buffer);
    if (!fut_native_constants_finish(&native, NULL, 2000, NULL)) return 5;
    D3D11_MAPPED_SUBRESOURCE mapped;
    if (FAILED(ID3D11DeviceContext_Map(context, (ID3D11Resource *)staging, 0, D3D11_MAP_READ, 0, &mapped))) return 6;
    int equal = memcmp(mapped.pData, state.constants, sizeof(state.constants)) == 0;
    ID3D11DeviceContext_Unmap(context, (ID3D11Resource *)staging, 0);
    if (!equal) return 7;
    HANDLE stop = CreateEventA(NULL, TRUE, TRUE, NULL);
    if (!stop || fut_native_constants_finish(&native, state.constants, 2000, stop)) return 8;
    CloseHandle(stop);
    ID3D11Buffer_Release(staging);
    fut_native_constants_destroy(&native);
    ID3D11DeviceContext_Release(context);
    ID3D11Device_Release(device);
    puts("D3D11 constant upload/completion/readback preserved all raw bits; cancellation rejected");
    return 0;
}
