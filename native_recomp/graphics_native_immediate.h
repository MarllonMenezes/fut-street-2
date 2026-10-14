#ifndef FUT_GRAPHICS_NATIVE_IMMEDIATE_H
#define FUT_GRAPHICS_NATIVE_IMMEDIATE_H
#include <d3d11.h>
#include <d3dcompiler.h>
#include "graphics_immediate.h"
typedef struct {ID3D11Buffer *buffer;ID3D11InputLayout *layout;unsigned count;} FutNativeImmediate;
/* Upload/bind only: no Draw/consumption. Failure preserves old resources.
 * Used attributes must be completely defined in every vertex. */
HRESULT fut_native_immediate_upload(FutNativeImmediate *native,ID3D11Device *device,
    ID3D11DeviceContext *context,ID3DBlob *vertex_bytecode,const FutGraphicsImmediate *batch,uint16_t required);
void fut_native_immediate_destroy(FutNativeImmediate *native);
#endif
