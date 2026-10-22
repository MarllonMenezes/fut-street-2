#define COBJMACROS
#include "graphics_native_surfaces.h"
#include "graphics_native_constants.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#define CHECK(expr, code) do { if (!(expr)) { fprintf(stderr, "surface test failed: %d\n", code); return code; } } while (0)

typedef struct {
    uint8_t color[256], depth[256], next_color[256], next_depth[256];
    int fail;
    unsigned reads, writes;
    uint32_t fail_write_address;
} Memory;
static int memory_read(void *context, uint32_t address, void *data, size_t size)
{
    Memory *memory = context; ++memory->reads;
    if (memory->fail || size > 256) return 0;
    if (address == 0x80001000) memcpy(data, memory->color, size);
    else if (address == 0x80002000) memcpy(data, memory->depth, size);
    else if (address == 0x80003000) memcpy(data, memory->next_color, size);
    else if (address == 0x80004000) memcpy(data, memory->next_depth, size);
    else return 0;
    return 1;
}
static int memory_write(void *context, uint32_t address, const void *data, size_t size)
{
    Memory *memory = context;
    ++memory->writes;
    if (memory->fail || size > 256 || address == memory->fail_write_address) return 0;
    if (address == 0x80001000) memcpy(memory->color, data, size);
    else if (address == 0x80002000) memcpy(memory->depth, data, size);
    else if (address == 0x80003000) memcpy(memory->next_color, data, size);
    else if (address == 0x80004000) memcpy(memory->next_depth, data, size);
    else return 0;
    return 1;
}
static int verify_texture(ID3D11Device *device, ID3D11DeviceContext *context,
    FutNativeConstants *completion, ID3D11Texture2D *texture, const uint8_t *expected,
    unsigned source_pitch, int depth, uint32_t cleared, int after_clear)
{
    D3D11_TEXTURE2D_DESC desc; ID3D11Texture2D_GetDesc(texture, &desc);
    CHECK(desc.Width > 0 && desc.Width <= 8 && desc.Height == 3 && desc.SampleDesc.Count == 1, 40);
    desc.Usage = D3D11_USAGE_STAGING; desc.BindFlags = 0; desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
    ID3D11Texture2D *staging = NULL;
    HRESULT hr = ID3D11Device_CreateTexture2D(device, &desc, NULL, &staging);
    CHECK(SUCCEEDED(hr), 41);
    ID3D11DeviceContext_CopyResource(context, (ID3D11Resource *)staging, (ID3D11Resource *)texture);
    CHECK(fut_native_constants_finish(completion, NULL, 2000, NULL), 42);
    D3D11_MAPPED_SUBRESOURCE mapped;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context, (ID3D11Resource *)staging, 0, D3D11_MAP_READ, 0, &mapped)), 43);
    int correct = 1;
    for (unsigned y = 0; y < desc.Height; ++y) for (unsigned x = 0; x < desc.Width; ++x) {
        uint32_t actual, wanted;
        memcpy(&actual, (const uint8_t *)mapped.pData + (size_t)y * mapped.RowPitch + x * 4, 4);
        if (depth) actual = fut_graphics_depth_to_guest(actual);
        if (after_clear) wanted = cleared;
        else memcpy(&wanted, expected + y * source_pitch + x * 4, 4);
        if (actual != wanted) {
            fprintf(stderr, "surface pixel %u,%u got %08X expected %08X\n", x, y, actual, wanted); correct = 0;
        }
    }
    ID3D11DeviceContext_Unmap(context, (ID3D11Resource *)staging, 0);
    ID3D11Texture2D_Release(staging);
    return correct ? 0 : 44;
}
int main(void)
{
    FutGraphicsSurfaceState state = {0};
    FutGraphicsDmaView color = {0x80000000, 0x4000}, depth = color;
    FutGraphicsSurfacePlan plan = {0}, unchanged = {0};
    CHECK(fut_graphics_surface_plan(&state, &color, &depth, &plan) == FUT_SURFACE_INCOMPLETE, 1);
    const uint32_t values[] = {4u<<16,3u<<16,0x1128, (44u<<16)|40,0x1000,0x2000};
    for (unsigned i = 0; i < 6; ++i) CHECK(fut_graphics_surface_state(&state, 0x200+i*4, values[i]), 2);
    CHECK(fut_graphics_surface_plan(&state, &color, &depth, &plan) == FUT_SURFACE_READY, 3);
    CHECK(plan.width == 8 && plan.height == 3 && plan.samples == 2 &&
        plan.color_span == 112 && plan.depth_span == 120 && plan.color_address == 0x80001000 &&
        plan.depth_address == 0x80002000, 4);
    FutGraphicsSurfaceState bad = state;
    bad.pitch = (44u<<16)|28;
    CHECK(fut_graphics_surface_plan(&bad, &color, &depth, &unchanged) == FUT_SURFACE_INVALID_RANGE &&
        !memcmp(&unchanged, &(FutGraphicsSurfacePlan){0}, sizeof(unchanged)), 5);
    bad = state; bad.format = 0x1228;
    CHECK(fut_graphics_surface_plan(&bad, &color, &depth, &unchanged) == FUT_SURFACE_UNSUPPORTED, 6);
    bad = state; bad.depth_offset = bad.color_offset;
    CHECK(fut_graphics_surface_plan(&bad, &color, &depth, &unchanged) == FUT_SURFACE_UNSUPPORTED, 7);
    FutGraphicsDmaView short_view = {0x80000000,0x2060};
    CHECK(fut_graphics_surface_plan(&state, &color, &short_view, &unchanged) == FUT_SURFACE_INVALID_RANGE, 8);
    bad = state; bad.horizontal |= 1;
    CHECK(fut_graphics_surface_plan(&bad, &color, &depth, &unchanged) == FUT_SURFACE_UNSUPPORTED, 9);
    bad = state; bad.format = 0x2128;
    CHECK(fut_graphics_surface_plan(&bad, &color, &depth, &unchanged) == FUT_SURFACE_READY &&
        unchanged.samples == 4 && unchanged.width == 8 && unchanged.height == 6, 10);
    bad = state; bad.format = 0x0128;
    CHECK(fut_graphics_surface_plan(&bad, &color, &depth, &unchanged) == FUT_SURFACE_READY &&
        unchanged.samples == 1 && unchanged.width == 4 && unchanged.height == 3, 23);
    FutGraphicsSurfaceState untouched = state;
    CHECK(!fut_graphics_surface_state(&state, 0x201, 0) &&
        !fut_graphics_surface_state(&state, 0x218, 0) && !memcmp(&state, &untouched, sizeof(state)), 24);
    Memory memory = {0}; memset(memory.color, 0xCD, sizeof(memory.color)); memset(memory.depth, 0xAB, sizeof(memory.depth));
    for (unsigned y = 0; y < 3; ++y) for (unsigned x = 0; x < 8; ++x) {
        uint32_t c = 0x80000000u | (y << 16) | (x << 8) | (x+y);
        uint32_t z = (0x123456u+x+y*8)<<8 | (0xA0u+x);
        memcpy(memory.color+y*40+x*4, &c, 4); memcpy(memory.depth+y*44+x*4, &z, 4);
        CHECK(fut_graphics_depth_to_guest(fut_graphics_depth_to_native(z)) == z, 11);
    }
    Memory before = memory;
    ID3D11Device *device = NULL; ID3D11DeviceContext *context = NULL; D3D_FEATURE_LEVEL level;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL, D3D_DRIVER_TYPE_WARP, NULL, 0, NULL, 0,
        D3D11_SDK_VERSION, &device, &level, &context)), 12);
    FutNativeConstants completion;
    CHECK(SUCCEEDED(fut_native_constants_init(&completion, device, context)), 13);
    FutNativeSurfaces native = {0};
    FutGraphicsSurfacePlan invalid = plan; invalid.logical_width = UINT32_MAX;
    CHECK(fut_native_surfaces_prepare(&native, device, &invalid, memory_read, &memory) == E_INVALIDARG &&
        !native.valid && !memory.reads, 25);
    memory.fail = 1;
    CHECK(FAILED(fut_native_surfaces_prepare(&native, device, &plan, memory_read, &memory)) && !native.valid, 14);
    memory.fail = 0; memory.reads = 0;
    HRESULT hr = fut_native_surfaces_prepare(&native, device, &plan, memory_read, &memory);
    if (FAILED(hr)) fprintf(stderr, "native surface creation HRESULT=%08lX\n", (unsigned long)hr);
    CHECK(SUCCEEDED(hr) && native.valid && memory.reads == 2, 15);
    CHECK(!memcmp(memory.color, before.color, sizeof(memory.color)) &&
        !memcmp(memory.depth, before.depth, sizeof(memory.depth)), 16);
    CHECK(verify_texture(device, context, &completion, native.color, memory.color, 40, 0, 0, 0) == 0, 17);
    CHECK(verify_texture(device, context, &completion, native.depth, memory.depth, 44, 1, 0, 0) == 0, 18);
    uint32_t cpu_word = 0xDEADBEEF; memcpy(memory.color, &cpu_word, 4);
    CHECK(SUCCEEDED(fut_native_surfaces_prepare(&native, device, &plan, memory_read, &memory)) &&
        memory.reads == 4, 19);
    CHECK(verify_texture(device, context, &completion, native.color, memory.color, 40, 0, 0, 0) == 0, 45);
    ID3D11Texture2D *saved = native.color;
    FutGraphicsSurfacePlan changed = plan; changed.color_address += 4;
    CHECK(FAILED(fut_native_surfaces_prepare(&native, device, &changed, memory_read, &memory)) &&
        native.color == saved && !memcmp(&native.plan, &plan, sizeof(plan)), 20);
    const float clear[] = {17.0f/255,34.0f/255,51.0f/255,1};
    ID3D11DeviceContext_ClearRenderTargetView(context, native.target, clear);
    ID3D11DeviceContext_ClearDepthStencilView(context, native.depth_target,
        D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, 1.0f, 0x5A);
    CHECK(verify_texture(device, context, &completion, native.color, NULL, 0, 0, 0xFF112233u, 1) == 0, 21);
    CHECK(verify_texture(device, context, &completion, native.depth, NULL, 0, 1, 0xFFFFFF5Au, 1) == 0, 22);
    FutGraphicsKelvinState clear_state = {0};
    fut_graphics_kelvin_state(&clear_state, 0x1D98, 3u<<16);
    fut_graphics_kelvin_state(&clear_state, 0x1D9C, 2u<<16);
    fut_graphics_kelvin_state(&clear_state, 0x1D90, 0xAA123456u);
    fut_graphics_kelvin_state(&clear_state, 0x1D8C, 0xFFFFFF79u);
    CHECK(SUCCEEDED(fut_native_surfaces_clear(&native,device,context,&completion,&clear_state,0xF3,memory_read,memory_write,&memory,NULL)),26);
    CHECK(!native.gpu_dirty, 46);
    for (unsigned y=0;y<3;++y) for (unsigned x=0;x<8;++x) {
        uint32_t c,z;memcpy(&c,memory.color+y*40+x*4,4);memcpy(&z,memory.depth+y*44+x*4,4);
        CHECK(c==0xAA123456u && z==0xFFFFFF79u,27);
    }
    CHECK(memory.color[32]==0xCD && memory.color[72]==0xCD && memory.depth[32]==0xAB && memory.depth[76]==0xAB,28);
    Memory cleared_memory=memory;
    clear_state.clear_x=2u<<16;
    CHECK(fut_native_surfaces_clear(&native,device,context,&completion,&clear_state,0xF3,memory_read,memory_write,&memory,NULL)==E_NOTIMPL &&
        !memcmp(memory.color,cleared_memory.color,256) && !memcmp(memory.depth,cleared_memory.depth,256),29);
    clear_state.clear_x=3u<<16;
    CHECK(fut_native_surfaces_clear(&native,device,context,&completion,&clear_state,0x10,memory_read,memory_write,&memory,NULL)==E_NOTIMPL,30);
    clear_state.clear_depth=0x12345600;
    CHECK(fut_native_surfaces_clear(&native,device,context,&completion,&clear_state,1,memory_read,memory_write,&memory,NULL)==E_NOTIMPL,31);
    clear_state.clear_depth=0x58;
    CHECK(SUCCEEDED(fut_native_surfaces_clear(&native,device,context,&completion,&clear_state,2,memory_read,memory_write,&memory,NULL)),32);
    uint32_t preserved_depth;memcpy(&preserved_depth,memory.depth,4);CHECK(preserved_depth==0xFFFFFF58u,33);
    HANDLE stop=CreateEventA(NULL,TRUE,TRUE,NULL); CHECK(stop!=NULL,34);
    CHECK(fut_native_surfaces_clear(&native,device,context,&completion,&clear_state,3,memory_read,memory_write,&memory,stop)==E_ABORT,35);
    CloseHandle(stop);

    /* Real GPU mutations: dirty data cannot be thrown away by prepare, and
     * a partial guest-write failure leaves only the unwritten target dirty. */
    fut_native_surfaces_mark_dirty(&native, 3);
    ID3D11DeviceContext_ClearRenderTargetView(context, native.target, clear);
    ID3D11DeviceContext_ClearDepthStencilView(context, native.depth_target,
        D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, 1.0f, 0x5A);
    FutGraphicsSurfacePlan next_plan = plan;
    next_plan.color_address=0x80003000; next_plan.depth_address=0x80004000;
    unsigned reads = memory.reads;
    CHECK(fut_native_surfaces_prepare(&native,device,&next_plan,memory_read,&memory)==E_PENDING &&
        native.color==saved && memory.reads==reads, 47);
    stop=CreateEventA(NULL,TRUE,TRUE,NULL); CHECK(stop!=NULL,48);
    CHECK(fut_native_surfaces_writeback(&native,device,context,&completion,memory_read,memory_write,&memory,stop)==E_ABORT &&
        native.gpu_dirty==3,49);
    CloseHandle(stop);
    memory.fail_write_address=plan.depth_address;
    CHECK(FAILED(fut_native_surfaces_writeback(&native,device,context,&completion,memory_read,memory_write,&memory,NULL)) &&
        native.gpu_dirty==2 && native.color==saved && !memcmp(&native.plan,&plan,sizeof(plan)),50);
    uint32_t downloaded; memcpy(&downloaded,memory.color,4); CHECK(downloaded==0xFF112233u,51);
    memcpy(&downloaded,memory.depth,4); CHECK(downloaded==0xFFFFFF58u,52);
    CHECK(fut_native_surfaces_prepare(&native,device,&next_plan,memory_read,&memory)==E_PENDING,53);
    memory.fail_write_address=0;
    CHECK(SUCCEEDED(fut_native_surfaces_writeback(&native,device,context,&completion,memory_read,memory_write,&memory,NULL)) &&
        !native.gpu_dirty,54);
    for (unsigned y=0;y<3;++y) for (unsigned x=0;x<8;++x) {
        uint32_t c,z;memcpy(&c,memory.color+y*40+x*4,4);memcpy(&z,memory.depth+y*44+x*4,4);
        CHECK(c==0xFF112233u && z==0xFFFFFF5Au,55);
    }
    CHECK(memory.color[32]==0xCD && memory.depth[32]==0xAB,56);
    memcpy(memory.next_color,before.color,256); memcpy(memory.next_depth,before.depth,256);
    CHECK(SUCCEEDED(fut_native_surfaces_prepare(&native,device,&next_plan,memory_read,&memory)) &&
        native.plan.color_address==0x80003000 && !native.gpu_dirty,57);
    CHECK(verify_texture(device,context,&completion,native.color,memory.next_color,40,0,0,0)==0 &&
        verify_texture(device,context,&completion,native.depth,memory.next_depth,44,1,0,0)==0,58);
    /* Change packed storage width, pitch AND address, then return to the old
     * plan: the original clear data must survive, not an obsolete snapshot. */
    next_plan.width=next_plan.logical_width; next_plan.samples=1;
    next_plan.color_pitch=20; next_plan.depth_pitch=24;
    next_plan.color_span=56; next_plan.depth_span=64;
    CHECK(SUCCEEDED(fut_native_surfaces_prepare(&native,device,&next_plan,memory_read,&memory)) &&
        native.plan.width==4 && native.plan.samples==1,59);
    CHECK(verify_texture(device,context,&completion,native.color,memory.next_color,20,0,0,0)==0 &&
        verify_texture(device,context,&completion,native.depth,memory.next_depth,24,1,0,0)==0,60);
    /* Capture must omit row padding, preserve top-down pixel order and never
     * consume guest dirty state or issue guest reads/writes. */
    const char *capture_path="surface-capture-contract.bmp";
    unsigned capture_reads=memory.reads, capture_writes=memory.writes;
    native.gpu_dirty=1;
    CHECK(fut_native_surfaces_capture_bmp(&native,device,context,&completion,
        capture_path,NULL)==S_OK && native.gpu_dirty==1 &&
        memory.reads==capture_reads && memory.writes==capture_writes,64);
    FILE *capture=fopen(capture_path,"rb"); CHECK(capture!=NULL,65);
    BITMAPFILEHEADER bmp_file; BITMAPINFOHEADER bmp_info;
    CHECK(fread(&bmp_file,sizeof(bmp_file),1,capture)==1 &&
        fread(&bmp_info,sizeof(bmp_info),1,capture)==1,66);
    CHECK(bmp_file.bfType==0x4D42 && bmp_file.bfOffBits==54 && bmp_file.bfSize==102 &&
        bmp_info.biWidth==4 && bmp_info.biHeight==-3 && bmp_info.biBitCount==32 &&
        bmp_info.biSizeImage==48,67);
    uint8_t row[16];
    for(unsigned y=0;y<3;++y)
        CHECK(fread(row,1,sizeof(row),capture)==sizeof(row) &&
            !memcmp(row,memory.next_color+y*20,sizeof(row)),68);
    CHECK(fgetc(capture)==EOF && fclose(capture)==0 && remove(capture_path)==0,69);
    stop=CreateEventA(NULL,TRUE,TRUE,NULL); CHECK(stop!=NULL,70);
    CHECK(fut_native_surfaces_capture_bmp(&native,device,context,&completion,
        capture_path,stop)==E_ABORT,71);
    CloseHandle(stop);
    native.gpu_dirty=0;
    CHECK(SUCCEEDED(fut_native_surfaces_prepare(&native,device,&plan,memory_read,&memory)),61);
    CHECK(fut_native_surfaces_capture_bmp(&native,device,context,&completion,
        capture_path,NULL)==E_INVALIDARG,72);
    CHECK(verify_texture(device,context,&completion,native.color,NULL,0,0,0xFF112233u,1)==0 &&
        verify_texture(device,context,&completion,native.depth,NULL,0,1,0xFFFFFF5Au,1)==0,62);
    saved=native.color; memory.fail=1;
    CHECK(FAILED(fut_native_surfaces_prepare(&native,device,&next_plan,memory_read,&memory)) &&
        native.color==saved && !memcmp(&native.plan,&plan,sizeof(plan)),63);
    memory.fail=0;
    /* Allocation reuse must retain independent caller references and must not
     * permit invalid descriptors. Existing checks above validate fresh pixels. */
    char previous_cache[128]={0};
    const char *previous=getenv("RECOMP_READBACK_RESOURCE_CACHE");
    int had_previous=previous!=NULL;
    if(previous) snprintf(previous_cache,sizeof(previous_cache),"%s",previous);
    _putenv_s("RECOMP_READBACK_RESOURCE_CACHE","1");
    D3D11_TEXTURE2D_DESC read_desc={0};
    read_desc.Width=17; read_desc.Height=3; read_desc.MipLevels=1;
    read_desc.ArraySize=1; read_desc.Format=DXGI_FORMAT_B8G8R8A8_UNORM;
    read_desc.SampleDesc.Count=1; read_desc.Usage=D3D11_USAGE_STAGING;
    read_desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    ID3D11Texture2D *held=NULL,*again=NULL;
    uint64_t creations=completion.readback_creations, hits=completion.readback_hits;
    CHECK(fut_native_readback_texture(&completion,device,&read_desc,&held)==S_OK,73);
    CHECK(fut_native_readback_texture(&completion,device,&read_desc,&again)==S_OK &&
        held==again && completion.readback_creations==creations+1 &&
        completion.readback_hits==hits+1,74);
    ID3D11Texture2D_Release(again);
    for(unsigned i=0;i<4;++i) {
        read_desc.Width=18+i;
        CHECK(fut_native_readback_texture(&completion,device,&read_desc,&again)==S_OK,75);
        ID3D11Texture2D_Release(again);
    }
    D3D11_TEXTURE2D_DESC held_desc;
    ID3D11Texture2D_GetDesc(held,&held_desc);
    CHECK(held_desc.Width==17,76);
    read_desc.Width=17;
    CHECK(fut_native_readback_texture(&completion,device,&read_desc,&again)==S_OK &&
        held!=again,77);
    ID3D11Texture2D_Release(again);
    creations=completion.readback_creations;
    read_desc.BindFlags=D3D11_BIND_SHADER_RESOURCE; again=held;
    CHECK(fut_native_readback_texture(&completion,device,&read_desc,&again)==E_INVALIDARG &&
        !again && completion.readback_creations==creations,78);
    read_desc.BindFlags=0;
    _putenv_s("RECOMP_READBACK_RESOURCE_CACHE","0");
    ID3D11Texture2D *uncached=NULL;
    CHECK(fut_native_readback_texture(&completion,device,&read_desc,&again)==S_OK &&
        fut_native_readback_texture(&completion,device,&read_desc,&uncached)==S_OK &&
        again!=uncached && completion.readback_creations==creations+2,79);
    ID3D11Texture2D_Release(again); ID3D11Texture2D_Release(uncached);
    _putenv_s("RECOMP_READBACK_RESOURCE_CACHE",had_previous?previous_cache:"");
    fut_native_surfaces_destroy(&native); fut_native_constants_destroy(&completion);
    ID3D11Texture2D_GetDesc(held,&held_desc);
    CHECK(held_desc.Width==17,80);
    ID3D11Texture2D_Release(held);
    ID3D11DeviceContext_Release(context); ID3D11Device_Release(device);
    puts("Pitched storage: native clear, dirty writeback, CPU refresh and coherent plan transitions OK; no original draw claimed");
    return 0;
}
