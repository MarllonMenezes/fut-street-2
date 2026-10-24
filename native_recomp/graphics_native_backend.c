#define COBJMACROS
#include <windows.h>
#include <d3d11.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "xbox_memory_layout.h"
#include "graphics_command_stream.h"
#include "graphics_native_backend.h"
#include "graphics_objects.h"
#include "graphics_kelvin_state.h"
#include "graphics_native_constants.h"
#include "graphics_native_vertex.h"
#include "graphics_native_surfaces.h"
#include "graphics_native_textures.h"
#include "graphics_native_fragment.h"
#include "graphics_native_pipeline.h"
#include "graphics_software_state.h"
#include "graphics_native_immediate.h"
#include "graphics_vertex_array.h"
#include "graphics_native_draw.h"
#include "graphics_native_display.h"
#include "graphics_native_vblank.h"
#include "graphics_scanout.h"
#include "graphics_native_scanout_dispatch.h"
#include "guest_interrupt_gate.h"
#include "graphics_vertex_program.h"
#include "graphics_replay_trace.h"
#include "graphics_experiment.h"

static HANDLE backend_stop;
static HANDLE backend_thread;
/* The software notification callback is invoked only through the scoped
 * recompilation dispatcher below, after its exact target is validated. */
extern uint32_t xbox_GetConnectedInterrupt(uint32_t vector);
extern int xbox_kernel_post_host_vblank(void);

static HRESULT native_vblank_start_for_device(ID3D11Device *device,
                                               FutNativeVBlank *clock)
{
    IDXGIDevice *dxgi_device = NULL;
    IDXGIAdapter *adapter = NULL;
    HRESULT hr = ID3D11Device_QueryInterface(device, &IID_IDXGIDevice,
                                              (void **)&dxgi_device);
    if (FAILED(hr))
        return hr;
    hr = IDXGIDevice_GetAdapter(dxgi_device, &adapter);
    IDXGIDevice_Release(dxgi_device);
    if (FAILED(hr))
        return hr;

    HRESULT result = DXGI_ERROR_NOT_FOUND;
    for (UINT index = 0; index < 16; ++index) {
        IDXGIOutput *output = NULL;
        hr = IDXGIAdapter_EnumOutputs(adapter, index, &output);
        if (hr == DXGI_ERROR_NOT_FOUND)
            break;
        if (FAILED(hr)) {
            result = hr;
            break;
        }
        DXGI_OUTPUT_DESC desc;
        hr = IDXGIOutput_GetDesc(output, &desc);
        if (SUCCEEDED(hr) && desc.AttachedToDesktop)
            result = fut_native_vblank_start(clock, output);
        IDXGIOutput_Release(output);
        if (SUCCEEDED(result))
            break;
    }
    IDXGIAdapter_Release(adapter);
    return result;
}

static int read_guest(uint32_t va, void *data, size_t size)
{
    SIZE_T copied = 0;
    if (size > UINT32_MAX || va > UINT32_MAX - size)
        return 0;
    if(fut_trace_file && fut_trace_active && fut_trace_replaying)
        return (int)fut_trace_event(FUT_TRACE_READ,va,0,0,data,(uint32_t)size);
    int ok=ReadProcessMemory(GetCurrentProcess(),
        (const void *)((uintptr_t)xbox_GetMemoryOffset() + va), data, size, &copied)
        && copied == size;
    if(fut_trace_file && fut_trace_active) {
        if(!ok) memset(data,0,size);
        fut_trace_event(FUT_TRACE_READ,va,0,ok,data,(uint32_t)size);
    }
    return ok;
}

static int read32(uint32_t va, uint32_t *value)
{
    return read_guest(va, value, sizeof(*value));
}

static int write32(uint32_t va, uint32_t value)
{
    SIZE_T written = 0;
    if(fut_trace_file && fut_trace_active && fut_trace_replaying)
        return (int)fut_trace_event(FUT_TRACE_WRITE,va,0,0,&value,4);
    int ok=WriteProcessMemory(GetCurrentProcess(),
        (void *)((uintptr_t)xbox_GetMemoryOffset() + va), &value, sizeof(value), &written)
        && written == sizeof(value);
    if(fut_trace_file && fut_trace_active) fut_trace_event(FUT_TRACE_WRITE,va,0,ok,&value,4);
    return ok;
}

static uint32_t native_connected_interrupt(uint32_t vector)
{
    if(fut_trace_file && fut_trace_active && fut_trace_replaying)
        return fut_trace_event(FUT_TRACE_INTERRUPT,vector,0,0,NULL,0);
    uint32_t result=xbox_GetConnectedInterrupt(vector);
    if(fut_trace_file && fut_trace_active)
        fut_trace_event(FUT_TRACE_INTERRUPT,vector,0,result,NULL,0);
    return result;
}

static int native_scanout_context(uint32_t *address,uint32_t words[119])
{
    uint32_t object=native_connected_interrupt(3),context=0;
    uint32_t snapshot[119];
    if(!address || !words || !object || object>UINT32_MAX-8 ||
        !read32(object+4,&context) || !context || (context&3) ||
        !read_guest(context,snapshot,sizeof(snapshot))) return 0;
    memcpy(words,snapshot,sizeof(snapshot));*address=context;return 1;
}
static void native_scanout_diagnostic(uint32_t notification)
{
    uint32_t address=0,w[119];
    uint32_t data=notification>>5;
    fprintf(stderr,"[GRAPHICS] scanout request pending: physical=%08X interval=%u flags=%X; no presentation/flip/VBlank acknowledged\n",
        data&0xFFFFFFF0u,data&7u,data&8u);
    if(!native_scanout_context(&address,w)) {
        fprintf(stderr,"[GRAPHICS] scanout ISR context unavailable; no state changed\n");return;
    }
    fprintf(stderr,"[GRAPHICS] scanout context snapshot: context=%08X registers=%08X pitch=%u mode=%08X disabled=%u callback=%08X completed=%u scheduled=%u last=%u enqueue=%u retire=%u ticks=%u/%u\n",
        address,w[0],w[1],w[2],w[0x1B8/4],w[0x18C/4],w[0x1C0/4],w[0x1C4/4],w[0x1C8/4],
        w[0x1CC/4],w[0x1BC/4],w[0x1D4/4],w[0x1D8/4]);
    fprintf(stderr,"[GRAPHICS] scanout slots snapshot: slot0=%u/%u/%08X slot1=%u/%u/%08X\n",
        w[0x174/4],w[0x178/4],w[0x17C/4],w[0x180/4],w[0x184/4],w[0x188/4]);
    fprintf(stderr,"[GRAPHICS] scanout interrupt snapshot: enabled=%u servicing=%u dpc_routine=%08X dpc_context=%08X flags=%08X vblank_callback=%08X\n",
        w[0xA0/4],w[0xA4/4],w[0x90/4],w[0x94/4],w[0x1B4/4],w[0x190/4]);
    uint32_t object=native_connected_interrupt(3),irq[5];
    if(object && read_guest(object,irq,sizeof(irq)))
        fprintf(stderr,"[GRAPHICS] registered interrupt object: address=%08X routine=%08X context=%08X bus=%u irql=%u flags=%08X\n",
            object,irq[0],irq[1],irq[2],irq[3],irq[4]);
}


static int object_memory_write(void *context, uint32_t address, const void *data, size_t size)
{
    (void)context;
    SIZE_T written = 0;
    if (address < 0x80000000u || address >= 0x84000000u || size > 0x84000000u - address) return 0;
    if(fut_trace_file && fut_trace_active && fut_trace_replaying)
        return (int)fut_trace_event(FUT_TRACE_WRITE,address,0,0,(void *)data,(uint32_t)size);
    int ok=WriteProcessMemory(GetCurrentProcess(), (void *)((uintptr_t)xbox_GetMemoryOffset() + address),
        data, size, &written) && written == size;
    if(fut_trace_file && fut_trace_active)
        fut_trace_event(FUT_TRACE_WRITE,address,0,ok,(void *)data,(uint32_t)size);
    return ok;
}

static void fail_backend(const char *reason)
{
    fprintf(stderr, "[GRAPHICS] %s\n", reason);
    fflush(NULL);
    _Exit(3);
}

typedef struct {
    ID3D11Device *device;
    ID3D11DeviceContext *context;
    FutNativeConstants native_constants;
    ID3D11VertexShader *vertex_shader;
    ID3DBlob *vertex_bytecode;
    FutNativeVertexCache vertex_cache;
    ID3D11VertexShader *aa2_vertex_shader;
    ID3DBlob *aa2_vertex_bytecode;
    FutNativeVertexSurface vertex_surface;
    ID3D11PixelShader *pixel_shader;
    ID3DBlob *pixel_bytecode;
    FutNativeFragmentCache fragment_cache;
    FutGraphicsBindings bindings;
    FutGraphicsKelvinState kelvin;
    FutGraphicsSurfaceState surfaces;
    FutNativeSurfaces native_surfaces;
    FutNativeTexture textures[4];
    FutNativePipeline pipeline;
    FutGraphicsSoftwareState software;
    FutGraphicsImmediate immediate;
    FutGraphicsVertexArray vertex_array;
    FutNativeImmediate native_immediate;
    FutNativeDisplay diagnostic_preview;
    FutNativeDisplay scanout_display;
    FutNativeVBlank host_vblank;
    uint64_t host_vblank_sequence;
    FutGraphicsScanoutEnqueue scanout_plan;
    FutNativeScanoutDispatch scanout_dispatch;
    FutNativeDisplayReceipt scanout_receipt;
    ULONGLONG scanout_receipt_report_at;
    uint32_t scanout_context;
    uint32_t scanout_before,scanout_disabled;
    int scanout_pending,scanout_received;
    int scanout_callback_pending;
    int capture_pending;
    uint32_t capture_pending_physical;
    int diagnostic_preview_enabled;
    int diagnostic_preview_submitted;
    ULONGLONG scanout_idle_report;
    uint32_t deferred_scanout_notification;
    unsigned constant_coordinates;
    unsigned projective_coordinates;
    int aa2_projective;
    int quincunx_coordinates;
    uint32_t semaphore_offset;
    FutGraphicsDmaView semaphore_view;
} NativeGraphicsState;

static const char native_pending_capture_path[]="native-staged-pending.bmp";
static const char native_presented_capture_path[]="native-presented-latest.bmp";

/* Called only after the existing receipt/guest-retirement checks succeed.
 * File publication is diagnostic: it cannot complete or acknowledge a frame. */
static void native_confirm_presented_capture(NativeGraphicsState *state,
    uint32_t physical,int retired)
{
    if(!state || !state->capture_pending) return;
    state->capture_pending=0;
    if(!retired || physical!=state->capture_pending_physical ||
        physical!=state->scanout_plan.physical_address) {
        fprintf(stderr,"[FRAME-CAPTURE] latest not confirmed: retired=%d physical=%08X captured=%08X planned=%08X; previous confirmed image preserved\n",
            retired,physical,state->capture_pending_physical,state->scanout_plan.physical_address);
        return;
    }
    if(!MoveFileExA(native_pending_capture_path,native_presented_capture_path,
        MOVEFILE_REPLACE_EXISTING|MOVEFILE_WRITE_THROUGH)) {
        fprintf(stderr,"[FRAME-CAPTURE] latest not confirmed: publish_error=%lu physical=%08X ordinal=%u; previous confirmed image preserved\n",
            (unsigned long)GetLastError(),physical,state->scanout_plan.next_enqueue);
        return;
    }
    fprintf(stderr,"[FRAME-CAPTURE] latest confirmed: file=%s physical=%08X ordinal=%u DXGI_present=%u receipt_source=%s observed_QPC=%lld; matching DXGI receipt and original ISR retirement, host timing approximation\n",
        native_presented_capture_path,physical,state->scanout_plan.next_enqueue,
        state->scanout_receipt.present_count,
        state->scanout_receipt.source==1?"frame-latency-event":"frame-statistics",
        (long long)state->scanout_receipt.observed_qpc.QuadPart);
}

static void native_scanout_trace_failure(NativeGraphicsState *state,uint32_t notification,
    const char *reason,uint32_t address,const uint32_t words[119])
{
    static unsigned samples;
    if(!getenv("RECOMP_TRACE_SCANOUT_FAILURE") || (notification&31u)!=1u || samples>=64u) return;
    ++samples;
    const uint32_t data=notification>>5;
    fprintf(stderr,"[SCANOUT-FAILURE] sample=%u reason=%s notification=%08X physical=%08X interval=%u flags=%X "
        "host_vblank=%p pending=%u received=%u callback_pending=%u surfaces=%u size=%ux%u samples=%u color=%08X pitch=%u "
        "context=%08X regs=%08X context_pitch=%u mode=%08X disabled=%u callback=%08X completed=%u scheduled=%u last=%u enqueue=%u retire=%u "
        "slot0=%u/%u/%08X slot1=%u/%u/%08X\n",
        samples,reason,notification,data&0xFFFFFFF0u,data&7u,data&8u,
        state->host_vblank.state,state->scanout_pending,state->scanout_received,
        state->scanout_callback_pending,state->native_surfaces.valid,
        state->native_surfaces.plan.width,state->native_surfaces.plan.height,
        state->native_surfaces.plan.samples,state->native_surfaces.plan.color_address,
        state->native_surfaces.plan.color_pitch,address,
        words?words[0]:0,words?words[1]:0,words?words[2]:0,
        words?words[0x1B8/4]:0,words?words[0x18C/4]:0,
        words?words[0x1C0/4]:0,words?words[0x1C4/4]:0,
        words?words[0x1C8/4]:0,words?words[0x1CC/4]:0,
        words?words[0x1BC/4]:0,words?words[0x174/4]:0,
        words?words[0x178/4]:0,words?words[0x17C/4]:0,
        words?words[0x180/4]:0,words?words[0x184/4]:0,
        words?words[0x188/4]:0);
    fflush(stderr);
}

static int native_observe_vblank(NativeGraphicsState *state)
{
    if(!state->host_vblank.state) return 1;
    FutNativeVBlankReceipt receipt;
    HRESULT hr=fut_native_vblank_wait(&state->host_vblank,state->host_vblank_sequence,
        2,backend_stop,&receipt);
    if(hr==S_OK) {
        state->host_vblank_sequence=receipt.sequence;
        return xbox_kernel_post_host_vblank();
    }
    return hr==HRESULT_FROM_WIN32(WAIT_TIMEOUT) || hr==E_ABORT;
}

