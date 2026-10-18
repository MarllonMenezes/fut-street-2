#define COBJMACROS
#include "graphics_native_draw.h"
#include "graphics_sample_pattern.h"
#include "graphics_experiment.h"

static HRESULT draw_sample_passes(ID3D11Device *device,ID3D11DeviceContext *context,
    const FutGraphicsSurfacePlan *surface,unsigned vertices,HANDLE stop)
{
    ID3D11Buffer *buffers[2]={0};HRESULT hr=S_OK;
    for(unsigned slot=0;slot<2 && SUCCEEDED(hr);++slot) {
        FutGraphicsSamplePass pass;
        if(!fut_graphics_aa2_coverage_pass(surface->logical_width,surface->logical_height,slot,&pass)) {hr=E_INVALIDARG;break;}
        D3D11_BUFFER_DESC desc={0};desc.ByteWidth=sizeof(pass);
        desc.Usage=D3D11_USAGE_IMMUTABLE;desc.BindFlags=D3D11_BIND_CONSTANT_BUFFER;
        D3D11_SUBRESOURCE_DATA data={&pass,0,0};
        hr=ID3D11Device_CreateBuffer(device,&desc,&data,&buffers[slot]);
    }
    for(unsigned slot=0;slot<2 && SUCCEEDED(hr);++slot) {
        if(stop && WaitForSingleObject(stop,0)!=WAIT_TIMEOUT) {hr=E_ABORT;break;}
        ID3D11DeviceContext_VSSetConstantBuffers(context,4,1,&buffers[slot]);
        ID3D11DeviceContext_PSSetConstantBuffers(context,4,1,&buffers[slot]);
        ID3D11DeviceContext_Draw(context,vertices,0);
    }
    ID3D11Buffer *empty=NULL;
    ID3D11DeviceContext_VSSetConstantBuffers(context,4,1,&empty);
    ID3D11DeviceContext_PSSetConstantBuffers(context,4,1,&empty);
    for(unsigned slot=0;slot<2;++slot) if(buffers[slot]) ID3D11Buffer_Release(buffers[slot]);
    return hr;
}

