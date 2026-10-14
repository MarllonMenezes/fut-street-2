#define COBJMACROS
#include "graphics_native_draw.h"

HRESULT fut_native_draw_immediate(FutNativeImmediate *native,FutGraphicsImmediate *batch,
    FutNativeSurfaces *surfaces,FutNativePipeline *pipeline,
    const FutGraphicsKelvinState *state,const FutGraphicsSoftwareState *software,
    ID3D11Device *device,ID3D11DeviceContext *context,
    ID3D11VertexShader *vertex,ID3D11PixelShader *pixel,FutNativeConstants *completion,
    FutGraphicsMemoryRead read,FutGraphicsMemoryWrite write,void *memory,HANDLE stop)
{
    if(!native || !batch || !surfaces || !pipeline || !state || !software || !device ||
        !context || !vertex || !pixel || !completion || !completion->buffer ||
        !completion->completion || completion->context!=context || !read || !write ||
        !native->buffer || !native->layout || !surfaces->valid || !surfaces->target ||
        !surfaces->depth_target || batch->active || !batch->ready || batch->pending_x ||
        batch->primitive!=5 || !batch->count || batch->count>FUT_IMMEDIATE_CAPACITY ||
        batch->count%3 || native->count!=batch->count) return E_INVALIDARG;
    if(native->submitted || surfaces->gpu_dirty) return E_PENDING;
    if(stop && WaitForSingleObject(stop,0)!=WAIT_TIMEOUT) return E_ABORT;
    /* Expanded storage used for MSAA clears is NOT multisample rasterization. */
    if(surfaces->plan.samples!=1 || (state->program_mode&3u)!=2u ||
        !fut_graphics_software_linear_draw_ready(software,state,&surfaces->plan)) return E_NOTIMPL;
    ID3D11GeometryShader *gs=NULL;
    ID3D11DeviceContext_GSGetShader(context,&gs,NULL,NULL);
    if(gs) {ID3D11GeometryShader_Release(gs);return E_NOTIMPL;}
    ID3D11Buffer *targets[D3D11_SO_BUFFER_SLOT_COUNT]={0};int occupied=0;
    ID3D11DeviceContext_SOGetTargets(context,D3D11_SO_BUFFER_SLOT_COUNT,targets);
    for(unsigned i=0;i<D3D11_SO_BUFFER_SLOT_COUNT;++i) if(targets[i]) {
        occupied=1;ID3D11Buffer_Release(targets[i]);
    }
    if(occupied) return E_NOTIMPL;
    HRESULT hr=fut_native_pipeline_apply(pipeline,device,context,state,&surfaces->plan);
    if(FAILED(hr)) return hr;
    UINT stride=16*4*sizeof(float),offset=0;
    ID3D11DeviceContext_IASetInputLayout(context,native->layout);
    ID3D11DeviceContext_IASetVertexBuffers(context,0,1,&native->buffer,&stride,&offset);
    ID3D11DeviceContext_IASetPrimitiveTopology(context,D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    ID3D11DeviceContext_VSSetShader(context,vertex,NULL,0);
    ID3D11DeviceContext_PSSetShader(context,pixel,NULL,0);
    ID3D11DeviceContext_OMSetRenderTargets(context,1,&surfaces->target,surfaces->depth_target);
    if(stop && WaitForSingleObject(stop,0)!=WAIT_TIMEOUT) return E_ABORT;
    /* Conservative dirty marking preserves even unchanged channels and Z/S.
     * Mark before Draw: any failure keeps resources/batch pending, not replayed. */
    fut_native_surfaces_mark_dirty(surfaces,3);
    native->submitted=1;
    ID3D11DeviceContext_Draw(context,native->count,0);
    if(!fut_native_constants_finish(completion,NULL,2000,stop)) return E_FAIL;
    hr=fut_native_surfaces_writeback(surfaces,device,context,completion,read,write,memory,stop);
    if(FAILED(hr)) return hr;
    if(!fut_graphics_immediate_consume(batch)) return E_FAIL;
    native->submitted=0;native->count=0;
    return S_OK;
}