static int native_scanout_service(NativeGraphicsState *state)
{
    if(!state->scanout_pending) return 1;
    if(!state->scanout_received) {
        HRESULT hr=fut_native_display_poll(&state->scanout_display,&state->scanout_receipt);
        if(hr==S_FALSE) {
            ULONGLONG now=GetTickCount64();
            if(state->scanout_receipt_report_at && now>=state->scanout_receipt_report_at) {
                DXGI_FRAME_STATISTICS stats={0};
                HRESULT stats_hr=IDXGISwapChain2_GetFrameStatistics(state->scanout_display.swap,&stats);
                fprintf(stderr,"[GRAPHICS] pending DXGI receipt diagnostic: target=%u count_valid=%d stats_hr=%08lX present=%u sync=%u qpc=%lld host_vblank_sequence=%llu; no receipt accepted\n",
                    state->scanout_display.target_present,state->scanout_display.count_valid,stats_hr,
                    stats.PresentCount,stats.SyncRefreshCount,(long long)stats.SyncQPCTime.QuadPart,
                    (unsigned long long)state->host_vblank_sequence);
                if(getenv("RECOMP_DISPLAY_DIAG")) {
                    HWND window=state->scanout_display.window;RECT rect={0};
                    int rect_ok=GetWindowRect(window,&rect);
                    HRESULT test=IDXGISwapChain2_Present(state->scanout_display.swap,0,DXGI_PRESENT_TEST);
                    fprintf(stderr,"[DISPLAY-DIAG] test=%08lX visible=%d iconic=%d rect_ok=%d rect=%ld,%ld,%ld,%ld\n",
                        test,IsWindowVisible(window),IsIconic(window),rect_ok,
                        rect.left,rect.top,rect.right,rect.bottom);
                    UINT submitted=0;
                    HRESULT count_hr=IDXGISwapChain2_GetLastPresentCount(state->scanout_display.swap,&submitted);
                    RECT client={0};POINT center={0};
                    int center_ok=GetClientRect(window,&client);
                    if(center_ok) {
                        center.x=(client.right-client.left)/2;center.y=(client.bottom-client.top)/2;
                        center_ok=ClientToScreen(window,&center);
                    }
                    HWND at_center=center_ok?WindowFromPoint(center):NULL;
                    HWND center_root=at_center?GetAncestor(at_center,GA_ROOT):NULL;
                    char center_class[128]={0};DWORD center_pid=0;
                    if(center_root) {
                        GetClassNameA(center_root,center_class,sizeof(center_class));
                        GetWindowThreadProcessId(center_root,&center_pid);
                    }
                    fprintf(stderr,"[DISPLAY-DIAG] hung=%d center_class=%s center_same_process=%d; no window titles captured\n",
                        IsHungAppWindow(window),center_class,center_pid==GetCurrentProcessId());
                    fprintf(stderr,"[DISPLAY-DIAG] submitted_hr=%08lX submitted=%u device_hr=%08lX center_valid=%d center_owned=%d monitor_present=%d; observation only\n",
                        count_hr,submitted,ID3D11Device_GetDeviceRemovedReason(state->device),center_ok,
                        at_center && GetAncestor(at_center,GA_ROOT)==window,
                        MonitorFromWindow(window,MONITOR_DEFAULTTONULL)!=NULL);
                }
                state->scanout_receipt_report_at=now+1000;
            }
            return 1;
        }
        if(hr!=S_OK) {
            fprintf(stderr,"[GRAPHICS] scanout receipt failed: HRESULT=%08lX; no flip-read advance\n",hr);
            return 0;
        }
        state->scanout_received=1;
        state->scanout_receipt_report_at=0;
    }
    if(!fut_guest_interrupt_try_enter()) return 1;
    uint32_t address,w[FUT_SCANOUT_CONTEXT_WORDS],physical,title_physical,increment,gamma_pending;
    if(state->scanout_callback_pending) {
        uint32_t callback_tsc_before=0,callback_tsc_after=0;
        FutGraphicsScanoutEnqueue plan;
        if(!native_scanout_context(&address,w) || address!=state->scanout_context ||
            !fut_graphics_scanout_enqueue_plan(w,state->deferred_scanout_notification,&plan) ||
            plan.physical_address!=state->scanout_plan.physical_address ||
            plan.slot!=state->scanout_plan.slot || plan.next_enqueue!=state->scanout_plan.next_enqueue ||
            w[0x1CC/4]!=w[0x1BC/4] ||
            fut_native_scanout_dispatch_preflight(&state->scanout_dispatch,address,
                state->deferred_scanout_notification,0x00191340u)!=FUT_SCANOUT_DISPATCH_OK) {
            fut_guest_interrupt_leave();
            fputs("[GRAPHICS] callback scanout state changed after its DXGI receipt; no callback replay\n",stderr);
            return 0;
        }
        FutNativeScanoutDispatchResult call_result=fut_native_scanout_dispatch_call(&state->scanout_dispatch,address,
            state->deferred_scanout_notification,0x00191340u,
            &callback_tsc_before,&callback_tsc_after);
        int snapshot_ok=native_scanout_context(&address,w) && address==state->scanout_context;
        int handler_ok=call_result==FUT_SCANOUT_DISPATCH_OK && snapshot_ok &&
            w[0x1CC/4]==plan.next_enqueue && w[0x1BC/4]==state->scanout_plan.next_enqueue-1u &&
            w[0x1C4/4]==plan.next_deadline && w[0x1C8/4]==plan.deadline &&
            w[0x18C/4]==0x00191340u && w[0x174/4+plan.slot*3u]==1 &&
            w[0x178/4+plan.slot*3u]==plan.deadline &&
            w[0x17C/4+plan.slot*3u]==plan.physical_address;
        if(!handler_ok) {
            if(snapshot_ok) fprintf(stderr,"[GRAPHICS] callback effect diagnostic: result=%d ordinal=%u/%u slot=%u/%u deadline=%u/%u next=%u/%u callback=%08X physical=%08X/%08X current=%u\n",
                call_result,w[0x1CC/4],w[0x1BC/4],plan.slot,
                w[0x174/4+plan.slot*3u],plan.deadline,w[0x178/4+plan.slot*3u],
                plan.next_deadline,w[0x1C4/4],w[0x18C/4],
                w[0x17C/4+plan.slot*3u],plan.physical_address,w[0x1C0/4]);
            else fprintf(stderr,"[GRAPHICS] callback effect diagnostic: result=%d context snapshot unavailable\n",call_result);
            fut_guest_interrupt_leave();
            fputs("[GRAPHICS] original scanout handler did not produce the expected enqueue; no replay\n",stderr);
            return 0;
        }
        state->scanout_plan=plan;state->scanout_callback_pending=0;
        fprintf(stderr,"[GRAPHICS] original recompilation callback ran after DXGI receipt: TSC=%08X->%08X; original enqueue=%u, awaiting next ISR retirement\n",
            callback_tsc_before,callback_tsc_after,plan.next_enqueue);
    }
    int valid=native_scanout_context(&address,w) && address==state->scanout_context &&
        read32(0xFD600800u,&physical) && read32(0x002A1128u,&title_physical) &&
        read32(0xFD40071Cu,&increment) &&
        read32(address+0x7DC+state->scanout_plan.slot*4,&gamma_pending);
    int capture_retired=0;
    if(valid && !gamma_pending && fut_graphics_scanout_retirement_matches(w,&state->scanout_plan,
        state->scanout_received,physical,state->scanout_before,state->scanout_disabled,title_physical,increment)) {
        if(fut_graphics_flip_available(&state->kelvin)<0 ||
            !write32(0xFD40071Cu,increment&~2u)) {
            fut_guest_interrupt_leave();return 0;
        }
        state->kelvin.flip_read=(state->kelvin.flip_read+1u)%state->kelvin.flip_modulo;
        fprintf(stderr,"[GRAPHICS] DXGI receipt source=%s observed_QPC=%lld; latency-event receipts carry no refresh/sync timestamp\n",
            state->scanout_receipt.source==1?"frame-latency-event":"frame-statistics",
            (long long)state->scanout_receipt.observed_qpc.QuadPart);
        fprintf(stderr,"[GRAPHICS] original queued surface presented and ISR retirement matched: physical=%08X PCRTC=%08X disabled=%u ordinal=%u DXGI_present=%u refresh=%u/%u QPC=%lld; flip read=%u write=%u modulo=%u; host timing approximation\n",
            title_physical,physical,state->scanout_disabled,state->scanout_plan.next_enqueue,state->scanout_receipt.present_count,
            state->scanout_receipt.present_refresh,state->scanout_receipt.sync_refresh,
            (long long)state->scanout_receipt.sync_qpc.QuadPart,state->kelvin.flip_read,
            state->kelvin.flip_write,state->kelvin.flip_modulo);
        state->scanout_pending=state->scanout_received=0;
        state->scanout_idle_report=0;
        capture_retired=1;
    }
    else if(valid && state->scanout_idle_report && GetTickCount64()>=state->scanout_idle_report) {
        fprintf(stderr,"[GRAPHICS] scanout correlation pending: receipt=%d ordinal=%u/%u expected=%u disabled=%u/%u gamma_pending=%u PCRTC=%08X before=%08X title=%08X expected=%08X increment=%08X\n",
            state->scanout_received,w[0x1CC/4],w[0x1BC/4],state->scanout_plan.next_enqueue,
            w[0x1B8/4],state->scanout_disabled,gamma_pending,physical,state->scanout_before,
            title_physical,state->scanout_plan.physical_address,increment);
        state->scanout_idle_report=0;
    }
    fut_guest_interrupt_leave();
    if(capture_retired) native_confirm_presented_capture(state,title_physical,1);
    return valid;
}

static int object_memory_read(void *context, uint32_t address, void *data, size_t size);

static void native_preview_log_source_stats(const FutGraphicsSurfacePlan *plan)
{
    size_t row_bytes = (size_t)plan->width * 4u;
    size_t total_bytes = (size_t)plan->color_pitch * plan->height;
    uint8_t *pixels;
    uint64_t hash = UINT64_C(14695981039346656037);
    uint64_t nonblack = 0, nonzero_alpha = 0;
    uint32_t first = 0, center = 0, last = 0;

    if ((plan->width != 640 && !(fut_graphics_aa2_approximation_enabled() && plan->width==1280 && plan->samples==2)) || plan->height != 480 ||
        plan->color_pitch < row_bytes || total_bytes > 16u * 1024u * 1024u) {
        fputs("[GRAPHICS] diagnostic preview source stats unavailable: invalid bounds\n", stderr);
        return;
    }
    pixels = malloc(total_bytes);
    if (!pixels || !read_guest(plan->color_address, pixels, total_bytes)) {
        fputs("[GRAPHICS] diagnostic preview source stats unavailable: guest read failed\n", stderr);
        free(pixels);
        return;
    }
    for (unsigned y = 0; y < plan->height; ++y) {
        const uint8_t *row_bytes_ptr = pixels + (size_t)y * plan->color_pitch;
        const uint32_t *row = (const uint32_t *)(pixels + (size_t)y * plan->color_pitch);
        for (size_t byte = 0; byte < row_bytes; ++byte)
            hash = (hash ^ row_bytes_ptr[byte]) * UINT64_C(1099511628211);
        for (unsigned x = 0; x < plan->width; ++x) {
            uint32_t pixel = row[x];
            nonblack += (pixel & 0x00FFFFFFu) != 0;
            nonzero_alpha += (pixel >> 24) != 0;
            if (x == 0 && y == 0) first = pixel;
            if (x == plan->width / 2 && y == plan->height / 2) center = pixel;
            if (x == plan->width - 1 && y == plan->height - 1) last = pixel;
        }
    }
    fprintf(stderr, "[GRAPHICS] diagnostic source read-only stats: %ux%u pitch=%u RGB_nonblack=%llu/%llu alpha_nonzero=%llu/%llu FNV64=%016llX first/center/last=%08X/%08X/%08X\n",
        plan->width, plan->height, plan->color_pitch,
        (unsigned long long)nonblack, (unsigned long long)plan->width * plan->height,
        (unsigned long long)nonzero_alpha, (unsigned long long)plan->width * plan->height,
        (unsigned long long)hash, first, center, last);
    free(pixels);
}

/* Diagnostic-only host view of the pixels produced by an original guest draw.
 * It deliberately does not consume guest flip state or signal guest timing. */
static int native_preview_original_draw(NativeGraphicsState *state)
{
    if (!state->diagnostic_preview_enabled || state->diagnostic_preview_submitted)
        return 1;
    FutGraphicsSurfacePlan *plan = &state->native_surfaces.plan;
    if (!state->native_surfaces.valid || !state->native_surfaces.color ||
        plan->width != 640 || plan->height != 480 || plan->samples != 1) {
        fprintf(stderr, "[GRAPHICS] diagnostic preview rejected: expected BGRA8 single-sample 640x480 draw target\n");
        return 0;
    }
    native_preview_log_source_stats(plan);
    HRESULT hr = fut_native_display_open(&state->diagnostic_preview,
        state->device, plan->width, plan->height, 1);
    if (hr == S_OK)
        hr = fut_native_display_stage(&state->diagnostic_preview, state->context,
            state->native_surfaces.color, 2000, backend_stop);
    if (hr == S_OK)
        hr = fut_native_display_submit(&state->diagnostic_preview, 1);
    FutNativeDisplayReceipt receipt;
    if (hr == S_OK) {
        ULONGLONG deadline = GetTickCount64() + 2000;
        do {
            hr = fut_native_display_poll(&state->diagnostic_preview, &receipt);
            if (hr == S_FALSE) Sleep(1);
        } while (hr == S_FALSE && GetTickCount64() < deadline);
    }
    if (hr != S_OK) {
        fprintf(stderr, "[GRAPHICS] diagnostic preview failed: HRESULT=%08lX; guest flip/IRQ unchanged\n", hr);
        fut_native_display_close(&state->diagnostic_preview);
        return 0;
    }
    state->diagnostic_preview_submitted = 1;
    fprintf(stderr, "[GRAPHICS] diagnostic preview received DXGI frame statistics: present=%u present_refresh=%u sync_refresh=%u; host receipt only, no guest flip/retirement/VBlank/IRQ\n",
        receipt.present_count, receipt.present_refresh, receipt.sync_refresh);
    return 1;
}

static int native_writeback_surfaces(NativeGraphicsState *state)
{
    HRESULT hr = fut_native_surfaces_writeback(&state->native_surfaces, state->device,
        state->context, &state->native_constants, object_memory_read, object_memory_write, NULL, backend_stop);
    if (FAILED(hr)) {
        fprintf(stderr, "[GRAPHICS] current surface writeback failed: HRESULT=%08lX dirty=%u\n",
            hr, state->native_surfaces.gpu_dirty);
        return 0;
    }
    return 1;
}
static int native_prepare_surfaces(NativeGraphicsState *state)
{
    if (state->surfaces.valid != 63) {
        fprintf(stderr, "[GRAPHICS] incomplete surface description at use: valid=%u\n", state->surfaces.valid);
        return 0;
    }
    FutGraphicsDmaView color, depth;
    FutGraphicsSurfacePlan plan;
    const unsigned mask = (1u << 5) | (1u << 6);
    if ((state->bindings.reference_masks[0] & mask) != mask ||
        !fut_graphics_dma_surface_view(&state->bindings.references[0][5], &color) ||
        !fut_graphics_dma_surface_view(&state->bindings.references[0][6], &depth)) {
        fprintf(stderr, "[GRAPHICS] unsupported surface DMA descriptors: color=%08X/%08X/%08X depth=%08X/%08X/%08X\n",
            state->bindings.references[0][5].words[0], state->bindings.references[0][5].words[1],
            state->bindings.references[0][5].words[2], state->bindings.references[0][6].words[0],
            state->bindings.references[0][6].words[1], state->bindings.references[0][6].words[2]);
        return 0;
    }
    FutGraphicsSurfaceStatus status=FUT_SURFACE_UNSUPPORTED;
    if(state->native_surfaces.valid)
        status=fut_graphics_surface_plan_clipped(&state->surfaces,&color,&depth,
            &state->native_surfaces.plan,&plan);
    if(status!=FUT_SURFACE_READY)
        status=fut_graphics_surface_plan(&state->surfaces,&color,&depth,&plan);
    if (status != FUT_SURFACE_READY || (state->kelvin.control0 & 0x1000u)) {
        fprintf(stderr, "[GRAPHICS] unsupported/invalid surface plan: status=%d format=%08X pitch=%08X control=%08X\n",
            status, state->surfaces.format, state->surfaces.pitch, state->kelvin.control0);
        return 0;
    }
    if (!native_writeback_surfaces(state)) return 0;
    int changed = !state->native_surfaces.valid ||
        memcmp(&state->native_surfaces.plan, &plan, sizeof(plan));
    HRESULT hr = fut_native_surfaces_prepare(&state->native_surfaces, state->device, &plan, object_memory_read, NULL);
    if (FAILED(hr)) {
        fprintf(stderr, "[GRAPHICS] native surface preparation failed: HRESULT=%08lX\n", hr);
        return 0;
    }
    ID3D11DeviceContext_OMSetRenderTargets(state->context, 1, &state->native_surfaces.target,
        state->native_surfaces.depth_target);
    if (changed) fprintf(stderr,
        "[GRAPHICS] native surfaces uploaded: logical=%ux%u stored=%ux%u samples=%u color=%08X depth=%08X; no draw\n",
        plan.logical_width, plan.logical_height, plan.width, plan.height, plan.samples, plan.color_address, plan.depth_address);
    return 1;
}

static int native_prepare_vertex(NativeGraphicsState *state)
{
    if (state->kelvin.program_dirty && (state->kelvin.program_mode & 3u) == 2u) {
        unsigned first = state->kelvin.program_start, count = 0;
        while (first + count < 136 && state->kelvin.program_valid[first + count] == 15) {
            ++count;
            if (state->kelvin.program[first + count - 1][3] & 1u) break;
        }
        const char *cache_option = getenv("RECOMP_VERTEX_SHADER_CACHE");
        int cache_enabled = !(cache_option && cache_option[0] == '0' && cache_option[1] == '\0');
        int cache_hit=0;
        ID3D11VertexShader *shader = NULL; ID3DBlob *bytecode = NULL;
        HRESULT hr = fut_native_vertex_compile_screen_cached(state->device, state->kelvin.program + first,
            count, 0, cache_enabled?&state->vertex_cache:NULL, &shader, &bytecode, &cache_hit);
        if (FAILED(hr)) {
            fprintf(stderr, "[GRAPHICS] vertex program recompilation failed: start=%u count=%u hr=%08lX\n",
                    first, count, hr);
            for (unsigned i = 0; i < count; ++i) {
                const uint32_t *w = state->kelvin.program[first + i];
                fprintf(stderr, "[VERTEX-PROGRAM] slot=%u words=%08X/%08X/%08X/%08X mac=%u ilu=%u\n",
                    first+i,w[0],w[1],w[2],w[3],(w[1]>>21)&15u,(w[1]>>25)&7u);
            }
            return 0;
        }
        if (state->vertex_shader) ID3D11VertexShader_Release(state->vertex_shader);
        if (state->vertex_bytecode) ID3D10Blob_Release(state->vertex_bytecode);
        state->vertex_shader = shader; state->vertex_bytecode = bytecode;
        ID3D11DeviceContext_VSSetShader(state->context, shader, NULL, 0);
        state->kelvin.program_dirty = 0;
        if(cache_hit)
            fprintf(stderr,"[GRAPHICS] original vertex program reused from exact instruction cache: start=%u instructions=%u\n",first,count);
        else
            fprintf(stderr, "[GRAPHICS] original vertex program recompiled to native shader: start=%u instructions=%u\n",
                first, count);
    }
    return 1;
}

static void native_capture_observed_yuy2(NativeGraphicsState *state,
    const FutGraphicsTextureState *texture,const FutGraphicsDmaView *dma);

