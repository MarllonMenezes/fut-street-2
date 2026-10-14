#ifndef FUT_GRAPHICS_NATIVE_DRAW_H
#define FUT_GRAPHICS_NATIVE_DRAW_H
#include "graphics_native_immediate.h"
#include "graphics_native_surfaces.h"
#include "graphics_native_pipeline.h"
#include "graphics_software_state.h"
/* Called only after translated shaders, textures and vertex-only validation.
 * Rechecks pipeline/software gates; single-sample triangles only. Success
 * means GPU completion AND attachment writeback, then batch consumption.
 * No GET/semaphore/present. Failure after submission latches native->submitted:
 * fatal to this batch, never safe to upload/retry Draw over pending work. */
HRESULT fut_native_draw_immediate(FutNativeImmediate *native,FutGraphicsImmediate *batch,
    FutNativeSurfaces *surfaces,FutNativePipeline *pipeline,
    const FutGraphicsKelvinState *state,const FutGraphicsSoftwareState *software,
    ID3D11Device *device,ID3D11DeviceContext *context,
    ID3D11VertexShader *vertex,ID3D11PixelShader *pixel,FutNativeConstants *completion,
    FutGraphicsMemoryRead read,FutGraphicsMemoryWrite write,void *memory,HANDLE stop);
#endif
