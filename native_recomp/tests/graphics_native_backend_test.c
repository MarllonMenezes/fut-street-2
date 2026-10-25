/* Exercise the real method receiver with synthetic guest memory, no game
 * binary, guest CPU, submission thread or generated code execution. */
#include "../graphics_native_backend.c"

/* This fixture never dispatches original guest code; the dedicated dispatcher
 * test supplies synthetic translated routines for that separate contract. */
int fut_native_scanout_dispatch_init(FutNativeScanoutDispatch *dispatch)
{ (void)dispatch;return 0; }
void fut_native_scanout_dispatch_destroy(FutNativeScanoutDispatch *dispatch)
{ (void)dispatch; }
FutNativeScanoutDispatchResult fut_native_event5_dispatch_call(
    FutNativeScanoutDispatch *dispatch,uint32_t context,uint32_t notification)
{ (void)dispatch;(void)context;(void)notification;return FUT_SCANOUT_DISPATCH_NO_CONTEXT; }
FutNativeScanoutDispatchResult fut_native_scanout_dispatch_preflight(
    FutNativeScanoutDispatch *dispatch,uint32_t context,uint32_t notification,
    uint32_t callback)
{ (void)dispatch;(void)context;(void)notification;(void)callback;return FUT_SCANOUT_DISPATCH_NO_CONTEXT; }
FutNativeScanoutDispatchResult fut_native_scanout_dispatch_call(
    FutNativeScanoutDispatch *dispatch,uint32_t context,uint32_t notification,
    uint32_t callback,uint32_t *before,uint32_t *after)
{ (void)dispatch;(void)context;(void)notification;(void)callback;(void)before;(void)after;return FUT_SCANOUT_DISPATCH_NO_CONTEXT; }