static int native_prepare_textures(NativeGraphicsState *state)
{
    /* Texture uploads read authoritative guest data, including any earlier
     * GPU render target used as input. Never sample an obsolete CPU snapshot. */
    if (!native_writeback_surfaces(state)) return 0;
    for (unsigned stage=0;stage<4;++stage) {
        const FutGraphicsTextureState *texture=&state->kelvin.textures[stage];
        if (!(texture->control0 & 0x40000000u)) {
            state->textures[stage].sampling_ready=0;
            state->textures[stage].zero_rgb_candidate=0;
            state->textures[stage].quincunx_experiment=0;
            ID3D11ShaderResourceView *empty_view=NULL;
            ID3D11SamplerState *empty_sampler=NULL;
            ID3D11DeviceContext_PSSetShaderResources(state->context,stage,1,&empty_view);
            ID3D11DeviceContext_PSSetSamplers(state->context,stage,1,&empty_sampler);
            continue;
        }
        unsigned select=texture->format & 3;
        FutGraphicsDmaView dma;
        if ((select != 1 && select != 2) ||
            !(state->bindings.reference_masks[0] & (1u<<select)) ||
            (!fut_graphics_dma_surface_view(&state->bindings.references[0][select],&dma) &&
             !fut_graphics_dma_view(&state->bindings.references[0][select],&dma))) {
            fprintf(stderr,"[GRAPHICS] texture DMA context missing/unsupported: stage=%u context=%u\n",stage,select);
            return 0;
        }
        FutGraphicsTexturePlan plan;
        FutGraphicsTextureStatus status=fut_graphics_texture_plan(texture,&dma,&plan);
        int yuy2_bt601_enabled=texture->offset==0x023F1580u?
            fut_graphics_yuy2_bt601_approximation_enabled():
            texture->offset==0x02487680u?
                fut_graphics_yuy2_second_bt601_approximation_enabled():
                fut_graphics_yuy2_family_bt601_approximation_enabled();
        if(status!=FUT_TEXTURE_READY && stage==0 &&
            texture->format==0x00012429u && yuy2_bt601_enabled &&
            state->native_surfaces.plan.samples==2 &&
            state->native_surfaces.plan.logical_width==640 &&
            state->native_surfaces.plan.logical_height==480 &&
            fut_graphics_texture_plan_observed_yuy2_approximation(texture,&dma,&plan)) {
            status=FUT_TEXTURE_READY;
            if(texture->offset==0x023F1580u)
                fputs("[GRAPHICS] AUTHORIZED_YUY2_BT601_APPROXIMATION: exact captured LC_IMAGE 640x480 state; limited-range BT.601 to BGRA8; matrix/range/rounding not NV2A-verified\n",stderr);
            else if(texture->offset==0x02487680u)
                fputs("[GRAPHICS] AUTHORIZED_YUY2_02487680_BT601_APPROXIMATION: exact captured LC_IMAGE 640x480 state; limited-range BT.601 to BGRA8; matrix/range/rounding not NV2A-verified\n",stderr);
            else
                fprintf(stderr,"[GRAPHICS] AUTHORIZED_YUY2_FAMILY_BT601_APPROXIMATION: format=%08X offset=%08X bounded 640x480 buffer; limited-range BT.601 to BGRA8; matrix/range/rounding not NV2A-verified\n",
                    texture->format,texture->offset);
        }
        if (status != FUT_TEXTURE_READY) {
            if(status==FUT_TEXTURE_UNSUPPORTED)
                native_capture_observed_yuy2(state,texture,&dma);
            fprintf(stderr,"[GRAPHICS] texture storage unsupported/invalid: stage=%u status=%d format=%08X offset=%08X address=%08X filter=%08X image_rect=%08X control0=%08X control1=%08X valid=%03X dma=%08X/%08X\n",
                stage,status,texture->format,texture->offset,texture->address,texture->filter,
                texture->image_rect,texture->control0,texture->control1,texture->valid,dma.guest_address,dma.size);
            return 0;
        }
        HRESULT hr=fut_native_texture_upload(&state->textures[stage],state->device,&plan,object_memory_read,NULL);
        if (FAILED(hr)) {
            fprintf(stderr,"[GRAPHICS] texture upload failed: stage=%u HRESULT=%08lX\n",stage,hr);
            return 0;
        }
        fprintf(stderr,"[GRAPHICS] native texture uploaded: stage=%u size=%ux%u pitch=%u source=%08X OR=%08X AND=%08X; no sample/draw\n",
            stage,plan.width,plan.height,plan.pitch,plan.address,state->textures[stage].texel_or,state->textures[stage].texel_and);
        hr=fut_native_texture_sampler(&state->textures[stage],state->device,texture);
        if(SUCCEEDED(hr) && plan.mip_levels>1) fprintf(stderr,
            "[GRAPHICS] native mip chain uploaded: stage=%u storage=%u levels=%u bytes=%u; trilinear LOD/bias translation, NV2A precision unverified NOT_READY\n",
            stage,plan.storage,plan.mip_levels,plan.span);
        if(FAILED(hr) && state->immediate.ready && !state->immediate.active &&
            (state->constant_coordinates&(1u<<stage)))
            hr=fut_native_texture_zero_rgb_candidate(&state->textures[stage],texture,1);
        if(FAILED(hr) && stage==0 && state->quincunx_coordinates &&
            state->immediate.ready && !state->immediate.active &&
            state->native_surfaces.plan.samples==1 &&
            state->native_surfaces.plan.width==640 && state->native_surfaces.plan.height==480)
            hr=fut_native_texture_quincunx_experiment(&state->textures[stage],texture,1);
        if (FAILED(hr)) {
            fprintf(stderr,"[GRAPHICS] texture sampling translation pending: stage=%u filter=%08X address=%08X control=%08X HRESULT=%08lX\n",
                stage,texture->filter,texture->address,texture->control0,hr);
            return 0;
        }
        if(state->textures[stage].zero_rgb_candidate) {
            ID3D11ShaderResourceView *empty_view=NULL;ID3D11SamplerState *empty_sampler=NULL;
            ID3D11DeviceContext_PSSetShaderResources(state->context,stage,1,&empty_view);
            ID3D11DeviceContext_PSSetSamplers(state->context,stage,1,&empty_sampler);
            fprintf(stderr,"[GRAPHICS] zero-RGB snapshot candidate: stage=%u finite unit-q coordinates proven; alpha/residual sample still unimplemented\n",stage);
            continue;
        }
        if(state->textures[stage].quincunx_experiment)
            fputs("[GRAPHICS] AUTHORIZED_QUINCUNX_APPROXIMATION: five taps center=1/2 corners=1/8, parity-preserving edge clamp, identity color space; 1280x480->640x480 only; no NV2A precision/edge validation\n",stderr);
        ID3D11DeviceContext_PSSetShaderResources(state->context,stage,1,&state->textures[stage].view);
        ID3D11DeviceContext_PSSetSamplers(state->context,stage,1,&state->textures[stage].sampler);
    }
    return 1;
}

static int native_finish(NativeGraphicsState *state)
{
    if (state->immediate.active || state->immediate.ready) {
        fprintf(stderr,"[GRAPHICS] completion refused: immediate primitive has not executed\n");
        return 0;
    }
    /* WAIT_IDLE/semaphore concerns submitted work, not setters for the next
     * target. In particular, never validate a new pitch with an old format. */
    if (!native_writeback_surfaces(state)) return 0;
    if (!fut_native_constants_finish(&state->native_constants,
        state->kelvin.constants_dirty ? state->kelvin.constants : NULL, 2000, backend_stop))
        return 0;
    state->kelvin.constants_dirty = 0;
    return 1;
}

static int native_scanout_enqueue_locked(NativeGraphicsState *state,uint32_t notification)
{
    uint32_t address=0,w[FUT_SCANOUT_CONTEXT_WORDS]={0},again[FUT_SCANOUT_CONTEXT_WORDS]={0},checked=0;
    uint32_t callback;
    FutGraphicsScanoutEnqueue plan;
    if(!state->native_surfaces.valid) {
        native_scanout_trace_failure(state,notification,"surface-invalid",address,w);
        native_scanout_diagnostic(notification);return 0;
    }
    if(state->native_surfaces.plan.samples!=1) {
        native_scanout_trace_failure(state,notification,"surface-samples",address,w);
        native_scanout_diagnostic(notification);return 0;
    }
    if(!native_scanout_context(&address,w)) {
        native_scanout_trace_failure(state,notification,"context-read",address,w);
        native_scanout_diagnostic(notification);return 0;
    }
    if(!fut_graphics_scanout_enqueue_plan(w,notification,&plan)) {
        native_scanout_trace_failure(state,notification,"enqueue-plan",address,w);
        native_scanout_diagnostic(notification);return 0;
    }
    if(w[0]!=0xFD000000u) {
        native_scanout_trace_failure(state,notification,"context-registers",address,w);
        native_scanout_diagnostic(notification);return 0;
    }
    if(w[1]!=state->native_surfaces.plan.color_pitch) {
        native_scanout_trace_failure(state,notification,"context-pitch",address,w);
        native_scanout_diagnostic(notification);return 0;
    }
    if((plan.physical_address|0x80000000u)!=state->native_surfaces.plan.color_address) {
        native_scanout_trace_failure(state,notification,"physical-address",address,w);
        native_scanout_diagnostic(notification);return 0;
    }
    callback=w[0x18C/4];
    if(state->host_vblank.state) {
        uint32_t gamma_pending=0;
        if(state->scanout_pending || w[0x1CC/4]!=w[0x1BC/4] || w[0x1B8/4]>1 ||
            !read32(0xFD600800u,&state->scanout_before) ||
            !read32(address+0x7DC+plan.slot*4,&gamma_pending) || gamma_pending>1) {
            fprintf(stderr,"[GRAPHICS] scanout presentation domain refused: pending=%d enqueue=%u retire=%u disabled=%u gamma_pending=%u\n",
                state->scanout_pending,w[0x1CC/4],w[0x1BC/4],w[0x1B8/4],gamma_pending);
            native_scanout_trace_failure(state,notification,"presentation-domain",address,w);
            return 0;
        }
        /* sub_0029613D uploads three 256-byte DAC ramps, not an overlay.
         * Only identity is representable by the current pixel-copy display. */
        uint8_t ramp[768];
        if(!read_guest(address+0x1DC+plan.slot*768,ramp,sizeof(ramp))) {
            native_scanout_trace_failure(state,notification,"gamma-read",address,w);return 0;
        }
        for(unsigned i=0;i<sizeof(ramp);++i) if(ramp[i]!=(uint8_t)i) {
            fprintf(stderr,"[GRAPHICS] nonidentity scanout gamma unsupported: offset=%u value=%u\n",i,ramp[i]);
            native_scanout_trace_failure(state,notification,"nonidentity-gamma",address,w);
            return 0;
        }
    }
    /* Gate excludes concurrent CLI-protected guest work and future host
     * interrupt delivery. Re-read also detects changes from nonparticipants;
     * it does not make unprotected guest writes safe. */
    if(!native_scanout_context(&checked,again)) {
        native_scanout_trace_failure(state,notification,"context-reread-failed",address,w);return 0;
    }
    if(checked!=address || memcmp(w,again,sizeof(w))) {
        native_scanout_trace_failure(state,notification,"context-changed-before-enqueue",checked,again);return 0;
    }
    if(!callback) {
        /* Keep the already exercised callback-free black-surface path. */
        unsigned slot_offset=0x174+plan.slot*12;
        if(!write32(address+0x1C4,plan.next_deadline) || !write32(address+0x1C8,plan.deadline) ||
            !write32(address+slot_offset,1) || !write32(address+slot_offset+4,plan.deadline) ||
            !write32(address+slot_offset+8,plan.physical_address) ||
            !write32(address+0x1CC,plan.next_enqueue)) {
            native_scanout_trace_failure(state,notification,"callback-free-write",address,w);return 0;
        }
        if(!native_scanout_context(&checked,again) || checked!=address ||
            again[0x1CC/4]!=plan.next_enqueue || again[0x1BC/4]!=w[0x1BC/4] ||
            again[0x1C4/4]!=plan.next_deadline || again[0x1C8/4]!=plan.deadline ||
            again[0x18C/4]!=0 || again[0x174/4+plan.slot*3u]!=1 ||
            again[0x178/4+plan.slot*3u]!=plan.deadline ||
            again[0x17C/4+plan.slot*3u]!=plan.physical_address) {
            fputs("[GRAPHICS] callback-free scanout enqueue diverged from its plan\n",stderr);
            native_scanout_trace_failure(state,notification,"callback-free-reread",checked,again);
            return 0;
        }
        fprintf(stderr,"[GRAPHICS] callback-free original scanout enqueued: slot=%u physical=%08X deadline=%u; awaiting original ISR retirement\n",
            plan.slot,plan.physical_address,plan.deadline);
    } else {
        if(fut_trace_file && fut_trace_active) {
            fputs("[GRAPHICS] original callback dispatch is unavailable during graphics trace/replay\n",stderr);
            native_scanout_trace_failure(state,notification,"trace-replay-active",address,w);
            return 0;
        }
        FutNativeScanoutDispatchResult dispatched=fut_native_scanout_dispatch_preflight(
            &state->scanout_dispatch,address,notification,0x00191340u);
        if(dispatched!=FUT_SCANOUT_DISPATCH_OK) {
            fprintf(stderr,"[GRAPHICS] original scanout callback dispatch refused: result=%d; queue mutation not accepted\n",dispatched);
            native_scanout_trace_failure(state,notification,"callback-preflight",address,w);
            return 0;
        }
        fprintf(stderr,"[GRAPHICS] original callback 00191340 validated; waiting for DXGI receipt before executing its enqueue/retirement handler\n");
    }
    state->deferred_scanout_notification=notification;
    state->scanout_idle_report=GetTickCount64()+1000;
    if(state->host_vblank.state) {
        state->scanout_plan=plan;state->scanout_context=address;
        state->scanout_disabled=w[0x1B8/4];
        state->scanout_callback_pending=callback!=0;
        state->scanout_receipt_report_at=GetTickCount64()+1000;
        state->scanout_pending=1;state->scanout_received=0;
    }
    return 1;
}

