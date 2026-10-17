/* Reconstruct the receiver from its first call. No guest CPU or XBE loading.
 * Recorded reads are supplied in order; native writes must match byte for byte.
 * This checks reproducibility, not agreement with Xbox hardware. */
#include "graphics_native_backend.c"
/* Replay has no translated guest routines or worker stacks. Never turn a
 * recorded notification into a synthetic successful callback. */
int fut_native_scanout_dispatch_init(FutNativeScanoutDispatch *dispatch)
{ (void)dispatch; return 0; }
void fut_native_scanout_dispatch_destroy(FutNativeScanoutDispatch *dispatch)
{ (void)dispatch; }
FutNativeScanoutDispatchResult fut_native_event5_dispatch_call(
    FutNativeScanoutDispatch *dispatch,uint32_t context,uint32_t notification)
{ (void)dispatch;(void)context;(void)notification;return FUT_SCANOUT_DISPATCH_NO_CONTEXT; }
FutNativeScanoutDispatchResult fut_native_scanout_dispatch_preflight(
    FutNativeScanoutDispatch *dispatch, uint32_t context, uint32_t notification,
    uint32_t callback)
{ (void)dispatch; (void)context; (void)notification; (void)callback;
  return FUT_SCANOUT_DISPATCH_NO_CONTEXT; }
FutNativeScanoutDispatchResult fut_native_scanout_dispatch_call(
    FutNativeScanoutDispatch *dispatch, uint32_t context, uint32_t notification,
    uint32_t callback, uint32_t *before, uint32_t *after)
{ (void)dispatch; (void)context; (void)notification; (void)callback;
  (void)before; (void)after; return FUT_SCANOUT_DISPATCH_NO_CONTEXT; }
ptrdiff_t xbox_GetMemoryOffset(void) { return 0; }
uint32_t xbox_GetConnectedInterrupt(uint32_t vector) { (void)vector; return 0; }
int xbox_kernel_post_host_vblank(void) { return 0; }
int main(int argc,char **argv)
{
    int probe_fragment=argc==3 && !strcmp(argv[2],"--probe-fragment");
    int probe_aa2=argc==3 && !strcmp(argv[2],"--probe-aa2");
    if(argc!=2 && !probe_fragment && !probe_aa2) {
        fputs("usage: graphics_replay trace.bin [--probe-fragment|--probe-aa2]\n",stderr);return 2;
    }
    fut_trace_open(argv[1],1);
    NativeGraphicsState state={0};
    D3D_FEATURE_LEVEL level;
    HRESULT hr=D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_HARDWARE,NULL,0,NULL,0,
        D3D11_SDK_VERSION,&state.device,&level,&state.context);
    if(FAILED(hr)) hr=D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,
        D3D11_SDK_VERSION,&state.device,&level,&state.context);
    if(FAILED(hr) || FAILED(fut_native_constants_init(&state.native_constants,
        state.device,state.context))) return 5;
    state.bindings.read=object_memory_read;
    unsigned methods=0;
    for(;;) {
        FutTraceRecord r;
        size_t n=fread(&r,1,sizeof(r),fut_trace_file);
        if(!n && feof(fut_trace_file)) break;
        if(n!=sizeof(r) || r.size) fut_trace_error();
        fut_trace_bytes+=sizeof(r);
        int result;
        if(r.kind==FUT_TRACE_METHOD) {
            uint32_t offset=fut_trace_event(FUT_TRACE_RESULT,0,0,0,NULL,0);
            fut_trace_active=1;
            result=native_method(&state,r.a,r.b,r.c,offset);
            ++methods;
        } else if(r.kind==FUT_TRACE_FINISH) {
            fut_trace_active=1;result=native_finish(&state);
        } else fut_trace_error();
        fut_trace_active=0;
        uint32_t expected=fut_trace_event(FUT_TRACE_RESULT,1,0,0,NULL,0);
        if(expected!=(uint32_t)result) fut_trace_error();
        if(!result) {
            /* A rejected terminal call is a complete capture, not draw success. */
            if(fgetc(fut_trace_file)!=EOF || ferror(fut_trace_file)) fut_trace_error();
            fprintf(stderr,"[REPLAY] reproduced terminal rejection after %u methods; no gameplay/fidelity claim\n",methods);
            if(probe_fragment) {
                if(!native_prepare_fragment(&state) || FAILED(fut_native_fragment_linkage(
                    state.vertex_bytecode,state.pixel_bytecode))) return 6;
                fprintf(stderr,"[REPLAY] captured fragment compiled and linked: alpha_test=%u func=%04X ref=%u; no draw\n",
                    state.kelvin.draw.alpha_test,state.kelvin.draw.alpha_func,state.kelvin.draw.alpha_ref);
            }
            if(probe_aa2) {
                /* A nonzero certificate requires unit screen/clip W for ALL
                 * probed vertices, not only those printed in the diagnostic. */
                if(!state.constant_coordinates || state.native_surfaces.plan.samples!=2) return 6;
                unsigned first=state.kelvin.program_start,count=0;
                while(first+count<136 && state.kelvin.program_valid[first+count]==15) {
                    ++count;if(state.kelvin.program[first+count-1][3]&1u) break;
                }
                ID3D11VertexShader *aa_vs=NULL;ID3DBlob *aa_vblob=NULL;
                ID3D11PixelShader *aa_ps=NULL;ID3DBlob *aa_pblob=NULL;
                HRESULT probe=fut_native_vertex_compile_screen_aa2(state.device,
                    state.kelvin.program+first,count,&aa_vs,&aa_vblob);
                if(SUCCEEDED(probe)) probe=fut_native_fragment_compile_aa2_affine(state.device,
                    &state.kelvin,state.textures,1,&aa_ps,&aa_pblob);
                if(SUCCEEDED(probe)) probe=fut_native_fragment_linkage(aa_vblob,aa_pblob);
                if(aa_vs) ID3D11VertexShader_Release(aa_vs);
                if(aa_ps) ID3D11PixelShader_Release(aa_ps);
                if(aa_vblob) ID3D10Blob_Release(aa_vblob);
                if(aa_pblob) ID3D10Blob_Release(aa_pblob);
                if(FAILED(probe)) {fprintf(stderr,"[REPLAY] AA2 shader probe failed: HRESULT=%08lX\n",probe);return 6;}
                fprintf(stderr,"[REPLAY] captured AA2 VS/PS compiled and linked: vertices=%u unit-W certificate=%X; no draw, dithering/raw controls remain blocked\n",
                    state.native_immediate.count,state.constant_coordinates);
            }
            return 0;
        }
    }
    fprintf(stderr,"[REPLAY] incomplete capture: %u methods, no terminal rejection\n",methods);
    return 4;
}
