#ifndef FUT_GRAPHICS_NATIVE_IMMEDIATE_H
#define FUT_GRAPHICS_NATIVE_IMMEDIATE_H
#include <d3d11.h>
#include <d3dcompiler.h>
#include "graphics_immediate.h"
#include "graphics_native_constants.h"
typedef struct {
    ID3D11Buffer *buffer;ID3D11InputLayout *layout;unsigned count,submitted,primitive;
    uint64_t ia_vertices,ps_invocations; /* Last completed raster query, not NV2A fidelity. */
} FutNativeImmediate;
/* Upload/bind only: no Draw/consumption. Failure preserves old resources.
 * Used attributes must be completely defined in every vertex. */
HRESULT fut_native_immediate_upload(FutNativeImmediate *native,ID3D11Device *device,
    ID3D11DeviceContext *context,ID3DBlob *vertex_bytecode,const FutGraphicsImmediate *batch,uint16_t required);
void fut_native_immediate_destroy(FutNativeImmediate *native);
typedef struct {float clip[4],screen[4],texture[4][4];} FutNativePosition;
/* Stage mask for the conservative constant-RGB specialization domain:
 * screen/clip W=1, bounded finite positions and finite UV with q=1.
 * Not a generic projective/interpolation or sampler fidelity certificate. */
unsigned fut_native_immediate_constant_coordinates(const FutNativePosition *positions,unsigned count);
/* Separate finite signed-W proof; used UVs remain bounded with unit q.
 * Positive-W vertices outside XY clip planes admit screen XY up to2^21;
 * ordinary/affine screen bounds and homogeneous XY bounds remain unchanged. */
unsigned fut_native_immediate_projective_coordinates(const FutNativePosition *positions,unsigned count);
/* Exact observed fullscreen triangle and texel-space mapping only. A scope
 * certificate for the authorized resolve experiment, NOT NV2A fidelity. */
int fut_native_immediate_quincunx_coordinates(const FutNativePosition *positions,unsigned count);
/* Execute VS only with SO_NO_RASTERIZED_STREAM, query, copy and validate.
 * Finite signed screen W with magnitude [2^-64,2^64]; preserve its sign for
 * homogeneous clipping. No pixels, batch consumption,
 * guest marker or original submission completion. Output unchanged on failure.
 * The renderer owns the context: preexisting GS/SO bindings are refused. */
HRESULT fut_native_immediate_probe_positions(const FutNativeImmediate *native,
    ID3D11Device *device,ID3D11DeviceContext *context,ID3D11VertexShader *vertex_shader,ID3DBlob *vertex_bytecode,
    FutNativeConstants *completion,FutNativePosition *positions,unsigned capacity,
    DWORD timeout_ms,HANDLE stop_event);
#endif