static int native_scanout_enqueue(NativeGraphicsState *state,uint32_t notification)
{
    /* Never wait for GPU completion while holding the guest interrupt gate. */
    if(!state->host_vblank.state) {
        fputs("[GRAPHICS] callback scanout requires the observed host-output receipt path\n",stderr);
        native_scanout_trace_failure(state,notification,"host-vblank-unavailable",0,NULL);
        return 0;
    }
    if(!native_finish(state)) {
        native_scanout_trace_failure(state,notification,"native-finish",0,NULL);return 0;
    }
    if(state->host_vblank.state) {
        unsigned interval=(notification>>5)&7u;
        if(state->scanout_pending || !interval || interval>4 ||
            !state->native_surfaces.valid || state->native_surfaces.plan.samples!=1) {
            fprintf(stderr,"[GRAPHICS] scanout stage domain refused: notification=%08X interval=%u pending=%d samples=%u\n",
                notification,interval,state->scanout_pending,state->native_surfaces.plan.samples);
            native_scanout_trace_failure(state,notification,"stage-domain",0,NULL);return 0;
        }
        HRESULT hr=S_OK;
        if(!state->scanout_display.swap)
            hr=fut_native_display_open(&state->scanout_display,state->device,
                state->native_surfaces.plan.width,state->native_surfaces.plan.height,1);
        if(hr==S_OK) hr=fut_native_display_stage(&state->scanout_display,state->context,
            state->native_surfaces.color,2000,backend_stop);
        if(hr!=S_OK) {
            fprintf(stderr,"[GRAPHICS] scanout staging failed: HRESULT=%08lX\n",hr);
            native_scanout_trace_failure(state,notification,"display-stage",0,NULL);return 0;
        }
        const char *capture=getenv("RECOMP_CAPTURE_PRESENTED_FRAMES");
        static unsigned capture_count;
        static ULONGLONG capture_at;
        ULONGLONG capture_now=capture && !strcmp(capture,"1")?GetTickCount64():0;
        unsigned long capture_interval=1000u;
        if(capture_now) {
            const char *interval=getenv("RECOMP_FRAME_CAPTURE_INTERVAL_MS");
            if(interval) {
                char *end=NULL;
                capture_interval=strtoul(interval,&end,10);
                if(!interval[0] || !end || *end || capture_interval<1000u || capture_interval>10000u) {
                    fputs("[FRAME-CAPTURE] invalid interval; use 1000..10000ms\n",stderr);
                    return 0;
                }
            }
        }
        state->capture_pending=0;
        if(capture_now && state->native_surfaces.plan.width==640u &&
            state->native_surfaces.plan.height==480u) {
            hr=fut_native_surfaces_capture_bmp(&state->native_surfaces,state->device,
                state->context,&state->native_constants,native_pending_capture_path,backend_stop);
            if(hr==S_OK) {
                state->capture_pending=1;
                state->capture_pending_physical=state->native_surfaces.plan.color_address&0x7FFFFFFFu;
            } else {
                fprintf(stderr,"[FRAME-CAPTURE] pending capture failed: hr=%08lX; latest image cannot be confirmed for this frame\n",hr);
            }
        } else if(capture_now) {
            fprintf(stderr,"[FRAME-CAPTURE] pending capture skipped: size=%ux%u; fixed latest capture supports 640x480 only\n",
                state->native_surfaces.plan.width,state->native_surfaces.plan.height);
        }
        if(capture && !strcmp(capture,"1") && capture_count<64u &&
            (capture_count<8u || capture_now-capture_at>=capture_interval)) {
            char path[80];
            snprintf(path,sizeof(path),"native-staged-frame-%03u.bmp",capture_count+1u);
            /* The fixed pending file is the same staged source; avoid a second
             * GPU readback when retaining a periodic archive of this frame. */
            if(state->capture_pending)
                hr=CopyFileA(native_pending_capture_path,path,FALSE)?S_OK:HRESULT_FROM_WIN32(GetLastError());
            else
                hr=fut_native_surfaces_capture_bmp(&state->native_surfaces,state->device,
                    state->context,&state->native_constants,path,backend_stop);
            if(hr!=S_OK) {
                fprintf(stderr,"[FRAME-CAPTURE] failed: hr=%08lX; no presentation claim\n",hr);
                return 0;
            }
            ++capture_count;
            capture_at=capture_now;
            fprintf(stderr,"[FRAME-CAPTURE] capture=%u file=%s size=%ux%u color=%08X notification=%08X; receipt pending\n",
                capture_count,path,state->native_surfaces.plan.width,
                state->native_surfaces.plan.height,state->native_surfaces.plan.color_address,notification);
        }
    }
    uint32_t observed_context,observed_words[FUT_SCANOUT_CONTEXT_WORDS];
    if(native_scanout_context(&observed_context,observed_words) &&
        observed_words[0x18C/4]==0x00191340u &&
        !fut_native_scanout_dispatch_init(&state->scanout_dispatch)) {
        fputs("[GRAPHICS] original scanout guest context unavailable (private stack/TIB)\n",stderr);
        native_scanout_trace_failure(state,notification,"dispatch-init",observed_context,observed_words);
        return 0;
    }
    ULONGLONG deadline=GetTickCount64()+2000;
    for(;;) {
        if(backend_stop && WaitForSingleObject(backend_stop,0)!=WAIT_TIMEOUT) return 0;
        if(fut_guest_interrupt_try_enter()) break;
        if(GetTickCount64()>=deadline) {
            fputs("[GRAPHICS] scanout interrupt gate timed out; no queue mutation\n",stderr);
            native_scanout_trace_failure(state,notification,"interrupt-gate-timeout",0,NULL);
            return 0;
        }
        Sleep(1);
    }
    int result=native_scanout_enqueue_locked(state,notification);
    fut_guest_interrupt_leave();
    if(!result) native_scanout_trace_failure(state,notification,"locked-enqueue-return",0,NULL);
    if(result && state->host_vblank.state) {
        HRESULT hr=fut_native_display_submit(&state->scanout_display,(notification>>5)&7u);
        if(hr!=S_OK) {
            fprintf(stderr,"[GRAPHICS] scanout submit failed: HRESULT=%08lX; no flip-read advance\n",hr);
            native_scanout_trace_failure(state,notification,"display-submit",0,NULL);
            return 0;
        }
        if(state->scanout_callback_pending)
            fputs("[GRAPHICS] resolved surface submitted to DXGI; original callback/enqueue waits for its matching receipt\n",stderr);
        else
            fputs("[GRAPHICS] original queued surface submitted to DXGI; matching receipt and original ISR retirement still required\n",stderr);
    }
    return result;
}

static int native_fragment_cache_enabled(void)
{
    const char *vertex=getenv("RECOMP_VERTEX_SHADER_CACHE");
    const char *fragment=getenv("RECOMP_FRAGMENT_SHADER_CACHE");
    return !(vertex && !strcmp(vertex,"0")) && !(fragment && !strcmp(fragment,"0"));
}
static int native_prepare_fragment(NativeGraphicsState *state)
{
    ID3D11PixelShader *shader=NULL;ID3DBlob *bytecode=NULL;
    int cache_hit=0;
    HRESULT hr=fut_native_fragment_compile_cached(state->device,&state->kelvin,state->textures,
        native_fragment_cache_enabled()?&state->fragment_cache:NULL,&shader,&bytecode,&cache_hit);
    if(FAILED(hr)) {
        fprintf(stderr,"[GRAPHICS] fragment-program translation pending/failed: control=%08X HRESULT=%08lX\n",
            state->kelvin.fragment.control,hr);
        return 0;
    }
    if(state->pixel_shader) ID3D11PixelShader_Release(state->pixel_shader);
    if(state->pixel_bytecode) ID3D10Blob_Release(state->pixel_bytecode);
    state->pixel_shader=shader;state->pixel_bytecode=bytecode;state->kelvin.fragment.dirty=0;
    ID3D11DeviceContext_PSSetShader(state->context,shader,NULL,0);
    fprintf(stderr,"[GRAPHICS] register-combiner equations %s; no draw\n",
        cache_hit?"reused from exact generated HLSL cache":"compiled to native pixel shader");
    return 1;
}

static int object_memory_read(void *context, uint32_t address, void *data, size_t size)
{
    (void)context;
    return read_guest(address, data, size);
}

static void native_capture_observed_yuy2(NativeGraphicsState *state,
    const FutGraphicsTextureState *texture,const FutGraphicsDmaView *dma)
{
    const char *enabled=getenv("RECOMP_CAPTURE_LC_IMAGE");
    if(!enabled || strcmp(enabled,"1")) return;
    uint8_t *bytes=HeapAlloc(GetProcessHeap(),0,FUT_GRAPHICS_OBSERVED_YUY2_BYTES);
    if(!bytes) {
        fputs("[GRAPHICS] cannot allocate isolated YUY2 capture buffer; draw remains unsupported\n",stderr);return;
    }
    if(!fut_graphics_texture_capture_observed_yuy2(object_memory_read,NULL,texture,dma,bytes)) {
        fputs("[GRAPHICS] observed YUY2 capture rejected by exact read-only scope; draw remains unsupported\n",stderr);
        HeapFree(GetProcessHeap(),0,bytes);return;
    }
    const char *capture_path=texture->offset==0x02487680u?
        "graphics-lc-yuy2-02487680.bin":"graphics-lc-yuy2.bin";
    FILE *capture=fopen(capture_path,"wb");
    if(!capture) {
        fputs("[GRAPHICS] cannot create isolated YUY2 capture; draw remains unsupported\n",stderr);
        HeapFree(GetProcessHeap(),0,bytes);return;
    }
    size_t written=fwrite(bytes,1,FUT_GRAPHICS_OBSERVED_YUY2_BYTES,capture);
    int closed=fclose(capture)==0;
    if(written!=FUT_GRAPHICS_OBSERVED_YUY2_BYTES || !closed)
        fputs("[GRAPHICS] observed YUY2 capture write incomplete; draw remains unsupported\n",stderr);
    else fprintf(stderr,"[GRAPHICS] read-only observed YUY2 texture capture: bytes=%u offset=%08X width=640 height=480 pitch=1280 file=%s; no format translation or draw\n",
        FUT_GRAPHICS_OBSERVED_YUY2_BYTES,texture->offset,capture_path);
    if(written==FUT_GRAPHICS_OBSERVED_YUY2_BYTES && closed) {
        const char *diagnostic_path=texture->offset==0x02487680u?
            "graphics-lc-yuy2-02487680-combiner.txt":"graphics-lc-yuy2-combiner.txt";
        FILE *diagnostic=fopen(diagnostic_path,"wb");
        if(!diagnostic) {
            fputs("[GRAPHICS] cannot create isolated combiner diagnostic; draw remains unsupported\n",stderr);
        } else {
            const FutGraphicsFragmentState *f=&state->kelvin.fragment;
            fprintf(diagnostic,"scope=observed-lc-yuy2-read-only\n"
                "texture format=%08X offset=%08X address=%08X control0=%08X control1=%08X filter=%08X image_rect=%08X valid=%03X\n"
                "fragment valid=%X control=%08X stage_program=%08X dot_rgbmapping=%08X dirty=%d\n"
                "final_words=%08X/%08X final_factor_valid=%X final_factor=%08X/%08X\n"
                "color_input_valid=%02X alpha_input_valid=%02X color_output_valid=%02X alpha_output_valid=%02X\n"
                "factor_valid=%02X/%02X\n",
                texture->format,texture->offset,texture->address,texture->control0,texture->control1,
                texture->filter,texture->image_rect,texture->valid,
                f->valid,f->control,f->stage_program,f->dot_rgbmapping,f->dirty,
                f->final_words[0],f->final_words[1],f->final_factor_valid,f->final_factor[0],f->final_factor[1],
                f->color_input_valid,f->alpha_input_valid,f->color_output_valid,f->alpha_output_valid,
                f->factor_valid[0],f->factor_valid[1]);
            for(unsigned i=0;i<8;++i)
                fprintf(diagnostic,"stage[%u] color_input=%08X alpha_input=%08X color_output=%08X alpha_output=%08X factor0=%08X factor1=%08X\n",
                    i,f->color_input[i],f->alpha_input[i],f->color_output[i],f->alpha_output[i],f->factors[0][i],f->factors[1][i]);
            char *hlsl=HeapAlloc(GetProcessHeap(),0,65536);
            size_t hlsl_length=0;
            int translated=hlsl && fut_fragment_program_hlsl(f,hlsl,65536,&hlsl_length);
            fprintf(diagnostic,"hlsl_translation=%s length=%zu\n",translated?"accepted":"declined",translated?hlsl_length:0);
            if(translated) fwrite(hlsl,1,hlsl_length,diagnostic);
            if(hlsl) HeapFree(GetProcessHeap(),0,hlsl);
            int diagnostic_error=ferror(diagnostic);
            int diagnostic_close=fclose(diagnostic);
            int diagnostic_ok=diagnostic_error==0 && diagnostic_close==0;
            if(diagnostic_ok)
                fputs("[GRAPHICS] read-only combiner/HLSL diagnostic captured; shader not compiled or submitted, draw remains unsupported\n",stderr);
            else
                fputs("[GRAPHICS] combiner diagnostic write incomplete; draw remains unsupported\n",stderr);
        }
    }
    HeapFree(GetProcessHeap(),0,bytes);
}

static void native_capture_observed_lines(NativeGraphicsState *state)
{
    if(!getenv("RECOMP_CAPTURE_OBSERVED_LINES")) return;
    const unsigned refs=(1u<<7)|(1u<<8);
    FutGraphicsDmaView view_a,view_b;
    uint8_t bytes[96];
    if((state->bindings.reference_masks[0]&refs)!=refs ||
        state->kelvin.vertex_array_format_valid!=0xFFFFu ||
        !fut_graphics_dma_surface_view(&state->bindings.references[0][7],&view_a) ||
        !fut_graphics_dma_surface_view(&state->bindings.references[0][8],&view_b) ||
        !fut_graphics_vertex_array_capture_observed_lines(object_memory_read,NULL,
            &view_a,&view_b,state->kelvin.vertex_array_format[0],
            state->kelvin.vertex_array_offset[0],bytes)) {
        fputs("[GRAPHICS] observed LINES vertex capture rejected by exact read-only scope; draw remains unsupported\n",stderr);
        return;
    }
    FILE *capture=fopen("graphics-line-vertices.bin","wb");
    if(!capture) {
        fputs("[GRAPHICS] cannot create isolated observed LINES vertex capture; draw remains unsupported\n",stderr);
        return;
    }
    size_t written=fwrite(bytes,1,sizeof(bytes),capture);
    int closed=fclose(capture)==0;
    if(written!=sizeof(bytes) || !closed) {
        fputs("[GRAPHICS] observed LINES vertex capture write incomplete; draw remains unsupported\n",stderr);
        return;
    }
    fprintf(stderr,"[GRAPHICS] read-only observed LINES vertex capture: bytes=%zu base=%08X offset=%08X format=%08X vertices=16; no draw or completion\n",
        sizeof(bytes),view_a.guest_address,state->kelvin.vertex_array_offset[0],state->kelvin.vertex_array_format[0]);
}

static int native_graphics_stage_timing_enabled(void);
static uint64_t native_graphics_stage_timing_begin(void);
static void native_graphics_stage_timing_end(uint64_t started,const char *phase,
    unsigned subchannel,uint32_t method,uint32_t value,size_t offset);

static int native_prepare_aa2_programs(NativeGraphicsState *state)
{
    int projective=!state->constant_coordinates && (state->projective_coordinates&1u) &&
        fut_graphics_projective_aa2_approximation_enabled();
    if(!state->constant_coordinates && !projective) {
        fprintf(stderr,"[GRAPHICS] AA2 coordinate certificate refused: vertices=%u affine_mask=%X projective_mask=%X opt_in=%d; no draw\n",
            state->native_immediate.count,state->constant_coordinates,state->projective_coordinates,
            fut_graphics_projective_aa2_approximation_enabled());return 0;
    }
    state->aa2_projective=projective;
    unsigned first=state->kelvin.program_start,count=0;
    while(first+count<136 && state->kelvin.program_valid[first+count]==15) {
        ++count;if(state->kelvin.program[first+count-1][3]&1u) break;
    }
    ID3D11VertexShader *vs=NULL;ID3DBlob *vblob=NULL;
    ID3D11PixelShader *ps=NULL;ID3DBlob *pblob=NULL;
    int timing=native_graphics_stage_timing_enabled();
    const char *cache_option=getenv("RECOMP_VERTEX_SHADER_CACHE");
    int cache_enabled=!(cache_option && cache_option[0]=='0' && cache_option[1]=='\0');
    int cached_vertex=0;
    uint64_t stage_started=timing?native_graphics_stage_timing_begin():0;
    HRESULT hr=fut_native_vertex_compile_screen_cached(state->device,state->kelvin.program+first,
        count,1,cache_enabled?&state->vertex_cache:NULL,&vs,&vblob,&cached_vertex);
    if(cached_vertex) {
        fprintf(stderr,"[GRAPHICS] original AA2 vertex program reused from exact instruction cache: start=%u instructions=%u\n",
            first,count);
    } else {
        if(timing) native_graphics_stage_timing_end(stage_started,"aa2-vertex-compile",0,0,0,0);
    }
    if(SUCCEEDED(hr)) {
        stage_started=timing?native_graphics_stage_timing_begin():0;
        int fragment_cache_hit=0;
        if(projective)
            hr=fut_native_fragment_compile_aa2_projective(state->device,&state->kelvin,
                state->textures,1,native_fragment_cache_enabled()?&state->fragment_cache:NULL,
                &ps,&pblob,&fragment_cache_hit);
        else if(native_fragment_cache_enabled())
            hr=fut_native_fragment_compile_aa2_affine_cached(state->device,&state->kelvin,
                state->textures,1,&state->fragment_cache,&ps,&pblob,&fragment_cache_hit);
        else
            hr=fut_native_fragment_compile_aa2_affine(state->device,&state->kelvin,
                state->textures,1,&ps,&pblob);
        if(timing) native_graphics_stage_timing_end(stage_started,"aa2-fragment-compile",0,0,0,0);
        if(fragment_cache_hit)
            fprintf(stderr,"[GRAPHICS] original AA2 pixel program reused from exact generated HLSL cache\n");
    }
    if(SUCCEEDED(hr)) {
        stage_started=timing?native_graphics_stage_timing_begin():0;
        hr=fut_native_fragment_linkage(vblob,pblob);
        if(timing) native_graphics_stage_timing_end(stage_started,"aa2-fragment-linkage",0,0,0,0);
    }
    if(FAILED(hr)) {
        if(vs) ID3D11VertexShader_Release(vs);if(vblob) ID3D10Blob_Release(vblob);
        if(ps) ID3D11PixelShader_Release(ps);if(pblob) ID3D10Blob_Release(pblob);
        fprintf(stderr,"[GRAPHICS] AA2 shader preparation failed: HRESULT=%08lX\n",hr);return 0;
    }
    if(state->aa2_vertex_shader) ID3D11VertexShader_Release(state->aa2_vertex_shader);
    if(state->aa2_vertex_bytecode) ID3D10Blob_Release(state->aa2_vertex_bytecode);
    if(state->pixel_shader) ID3D11PixelShader_Release(state->pixel_shader);
    if(state->pixel_bytecode) ID3D10Blob_Release(state->pixel_bytecode);
    state->aa2_vertex_shader=vs;state->aa2_vertex_bytecode=vblob;
    state->pixel_shader=ps;state->pixel_bytecode=pblob;
    state->kelvin.fragment.dirty=0;
    return 1;
}

