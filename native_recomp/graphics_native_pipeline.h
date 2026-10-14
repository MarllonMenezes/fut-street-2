#ifndef FUT_GRAPHICS_NATIVE_PIPELINE_H
#define FUT_GRAPHICS_NATIVE_PIPELINE_H
#include <d3d11.h>
#include "graphics_kelvin_state.h"
#include "graphics_surfaces.h"
typedef struct {
    ID3D11BlendState *blend;
    ID3D11DepthStencilState *depth;
    ID3D11RasterizerState *raster;
    ID3D11Buffer *windows;
} FutNativePipeline;
/* Fragment programs must include this predicate before any color output.
 * PS b2 stores inclusive windows plus physical-to-logical storage factors. */
const char *fut_native_window_hlsl(void);
HRESULT fut_native_pipeline_apply(FutNativePipeline *native, ID3D11Device *device,
    ID3D11DeviceContext *context, const FutGraphicsKelvinState *state, const FutGraphicsSurfacePlan *surface);
void fut_native_pipeline_destroy(FutNativePipeline *native);
#endif