#define CHECK(expr, code) do { if (!(expr)) { fprintf(stderr, "backend lifecycle test failed: %d\n", code); return code; } } while (0)
static uint8_t guest_memory[0x10000];
static uint32_t connected_interrupt;
uint32_t xbox_GetConnectedInterrupt(uint32_t vector)
{
    return vector==3?connected_interrupt:0;
}
int xbox_kernel_post_host_vblank(void)
{
    return 1;
}
ptrdiff_t xbox_GetMemoryOffset(void)
{
    return (ptrdiff_t)((uintptr_t)guest_memory - (uintptr_t)0x80000000u);
}
static uint32_t guest_word(unsigned offset)
{
    uint32_t word; memcpy(&word, guest_memory + offset, 4); return word;
}
static int capture_test_write(const char *path,uint32_t value)
{
    FILE *file=fopen(path,"wb");if(!file)return 0;
    int ok=fwrite(&value,sizeof(value),1,file)==1;
    return fclose(file)==0 && ok;
}
static int capture_test_read(const char *path,uint32_t expected)
{
    FILE *file=fopen(path,"rb");if(!file)return 0;
    uint32_t value=0;
    int ok=fread(&value,sizeof(value),1,file)==1 && value==expected && fgetc(file)==EOF;
    return fclose(file)==0 && ok;
}
static int verify_presented_capture_files(void)
{
#define CAPTURE_CHECK(expr) do{if(!(expr)){fprintf(stderr,"capture publication test failed at %d\n",__LINE__);goto cleanup;}}while(0)
    char previous[MAX_PATH],directory[MAX_PATH];
    DWORD length=GetCurrentDirectoryA(sizeof(previous),previous);
    if(!length || length>=sizeof(previous))return 0;
    int written=snprintf(directory,sizeof(directory),
        "D:\\projeto zeta\\artifacts\\tmp\\presented-capture-test-%lu-%llu",
        (unsigned long)GetCurrentProcessId(),(unsigned long long)GetTickCount64());
    /* Only a newly created, isolated directory may contain these fixed names. */
    if(written<=0 || written>=(int)sizeof(directory) || !CreateDirectoryA(directory,NULL))return 0;
    if(!SetCurrentDirectoryA(directory)) {RemoveDirectoryA(directory);return 0;}
    int passed=0;
    HANDLE blocked=INVALID_HANDLE_VALUE;
    NativeGraphicsState *state=calloc(1,sizeof(*state));
    CAPTURE_CHECK(state);
    const uint32_t old=0x11223344u,next=0x55667788u,physical=0x02E5C000u;
    state->scanout_plan.physical_address=physical;state->scanout_plan.next_enqueue=42;
    state->scanout_receipt.present_count=42;
    state->capture_pending_physical=physical;
    CAPTURE_CHECK(capture_test_write(native_presented_capture_path,old) &&
        capture_test_write(native_pending_capture_path,next));
    /* A stale file without a successful capture flag cannot be published. */
    native_confirm_presented_capture(state,physical,1);
    CAPTURE_CHECK(capture_test_read(native_presented_capture_path,old) &&
        capture_test_read(native_pending_capture_path,next));
    state->capture_pending=1;
    native_confirm_presented_capture(state,physical,0);
    CAPTURE_CHECK(!state->capture_pending && capture_test_read(native_presented_capture_path,old) &&
        capture_test_read(native_pending_capture_path,next));
    state->capture_pending=1;
    native_confirm_presented_capture(state,physical+16,1);
    CAPTURE_CHECK(!state->capture_pending && capture_test_read(native_presented_capture_path,old));
    state->capture_pending=1;state->scanout_plan.physical_address=physical+16;
    native_confirm_presented_capture(state,physical,1);
    CAPTURE_CHECK(!state->capture_pending && capture_test_read(native_presented_capture_path,old));
    state->scanout_plan.physical_address=physical;
    for(unsigned source=0;source<2;++source) {
        CAPTURE_CHECK(capture_test_write(native_pending_capture_path,next+source));
        state->capture_pending=1;state->scanout_receipt.source=source;
        native_confirm_presented_capture(state,physical,1);
        CAPTURE_CHECK(!state->capture_pending &&
            capture_test_read(native_presented_capture_path,next+source) &&
            GetFileAttributesA(native_pending_capture_path)==INVALID_FILE_ATTRIBUTES);
    }
    /* Missing source or a destination denying deletion preserves the previous
     * confirmed bytes. Neither error can manufacture a newer confirmed image. */
    state->capture_pending=1;
    native_confirm_presented_capture(state,physical,1);
    CAPTURE_CHECK(!state->capture_pending && capture_test_read(native_presented_capture_path,next+1));
    CAPTURE_CHECK(capture_test_write(native_pending_capture_path,old));
    blocked=CreateFileA(native_presented_capture_path,GENERIC_READ,FILE_SHARE_READ,
        NULL,OPEN_EXISTING,FILE_ATTRIBUTE_NORMAL,NULL);
    CAPTURE_CHECK(blocked!=INVALID_HANDLE_VALUE);
    state->capture_pending=1;
    native_confirm_presented_capture(state,physical,1);
    CAPTURE_CHECK(!state->capture_pending && capture_test_read(native_presented_capture_path,next+1) &&
        capture_test_read(native_pending_capture_path,old));
    passed=1;
cleanup:
    if(blocked!=INVALID_HANDLE_VALUE)CloseHandle(blocked);
    free(state);
    DeleteFileA(native_pending_capture_path);DeleteFileA(native_presented_capture_path);
    if(!SetCurrentDirectoryA(previous))passed=0;
    if(!RemoveDirectoryA(directory))passed=0;
    return passed;
#undef CAPTURE_CHECK
}
static int verify_indexed_promotion_guards(const NativeGraphicsState *state)
{
    NativeGraphicsState *guard=malloc(sizeof(*guard)),*before=malloc(sizeof(*before));
    CHECK(guard && before,171);
    for(unsigned profile=0;profile<2;++profile)for(unsigned variant=0;variant<3;++variant) {
        unsigned primitive=profile?8u:5u,method=profile?0x1810u:0x1800u;
        /* Shallow copies borrow COM resources; these paths must reject
         * before GPU work and are freed without releasing borrowed objects. */
        memcpy(guard,state,sizeof(*guard));
        memset(&guard->immediate,0,sizeof(guard->immediate));
        fut_graphics_vertex_array_reset(&guard->vertex_array);
        if(variant==2)for(unsigned slot=0;slot<16;++slot) {
            /* Create a valid pending legacy INLINE payload, independently
             * of the new DMA quad's different attribute slots. */
            unsigned format=profile?(slot==0?0xC32:slot==3?0xC40:slot==9?0xC22:0xC02):
                (slot==0?0x642:slot==3?0x640:0x602);
            CHECK(native_method(guard,0,0x1760+slot*4,format,0),172);
        }
        CHECK(native_method(guard,0,0x17FC,primitive,0),173);
        if(variant==2) {
            CHECK(native_method(guard,0,0x1818,0,0) && guard->immediate.inline_pending==1 &&
                guard->immediate.input_kind==2,174);
        } else {
            CHECK(native_method(guard,0,0x1880,0,0),175);
            if(!variant)CHECK(native_method(guard,0,0x1884,0,0) && guard->immediate.count==1,176);
            else CHECK(guard->immediate.pending_x==1 && !guard->immediate.count,177);
        }
        memcpy(before,guard,sizeof(*before));
        CHECK(!native_method(guard,0,method,profile?0x03000000u:0x00020005u,0) &&
            !memcmp(before,guard,sizeof(*guard)),178);
    }
    free(before);free(guard);return 0;
}
static int verify_draw_receiver(NativeGraphicsState *state)
{
    /* Separate synthetic fixture after the refusal tests. No state mutation
     * of a failed batch is allowed in the production receiver. */
    state->immediate=(FutGraphicsImmediate){0};
    state->kelvin=(FutGraphicsKelvinState){0};
    state->software=(FutGraphicsSoftwareState){0};
    const uint32_t commands[][2]={
        {0x2B4,0},{0x2C0,3u<<16},{0x2E0,2u<<16},
        {0x300,0},{0x304,0},{0x308,0},{0x30C,0},{0x310,0},{0x32C,0},
        {0x344,1},{0x348,0},{0x350,0x8006},{0x354,0x207},
        {0x358,0x01010101},{0x35C,1},{0x360,0xFF},{0x364,0x207},{0x368,0},{0x36C,0xFF},
        {0x370,0x1E00},{0x374,0x1E00},{0x378,0x1E00},{0x37C,0x1D01},
        {0x38C,0x1B02},{0x390,0x1B02},{0x39C,0x405},{0x3A0,0x901},
        {0x1E60,1},{0x1E70,0},{0xAC0,0x04200000},{0x260,0x14200000},
        {0x1E40,0xC00},{0xAA0,0xC00},{0x288,0xC},{0x28C,0x1C00}
    };
    for(unsigned i=0;i<sizeof(commands)/sizeof(commands[0]);++i)
        CHECK(native_method(state,0,commands[i][0],commands[i][1],0),60);
    const uint32_t program[1][4]={{0,(1u<<21)|0x1Bu,2u<<26,(15u<<12)|0x800u|1u}};
    memcpy(state->kelvin.program,program,sizeof(program));
    state->kelvin.program_valid[0]=15;state->kelvin.program_dirty=1;
    CHECK(native_method(state,0,0x1E94,6,0),61);
    uint8_t depth_before[72];memcpy(depth_before,guest_memory+0x4000,sizeof(depth_before));
    CHECK(native_method(state,0,0x17FC,5,0),62);
    const float points[3][2]={{0,0},{8,0},{0,6}};
    for(unsigned i=0;i<3;++i) {
        uint32_t x,y;memcpy(&x,&points[i][0],4);memcpy(&y,&points[i][1],4);
        CHECK(native_method(state,0,0x1880,x,0) && native_method(state,0,0x1884,y,0),63);
    }
    CHECK(native_method(state,0,0x17FC,0,0) && !state->immediate.ready &&
        !state->immediate.count && !state->native_immediate.submitted &&
        !state->native_surfaces.gpu_dirty && native_finish(state) &&
        state->native_immediate.ia_vertices==3 && state->native_immediate.ps_invocations>0,64);
    for(unsigned i=0;i<12;++i) {
        uint32_t actual=guest_word(0x3000+i*4);
        if(actual!=0xFF000000) fprintf(stderr,"synthetic receiver pixel%u=%08X\n",i,actual);
        CHECK(actual==0xFF000000,65);
    }
    CHECK(!memcmp(depth_before,guest_memory+0x4000,sizeof(depth_before)),66);
    CHECK(state->kelvin.flip_read==0 && state->kelvin.flip_write==0,67);
    connected_interrupt=0x80000080;
    uint32_t queue_context=0x80006000;memcpy(guest_memory+0x84,&queue_context,4);
    memset(guest_memory+0x6000,0,FUT_SCANOUT_CONTEXT_WORDS*4);
    uint32_t registers=0xFD000000,pitch=16;
    memcpy(guest_memory+0x6000,&registers,4);memcpy(guest_memory+0x6004,&pitch,4);
    uint8_t before_mask[FUT_SCANOUT_CONTEXT_WORDS*4];
    memcpy(before_mask,guest_memory+0x6000,sizeof(before_mask));
    fut_guest_interrupt_cli();
    ULONGLONG gate_started=GetTickCount64();
    CHECK(!native_scanout_enqueue(state,(0x3001u<<5)|1) &&
        !memcmp(before_mask,guest_memory+0x6000,sizeof(before_mask)) &&
        !fut_guest_interrupt_enabled() && GetTickCount64()-gate_started<3500,77);
    fut_guest_interrupt_sti();
    backend_stop=CreateEventA(NULL,TRUE,TRUE,NULL);CHECK(backend_stop,78);
    CHECK(!native_scanout_enqueue(state,(0x3001u<<5)|1) &&
        !memcmp(before_mask,guest_memory+0x6000,sizeof(before_mask)) &&
        fut_guest_interrupt_enabled(),79);
    CloseHandle(backend_stop);backend_stop=NULL;
    CHECK(!native_method(state,0,0x100,(0x3001u<<5)|1,0) &&
        !memcmp(before_mask,guest_memory+0x6000,sizeof(before_mask)),73);
    CHECK(fut_guest_interrupt_enabled(),80);
    CHECK(!native_method(state,0,0x100,(0x4001u<<5)|1,0) &&
        !memcmp(before_mask,guest_memory+0x6000,sizeof(before_mask)),77);
    CHECK(!native_method(state,0,0x100,(0x3001u<<5)|1,0) &&
        !memcmp(before_mask,guest_memory+0x6000,sizeof(before_mask)),74);
    CHECK(state->kelvin.flip_read==0 && state->kelvin.flip_write==0 &&
        !memcmp(depth_before,guest_memory+0x4000,sizeof(depth_before)),76);
    connected_interrupt=0;
    /* A second original-format submission exercises the real AA2 shader
     * selection, original VS position probe and two-pass draw routing. */
    CHECK(native_method(state,0,0x208,0x1128,0) &&
        native_method(state,0,0x20C,0x00200020,0) &&
        native_method(state,0,0x1D7C,0xFFFF0001,0),81);
    memset(guest_memory+0x3000,0xCD,96);
    memset(guest_memory+0x4000,0xAB,96);
    uint8_t aa_depth_before[96];memcpy(aa_depth_before,guest_memory+0x4000,96);
    CHECK(native_method(state,0,0x17FC,5,0),82);
    const float aa_points[3][2]={{0,0},{4,0},{0,3}};
    for(unsigned i=0;i<3;++i) {
        uint32_t x,y;memcpy(&x,&aa_points[i][0],4);memcpy(&y,&aa_points[i][1],4);
        CHECK(native_method(state,0,0x1880,x,0) && native_method(state,0,0x1884,y,0),83);
    }
    CHECK(native_method(state,0,0x17FC,0,0) && native_finish(state) &&
        !state->immediate.ready && !state->native_immediate.submitted &&
        !state->native_surfaces.gpu_dirty && state->native_immediate.ia_vertices==6 &&
        state->aa2_vertex_shader && state->aa2_vertex_bytecode,84);
    for(unsigned y=0;y<3;++y) for(unsigned x=0;x<8;++x) {
        double sx=x/2+((x&1)?.5:0),sy=y+((x&1)?.5:0);
        CHECK(guest_word(0x3000+y*32+x*4)==(3*sx+4*sy<12?0xFF000000:0xCDCDCDCD),85);
    }
    CHECK(!memcmp(aa_depth_before,guest_memory+0x4000,96),86);
    /* The same screen-space triangle with original MOV oPos,v0 and W=2
     * must use the projective AA2 route. NV screen outputs retain x/y; the
     * translated clip position multiplies by W before the host divide. */
    const char *inherited_projective=getenv("RECOMP_PROJECTIVE_AA2_APPROXIMATION");
    char *saved_projective=inherited_projective?_strdup(inherited_projective):NULL;
    CHECK(!inherited_projective || saved_projective,148);
    CHECK(!_putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION","1"),149);
    for(unsigned i=0;i<16;++i) CHECK(native_method(state,0,0x1760+i*4,
        i==0?0x642:i==3?0x640:0x602,0),150);
    memset(guest_memory+0x3000,0xCD,96);
    CHECK(native_method(state,0,0x17FC,5,0),151);
    for(unsigned i=0;i<3;++i) {
        float position[4]={aa_points[i][0],aa_points[i][1],0,2};uint32_t data[5];
        memcpy(data,position,16);data[4]=0xFFFF00FF;
        for(unsigned j=0;j<5;++j) CHECK(native_method(state,0,0x1818,data[j],0),152);
    }
    CHECK(native_method(state,0,0x17FC,0,0) && native_finish(state) &&
        !state->immediate.ready && !state->immediate.count &&
        !state->native_immediate.submitted && !state->native_surfaces.gpu_dirty &&
        state->native_immediate.ia_vertices==6 && state->native_immediate.ps_invocations>0 &&
        state->aa2_projective==1 && !state->constant_coordinates &&
        (state->projective_coordinates&1u),153);
    for(unsigned y=0;y<3;++y) for(unsigned x=0;x<8;++x) {
        double sx=x/2+((x&1)?.5:0),sy=y+((x&1)?.5:0);
        CHECK(guest_word(0x3000+y*32+x*4)==(3*sx+4*sy<12?0xFF000000:0xCDCDCDCD),154);
    }
    CHECK(!memcmp(aa_depth_before,guest_memory+0x4000,96),155);
    CHECK(!_putenv_s("RECOMP_PROJECTIVE_AA2_APPROXIMATION",saved_projective?saved_projective:""),156);
    free(saved_projective);
    /* Switching back must retain the unshifted original VS, not reuse the
     * AA2 raster variant from the previous batch's coverage passes. */
    CHECK(native_method(state,0,0x208,0x128,0) &&
        native_method(state,0,0x20C,0x00100010,0) &&
        native_method(state,0,0x1D7C,0xFFFF0000,0),87);
    memset(guest_memory+0x3000,0xCD,48);
    CHECK(native_method(state,0,0x17FC,5,0),88);
    for(unsigned i=0;i<3;++i) {
        uint32_t x,y;memcpy(&x,&points[i][0],4);memcpy(&y,&points[i][1],4);
        CHECK(native_method(state,0,0x1880,x,0) && native_method(state,0,0x1884,y,0),89);
    }
    CHECK(native_method(state,0,0x17FC,0,0) && native_finish(state) &&
        state->native_immediate.ia_vertices==3,90);
    for(unsigned i=0;i<12;++i) CHECK(guest_word(0x3000+i*4)==0xFF000000,91);
    /* Captured triangle INLINE_ARRAY layout through the real receiver and
     * GPU completion/writeback, using independent synthetic vertex data. */
    for(unsigned i=0;i<16;++i) CHECK(native_method(state,0,0x1760+i*4,
        i==0?0x642:i==3?0x640:0x602,0),120);
    memset(guest_memory+0x3000,0xCD,48);
    CHECK(native_method(state,0,0x17FC,5,0),121);
    for(unsigned i=0;i<3;++i) {
        float position[4]={points[i][0],points[i][1],0,1};uint32_t data[5];
        memcpy(data,position,16);data[4]=0xFFFF00FF;
        for(unsigned j=0;j<5;++j) CHECK(native_method(state,0,0x1818,data[j],0),122);
    }
    CHECK(native_method(state,0,0x17FC,0,0) && native_finish(state) &&
        !state->immediate.ready && !state->native_immediate.submitted &&
        !state->native_surfaces.gpu_dirty && state->native_immediate.ia_vertices==3,123);
    for(unsigned i=0;i<12;++i) CHECK(guest_word(0x3000+i*4)==0xFF000000,124);
    FutGraphicsObject vertex_dma={0};vertex_dma.words[0]=0xB03D;
    vertex_dma.words[1]=sizeof(guest_memory)-1;
    state->bindings.references[0][7]=state->bindings.references[0][8]=vertex_dma;
    state->bindings.reference_masks[0]|=(1u<<7)|(1u<<8);
    state->bindings.read=object_memory_read;
    int16_t indexed_points[4][3]={{0,0,0},{8,0,0},{0,6,0},{8,6,0}};
    memcpy(guest_memory+0x9000,indexed_points,sizeof(indexed_points));
    for(unsigned i=0;i<16;++i) CHECK(native_method(state,0,0x1760+i*4,i?0x602:0x635,0),125);
    CHECK(native_method(state,0,0x1720,0x9000,0),126);
    memset(guest_memory+0x3000,0xCD,48);
    CHECK(native_method(state,0,0x17FC,6,0) && native_method(state,0,0x1800,0,0) &&
        native_method(state,0,0x1800,0x00020001,0) && native_method(state,0,0x1808,3,0),127);
    CHECK(native_method(state,0,0x17FC,0,0) && native_finish(state) &&
        !state->vertex_array.ready && state->native_immediate.ia_vertices==9 &&
        state->native_immediate.ps_invocations>0,128);
    for(unsigned i=0;i<12;++i) CHECK(guest_word(0x3000+i*4)==0xFF000000,129);
    /* FORMAT1042 is four floats at stride16. Keep W=2 from DMA through
     * the original MOV oPos,v0 and real single-sample strip draw receiver. */
    const float float4_points[4][4]={{0,0,0,2},{8,0,0,2},{0,6,0,2},{8,6,0,2}};
    memcpy(guest_memory+0x9000,float4_points,sizeof(float4_points));
    for(unsigned i=0;i<16;++i)
        CHECK(native_method(state,0,0x1760+i*4,i?0x1002:0x1042,0),157);
    CHECK(state->kelvin.vertex_array_format[0]==0x1042 &&
        native_method(state,0,0x1720,0x9000,0),158);
    uint8_t float4_depth_before[72];memcpy(float4_depth_before,guest_memory+0x4000,72);
    memset(guest_memory+0x3000,0xCD,48);
    CHECK(native_method(state,0,0x17FC,6,0) &&
        native_method(state,0,0x1800,0x00010000,0) &&
        native_method(state,0,0x1800,0x00030002,0),159);
    CHECK(native_method(state,0,0x17FC,0,0) && native_finish(state) &&
        !state->vertex_array.ready && !state->immediate.ready && !state->immediate.count &&
        !state->native_immediate.count && !state->native_immediate.submitted &&
        !state->native_surfaces.gpu_dirty && state->native_immediate.ia_vertices==6 &&
        state->native_immediate.ps_invocations>0 && !state->constant_coordinates &&
        (state->projective_coordinates&1u),160);
    static const unsigned float4_order[6]={0,1,2,2,1,3};
    for(unsigned i=0;i<6;++i)
        CHECK(!memcmp(state->immediate.vertices[i][0],float4_points[float4_order[i]],16),161);
    for(unsigned i=0;i<12;++i)CHECK(guest_word(0x3000+i*4)==0xFF000000,162);
    CHECK(!memcmp(float4_depth_before,guest_memory+0x4000,72),163);
    /* DRAW_ARRAYS first1 skips an authored guard record. Two adjacent
     * count2 ranges must form the same four-record strip, not two draws. */
    float ranged_points[5][4]={{1000,1000,0,7}};
    memcpy(ranged_points+1,float4_points,sizeof(float4_points));
    memcpy(guest_memory+0x9000,ranged_points,sizeof(ranged_points));
    CHECK(native_method(state,0,0x1760,0x1042,0),164);
    memset(guest_memory+0x3000,0xCD,48);
    CHECK(native_method(state,0,0x17FC,6,0) &&
        native_method(state,0,0x1810,0x01000001,0) &&
        state->vertex_array.active && state->vertex_array.input_mode==3 &&
        state->vertex_array.count==2 && state->vertex_array.indices[0]==1 &&
        state->vertex_array.indices[1]==2,165);
    CHECK(native_method(state,0,0x1810,0x01000003,0) &&
        state->vertex_array.count==4 && state->vertex_array.indices[2]==3 &&
        state->vertex_array.indices[3]==4 && guest_word(0x3000)==0xCDCDCDCD,166);
    CHECK(native_method(state,0,0x17FC,0,0) && native_finish(state) &&
        !state->vertex_array.ready && !state->vertex_array.count &&
        !state->immediate.ready && !state->immediate.count &&
        !state->native_immediate.count && !state->native_immediate.submitted &&
        !state->native_surfaces.gpu_dirty && state->native_immediate.ia_vertices==6 &&
        state->native_immediate.ps_invocations>0 && !state->constant_coordinates &&
        (state->projective_coordinates&1u),167);
    for(unsigned i=0;i<6;++i)
        CHECK(!memcmp(state->immediate.vertices[i][0],float4_points[float4_order[i]],16),168);
    for(unsigned i=0;i<12;++i)CHECK(guest_word(0x3000+i*4)==0xFF000000,169);
    CHECK(!memcmp(float4_depth_before,guest_memory+0x4000,72) &&
        !memcmp(ranged_points,guest_memory+0x9000,sizeof(ranged_points)),170);
    /* Observed indexed TRIANGLES: float3/UV float2/ARGB8 in slots0/1/2.
     * Separate DMA streams and shuffled indices must keep original slots. */
    float list_positions[10][3],list_uv[10][2];uint32_t list_colors[10];
    for(unsigned i=0;i<10;++i) {
        list_positions[i][0]=1000.0f+i;list_positions[i][1]=1000;list_positions[i][2]=0;
        list_uv[i][0]=i*.125f;list_uv[i][1]=.875f-i*.03125f;
        list_colors[i]=0xFF000000u|i;
    }
    static const unsigned list_order[6]={5,2,7,7,2,9};
    static const unsigned corners[4]={5,2,7,9};
    for(unsigned i=0;i<4;++i) {
        memcpy(list_positions[corners[i]],float4_points[i],12);
        list_colors[corners[i]]=0xFF804020u;
    }
    memcpy(guest_memory+0x9000,list_positions,sizeof(list_positions));
    memcpy(guest_memory+0x9200,list_uv,sizeof(list_uv));
    memcpy(guest_memory+0x9300,list_colors,sizeof(list_colors));
    for(unsigned slot=0;slot<16;++slot)
        CHECK(native_method(state,0,0x1760+slot*4,
            slot==0?0xC32:slot==1?0x822:slot==2?0x440:0xC02,0),179);
    CHECK(native_method(state,0,0x1720,0x9000,0) && native_method(state,0,0x1724,0x9200,0) &&
        native_method(state,0,0x1728,0x9300,0),180);
    const uint32_t list_program[2][4]={
        {0,(1u<<21)|0x1Bu,2u<<26,(15u<<12)|0x800u},
        {0,(1u<<21)|(2u<<9)|0x1Bu,2u<<26,(15u<<12)|0x800u|(3u<<3)|1u}};
    memcpy(state->kelvin.program,list_program,sizeof(list_program));
    state->kelvin.program_valid[0]=state->kelvin.program_valid[1]=15;state->kelvin.program_dirty=1;
    memset(guest_memory+0x3000,0xCD,48);
    CHECK(native_method(state,0,0x17FC,5,0) && state->immediate.active &&
        native_method(state,0,0x1800,0x00020005,0) && !state->immediate.active &&
        state->vertex_array.active && state->vertex_array.primitive==5,181);
    CHECK(native_method(state,0,0x1800,0x00070007,0) &&
        native_method(state,0,0x1800,0x00090002,0) && state->vertex_array.count==6 &&
        !memcmp(state->vertex_array.indices,list_order,sizeof(list_order)),182);
    CHECK(native_method(state,0,0x17FC,0,0) && native_finish(state) &&
        !state->vertex_array.ready && !state->vertex_array.count && !state->immediate.ready &&
        !state->immediate.count && !state->native_immediate.count && !state->native_immediate.submitted &&
        !state->native_surfaces.gpu_dirty && state->native_immediate.ia_vertices==6 &&
        state->native_immediate.ps_invocations>0 && (state->constant_coordinates&1u),183);
    for(unsigned i=0;i<6;++i) {
        unsigned record=list_order[i];float position[4]={list_positions[record][0],list_positions[record][1],0,1};
        float uv[4]={list_uv[record][0],list_uv[record][1],0,1};
        const float color[4]={128.0f/255,64.0f/255,32.0f/255,1};
        CHECK(!memcmp(state->immediate.vertices[i][0],position,16) &&
            !memcmp(state->immediate.vertices[i][1],uv,16) &&
            !memcmp(state->immediate.vertices[i][2],color,16) && (state->immediate.defined[i]&7u)==7u,184);
    }
    for(unsigned i=0;i<12;++i)CHECK(guest_word(0x3000+i*4)==0xFF804020u,185);
    CHECK(!memcmp(float4_depth_before,guest_memory+0x4000,72) &&
        !memcmp(list_positions,guest_memory+0x9000,sizeof(list_positions)) &&
        !memcmp(list_uv,guest_memory+0x9200,sizeof(list_uv)) &&
        !memcmp(list_colors,guest_memory+0x9300,sizeof(list_colors)),186);
    CHECK(!verify_indexed_promotion_guards(state),187);
    /* Captured DMA QUADS profile: float4/ARGB8/float3 in original slots.
     * A start1 range must skip the guard and merge across a quad boundary. */
    const float array_quad[5][4]={{1000,1000,0,7},{0,0,0,2},{8,0,0,2},{8,6,0,2},{0,6,0,2}};
    const uint32_t quad_colors[5]={0xFF010203u,0xFF204080u,0xFF204080u,0xFF204080u,0xFF204080u};
    float quad_uv[5][3];
    for(unsigned i=0;i<5;++i) {
        quad_uv[i][0]=i*.125f;quad_uv[i][1]=.75f-i*.03125f;quad_uv[i][2]=-.5f+i*.25f;
    }
    memcpy(guest_memory+0x9000,array_quad,sizeof(array_quad));
    memcpy(guest_memory+0x9200,quad_colors,sizeof(quad_colors));
    memcpy(guest_memory+0x9300,quad_uv,sizeof(quad_uv));
    for(unsigned slot=0;slot<16;++slot)
        CHECK(native_method(state,0,0x1760+slot*4,
            slot==0?0x1042:slot==1?0x440:slot==2?0xC32:0x1002,0),188);
    CHECK(native_method(state,0,0x1720,0x9000,0) && native_method(state,0,0x1724,0x9200,0) &&
        native_method(state,0,0x1728,0x9300,0),189);
    const uint32_t quad_program[2][4]={
        {0,(1u<<21)|0x1Bu,2u<<26,(15u<<12)|0x800u},
        {0,(1u<<21)|(1u<<9)|0x1Bu,2u<<26,(15u<<12)|0x800u|(3u<<3)|1u}};
    memcpy(state->kelvin.program,quad_program,sizeof(quad_program));
    state->kelvin.program_valid[0]=state->kelvin.program_valid[1]=15;state->kelvin.program_dirty=1;
    memset(guest_memory+0x3000,0xCD,48);
    CHECK(native_method(state,0,0x17FC,8,0) && state->immediate.active &&
        native_method(state,0,0x1810,0x01000001,0) && !state->immediate.active &&
        state->vertex_array.active && state->vertex_array.primitive==8 && state->vertex_array.count==2 &&
        state->vertex_array.indices[0]==1 && state->vertex_array.indices[1]==2,190);
    CHECK(native_method(state,0,0x1810,0x01000003,0) && state->vertex_array.count==4 &&
        state->vertex_array.indices[2]==3 && state->vertex_array.indices[3]==4,191);
    CHECK(native_method(state,0,0x17FC,0,0) && native_finish(state) &&
        !state->vertex_array.ready && !state->vertex_array.count && !state->immediate.ready &&
        !state->immediate.count && !state->native_immediate.count && !state->native_immediate.submitted &&
        !state->native_surfaces.gpu_dirty && state->native_immediate.ia_vertices==6 &&
        state->native_immediate.ps_invocations>0 && !state->constant_coordinates &&
        (state->projective_coordinates&1u),192);
    static const unsigned quad_order[6]={1,2,3,1,3,4};
    for(unsigned i=0;i<6;++i) {
        unsigned record=quad_order[i];float uv[4]={quad_uv[record][0],quad_uv[record][1],quad_uv[record][2],1};
        const float color[4]={32.0f/255,64.0f/255,128.0f/255,1};
        CHECK(!memcmp(state->immediate.vertices[i][0],array_quad[record],16) &&
            !memcmp(state->immediate.vertices[i][1],color,16) &&
            !memcmp(state->immediate.vertices[i][2],uv,16) && (state->immediate.defined[i]&7u)==7u,193);
    }
    /* Borrow the already uploaded six-record buffer for an independent SO
     * observation; consumption counters in the real receiver stay zero. */
    FutNativeImmediate quad_probe=state->native_immediate;quad_probe.count=6;
    FutNativePosition quad_outputs[6];
    CHECK(SUCCEEDED(fut_native_immediate_probe_positions(&quad_probe,state->device,state->context,
        state->vertex_shader,state->vertex_bytecode,&state->native_constants,quad_outputs,6,2000,NULL)),194);
    for(unsigned i=0;i<6;++i)
        CHECK(!memcmp(quad_outputs[i].screen,array_quad[quad_order[i]],16) && quad_outputs[i].clip[3]==2,195);
    for(unsigned i=0;i<12;++i)CHECK(guest_word(0x3000+i*4)==0xFF204080u,196);
    CHECK(!memcmp(float4_depth_before,guest_memory+0x4000,72) &&
        !memcmp(array_quad,guest_memory+0x9000,sizeof(array_quad)) &&
        !memcmp(quad_colors,guest_memory+0x9200,sizeof(quad_colors)) &&
        !memcmp(quad_uv,guest_memory+0x9300,sizeof(quad_uv)) && !state->native_immediate.count,197);
    memcpy(state->kelvin.program,program,sizeof(program));state->kelvin.program_valid[1]=0;
    state->kelvin.program_dirty=1;
    for(unsigned profile=0;profile<4;++profile) {
    for(unsigned i=0;i<16;++i) {
        unsigned stride=profile==0?0u:profile==1?0x600u:profile==2?0xC00u:
            i==3?0u:i==9?0x800u:0xC00u;
        CHECK(native_method(state,0,0x1760+i*4,
            (i==0?0x32:i==3?0x40:i==9?0x22:2)|stride,0),92);
    }
    memset(guest_memory+0x3000,0xCD,48);
    CHECK(native_method(state,0,0x17FC,8,0),93);
    const float quad[4][3]={{0,0,0},{8,0,0},{8,6,0},{0,6,0}};
    for(unsigned i=0;i<4;++i) {
        uint32_t data[6];memcpy(data,quad[i],12);
        data[3]=0x80402010;data[4]=0;data[5]=0x3F800000;
        for(unsigned j=0;j<6;++j) CHECK(native_method(state,0,0x1818,data[j],0),94);
        CHECK(!native_method(state,0,0x194C,0,0),95);
    }
    CHECK(native_method(state,0,0x17FC,0,0) && native_finish(state) &&
        !state->immediate.ready && !state->native_surfaces.gpu_dirty &&
        state->native_immediate.ia_vertices==6 && state->native_immediate.ps_invocations>0,96);
    for(unsigned i=0;i<12;++i) CHECK(guest_word(0x3000+i*4)==0xFF000000,97);
    }
    return 0;
}
int main(void)
{
    CHECK(verify_presented_capture_files(),147);
    uint32_t context_address=0x12345678,context_words[119],saved_words[119];
    memset(context_words,0xA5,sizeof(context_words));memcpy(saved_words,context_words,sizeof(saved_words));
    CHECK(!native_scanout_context(&context_address,context_words) && context_address==0x12345678 &&
        !memcmp(context_words,saved_words,sizeof(saved_words)),70);
    connected_interrupt=0x80000080;
    uint32_t synthetic_context=0x80006000;
    memcpy(guest_memory+0x84,&synthetic_context,4);
    for(unsigned i=0;i<119;++i) memcpy(guest_memory+0x6000+i*4,&i,4);
    CHECK(native_scanout_context(&context_address,context_words) && context_address==synthetic_context &&
        context_words[0]==0 && context_words[118]==118,71);
    synthetic_context=4;memcpy(guest_memory+0x84,&synthetic_context,4);
    memcpy(saved_words,context_words,sizeof(saved_words));
    CHECK(!native_scanout_context(&context_address,context_words) &&
        !memcmp(context_words,saved_words,sizeof(saved_words)),72);
    connected_interrupt=0;
    NativeGraphicsState state = {0}; D3D_FEATURE_LEVEL level;
    memset(guest_memory, 0xCD, sizeof(guest_memory));
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,
        D3D11_SDK_VERSION,&state.device,&level,&state.context)),1);
    CHECK(SUCCEEDED(fut_native_constants_init(&state.native_constants,state.device,state.context)),2);
    state.bindings.bound_mask=1;
    state.bindings.objects[0].words[0]=0x97;
    state.bindings.bound_mask|=1u<<5;state.bindings.objects[5].words[0]=0x44;
    /* Replay only the MMIO write sink, leaving the real completion path active. */
    for(unsigned success=0;success<2;++success) {
        FILE *trace=tmpfile();CHECK(trace,98);
        uint32_t marker=0x500F167Cu;
        FutTraceRecord record={FUT_TRACE_WRITE,0xFD400B10u,0,success,4};
        CHECK(fwrite(&record,sizeof(record),1,trace)==1 && fwrite(&marker,4,1,trace)==1,99);
        rewind(trace);fut_trace_file=trace;fut_trace_replaying=1;fut_trace_active=1;fut_trace_bytes=0;
        state.kelvin.pattern_color=0x1234;
        CHECK(native_method(&state,5,0x310,marker,0)==(int)success &&
            state.kelvin.pattern_color==(success?marker:0x1234u) && ftell(trace)==24,100);
        fut_trace_file=NULL;fut_trace_replaying=0;fut_trace_active=0;fclose(trace);
    }
    /* A cancelled completion may not publish the marker at all. */
    backend_stop=CreateEventA(NULL,TRUE,TRUE,NULL);CHECK(backend_stop,101);
    FILE *cancel_trace=tmpfile();CHECK(cancel_trace,103);
    uint32_t cancel_marker=0xDEADBEEFu;
    FutTraceRecord cancel_record={FUT_TRACE_WRITE,0xFD400B10u,0,1,4};
    CHECK(fwrite(&cancel_record,sizeof(cancel_record),1,cancel_trace)==1 &&
        fwrite(&cancel_marker,4,1,cancel_trace)==1,104);
    rewind(cancel_trace);fut_trace_file=cancel_trace;fut_trace_replaying=1;
    fut_trace_active=1;fut_trace_bytes=0;
    CHECK(!native_method(&state,5,0x310,0xDEADBEEF,0) &&
        state.kelvin.pattern_color==0x500F167Cu && ftell(cancel_trace)==0,102);
    fut_trace_file=NULL;fut_trace_replaying=0;fut_trace_active=0;fclose(cancel_trace);
    CloseHandle(backend_stop);backend_stop=NULL;
    CHECK(native_method(&state,0,0x1760,0x32,0) &&
        state.kelvin.vertex_array_format[0]==0x32 &&
        state.kelvin.vertex_array_format_valid==1,76);
    CHECK(!native_method(&state,0,0x1760,0x132,0) &&
        state.kelvin.vertex_array_format[0]==0x32,77);
    CHECK(native_method(&state,0,0x1710,0,0) &&
        !native_method(&state,0,0x1710,1,0),73);
    /* Validation must not accidentally authorize unimplemented array draws. */
    CHECK(!native_method(&state,0,0x1810,0,0),74);
    state.bindings.reference_masks[0]=(1u<<5)|(1u<<6);
    const FutGraphicsObject dma={.words={0xB03D,0x07FFAFFF,3,0}};
    state.bindings.references[0][5]=dma; state.bindings.references[0][6]=dma;
    const uint32_t initial[]={4u<<16,3u<<16,0x1128,(44u<<16)|40,0x1000,0x2000};
    for(unsigned i=0;i<6;++i) {
        CHECK(native_method(&state,0,0x200+i*4,initial[i],i*4),3);
        CHECK(!state.native_surfaces.valid,4); /* No allocation at setters. */
    }
    CHECK(native_method(&state,0,0x110,0,0) && !state.native_surfaces.valid,5);
    CHECK(native_method(&state,0,0x1D98,3u<<16,0) &&
        native_method(&state,0,0x1D9C,2u<<16,0) &&
        native_method(&state,0,0x1D90,0xFF112233,0) &&
        native_method(&state,0,0x1D8C,0xFFFFFF5A,0) &&
        native_method(&state,0,0x1D94,0xF3,0),6);
    CHECK(state.native_surfaces.valid && !state.native_surfaces.gpu_dirty &&
        state.native_surfaces.plan.width==8 && guest_word(0x1000)==0xFF112233 &&
        guest_word(0x2000)==0xFFFFFF5A && guest_memory[0x1020]==0xCD,7);
    ID3D11Texture2D *old=state.native_surfaces.color;
    CHECK(native_method(&state,0,0x20C,(24u<<16)|16,0),8); /* Invalid with old AA. */
    CHECK(native_method(&state,0,0x110,0,0) && state.native_surfaces.color==old &&
        state.native_surfaces.plan.width==8,9);
    CHECK(native_method(&state,0,0x210,0x3000,0) && native_method(&state,0,0x110,0,0),10);
    CHECK(native_method(&state,0,0x214,0x4000,0) && native_method(&state,0,0x110,0,0),11);
    CHECK(native_method(&state,0,0x208,0x128,0) && state.native_surfaces.color==old,12);
    CHECK(native_method(&state,0,0x1D90,0xCC336699,0) && native_method(&state,0,0x1D94,0xF3,0),13);
    CHECK(state.native_surfaces.plan.width==4 && state.native_surfaces.plan.samples==1 &&
        state.native_surfaces.plan.color_address==0x80003000 &&
        guest_word(0x3000)==0xCC336699 && guest_word(0x4000)==0xFFFFFF5A &&
        guest_word(0x1000)==0xFF112233 && guest_word(0x2000)==0xFFFFFF5A,14);

    /* WAIT downloads previous GPU work even if the NEXT pitch is invalid. */
    old=state.native_surfaces.color;
    CHECK(native_method(&state,0,0x20C,(24u<<16)|8,0),15);
    fut_native_surfaces_mark_dirty(&state.native_surfaces,1);
    const float rgba[]={0.0f,1.0f,0.0f,1.0f};
    ID3D11DeviceContext_ClearRenderTargetView(state.context,state.native_surfaces.target,rgba);
    CHECK(native_method(&state,0,0x110,0,0) && !state.native_surfaces.gpu_dirty &&
        state.native_surfaces.color==old && guest_word(0x3000)==0xFF00FF00,16);
    CHECK(!native_method(&state,0,0x1D94,0xF3,0) && state.native_surfaces.color==old &&
        state.native_surfaces.plan.color_pitch==16 && guest_word(0x3000)==0xFF00FF00,17);
    CHECK(native_method(&state,0,0x20C,(24u<<16)|16,0),18);
    CHECK(native_method(&state,0,0x100,(1u<<5)|8,0),19);
    state.software.rdi_value[1]^=1; /* Unknown/inconsistent RDI remains gated. */
    CHECK(!native_prepare_draw(&state),20); /* SW raster gate stays closed. */
    state.software.rdi_value[1]^=1;
    CHECK(!native_method(&state,0,0x1FFC,0,0),21); /* Unknown methods stay rejected. */
    state.bindings.references[0][1]=dma; state.bindings.references[0][2]=dma;
    state.bindings.references[0][2].words[2]=0x1003; /* B has a distinct base. */
    state.bindings.reference_masks[0]|=(1u<<1)|(1u<<2);
    const uint32_t texture_commands[][2]={{0x1B00,0x3000},{0x1B04,0x11229},
        {0x1B10,16u<<16},{0x1B1C,(4u<<16)|3},{0x1B08,0x10303},
        {0x1B0C,0x4003FFC0},{0x1B14,0x01010000}};
    for(unsigned i=0;i<sizeof(texture_commands)/sizeof(texture_commands[0]);++i) {
        CHECK(native_method(&state,0,texture_commands[i][0],texture_commands[i][1],0),22);
        CHECK(!state.textures[0].valid,23); /* No eager upload at a setter. */
    }
    fut_native_surfaces_mark_dirty(&state.native_surfaces,1);
    const float red[]={1,0,0,1};
    ID3D11DeviceContext_ClearRenderTargetView(state.context,state.native_surfaces.target,red);
    CHECK(native_prepare_textures(&state) && guest_word(0x3000)==0xFFFF0000 &&
        !state.native_surfaces.gpu_dirty && state.textures[0].sampling_ready &&
        state.textures[0].plan.address==0x80003000 && state.textures[0].plan.dma_context==1,24);
    ID3D11ShaderResourceView *bound=NULL;
    ID3D11DeviceContext_PSGetShaderResources(state.context,0,1,&bound);
    CHECK(bound==state.textures[0].view,25); ID3D11ShaderResourceView_Release(bound);
    D3D11_TEXTURE2D_DESC texture_desc;ID3D11Texture2D_GetDesc(state.textures[0].texture,&texture_desc);
    texture_desc.Usage=D3D11_USAGE_STAGING;texture_desc.BindFlags=0;texture_desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    ID3D11Texture2D *texture_readback=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateTexture2D(state.device,&texture_desc,NULL,&texture_readback)),26);
    ID3D11DeviceContext_CopyResource(state.context,(ID3D11Resource*)texture_readback,(ID3D11Resource*)state.textures[0].texture);
    CHECK(fut_native_constants_finish(&state.native_constants,NULL,2000,NULL),27);
    D3D11_MAPPED_SUBRESOURCE mapped;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(state.context,(ID3D11Resource*)texture_readback,0,D3D11_MAP_READ,0,&mapped)),28);
    uint32_t imported;memcpy(&imported,mapped.pData,4);
    ID3D11DeviceContext_Unmap(state.context,(ID3D11Resource*)texture_readback,0);
    ID3D11Texture2D_Release(texture_readback);CHECK(imported==0xFFFF0000,29);
    CHECK(native_method(&state,0,0x1B04,0x1122A,0) && native_prepare_textures(&state) &&
        state.textures[0].plan.dma_context==2 && state.textures[0].plan.address==0x80004000,30);
    old=state.textures[0].texture;
    CHECK(native_method(&state,0,0x1B14,0x04072000,0) && state.textures[0].texture==old &&
        !native_prepare_textures(&state) && !state.textures[0].sampling_ready,31);
    CHECK(!native_prepare_draw(&state),32); /* Convolution cannot turn into a draw. */
    CHECK(native_method(&state,0,0x1B0C,0,0) && native_prepare_textures(&state),33);
    ID3D11DeviceContext_PSGetShaderResources(state.context,0,1,&bound);CHECK(!bound,34);
    CHECK(native_method(&state,0,0x120,0,0) && native_method(&state,0,0x124,0,0) &&
        native_method(&state,0,0x128,2,0),35);
    CHECK(!native_method(&state,0,0x130,0,0) && state.kelvin.flip_read==0 && state.kelvin.flip_write==0,36);
    CHECK(native_method(&state,0,0x12C,0,0) && state.kelvin.flip_write==1 && state.kelvin.flip_read==0,37);
    CHECK(native_method(&state,0,0x130,0,0) && state.kelvin.flip_write==1 && state.kelvin.flip_read==0,38);
    CHECK(native_method(&state,0,0x12C,0,0) && !native_method(&state,0,0x130,0,0) && state.kelvin.flip_read==0,39);
    /* Synthetic test state without SW dependencies: mode4 itself must not
     * reuse the previously programmable vertex-shader path. */
    state.software=(FutGraphicsSoftwareState){0};
    CHECK(native_method(&state,0,0x1E94,4,0) && !native_prepare_draw(&state),40);
    /* Synthetic pass-through program: input0->position, input9->texture0. */
    const uint32_t pass[2][4]={{0,(1u<<21)|0x1Bu,2u<<26,(15u<<12)|0x800u},
        {0,(1u<<21)|(9u<<9)|0x1Bu,2u<<26,(15u<<12)|0x800u|(9u<<3)|1u}};
    memcpy(state.kelvin.program,pass,sizeof(pass));
    state.kelvin.program_valid[0]=state.kelvin.program_valid[1]=15;
    CHECK(native_method(&state,0,0x1E94,6,0),41);
    CHECK(native_method(&state,0,0x1B0C,0x4003FFC0,0) &&
        native_method(&state,0,0x1B14,0x04072000,0),42);
    CHECK(native_method(&state,0,0x17FC,5,0) && state.immediate.active,43);
    uint32_t saved_format=state.kelvin.vertex_array_format[0];
    CHECK(!native_method(&state,0,0x1760,0x22,0) &&
        state.kelvin.vertex_array_format[0]==saved_format,78);
    CHECK(!native_method(&state,0,0x1710,0,0) && state.immediate.active &&
        !state.immediate.count,75);
    uint32_t previous=state.kelvin.textures[0].filter;
    CHECK(!native_method(&state,0,0x1B14,0x01010000,0) &&
        state.kelvin.textures[0].filter==previous && !native_finish(&state),44);
    CHECK(!native_method(&state,0,0x110,0,0) && !native_method(&state,0,0,0,0) &&
        state.bindings.objects[0].words[0]==0x97,45);
    for(unsigned i=0;i<3;++i) {
        CHECK(native_method(&state,0,0x18C8,0x3F000000,0) &&
            native_method(&state,0,0x18CC,0,0) && native_method(&state,0,0x1880,0,0) &&
            native_method(&state,0,0x1884,0,0),46);
    }
    CHECK(!native_method(&state,0,0x17FC,0,0) && state.immediate.ready && !state.immediate.active &&
        state.native_immediate.count==3 && state.native_immediate.buffer &&
        !state.textures[0].sampling_ready && !native_finish(&state),47);
    CHECK(!native_method(&state,0,0x17FC,5,0) && state.immediate.count==3 &&
        !native_method(&state,0,0x110,0,0) && !native_method(&state,0,0x1D70,8,0),48);
    /* Test-only alternate texture snapshot/state after the failed receiver
     * call. It remains pending: preparing a PS must NOT bypass the SW gate. */
    const uint32_t fragment[][2]={{0x1E60,1},{0x1E70,1},{0xAC0,0x08200000},
        {0x260,0x14200000},{0x1E40,0xC00},{0xAA0,0xC00},{0x288,0xC},{0x28C,0x1C80}};
    for(unsigned i=0;i<sizeof(fragment)/sizeof(fragment[0]);++i)
        CHECK(fut_graphics_kelvin_state(&state.kelvin,fragment[i][0],fragment[i][1])==FUT_KELVIN_APPLIED,49);
    uint32_t black=0xFF000000;
    for(unsigned y=0;y<3;++y) for(unsigned x=0;x<4;++x) memcpy(guest_memory+0x4000+y*16+x*4,&black,4);
    state.software.noise_valid=1;
    state.software.rdi_index[0]=0; /* Deliberately outside the admitted domain. */
    CHECK(!native_prepare_draw(&state) && state.textures[0].zero_rgb_candidate &&
        !state.textures[0].sampling_ready && state.pixel_shader && state.pixel_bytecode &&
        !native_finish(&state) && state.immediate.ready,50);
    guest_memory[0x4000+2*16+3*4]=1;
    CHECK(!native_prepare_draw(&state) && !state.textures[0].zero_rgb_candidate &&
        state.textures[0].texel_or==0xFF000001 && !state.textures[0].sampling_ready &&
        !native_finish(&state) && state.immediate.ready,51);
    CHECK(verify_draw_receiver(&state)==0,52);
    fut_native_immediate_destroy(&state.native_immediate);
    fut_native_vertex_surface_destroy(&state.vertex_surface);
    for(unsigned stage=0;stage<4;++stage) fut_native_texture_destroy(&state.textures[stage]);
    fut_native_surfaces_destroy(&state.native_surfaces);
    fut_native_constants_destroy(&state.native_constants);
    fut_native_pipeline_destroy(&state.pipeline);
    if(state.pixel_shader) ID3D11PixelShader_Release(state.pixel_shader);
    if(state.pixel_bytecode) ID3D10Blob_Release(state.pixel_bytecode);
    if(state.vertex_shader) ID3D11VertexShader_Release(state.vertex_shader);
    if(state.vertex_bytecode) ID3D10Blob_Release(state.vertex_bytecode);
    if(state.aa2_vertex_shader) ID3D11VertexShader_Release(state.aa2_vertex_shader);
    if(state.aa2_vertex_bytecode) ID3D10Blob_Release(state.aa2_vertex_bytecode);
    ID3D11DeviceContext_Release(state.context); ID3D11Device_Release(state.device);
    puts("Real backend: deferred description validation, WAIT old-plan writeback and independent draw gate OK");
    return 0;
}