static int native_prepare_draw(NativeGraphicsState *state)
{
    int timing=native_graphics_stage_timing_enabled();
    uint64_t stage_started=timing?native_graphics_stage_timing_begin():0;
    int prepared=native_prepare_surfaces(state);
    if(timing) native_graphics_stage_timing_end(stage_started,"draw-surfaces",0,0,0,0);
    if(!prepared) return 0;
    stage_started=timing?native_graphics_stage_timing_begin():0;
    prepared=native_prepare_textures(state);
    if(timing) native_graphics_stage_timing_end(stage_started,"draw-textures",0,0,0,0);
    if(!prepared) return 0;
    int aa2=state->native_surfaces.plan.samples==2;
    unsigned candidates=0;
    for(unsigned stage=0;stage<4;++stage) if(state->textures[stage].zero_rgb_candidate) candidates|=1u<<stage;
    if(candidates) {
        stage_started=timing?native_graphics_stage_timing_begin():0;
        prepared=native_prepare_fragment(state);
        if(timing) native_graphics_stage_timing_end(stage_started,"draw-fragment-specialize",0,0,0,0);
        if(!prepared) return 0;
        fprintf(stderr,"[GRAPHICS] zero-RGB specialization compiled: stages=%X; no candidate texture/alpha resources remain; generic quincunx unsupported\n",candidates);
    }
    int software_ready=aa2?
        fut_graphics_software_aa2_draw_ready(&state->software,&state->kelvin,&state->native_surfaces.plan):
        fut_graphics_software_linear_draw_ready(&state->software,&state->kelvin,&state->native_surfaces.plan);
    int projective_format=(state->kelvin.textures[0].format&~0x0FFF0000u)==0x00000629u ||
        ((state->kelvin.textures[0].format&~0x0FFF0000u)==0x00000B29u &&
         (((state->kelvin.textures[0].format>>16)&15u)>1 || state->kelvin.textures[0].filter==0x02063F1Bu ||
          (((state->kelvin.textures[0].format>>16)&15u)==1 && state->kelvin.textures[0].filter==0x02062000u && state->kelvin.textures[0].address==0x10101u))) ||
        ((state->kelvin.textures[0].format&~0x0FFF0000u)==0x00000E29u &&
         (((state->kelvin.textures[0].format>>16)&15u)>1 || state->kelvin.textures[0].filter==0x02063F1Bu)) ||
        state->kelvin.textures[0].format==0x09A30F29u ||
        ((state->kelvin.textures[0].format&~0x0FFF0000u)==0x00000C29u &&
         state->kelvin.textures[0].filter==0x02063F1Bu);
    if(aa2 && projective_format && state->surfaces.format!=0x1128u)
        software_ready=0;
    if(aa2 && (state->kelvin.textures[0].format==0x09A10E29u ||
        (fut_graphics_bc2_aa2_family_approximation_enabled() &&
         (state->kelvin.textures[0].format&~0x0FF00000u)==0x00010E29u)) &&
        state->surfaces.format!=0x00001128u) software_ready=0;
    if(aa2 && fut_graphics_p8_aa2_family_approximation_enabled() &&
        (state->kelvin.textures[0].format&~0x0FF00000u)==0x00010B29u &&
        state->surfaces.format!=0x00001128u) software_ready=0;
    const FutGraphicsTextureState *yuy2_texture=&state->kelvin.textures[0];
    int yuy2_second=yuy2_texture->offset==0x02487680u;
    int yuy2_first=yuy2_texture->offset==0x023F1580u;
    int yuy2_aa2_enabled=yuy2_first?
        fut_graphics_yuy2_aa2_approximation_enabled():
        yuy2_second?fut_graphics_yuy2_second_aa2_approximation_enabled():
        fut_graphics_yuy2_family_aa2_approximation_enabled();
    int yuy2_bt601_enabled=yuy2_first?
        fut_graphics_yuy2_bt601_approximation_enabled():
        yuy2_second?fut_graphics_yuy2_second_bt601_approximation_enabled():
        fut_graphics_yuy2_family_bt601_approximation_enabled();
    if(aa2 && yuy2_texture->format==0x00012429u &&
        (yuy2_aa2_enabled || yuy2_bt601_enabled) &&
        (!yuy2_aa2_enabled || !yuy2_bt601_enabled || state->surfaces.format!=0x00001128u))
        software_ready=0;
    if (!software_ready) {
        fprintf(stderr,"[GRAPHICS] original draw blocked: software graphics controls outside restricted translated domain; noise_valid=%u dxt1_noise=%08X RDI=%08X:%08X,%08X:%08X pgraph_registers_valid=%X reg400094=%08X reg400B80=%08X\n",
            state->software.noise_valid,state->software.dxt1_noise,state->software.rdi_index[0],state->software.rdi_value[0],
            state->software.rdi_index[1],state->software.rdi_value[1],state->software.register_valid,
            state->software.register_words[0],state->software.register_words[1]);
        fprintf(stderr,"[GRAPHICS] blocked draw domain: depth=%u stencil=%u depth_write=%u compress_depth=%u samples=%u format=%08X control0=%08X dither=%u anti_alias_control=%08X stage_program=%08X\n",
            state->kelvin.depth_test,state->kelvin.stencil_test,state->kelvin.draw.depth_write,
            state->kelvin.compress_depth,state->native_surfaces.plan.samples,state->surfaces.format,
            state->kelvin.control0,state->kelvin.draw.dither,state->kelvin.anti_aliasing_control,state->kelvin.fragment.stage_program);
        for(unsigned stage=0;stage<4;++stage) {
        const FutGraphicsTextureState *blocked_texture=&state->kelvin.textures[stage];
        fprintf(stderr,"[GRAPHICS] blocked texture%u state: valid=%X offset=%08X format=%08X address=%08X control0=%08X control1=%08X filter=%08X image_rect=%08X surface=%ux%u logical=%ux%u\n",stage,
            blocked_texture->valid,blocked_texture->offset,blocked_texture->format,
            blocked_texture->address,blocked_texture->control0,blocked_texture->control1,
            blocked_texture->filter,blocked_texture->image_rect,
            state->native_surfaces.plan.width,state->native_surfaces.plan.height,
            state->native_surfaces.plan.logical_width,state->native_surfaces.plan.logical_height);
        }
        FutGraphicsRopControls rop;
        if(fut_graphics_software_rop_controls(&state->software,&rop))
            fprintf(stderr,"[GRAPHICS] decoded XDK ROP controls: compare_always_read=%u depth_read=%u dont_cull_uncompressed=%u baseline=%08X/%08X; other baseline effects unmodeled\n",
                rop.compare_always_read,rop.depth_read,rop.dont_cull_uncompressed,rop.baseline[0],rop.baseline[1]);
        return 0;
    }
    if(aa2 && state->kelvin.textures[0].format==0x09910C29u &&
        fut_graphics_dxt1_noise_approximation_enabled())
        fputs("[GRAPHICS] AUTHORIZED_DXT1_NOISE_APPROXIMATION: BC1 standard decoding, unknown NV2A noise omitted; exact 512x512/640x480 AA2 scope; NOT_READY\n",stderr);
    if(aa2 && projective_format)
        fputs("[GRAPHICS] AUTHORIZED_PROJECTIVE_AA2_APPROXIMATION: rational pixel-center interpolation, original signed W and mip chain, logical-pixel gradients; NV2A precision/AA2 fidelity unverified NOT_READY\n",stderr);
    if(aa2 && projective_format) for(unsigned stage=1;stage<4;++stage) {
        const FutGraphicsTextureState *t=&state->kelvin.textures[stage];
        if(t->control0&0x40000000u)
            fprintf(stderr,"[GRAPHICS] AUTHORIZED_PROJECTIVE_SECONDARY_TEXTURE_APPROXIMATION: stage=%u format=%08X levels=%u sampled=%u; independent original mip chain/sampler, standard BC decoding with unknown NV2A BC1 noise omitted; AA2/filtering fidelity unverified NOT_READY\n",
                stage,t->format,(t->format>>16)&15u,((state->kelvin.fragment.stage_program>>(stage*5u))&31u)!=0);
    }
    if(aa2 && projective_format && (state->kelvin.textures[0].format&0xFF00u)==0x0C00u)
        fputs("[GRAPHICS] AUTHORIZED_PROJECTIVE_BC1_NOISE_APPROXIMATION: standard BC1 bytes/mips, unknown NV2A noise omitted; scoped projective experiment NOT_READY\n",stderr);
    if(aa2 && state->kelvin.textures[0].format==0x05510B29u &&
        fut_graphics_p8_aa2_approximation_enabled())
        fputs("[GRAPHICS] AUTHORIZED_P8_AA2_APPROXIMATION: lossless palette expansion, existing AA2 baseline approximation; exact 32x32/640x480 scope; NOT_READY\n",stderr);
    if(aa2 && state->kelvin.textures[0].format==0x05710B29u &&
        fut_graphics_p8_128x32_aa2_approximation_enabled())
        fputs("[GRAPHICS] AUTHORIZED_P8_128X32_AA2_APPROXIMATION: lossless palette expansion, existing AA2 and depth-write mappings; exact 128x32/640x480 captured state; compressed-depth and NV2A AA2 fidelity unverified; NOT_READY\n",stderr);
    if(aa2 && state->kelvin.textures[0].format==0x06610E29u &&
        fut_graphics_bc2_aa2_approximation_enabled() &&
        state->kelvin.textures[0].offset==0x00D1F980u &&
        state->kelvin.textures[0].control1==0x14000000u &&
        state->kelvin.textures[0].image_rect==0x050001E0u)
        fputs("[GRAPHICS] AUTHORIZED_BC2_AA2_APPROXIMATION: standard BC2 decode, FORMAT dimensions 64x64; exact captured offset/retained pitch/rectangle and 640x480 AA2 state; BC2 filtering, compress-depth and NV2A fidelity unverified; NOT_READY\n",stderr);
    if(aa2 && state->kelvin.textures[0].format==0x09A10E29u &&
        fut_graphics_bc2_c9df00_aa2_approximation_enabled())
        fputs("[GRAPHICS] AUTHORIZED_BC2_C9DF00_AA2_APPROXIMATION: exact offset=00C9DF00 FORMAT=09A10E29 1024x512 BC2, pitch=4096; existing 640x480 AA2 and depth-write mappings; filtering/compress-depth/NV2A fidelity unverified; NOT_READY\n",stderr);
    if(aa2 && fut_graphics_untextured_aa2_approximation_enabled() &&
        state->kelvin.textures[0].control0==0 && state->kelvin.fragment.stage_program==0)
        fprintf(stderr,"[GRAPHICS] AUTHORIZED_UNTEXTURED_AA2_APPROXIMATION: disabled texture stages; no retained resource sampling; stencil=%u func=%08X ref=%08X read=%08X write=%08X ops=%08X/%08X/%08X depth_func=%08X; AA2/depth/stencil fidelity unverified NOT_READY\n",
            state->kelvin.stencil_test,state->kelvin.draw.stencil_func,state->kelvin.draw.stencil_ref,
            state->kelvin.draw.stencil_read,state->kelvin.draw.stencil_write,state->kelvin.draw.stencil_op[0],
            state->kelvin.draw.stencil_op[1],state->kelvin.draw.stencil_op[2],state->kelvin.draw.depth_func);
    if(aa2 && fut_graphics_bc2_aa2_family_approximation_enabled() &&
        (state->kelvin.textures[0].control0&0x40000000u) &&
        (state->kelvin.textures[0].format&~0x0FF00000u)==0x00010E29u &&
        state->kelvin.textures[0].valid==0x3BFu && state->kelvin.draw.dither==1)
        fprintf(stderr,"[GRAPHICS] AUTHORIZED_BC2_AA2_FAMILY_APPROXIMATION: format=%08X offset=%08X texture=%ux%u depth_write=%u address=%08X; one-mip BC2 under observed 640x480 AA2/sampler baseline; filtering/compress-depth/NV2A fidelity unverified; NOT_READY\n",
            state->kelvin.textures[0].format,state->kelvin.textures[0].offset,
            state->textures[0].plan.width,state->textures[0].plan.height,
            state->kelvin.draw.depth_write,state->kelvin.textures[0].address);
    if(aa2 && fut_graphics_p8_aa2_family_approximation_enabled() &&
        (state->kelvin.textures[0].format&~0x0FF00000u)==0x00010B29u &&
        state->kelvin.draw.depth_write<=1 && state->kelvin.draw.dither==1 &&
        state->kelvin.textures[0].valid==0x3BFu)
        fprintf(stderr,"[GRAPHICS] AUTHORIZED_P8_AA2_FAMILY_APPROXIMATION: format=%08X offset=%08X texture=%ux%u depth_write=%u; lossless palette expansion under observed AA2 baseline; NV2A fidelity unverified; NOT_READY\n",
            state->kelvin.textures[0].format,state->kelvin.textures[0].offset,
            state->textures[0].plan.width,state->textures[0].plan.height,state->kelvin.draw.depth_write);
    if(aa2 && yuy2_texture->format==0x00012429u && yuy2_aa2_enabled && yuy2_bt601_enabled &&
        (yuy2_first || yuy2_second))
        fprintf(stderr,"[GRAPHICS] AUTHORIZED_YUY2_%sAA2_APPROXIMATION: exact captured 640x480 state using existing host AA2 mapping and BT.601 candidate; depth/compress-depth and NV2A fidelity unverified; NOT_READY\n",
            yuy2_second?"02487680_":"");
    if(aa2 && yuy2_texture->format==0x00012429u &&
        fut_graphics_yuy2_family_aa2_approximation_enabled() &&
        fut_graphics_yuy2_family_bt601_approximation_enabled())
        fprintf(stderr,"[GRAPHICS] AUTHORIZED_YUY2_AA2_FAMILY_APPROXIMATION: format=%08X offset=%08X 640x480 bounded YUY2 buffer; BT.601 and existing host AA2 mappings are approximations; depth/compress-depth and NV2A fidelity unverified; NOT_READY\n",
            yuy2_texture->format,yuy2_texture->offset);
    if(state->software.noise_valid || state->software.register_valid) fprintf(stderr,aa2?
        "[GRAPHICS] AUTHORIZED_AA2_APPROXIMATION: observed selector8/9 baseline treated as default; no NV2A equivalence claimed\n":
        "[GRAPHICS] recognized selector-8/9 controls admitted only in the restricted single-sample/no-depth/no-stencil domain; no NV2A equivalence claimed\n");
    if ((state->kelvin.program_mode & 3u)!=2u) {
        fprintf(stderr,"[GRAPHICS] fixed-function vertex execution remains untranslated; refusing stale programmable shader\n");
        return 0;
    }
    if (!native_prepare_vertex(state)) return 0;
    if(aa2) {
        stage_started=timing?native_graphics_stage_timing_begin():0;
        prepared=native_prepare_aa2_programs(state);
        if(timing) native_graphics_stage_timing_end(stage_started,"draw-aa2-programs",0,0,0,0);
        if(!prepared) return 0;
    } else if(!candidates) {
        stage_started=timing?native_graphics_stage_timing_begin():0;
        prepared=native_prepare_fragment(state);
        if(timing) native_graphics_stage_timing_end(stage_started,"draw-fragment-program",0,0,0,0);
        if(!prepared) return 0;
    }
    stage_started=timing?native_graphics_stage_timing_begin():0;
    HRESULT linkage=fut_native_fragment_linkage(aa2?state->aa2_vertex_bytecode:state->vertex_bytecode,state->pixel_bytecode);
    if(timing) native_graphics_stage_timing_end(stage_started,"draw-shader-linkage",0,0,0,0);
    if(FAILED(linkage)) {
        fprintf(stderr,"[GRAPHICS] VS/PS compiled signature mismatch: HRESULT=%08lX; no rasterized draw\n",linkage);
        return 0;
    }
    if (!fut_graphics_draw_ready(&state->kelvin.draw)) {
        fprintf(stderr,"[GRAPHICS] incomplete native draw state at use\n");
        return 0;
    }
    HRESULT hr=fut_native_pipeline_apply(&state->pipeline,state->device,state->context,
        &state->kelvin,&state->native_surfaces.plan);
    if (FAILED(hr)) {
        fprintf(stderr,"[GRAPHICS] native draw-state preparation failed: HRESULT=%08lX\n",hr);
        fprintf(stderr,"[GRAPHICS] draw raster controls: dither=%u alpha=%u logic=%u stipple=%u line_smooth=%u polygon_smooth=%u front=%08X back=%08X cull=%u/%08X offset=%u,%u,%u factor=%08X bias=%08X\n",
            state->kelvin.draw.dither,state->kelvin.draw.alpha_test,state->kelvin.draw.logic_op,
            state->kelvin.draw.stipple,state->kelvin.draw.line_smooth,state->kelvin.draw.polygon_smooth,
            state->kelvin.draw.front_mode,state->kelvin.draw.back_mode,state->kelvin.draw.cull_enable,
            state->kelvin.draw.cull_face,state->kelvin.draw.offset_enable[0],state->kelvin.draw.offset_enable[1],
            state->kelvin.draw.offset_enable[2],state->kelvin.draw.offset_factor,state->kelvin.draw.offset_bias);
        fprintf(stderr,"[GRAPHICS] draw pipeline controls: program_mode=%08X point_params=%u point_smooth=%u fog=%u lighting=%u specular=%u light_control=%08X light_mask=%08X texture_matrices=%u,%u,%u,%u blend=%u/%08X/%08X/%08X alpha=%08X/%u\n",
            state->kelvin.program_mode,state->kelvin.point_params_enable,state->kelvin.point_smooth,
            state->kelvin.fog_enable,state->kelvin.lighting_enable,state->kelvin.specular_enable,state->kelvin.light_control,
            state->kelvin.light_enable_mask,state->kelvin.texture_matrix_enable[0],
            state->kelvin.texture_matrix_enable[1],state->kelvin.texture_matrix_enable[2],state->kelvin.texture_matrix_enable[3],
            state->kelvin.draw.blend_enable,state->kelvin.draw.blend_src,state->kelvin.draw.blend_dst,state->kelvin.draw.blend_op,
            state->kelvin.draw.alpha_func,state->kelvin.draw.alpha_ref);
        return 0;
    }
    state->kelvin.draw.dirty=0;
    if(state->kelvin.specular_enable)
        fprintf(stderr,"[GRAPHICS] programmable secondary color prepared: original oD1 RGB, alpha_from_program=%u light_control=%08X; finite host color clamp, NV2A exceptional/precision behavior unverified; no draw/completion yet\n",
            (state->kelvin.light_control&0x00020000u)!=0,state->kelvin.light_control);
    if(state->kelvin.draw.dither) fprintf(stderr,aa2?
        "[GRAPHICS] AUTHORIZED_AA2_APPROXIMATION: ARGB8 dither uses host UNORM conversion without added noise; no NV2A hardware validation\n":
        "[GRAPHICS] ACCEPTED_APPROXIMATION: ARGB8 single-sample dither uses host UNORM conversion without added noise; no NV2A hardware validation\n");
    return 1;
}

