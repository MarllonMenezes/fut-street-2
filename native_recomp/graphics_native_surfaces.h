#ifndef FUT_GRAPHICS_NATIVE_SURFACES_H
#define FUT_GRAPHICS_NATIVE_SURFACES_H
#include <d3d11.h>
#include "graphics_surfaces.h"
#include "graphics_kelvin_state.h"
#include "graphics_native_constants.h"

typedef struct {
    ID3D11Texture2D *color, *depth;
    ID3D11RenderTargetView *target;
    ID3D11DepthStencilView *depth_target;
    FutGraphicsSurfacePlan plan;
    int valid;
} FutNativeSurfaces;
/* Native backing of each stored sample, NOT a resolved 640x480 texture.
 * No draw/present/sample-position equivalence is asserted by allocation.
 * On failure the existing resources and plan remain unchanged. */
HRESULT fut_native_surfaces_prepare(FutNativeSurfaces *native, ID3D11Device *device,
    const FutGraphicsSurfacePlan *plan, FutGraphicsMemoryRead read, void *context);
void fut_native_surfaces_destroy(FutNativeSurfaces *native);
typedef int (*FutGraphicsMemoryWrite)(void *context, uint32_t address, const void *data, size_t size);
/* Full-rectangle clears with all/none color channels and integer Z24S8
 * endpoints. Query completion and exact selected-bit readback precede guest
 * writeback; padding is preserved. Partial/unsupported forms fail explicitly. */
HRESULT fut_native_surfaces_clear(FutNativeSurfaces *native, ID3D11Device *device,
    ID3D11DeviceContext *context, FutNativeConstants *completion,
    const FutGraphicsKelvinState *state, uint32_t flags, FutGraphicsMemoryRead read,
    FutGraphicsMemoryWrite write, void *memory_context, HANDLE stop_event);
#endif