static HRESULT draw_immediate(FutNativeImmediate *native,FutGraphicsImmediate *batch,
    FutNativeSurfaces *surfaces,FutNativePipeline *pipeline,
    const FutGraphicsKelvinState *state,const FutGraphicsSoftwareState *software,
    ID3D11Device *device,ID3D11DeviceContext *context,
    ID3D11VertexShader *vertex,ID3D11PixelShader *pixel,FutNativeConstants *completion,
    FutGraphicsMemoryRead read,FutGraphicsMemoryWrite write,void *memory,HANDLE stop,int aa2,int line_list)
{
    if(!native || !batch || !surfaces || !pipeline || !state || !software || !device ||
        !context || !vertex || !pixel || !completion || !completion->buffer ||
        !completion->completion || completion->context!=context || !read || !write ||
        !native->buffer || !native->layout || !surfaces->valid || !surfaces->target ||
        !surfaces->depth_target || batch->active || !batch->ready || batch->pending_x ||
        native->count!=batch->count || native->primitive!=batch->primitive) return E_INVALIDARG;
    if(line_list) {
        if(!aa2 || !fut_graphics_lines_16segment_aa2_approximation_enabled() ||
            batch->primitive!=2 || batch->count!=32 || batch->input_kind!=4) return E_INVALIDARG;
    } else if(batch->primitive!=5 || !batch->count ||
        batch->count>FUT_IMMEDIATE_DRAW_CAPACITY || batch->count%3) return E_INVALIDARG;
    if(native->submitted || surfaces->gpu_dirty) return E_PENDING;
    if(stop && WaitForSingleObject(stop,0)!=WAIT_TIMEOUT) return E_ABORT;
    if((state->program_mode&3u)!=2u || (aa2?
        !fut_graphics_software_aa2_draw_ready(software,state,&surfaces->plan):
        !fut_graphics_software_linear_draw_ready(software,state,&surfaces->plan))) return E_NOTIMPL;
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
    ID3D11DeviceContext_IASetPrimitiveTopology(context,line_list?
        D3D11_PRIMITIVE_TOPOLOGY_LINELIST:D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    ID3D11DeviceContext_VSSetShader(context,vertex,NULL,0);
    ID3D11DeviceContext_PSSetShader(context,pixel,NULL,0);
    ID3D11DeviceContext_OMSetRenderTargets(context,1,&surfaces->target,surfaces->depth_target);
    if(stop && WaitForSingleObject(stop,0)!=WAIT_TIMEOUT) return E_ABORT;
    D3D11_QUERY_DESC statistics_desc={D3D11_QUERY_PIPELINE_STATISTICS,0};
    ID3D11Query *statistics=NULL;
    hr=ID3D11Device_CreateQuery(device,&statistics_desc,&statistics);
    if(FAILED(hr)) return hr;
    /* Untouched attachments may alias CPU-owned guest memory (the resolve
     * uses a dummy depth address). Publishing their upload snapshot would
     * roll back CPU writes made since prepare, including heap metadata.
     * Mark before Draw so failures still latch every possible GPU write. */
    const FutGraphicsDrawState *draw=&state->draw;
    unsigned dirty=(draw->color_mask&0x01010101u)?1u:0u;
    int stencil_writes=state->stencil_test && (draw->stencil_write&255u) &&
        (draw->stencil_op[0]!=0x1E00 || draw->stencil_op[1]!=0x1E00 || draw->stencil_op[2]!=0x1E00);
    if((state->depth_test && draw->depth_write) || stencil_writes) dirty|=2u;
    fut_native_surfaces_mark_dirty(surfaces,dirty);
    native->submitted=1;
    ID3D11DeviceContext_Begin(context,(ID3D11Asynchronous*)statistics);
    if(aa2) hr=draw_sample_passes(device,context,&surfaces->plan,native->count,stop);
    else ID3D11DeviceContext_Draw(context,native->count,0);
    ID3D11DeviceContext_End(context,(ID3D11Asynchronous*)statistics);
    if(FAILED(hr)) {ID3D11Query_Release(statistics);return hr;}
    if(!fut_native_constants_finish(completion,NULL,2000,stop)) {
        ID3D11Query_Release(statistics);return E_FAIL;
    }
    D3D11_QUERY_DATA_PIPELINE_STATISTICS measured={0};
    hr=ID3D11DeviceContext_GetData(context,(ID3D11Asynchronous*)statistics,&measured,
        sizeof(measured),D3D11_ASYNC_GETDATA_DONOTFLUSH);
    ID3D11Query_Release(statistics);
    if(hr!=S_OK) return FAILED(hr)?hr:E_FAIL;
    native->ia_vertices=measured.IAVertices;native->ps_invocations=measured.PSInvocations;
    hr=fut_native_surfaces_writeback(surfaces,device,context,completion,read,write,memory,stop);
    if(FAILED(hr)) return hr;
    if(!fut_graphics_immediate_consume(batch)) return E_FAIL;
    native->submitted=0;native->count=0;
    return S_OK;
}

HRESULT fut_native_draw_immediate(FutNativeImmediate *native,FutGraphicsImmediate *batch,
    FutNativeSurfaces *surfaces,FutNativePipeline *pipeline,
    const FutGraphicsKelvinState *state,const FutGraphicsSoftwareState *software,
    ID3D11Device *device,ID3D11DeviceContext *context,
    ID3D11VertexShader *vertex,ID3D11PixelShader *pixel,FutNativeConstants *completion,
    FutGraphicsMemoryRead read,FutGraphicsMemoryWrite write,void *memory,HANDLE stop)
{
    return draw_immediate(native,batch,surfaces,pipeline,state,software,device,context,
        vertex,pixel,completion,read,write,memory,stop,0,0);
}
HRESULT fut_native_draw_immediate_aa2(FutNativeImmediate *native,FutGraphicsImmediate *batch,
    FutNativeSurfaces *surfaces,FutNativePipeline *pipeline,
    const FutGraphicsKelvinState *state,const FutGraphicsSoftwareState *software,
    ID3D11Device *device,ID3D11DeviceContext *context,
    ID3D11VertexShader *vertex,ID3D11PixelShader *pixel,FutNativeConstants *completion,
    FutGraphicsMemoryRead read,FutGraphicsMemoryWrite write,void *memory,HANDLE stop,int unit_w_verified)
{
    if(unit_w_verified!=1) return E_NOTIMPL;
    return draw_immediate(native,batch,surfaces,pipeline,state,software,device,context,
        vertex,pixel,completion,read,write,memory,stop,1,0);
}

HRESULT fut_native_draw_immediate_aa2_lines(FutNativeImmediate *native,FutGraphicsImmediate *batch,
    FutNativeSurfaces *surfaces,FutNativePipeline *pipeline,
    const FutGraphicsKelvinState *state,const FutGraphicsSoftwareState *software,
    ID3D11Device *device,ID3D11DeviceContext *context,
    ID3D11VertexShader *vertex,ID3D11PixelShader *pixel,FutNativeConstants *completion,
    FutGraphicsMemoryRead read,FutGraphicsMemoryWrite write,void *memory,HANDLE stop,int unit_w_verified)
{
    if(unit_w_verified!=1 || !fut_graphics_lines_16segment_aa2_approximation_enabled()) return E_NOTIMPL;
    return draw_immediate(native,batch,surfaces,pipeline,state,software,device,context,
        vertex,pixel,completion,read,write,memory,stop,1,1);
}