static int native_prepare_immediate(NativeGraphicsState *state)
{
    state->constant_coordinates=0;
    state->projective_coordinates=0;state->aa2_projective=0;
    state->quincunx_coordinates=0;
    if(!native_prepare_surfaces(state)) return 0;
    HRESULT hr=fut_native_vertex_surface_prepare(&state->vertex_surface,state->device,state->context,
        &state->native_surfaces.plan);
    if(FAILED(hr)) {
        fprintf(stderr,"[GRAPHICS] vertex screen-space preparation failed: HRESULT=%08lX\n",hr);return 0;
    }
    if ((state->kelvin.program_mode&3u)!=2u || !native_prepare_vertex(state) ||
        !state->vertex_shader || !state->vertex_bytecode) return 0;
    unsigned first=state->kelvin.program_start,count=0;
    while(first+count<136 && state->kelvin.program_valid[first+count]==15) {
        ++count;
        if(state->kelvin.program[first+count-1][3]&1u) break;
    }
    uint16_t required=0;
    if(!fut_vertex_program_input_mask(state->kelvin.program+first,count,&required)) return 0;
    hr=fut_native_immediate_upload(&state->native_immediate,state->device,state->context,
        state->vertex_bytecode,&state->immediate,required);
    if(FAILED(hr)) {
        fprintf(stderr,"[GRAPHICS] immediate vertex upload refused: count=%u required=%04X HRESULT=%08lX\n",
            state->immediate.count,required,hr);
        return 0;
    }
    fprintf(stderr,"[GRAPHICS] immediate vertices uploaded: count=%u required=%04X; no draw/completion\n",
        state->immediate.count,required);
    if(!fut_native_constants_finish(&state->native_constants,
        state->kelvin.constants_dirty?state->kelvin.constants:NULL,2000,backend_stop)) return 0;
    state->kelvin.constants_dirty=0;
    FutNativePosition positions[FUT_IMMEDIATE_DRAW_CAPACITY];
    hr=fut_native_immediate_probe_positions(&state->native_immediate,state->device,state->context,
        state->vertex_shader,state->vertex_bytecode,&state->native_constants,positions,FUT_IMMEDIATE_DRAW_CAPACITY,2000,backend_stop);
    if(FAILED(hr)) {
        fprintf(stderr,"[GRAPHICS] vertex-only execution/readback failed: HRESULT=%08lX\n",hr);
        const char *capture=getenv("RECOMP_GRAPHICS_SNAPSHOT");
        if(capture && !strcmp(capture,"1")) {
            FILE *file=fopen("graphics-vertex-rejected-input.bin","wb");
            if(file) {fwrite(state->immediate.vertices,sizeof(state->immediate.vertices[0]),state->immediate.count,file);fclose(file);}
            file=fopen("graphics-vertex-rejected-program.bin","wb");
            if(file) {fwrite(state->kelvin.program+first,sizeof(state->kelvin.program[0]),count,file);fclose(file);}
            file=fopen("graphics-vertex-rejected-constants.bin","wb");
            if(file) {fwrite(state->kelvin.constants,sizeof(state->kelvin.constants),1,file);fclose(file);}
        }
        return 0;
    }
    state->constant_coordinates=fut_native_immediate_constant_coordinates(positions,state->native_immediate.count);
    state->projective_coordinates=fut_native_immediate_projective_coordinates(positions,state->native_immediate.count);
    state->quincunx_coordinates=fut_native_immediate_quincunx_coordinates(positions,state->native_immediate.count);
    if(!state->constant_coordinates && !(state->projective_coordinates&1u)) {
        const char *capture=getenv("RECOMP_GRAPHICS_SNAPSHOT");
        if(capture && !strcmp(capture,"1")) {
            FILE *file=fopen("graphics-coordinate-rejected-so.bin","wb");
            if(file){fwrite(positions,sizeof(*positions),state->native_immediate.count,file);fclose(file);}
            file=fopen("graphics-coordinate-rejected-input.bin","wb");
            if(file){fwrite(state->immediate.vertices,sizeof(state->immediate.vertices[0]),state->immediate.count,file);fclose(file);}
            file=fopen("graphics-coordinate-rejected-program.bin","wb");
            if(file){fwrite(state->kelvin.program+first,sizeof(state->kelvin.program[0]),count,file);fclose(file);}
            file=fopen("graphics-coordinate-rejected-constants.bin","wb");
            if(file){fwrite(state->kelvin.constants,sizeof(state->kelvin.constants),1,file);fclose(file);}
        }
    }
    fprintf(stderr,"[GRAPHICS] original vertex program executed with no rasterized stream: vertices=%u; no pixels/submission completion\n",
        state->native_immediate.count);
    for(unsigned i=0;i<state->native_immediate.count && i<3;++i) fprintf(stderr,
        "[GRAPHICS] vertex%u screen=(%g,%g,%g,%g) clip=(%g,%g,%g,%g)\n",i,
        positions[i].screen[0],positions[i].screen[1],positions[i].screen[2],positions[i].screen[3],
        positions[i].clip[0],positions[i].clip[1],positions[i].clip[2],positions[i].clip[3]);
    if(state->kelvin.textures[0].filter==0x04072000u)
        for(unsigned i=0;i<state->native_immediate.count && i<3;++i)
            fprintf(stderr,"[GRAPHICS] convolution coordinate probe vertex%u t0=(%g,%g,%g,%g) certificate=%d clip_exact=(%a,%a,%a,%a)\n",i,
                positions[i].texture[0][0],positions[i].texture[0][1],
                positions[i].texture[0][2],positions[i].texture[0][3],state->quincunx_coordinates,
                positions[i].clip[0],positions[i].clip[1],positions[i].clip[2],positions[i].clip[3]);
    return 1;
}

static int native_graphics_stage_timing_enabled(void)
{
    const char *value=getenv("RECOMP_GRAPHICS_STAGE_TIMING");
    return value && !strcmp(value,"1");
}

static uint64_t native_graphics_stage_timing_begin(void)
{
    LARGE_INTEGER value={0};
    return QueryPerformanceCounter(&value) && value.QuadPart>0?
        (uint64_t)value.QuadPart:0;
}

static void native_graphics_stage_timing_end(uint64_t started,const char *phase,
    unsigned subchannel,uint32_t method,uint32_t value,size_t offset)
{
    if(!started) return;
    LARGE_INTEGER finished={0},frequency={0};
    if(!QueryPerformanceCounter(&finished) ||
        !QueryPerformanceFrequency(&frequency) || frequency.QuadPart<=0 ||
        finished.QuadPart<(LONGLONG)started) return;
    uint64_t elapsed_us=(uint64_t)(finished.QuadPart-(LONGLONG)started)*1000000u/
        (uint64_t)frequency.QuadPart;
    if(elapsed_us>=20000u)
        fprintf(stderr,"[GRAPHICS-TIME] phase=%s duration_us=%llu thread=%lu sub=%u method=%04X value=%08X offset=%zu\n",
            phase,(unsigned long long)elapsed_us,(unsigned long)GetCurrentThreadId(),
            subchannel,method,value,offset);
}

static int native_method(void *context, unsigned subchannel, uint32_t method,
                         uint32_t value, size_t offset)
{
    NativeGraphicsState *state = context;
    if(state->vertex_array.active && (subchannel!=0 ||
        (method!=0x1800u && method!=0x1808u && method!=0x1810u && method!=0x1818u && method!=0x17FCu))) {
        fprintf(stderr,"[GRAPHICS] command refused inside array triangle strip: sub=%u method=%04X value=%08X\n",subchannel,method,value);
        return 0;
    }
    /* Only the observed attribute form is supported inside a primitive.
     * Reject before setters/binding mutation; no stale-state draw snapshots. */
    if (state->immediate.ready || (state->immediate.active && state->immediate.input_kind==2 &&
        (subchannel!=0 || (method!=0x1818u && method!=0x17FCu))) || (state->immediate.active &&
        (subchannel!=0 || (method!=0x17FCu && method!=0x1818u && (method<0x1518u || method>0x1524u) &&
            (method<0x1880u || method>=0x1900u) &&
            (method<0x1940u || method>=0x1980u))))) {
        fprintf(stderr,"[GRAPHICS] command refused inside/pending immediate primitive: sub=%u method=%04X value=%08X offset=%zu primitive=%u active=%u ready=%u kind=%u vertices=%u pending=%u/%u/%u format_valid=%04X formats=",
            subchannel,method,value,offset,state->immediate.primitive,state->immediate.active,
            state->immediate.ready,state->immediate.input_kind,state->immediate.count,
            state->immediate.pending_x,state->immediate.pending_position,state->immediate.inline_pending,
            state->kelvin.vertex_array_format_valid);
        for(unsigned slot=0;slot<16;++slot)
            fprintf(stderr,"%s%08X",slot?",":"",state->kelvin.vertex_array_format[slot]);
        fputs("; no draw/completion\n",stderr);
        return 0;
    }
    FutGraphicsBindingStatus binding = fut_graphics_apply_binding(&state->bindings,
        subchannel, method, value);
    if (binding == FUT_BINDING_APPLIED) {
        if (method == 0) {
            const FutGraphicsObject *object = &state->bindings.objects[subchannel];
            fprintf(stderr, "[GRAPHICS] bound sub=%u handle=%08X class=%02X instance=%08X\n",
                    subchannel, value, (unsigned)(object->words[0] & 0xFFu), object->instance);
        } else {
            fprintf(stderr, "[GRAPHICS] binding state sub=%u method=%04X value=%08X\n",
                    subchannel, method, value);
        }
        return 1;
    }
    if (binding == FUT_BINDING_REJECTED) {
        fprintf(stderr, "[GRAPHICS] invalid object/context binding: sub=%u method=%04X value=%08X\n",
                subchannel, method, value);
        return 0;
    }
    if (subchannel == 0 && method == 0x1D6Cu &&
        (state->bindings.objects[0].words[0] & 0xFFu) == 0x97u &&
        (state->bindings.reference_masks[0] & (1u << 9))) {
        FutGraphicsDmaView view;
        if ((value & 3u) || !fut_graphics_dma_view(
                &state->bindings.references[0][9], &view) ||
            value > view.size || view.size - value < 4) {
            fprintf(stderr, "[GRAPHICS] unsupported semaphore descriptor/offset=%08X\n", value);
            return 0;
        }
        state->semaphore_offset = value;
        state->semaphore_view = view;
        fprintf(stderr, "[GRAPHICS] semaphore destination=%08X size=%u; no release performed\n",
                view.guest_address + value, view.size);
        return 1;
    }
    const unsigned class_id = state->bindings.objects[subchannel].words[0] & 0xFFu;
    if (class_id == 0x44u && method == 0x310u) {
        /* NV044 MONOCHROME_COLOR0 -> NV20 PGRAPH PATTERN_MONO_COLOR0.
         * The title reuses that register as a FIFO fence observation. Publish
         * the actual command word only after prior native work has finished. */
        if(!native_finish(state) || !write32(0xFD400B10u,value)) return 0;
        state->kelvin.pattern_color = value;
        fprintf(stderr,"[GRAPHICS] pattern color0 committed after native GPU completion: sub=%u value=%08X register=FD400B10\n",subchannel,value);
        return 1;
    }
    if (class_id == 0x97u) {
        if(subchannel==0 && method==0x17FCu && value==2u)
            native_capture_observed_lines(state);
        if(subchannel==0 && method==0x17FCu && value==6u) {
            if(state->immediate.active || state->immediate.ready ||
                !fut_graphics_vertex_array_begin(&state->vertex_array,value)) {
                fputs("[GRAPHICS] indexed triangle-strip begin refused\n",stderr);return 0;
            }
            fputs("[GRAPHICS] triangle-strip begin captured; indexed and inline forms remain restricted to observed layouts\n",stderr);
            return 1;
        }
        if(subchannel==0 && method==0x17FCu && value==2u) {
            if((!fut_graphics_lines_16segment_aa2_approximation_enabled() &&
                !fut_graphics_lines_4segment_aa2_approximation_enabled()) ||
                state->immediate.active || state->immediate.ready ||
                !fut_graphics_vertex_array_begin_observed_lines(&state->vertex_array)) {
                fputs("[GRAPHICS] observed LINES begin refused; exact opt-in and idle state required\n",stderr);return 0;
            }
            fputs("[GRAPHICS] authorized captured AA2 LINELIST experiment began; endpoint count checked at END; D3D11 coverage is approximate\n",stderr);
            return 1;
        }
        if(subchannel==0 && method==0x1818u && state->vertex_array.active) {
            if(!fut_graphics_vertex_array_inline(&state->vertex_array,value)) {
                fprintf(stderr,"[GRAPHICS] INLINE_ARRAY word refused: value=%08X words=%u\n",value,state->vertex_array.inline_count);return 0;
            }
            return 1;
        }
        if(subchannel==0 && method==0x1810u && state->vertex_array.active) {
            if(!fut_graphics_vertex_array_draw_arrays(&state->vertex_array,value)) {
                fprintf(stderr,"[GRAPHICS] DRAW_ARRAYS range refused: word=%08X first=%u count=%u records=%u mode=%u; no draw/completion\n",
                    value,value&0x00FFFFFFu,(value>>24)+1u,state->vertex_array.count,state->vertex_array.input_mode);
                return 0;
            }
            fprintf(stderr,"[GRAPHICS] DRAW_ARRAYS range captured: first=%u count=%u total_records=%u; no vertex fetch/draw/completion yet\n",
                value&0x00FFFFFFu,(value>>24)+1u,state->vertex_array.count);
            return 1;
        }
        if(subchannel==0 && method==0x1800u && state->vertex_array.active) {
            if(!fut_graphics_vertex_array_indices(&state->vertex_array,value)) {
                fprintf(stderr,"[GRAPHICS] ARRAY_ELEMENT16 refused: value=%08X count=%u\n",value,state->vertex_array.count);return 0;
            }
            return 1;
        }
        if(subchannel==0 && method==0x1808u && state->vertex_array.active) {
            if(!fut_graphics_vertex_array_index32(&state->vertex_array,value)) {
                fprintf(stderr,"[GRAPHICS] ARRAY_ELEMENT32 refused: value=%08X count=%u\n",value,state->vertex_array.count);return 0;
            }
            return 1;
        }
        if(subchannel==0 && method==0x17FCu && value==0u && state->vertex_array.active) {
            if(!fut_graphics_vertex_array_end(&state->vertex_array)) {
                fprintf(stderr,"[GRAPHICS] indexed primitive end refused: primitive=%u mode=%u indices=%u inline_words=%u\n",
                    state->vertex_array.primitive,
                    state->vertex_array.input_mode,state->vertex_array.count,state->vertex_array.inline_count);return 0;
            }
            if(state->vertex_array.primitive==2u) {
                unsigned first=state->kelvin.program_start,count=0;
                while(first+count<136 && state->kelvin.program_valid[first+count]==15) {
                    ++count;if(state->kelvin.program[first+count-1][3]&1u) break;
                }
                uint16_t required=0;
                int built=fut_graphics_lines_aa2_count_enabled(state->vertex_array.count) &&
                    fut_vertex_program_input_mask(state->kelvin.program+first,count,&required);
                FutGraphicsDmaView view_a,view_b;const unsigned refs=(1u<<7)|(1u<<8);
                int has_refs=(state->bindings.reference_masks[0]&refs)==refs;
                int has_a=has_refs && fut_graphics_dma_surface_view(&state->bindings.references[0][7],&view_a);
                int has_b=has_refs && fut_graphics_dma_surface_view(&state->bindings.references[0][8],&view_b);
                built=built && has_a && has_b && fut_graphics_vertex_array_build_observed_lines(
                    &state->vertex_array,&state->immediate,state->bindings.read,state->bindings.memory_context,
                    &view_a,&view_b,state->kelvin.vertex_array_format,state->kelvin.vertex_array_offset,
                    state->kelvin.attributes,state->kelvin.attribute_valid,required);
                if(!built) {
                    fprintf(stderr,"[GRAPHICS] authorized LINES fetch refused: refs=%u/%u/%u required=%04X indices=%u format0=%08X offset0=%08X constant0_valid=%u; no draw/completion\n",
                        has_refs,has_a,has_b,required,state->vertex_array.count,
                        state->kelvin.vertex_array_format[0],state->kelvin.vertex_array_offset[0],state->kelvin.attribute_valid[0]);return 0;
                }
                fprintf(stderr,"[GRAPHICS] observed LINES fetched transactionally: endpoints=%u format0=%08X offset0=%08X; no draw/completion yet\n",
                    state->immediate.count,state->kelvin.vertex_array_format[0],state->kelvin.vertex_array_offset[0]);
                if(state->native_surfaces.plan.samples!=2 || !native_prepare_immediate(state) || !native_prepare_draw(state)) {
                    fputs("[GRAPHICS] exact LINES experiment requires prepared captured AA2/P8/depth state; no draw/completion\n",stderr);return 0;
                }
                unsigned drawn=state->immediate.count;
                HRESULT hr=fut_native_draw_immediate_aa2_lines(&state->native_immediate,&state->immediate,
                    &state->native_surfaces,&state->pipeline,&state->kelvin,&state->software,
                    state->device,state->context,state->aa2_vertex_shader,state->pixel_shader,
                    &state->native_constants,object_memory_read,object_memory_write,NULL,backend_stop,
                    state->constant_coordinates!=0);
                if(FAILED(hr)) {
                    fprintf(stderr,"[GRAPHICS] authorized LINES LINELIST draw/completion refused: HRESULT=%08lX; no completion claimed\n",hr);return 0;
                }
                if(!fut_graphics_vertex_array_consume(&state->vertex_array)) return 0;
                fprintf(stderr,"[GRAPHICS] AUTHORIZED_CAPTURED_AA2_LINELIST_APPROXIMATION: vertices=%u IA_vertices=%llu PS_invocations=%llu guest_surface_writeback=complete; D3D11 line coverage/depth compression not NV2A-validated; no present/VBlank\n",
                    drawn,(unsigned long long)state->native_immediate.ia_vertices,
                    (unsigned long long)state->native_immediate.ps_invocations);
                return native_preview_original_draw(state);
            }
            unsigned first=state->kelvin.program_start,count=0;
            while(first+count<136 && state->kelvin.program_valid[first+count]==15) {
                ++count;if(state->kelvin.program[first+count-1][3]&1u) break;
            }
            uint16_t required=0;
            int built=fut_vertex_program_input_mask(state->kelvin.program+first,count,&required);
            if(built && state->vertex_array.input_mode==2) {
                built=fut_graphics_vertex_array_build_inline(&state->vertex_array,&state->immediate,
                    state->kelvin.vertex_array_format,state->kelvin.attributes,
                    state->kelvin.attribute_valid,required);
            } else if(built && (state->vertex_array.input_mode==1 || state->vertex_array.input_mode==3)) {
                FutGraphicsDmaView view_a,view_b;const unsigned refs=(1u<<7)|(1u<<8);
                int has_refs=(state->bindings.reference_masks[0]&refs)==refs;
                /* These exact handles resolve to class 0x3D NVM objects. Reuse
                 * the existing NVM aperture resolver, which bounds its 128 MiB
                 * descriptor to the supported 64 MiB guest mapping. */
                int has_a=has_refs && fut_graphics_dma_surface_view(&state->bindings.references[0][7],&view_a);
                int has_b=has_refs && fut_graphics_dma_surface_view(&state->bindings.references[0][8],&view_b);
                if(has_a && has_b) fprintf(stderr,
                    "[GRAPHICS] indexed strip DMA diagnostic: A=%08X/%08X B=%08X/%08X format0=%08X offset0=%08X indices=%u,%u,%u,%u\n",
                    view_a.guest_address,view_a.size,view_b.guest_address,view_b.size,
                    state->kelvin.vertex_array_format[0],state->kelvin.vertex_array_offset[0],
                    state->vertex_array.indices[0],state->vertex_array.indices[1],
                    state->vertex_array.indices[2],state->vertex_array.indices[3]);
                else {
                    const FutGraphicsObject *aobj=&state->bindings.references[0][7];
                    const FutGraphicsObject *bobj=&state->bindings.references[0][8];
                    fprintf(stderr,"[GRAPHICS] indexed strip DMA diagnostic: refs=%u viewA=%u viewB=%u mask=%08X A(handle=%08X descriptor=%08X words=%08X/%08X/%08X/%08X) B(handle=%08X descriptor=%08X words=%08X/%08X/%08X/%08X)\n",
                        has_refs,has_a,has_b,state->bindings.reference_masks[0],
                        aobj->handle,aobj->descriptor,aobj->words[0],aobj->words[1],aobj->words[2],aobj->words[3],
                        bobj->handle,bobj->descriptor,bobj->words[0],bobj->words[1],bobj->words[2],bobj->words[3]);
                }
                built=has_a && has_b && fut_graphics_vertex_array_build(&state->vertex_array,&state->immediate,
                    state->bindings.read,state->bindings.memory_context,&view_a,&view_b,
                    state->kelvin.vertex_array_format,state->kelvin.vertex_array_offset,
                    state->kelvin.attributes,state->kelvin.attribute_valid,required);
            } else built=0;
            if(!built) {
                fprintf(stderr,"[GRAPHICS] observed triangle-strip fetch refused: mode=%u indices=%u inline_words=%u required=%04X; no draw/completion\n",
                    state->vertex_array.input_mode,state->vertex_array.count,state->vertex_array.inline_count,required);return 0;
            }
            fprintf(stderr,"[GRAPHICS] observed triangle strip fetched transactionally: mode=%u source_records=%u inline_words=%u expanded_vertices=%u; no pixels yet\n",
                state->vertex_array.input_mode,state->vertex_array.count,state->vertex_array.inline_count,state->immediate.count);
            if(!native_prepare_immediate(state) || !native_prepare_draw(state)) return 0;
            unsigned drawn=state->immediate.count;HRESULT hr;
            if(state->native_surfaces.plan.samples==2 && state->aa2_projective)
                hr=fut_native_draw_immediate_aa2_projective(&state->native_immediate,&state->immediate,
                    &state->native_surfaces,&state->pipeline,&state->kelvin,&state->software,
                    state->device,state->context,state->aa2_vertex_shader,state->pixel_shader,
                    &state->native_constants,object_memory_read,object_memory_write,NULL,backend_stop,(state->projective_coordinates&1u)!=0);
            else if(state->native_surfaces.plan.samples==2) hr=fut_native_draw_immediate_aa2(&state->native_immediate,&state->immediate,
                &state->native_surfaces,&state->pipeline,&state->kelvin,&state->software,
                state->device,state->context,state->aa2_vertex_shader,state->pixel_shader,
                &state->native_constants,object_memory_read,object_memory_write,NULL,backend_stop,state->constant_coordinates!=0);
            else hr=fut_native_draw_immediate(&state->native_immediate,&state->immediate,
                &state->native_surfaces,&state->pipeline,&state->kelvin,&state->software,
                state->device,state->context,state->vertex_shader,state->pixel_shader,
                &state->native_constants,object_memory_read,object_memory_write,NULL,backend_stop);
            if(FAILED(hr)) {
                fprintf(stderr,"[GRAPHICS] triangle-strip draw/completion refused: HRESULT=%08lX; no completion claimed\n",hr);return 0;
            }
            if(!fut_graphics_vertex_array_consume(&state->vertex_array)) return 0;
            fprintf(stderr,"[GRAPHICS] triangle-strip draw completed with guest surface writeback: vertices=%u IA_vertices=%llu PS_invocations=%llu; no present/VBlank\n",
                drawn,(unsigned long long)state->native_immediate.ia_vertices,(unsigned long long)state->native_immediate.ps_invocations);
            return native_preview_original_draw(state);
        }
        if (subchannel==0 && (method==0x17FCu || method==0x1818u ||
            (method>=0x1518u && method<=0x1524u) ||
            (method>=0x1880u && method<0x1900u))) {
            FutImmediateStatus status=fut_graphics_immediate_method(&state->immediate,&state->kelvin,method,value);
            if(status==FUT_IMMEDIATE_APPLIED) return 1;
            if(status!=FUT_IMMEDIATE_READY) {
                fprintf(stderr,"[GRAPHICS] immediate method refused: method=%04X value=%08X offset=%zu primitive=%u active=%u ready=%u kind=%u vertices=%u pending=%u/%u/%u format_valid=%04X formats=",
                    method,value,offset,state->immediate.primitive,state->immediate.active,
                    state->immediate.ready,state->immediate.input_kind,state->immediate.count,
                    state->immediate.pending_x,state->immediate.pending_position,state->immediate.inline_pending,
                    state->kelvin.vertex_array_format_valid);
                for(unsigned slot=0;slot<16;++slot)
                    fprintf(stderr,"%s%08X",slot?",":"",state->kelvin.vertex_array_format[slot]);
                fputs("; no draw/completion\n",stderr);
                return 0;
            }
            int timing=native_graphics_stage_timing_enabled();
            uint64_t stage_started=timing?native_graphics_stage_timing_begin():0;
            int prepared=native_prepare_immediate(state);
            if(timing) native_graphics_stage_timing_end(stage_started,"prepare-vertices",subchannel,method,value,offset);
            if(!prepared) return 0;
            stage_started=timing?native_graphics_stage_timing_begin():0;
            prepared=native_prepare_draw(state);
            if(timing) native_graphics_stage_timing_end(stage_started,"prepare-draw-state",subchannel,method,value,offset);
            if(!prepared) return 0;
            unsigned count=state->immediate.count;
            HRESULT hr;
            stage_started=timing?native_graphics_stage_timing_begin():0;
            if(state->native_surfaces.plan.samples==2 && state->aa2_projective)
                hr=fut_native_draw_immediate_aa2_projective(&state->native_immediate,&state->immediate,
                    &state->native_surfaces,&state->pipeline,&state->kelvin,&state->software,
                    state->device,state->context,state->aa2_vertex_shader,state->pixel_shader,
                    &state->native_constants,object_memory_read,object_memory_write,NULL,backend_stop,(state->projective_coordinates&1u)!=0);
            else if(state->native_surfaces.plan.samples==2) hr=fut_native_draw_immediate_aa2(&state->native_immediate,&state->immediate,
                &state->native_surfaces,&state->pipeline,&state->kelvin,&state->software,
                state->device,state->context,state->aa2_vertex_shader,state->pixel_shader,
                &state->native_constants,object_memory_read,object_memory_write,NULL,backend_stop,state->constant_coordinates!=0);
            else hr=fut_native_draw_immediate(&state->native_immediate,&state->immediate,
                &state->native_surfaces,&state->pipeline,&state->kelvin,&state->software,
                state->device,state->context,state->vertex_shader,state->pixel_shader,
                &state->native_constants,object_memory_read,object_memory_write,NULL,backend_stop);
            if(timing) native_graphics_stage_timing_end(stage_started,"draw-completion-writeback",subchannel,method,value,offset);
            if(FAILED(hr)) {
                fprintf(stderr,"[GRAPHICS] immediate draw/completion refused: HRESULT=%08lX submitted=%u dirty=%u; batch not consumed\n",
                    hr,state->native_immediate.submitted,state->native_surfaces.gpu_dirty);
                return 0;
            }
            fprintf(stderr,"[GRAPHICS] immediate draw completed and surfaces written to guest: vertices=%u IA_vertices=%llu PS_invocations=%llu projective=%u; no present/VBlank\n",
                count,(unsigned long long)state->native_immediate.ia_vertices,
                (unsigned long long)state->native_immediate.ps_invocations,state->aa2_projective!=0);
            if(state->native_surfaces.plan.samples==2 && fut_graphics_aa2_approximation_enabled())
                native_preview_log_source_stats(&state->native_surfaces.plan);
            if(state->textures[0].quincunx_experiment) {
                fputs("[GRAPHICS] experimental original resolve completed; inspecting 640x480 output, not a presentation receipt\n",stderr);
                native_preview_log_source_stats(&state->native_surfaces.plan);
            }
            stage_started=timing?native_graphics_stage_timing_begin():0;
            int previewed=native_preview_original_draw(state);
            native_graphics_stage_timing_end(stage_started,"preview-resolve",subchannel,method,value,offset);
            if (!previewed) return 0;
            return 1;
        }
        if (method == 0x100u && value) {
            if(value==5u) {
                if(subchannel!=0 || fut_trace_file || !native_finish(state)) return 0;
                if(!fut_native_scanout_dispatch_init(&state->scanout_dispatch)) return 0;
                ULONGLONG deadline=GetTickCount64()+2000;
                while(!fut_guest_interrupt_try_enter()) {
                    if(GetTickCount64()>=deadline || (backend_stop &&
                        WaitForSingleObject(backend_stop,0)!=WAIT_TIMEOUT)) return 0;
                    Sleep(1);
                }
                uint32_t address=0,w[119];
                int valid=native_scanout_context(&address,w) &&
                    (w[0x1A4/4]&0x00FF00FFu)==0x00040000u;
                FutNativeScanoutDispatchResult result=valid?
                    fut_native_event5_dispatch_call(&state->scanout_dispatch,address,value):
                    FUT_SCANOUT_DISPATCH_NO_CONTEXT;
                fut_guest_interrupt_leave();
                fprintf(stderr,"[GRAPHICS] original software event5 dispatcher result=%d context=%08X event=%08X; prior GPU work completed; no host event-state write\n",
                    result,address,valid?address+0x1A4u:0);
                return result==FUT_SCANOUT_DISPATCH_OK;
            }
            if((value&31u)==1u) {
                return native_scanout_enqueue(state,value);
            }
            if (!fut_graphics_software_state(&state->software, value, state->kelvin.clear_depth,
                    state->kelvin.clear_color, state->kelvin.clear_valid)) {
                fprintf(stderr,"[GRAPHICS] software notification untranslated: value=%08X selector=%u data=%u parameters=%08X/%08X valid=%X\n",
                    value,value&31,value>>5,state->kelvin.clear_depth,state->kelvin.clear_color,state->kelvin.clear_valid);
                return 0;
            }
            fprintf(stderr, "[GRAPHICS] software state retained: selector=%u data=%u; RDI-dependent draws remain gated\n",
                value & 31, value >> 5);
            return 1;
        }
        if (method == 0x1810u || method == 0x1818u || method == 0x1800u || method == 0x1808u) {
            if (!native_prepare_draw(state)) return 0;
        }
        if (method == 0x1D94u) {
            if (!native_prepare_surfaces(state)) return 0;
            HRESULT hr = fut_native_surfaces_clear(&state->native_surfaces, state->device, state->context,
                &state->native_constants, &state->kelvin, value, object_memory_read, object_memory_write, NULL, backend_stop);
            if (FAILED(hr)) { fprintf(stderr, "[GRAPHICS] native clear/readback failed: flags=%08X HRESULT=%08lX\n", value, hr); return 0; }
            fprintf(stderr, "[GRAPHICS] original clear completed with exact GPU readback and guest writeback: flags=%08X color=%08X zstencil=%08X\n",
                value, state->kelvin.clear_color, state->kelvin.clear_depth);
            return 1;
        }
        if (method == 0x110u) {
            if (value || !native_finish(state)) return 0;
            return 1; /* Wait for actual native work; no guest marker written. */
        }
        if(method==0x12Cu) {
            if(fut_graphics_kelvin_state(&state->kelvin,method,value)!=FUT_KELVIN_APPLIED) return 0;
            fprintf(stderr,"[GRAPHICS] flip write index advanced: read=%u write=%u modulo=%u; no frame presented\n",
                state->kelvin.flip_read,state->kelvin.flip_write,state->kelvin.flip_modulo);
            return 1;
        }
        if(method==0x130u) {
            int available=fut_graphics_flip_available(&state->kelvin);
            if(!value && available==0 && state->scanout_pending) {
                ULONGLONG deadline=GetTickCount64()+2000;
                if(!native_finish(state)) return 0;
                do {
                    if((backend_stop && WaitForSingleObject(backend_stop,0)!=WAIT_TIMEOUT) ||
                        !native_observe_vblank(state) || !native_scanout_service(state)) return 0;
                    available=fut_graphics_flip_available(&state->kelvin);
                    if(available!=0) break;
                    Sleep(1);
                } while(GetTickCount64()<deadline);
                if(!available) native_scanout_diagnostic(state->deferred_scanout_notification);
            }
            if(value || available!=1) {
                fprintf(stderr,"[GRAPHICS] flip stall requires valid free slot/real presentation: read=%u write=%u modulo=%u valid=%u\n",
                    state->kelvin.flip_read,state->kelvin.flip_write,state->kelvin.flip_modulo,state->kelvin.flip_valid);
                return 0;
            }
            if(!native_finish(state)) return 0;
            fprintf(stderr,"[GRAPHICS] flip write slot available; previous GPU work completed; no read-index advance/present\n");
            return 1;
        }
        if (fut_graphics_surface_state(&state->surfaces, method, value))
            return 1; /* Description only; allocation/range validation at use. */
        if (method == 0x1D70u) {
            FutGraphicsDmaView view;
            if (!(state->bindings.reference_masks[subchannel] & (1u << 9)) ||
                !fut_graphics_dma_view(&state->bindings.references[subchannel][9], &view) ||
                state->semaphore_offset > view.size || view.size - state->semaphore_offset < 4 ||
                !native_finish(state) || !write32(view.guest_address + state->semaphore_offset, value))
                return 0;
            fprintf(stderr, "[GRAPHICS] native GPU completion confirmed; semaphore=%08X value=%u\n",
                    view.guest_address + state->semaphore_offset, value);
            return 1;
        }
        FutGraphicsKelvinStatus applied = fut_graphics_kelvin_state(&state->kelvin, method, value);
        if (applied == FUT_KELVIN_APPLIED) {
            if (method >= 0xB00u && method < 0xB80u &&
                ((method - 0xB00u) / 4) % 4 == 3 && (value & 1u) &&
                !native_prepare_vertex(state))
                return 0;
            return 1;
        }
        if (applied == FUT_KELVIN_REJECTED) {
            fprintf(stderr, "[GRAPHICS] invalid/unsupported state method=%04X value=%08X\n", method, value);
            return 0;
        }
    }
    fprintf(stderr, "[GRAPHICS] native handler missing: sub=%u method=%04X "
            "value=%08X offset=%zu\n", subchannel, method, value, offset);
    return 0;
}

static int native_traced_method(void *context,unsigned subchannel,uint32_t method,
                                uint32_t value,size_t offset)
{
    const char *timing_env=getenv("RECOMP_GRAPHICS_STAGE_TIMING");
    int timing=timing_env && !strcmp(timing_env,"1");
    LARGE_INTEGER started={0};
    if(timing) QueryPerformanceCounter(&started);
    int result;
    if(!fut_trace_file) result=native_method(context,subchannel,method,value,offset);
    else {
        fut_trace_event(FUT_TRACE_METHOD,subchannel,method,value,NULL,0);
        fut_trace_event(FUT_TRACE_RESULT,0,0,(uint32_t)offset,NULL,0);
        fut_trace_active=1;
        result=native_method(context,subchannel,method,value,offset);
        fut_trace_active=0;
        fut_trace_event(FUT_TRACE_RESULT,1,0,result,NULL,0);
        if(fflush(fut_trace_file)) fut_trace_error();
    }
    if(timing) {
        LARGE_INTEGER finished={0},frequency={0};
        if(QueryPerformanceCounter(&finished) && QueryPerformanceFrequency(&frequency) &&
            frequency.QuadPart>0 && finished.QuadPart>=started.QuadPart) {
            uint64_t elapsed_us=(uint64_t)(finished.QuadPart-started.QuadPart)*1000000u/
                (uint64_t)frequency.QuadPart;
            if(elapsed_us>=20000u)
                fprintf(stderr,"[GRAPHICS-TIME] slow-method duration_us=%llu thread=%lu sub=%u method=%04X value=%08X offset=%zu result=%d\n",
                    (unsigned long long)elapsed_us,(unsigned long)GetCurrentThreadId(),
                    subchannel,method,value,offset,result);
        }
    }
    return result;
}

static int native_traced_finish(NativeGraphicsState *state)
{
    if(!fut_trace_file) return native_finish(state);
    fut_trace_event(FUT_TRACE_FINISH,0,0,0,NULL,0);
    fut_trace_active=1;
    int result=native_finish(state);
    fut_trace_active=0;
    fut_trace_event(FUT_TRACE_RESULT,1,0,result,NULL,0);
    if(fflush(fut_trace_file)) fut_trace_error();
    return result;
}

static void native_snapshot_command_changes(uint32_t start,const uint8_t *captured,size_t size)
{
    const char *enabled=getenv("RECOMP_GRAPHICS_SNAPSHOT");
    if(!enabled || strcmp(enabled,"1") || size>65536u || (size&3u)) return;
    uint8_t live[65536];
    if(!read_guest(start,live,size)) return;
    unsigned changed=0;
    for(size_t offset=0;offset<size;offset+=4) {
        uint32_t before,after;
        memcpy(&before,captured+offset,4);memcpy(&after,live+offset,4);
        if(before==after) continue;
        if(changed<16) fprintf(stderr,"[GRAPHICS-COMMAND-OBSERVATION] address=%08X snapshot=%08X live=%08X (read-only, non-atomic)\n",
            start+(uint32_t)offset,before,after);
        ++changed;
    }
    if(!changed) return;
    fprintf(stderr,"[GRAPHICS-COMMAND-OBSERVATION] batch=%08X bytes=%zu changed_words=%u; prefetch comparison only, execution fetched packets live\n",start,size,changed);
    char path[80];
    snprintf(path,sizeof(path),"graphics-mutated-%08X-snapshot.bin",start);
    FILE *file=fopen(path,"wb");
    if(file) {fwrite(captured,1,size,file);fclose(file);}
    snprintf(path,sizeof(path),"graphics-mutated-%08X-live.bin",start);
    file=fopen(path,"wb");
    if(file) {fwrite(live,1,size,file);fclose(file);}
}

typedef struct { uint32_t start; size_t size; } NativeCommandRange;

static int native_command_read(void *context, size_t offset, uint8_t *bytes, size_t size)
{
    const NativeCommandRange *range = context;
    if (offset > range->size || size > range->size - offset)
        return 0;
    return read_guest(range->start + (uint32_t)offset, bytes, size);
}

static void native_capture_queue_wrap(uint32_t start,uint32_t end,uint32_t cursor,
    uint32_t submitted,uint32_t write_ptr,uint32_t put)
{
    fprintf(stderr,"[GRAPHICS-QUEUE-WRAP] ring=[%08X,%08X) cursor=%08X submitted=%08X write_ptr=%08X PUT=%08X; refused, no completion\n",
        start,end,cursor,submitted,write_ptr,put);
    if(start<0x80000000u || end>0x84000000u || start>=end ||
        cursor<start || cursor>end || submitted<start || submitted>=cursor ||
        ((start|end|cursor|submitted)&3u)) return;
    uint8_t bytes[65536];
    const uint32_t bases[2]={cursor,start},limits[2]={end,submitted};
    const char *names[2]={"graphics-wrap-tail.bin","graphics-wrap-head.bin"};
    for(unsigned i=0;i<2;++i) {
        size_t total_size=limits[i]-bases[i];
        size_t size=total_size>sizeof(bytes)?sizeof(bytes):total_size;
        if(!size || !read_guest(bases[i],bytes,size)) {
            fprintf(stderr,"[GRAPHICS-QUEUE-WRAP] capture refused: part=%u bytes=%zu\n",i,size);
            continue;
        }
        FILE *file=fopen(names[i],"wb");
        if(!file) continue;
        size_t written=fwrite(bytes,1,size,file);int closed=fclose(file)==0;
        fprintf(stderr,"[GRAPHICS-QUEUE-WRAP] read-only non-atomic snapshot: part=%u base=%08X bytes=%zu span=%zu prefix_only=%d saved=%d; no execution/completion\n",
            i,bases[i],size,total_size,size<total_size,written==size && closed);
    }
}

static DWORD WINAPI native_worker(void *unused)
{
    (void)unused;
    ID3D11Device *device = NULL;
    ID3D11DeviceContext *context = NULL;
    D3D_FEATURE_LEVEL feature_level;
    HRESULT hr = D3D11CreateDevice(NULL, D3D_DRIVER_TYPE_HARDWARE, NULL, 0,
        NULL, 0, D3D11_SDK_VERSION, &device, &feature_level, &context);
    if (FAILED(hr)) {
        fprintf(stderr, "[GRAPHICS] hardware device unavailable: HRESULT=%08lX; trying WARP\n", hr);
        hr = D3D11CreateDevice(NULL, D3D_DRIVER_TYPE_WARP, NULL, 0,
            NULL, 0, D3D11_SDK_VERSION, &device, &feature_level, &context);
    }
    if (FAILED(hr))
        fail_backend("native D3D11 device initialization failed");
    fprintf(stderr, "[GRAPHICS] native D3D11 device initialized: feature_level=%04X\n",
            (unsigned)feature_level);
    NativeGraphicsState state = {0};
    state.device = device;
    state.context = context;
    state.diagnostic_preview_enabled = getenv("RECOMP_NATIVE_PREVIEW") != NULL;
    state.bindings.read = object_memory_read;
    if(getenv("RECOMP_GRAPHICS_TRACE")) fut_trace_open("graphics-replay.bin",0);
    if (FAILED(fut_native_constants_init(&state.native_constants, device, context)))
        fail_backend("cannot create native shader constants/completion query");

    if (getenv("RECOMP_HOST_OUTPUT_VBLANK")) {
        hr = native_vblank_start_for_device(device, &state.host_vblank);
        if (FAILED(hr)) {
            fprintf(stderr, "[VBLANK] host output source unavailable: HRESULT=%08lX\n",
                    (unsigned long)hr);
            fail_backend("requested host-output VBlank approximation is unavailable");
        }
        fputs("[VBLANK] observing attached desktop output via IDXGIOutput_WaitForVBlank; "
              "host-timing approximation only, not NV2A validation\n", stderr);
    }

    uint32_t ring_start = 0, ring_end = 0, channel = 0, cursor = 0;
    while (WaitForSingleObject(backend_stop, 5) == WAIT_TIMEOUT) {
        if(!native_observe_vblank(&state) || !native_scanout_service(&state))
            fail_backend("host VBlank or original scanout receipt service failed");
        uint32_t title_device = 0, start = 0, end = 0, write_ptr = 0;
        uint32_t title_channel = 0, put = 0;
        if (!read32(0x0029E968u, &title_device) || !title_device ||
            title_device > 0x04000000u - 0x1C28u)
            continue;
        if (!read32(title_device + 0x24u, &start) ||
            !read32(title_device + 0x28u, &end) || !read32(title_device, &write_ptr) ||
            !read32(title_device + 0x1C20u, &title_channel) ||
            title_channel != 0xFD800000u || !read32(title_channel + 0x40u, &put))
            continue;
        if (!cursor) {
            /* Initialize an empty native submission queue. There are no
             * commands between start and PUT, so no work is acknowledged.
             * The title's consumer semaphore is deliberately untouched. */
            if (!fut_graphics_empty_queue(start, end, write_ptr, put))
                continue;
            ring_start = start; ring_end = end; channel = title_channel; cursor = start;
            if (!write32(channel + 0x44u, put))
                fail_backend("cannot initialize empty native submission queue");
            fprintf(stderr, "[GRAPHICS] empty queue initialized: ring=[%08X,%08X) "
                    "cursor=%08X; no commands/fences completed\n", start, end, cursor);
            continue;
        }
        if (start != ring_start || end != ring_end || title_channel != channel)
            fail_backend("submission queue reconfiguration is not implemented");
        uint32_t submitted = put | 0x80000000u;
        if (submitted == cursor) {
            if(state.scanout_idle_report && GetTickCount64()>=state.scanout_idle_report) {
                fprintf(stderr,"[GRAPHICS] queue idle after GPU submission completion; checking deferred scanout without generating timing events\n");
                native_scanout_diagnostic(state.deferred_scanout_notification);
                state.scanout_idle_report=0;
            }
            continue;
        }
        if (submitted < ring_start || submitted > ring_end || (submitted & 3u)) {
            native_capture_queue_wrap(ring_start,ring_end,cursor,submitted,write_ptr,put);
            fail_backend("submission outside validated queue range");
        }
        int wrapped=submitted<cursor;
        uint8_t small_snapshot[65536];
        uint8_t *bytes=small_snapshot;
        size_t size = wrapped?0:submitted-cursor;
        if(!wrapped) {
            if(size>FUT_GRAPHICS_WRAP_MAX_BYTES) {
                fprintf(stderr,"[GRAPHICS] linear submission exceeds bounded capture: start=%08X end=%08X bytes=%zu limit=%u\n",
                    cursor,submitted,size,FUT_GRAPHICS_WRAP_MAX_BYTES);
                fail_backend("linear submission capture budget exceeded");
            }
            if(size>sizeof(small_snapshot)) {
                bytes=HeapAlloc(GetProcessHeap(),0,size);
                fprintf(stderr,"[GRAPHICS] bounded linear submission snapshot: start=%08X end=%08X bytes=%zu limit=%u\n",
                    cursor,submitted,size,FUT_GRAPHICS_WRAP_MAX_BYTES);
                if(!bytes) fail_backend("cannot allocate bounded submission snapshot");
            }
            if(!read_guest(cursor,bytes,size)) {
                if(bytes!=small_snapshot)HeapFree(GetProcessHeap(),0,bytes);
                fprintf(stderr,"[GRAPHICS] linear submission read failed: start=%08X end=%08X bytes=%zu\n",cursor,submitted,size);
                fail_backend("cannot read validated linear submission");
            }
        }
        FutGraphicsDecodeResult decoded;
        NativeCommandRange range = {wrapped?ring_start:cursor,wrapped?ring_end-ring_start:size};
        size_t jump_offset=SIZE_MAX;
        FutGraphicsDecodeStatus status = wrapped?
            fut_graphics_decode_wrapped_live(ring_start,ring_end,cursor,submitted,
                native_command_read,&range,native_traced_method,&state,&decoded,&jump_offset):
            fut_graphics_decode_live(size,native_command_read,&range,native_traced_method,&state,&decoded);
        if (status != FUT_GRAPHICS_DECODED) {
            FILE *capture = wrapped?NULL:fopen("graphics-rejected.bin", "wb");
            if (capture) { fwrite(bytes, 1, size, capture); fclose(capture); }
            if(wrapped) native_capture_queue_wrap(ring_start,ring_end,cursor,submitted,write_ptr,put);
            fprintf(stderr, "[GRAPHICS] submission blocked: start=%08X end=%08X "
                    "status=%d offset=%zu header=%08X bytes=%zu; completion unchanged\n",
                    cursor, submitted, status, decoded.blocked_offset, decoded.blocked_header, size);
            fail_backend("implement the reported native graphics method before continuing");
        }
        if (!native_traced_finish(&state))
            fail_backend("native completion failed");
        if(!wrapped) {
            native_snapshot_command_changes(cursor,bytes,size);
            if(bytes!=small_snapshot)HeapFree(GetProcessHeap(),0,bytes);
        }
        if (!write32(channel + 0x44u, put))
            fail_backend("native completion or submission cursor update failed");
        fprintf(stderr, "[GRAPHICS] submission completed: start=%08X end=%08X methods=%zu\n",
                cursor, submitted, decoded.methods);
        if(wrapped)
            fprintf(stderr,"[GRAPHICS] original ring jump completed after native GPU finish: jump=%08X target=%08X executed_bytes=%zu methods=%zu GET=%08X; no implicit padding execution\n",
                cursor+(uint32_t)jump_offset,ring_start,decoded.decoded_bytes,decoded.methods,put);
        cursor = submitted;
    }
    if (state.host_vblank.state) {
        hr = fut_native_vblank_close(&state.host_vblank, 2000);
        if (FAILED(hr))
            fprintf(stderr, "[VBLANK] observer remains alive after bounded close: HRESULT=%08lX\n",
                    (unsigned long)hr);
    }
    fut_native_constants_destroy(&state.native_constants);
    fut_native_display_close(&state.diagnostic_preview);
    fut_native_display_close(&state.scanout_display);
    fut_native_scanout_dispatch_destroy(&state.scanout_dispatch);
    fut_native_surfaces_destroy(&state.native_surfaces);
    for (unsigned stage=0;stage<4;++stage) fut_native_texture_destroy(&state.textures[stage]);
    fut_native_pipeline_destroy(&state.pipeline);
    fut_native_immediate_destroy(&state.native_immediate);
    fut_native_vertex_surface_destroy(&state.vertex_surface);
    if (state.vertex_shader) ID3D11VertexShader_Release(state.vertex_shader);
    if (state.vertex_bytecode) ID3D10Blob_Release(state.vertex_bytecode);
    if (state.aa2_vertex_shader) ID3D11VertexShader_Release(state.aa2_vertex_shader);
    if (state.aa2_vertex_bytecode) ID3D10Blob_Release(state.aa2_vertex_bytecode);
    fut_native_vertex_cache_destroy(&state.vertex_cache);
    fut_native_fragment_cache_destroy(&state.fragment_cache);
    if (state.pixel_shader) ID3D11PixelShader_Release(state.pixel_shader);
    if (state.pixel_bytecode) ID3D10Blob_Release(state.pixel_bytecode);
    ID3D11DeviceContext_Release(context);
    ID3D11Device_Release(device);
    if(fut_trace_file) { fclose(fut_trace_file);fut_trace_file=NULL; }
    return 0;
}

int fut_graphics_native_start(void)
{
    backend_stop = CreateEventA(NULL, TRUE, FALSE, NULL);
    if (!backend_stop)
        return 0;
    backend_thread = CreateThread(NULL, 0, native_worker, NULL, 0, NULL);
    if (!backend_thread) {
        CloseHandle(backend_stop); backend_stop = NULL;
        return 0;
    }
    return 1;
}

void fut_graphics_native_stop(void)
{
    if (backend_thread) {
        SetEvent(backend_stop);
        WaitForSingleObject(backend_thread, INFINITE);
        CloseHandle(backend_thread); backend_thread = NULL;
    }
    if (backend_stop) { CloseHandle(backend_stop); backend_stop = NULL; }
}
