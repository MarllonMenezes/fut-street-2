/* Exercise the real method receiver with synthetic guest memory, no game
 * binary, guest CPU, submission thread or generated code execution. */
#include "../graphics_native_backend.c"

#define CHECK(expr, code) do { if (!(expr)) { fprintf(stderr, "backend lifecycle test failed: %d\n", code); return code; } } while (0)
static uint8_t guest_memory[0x10000];
static uint32_t connected_interrupt;
uint32_t xbox_GetConnectedInterrupt(uint32_t vector)
{
    return vector==3?connected_interrupt:0;
}
ptrdiff_t xbox_GetMemoryOffset(void)
{
    return (ptrdiff_t)((uintptr_t)guest_memory - (uintptr_t)0x80000000u);
}
static uint32_t guest_word(unsigned offset)
{
    uint32_t word; memcpy(&word, guest_memory + offset, 4); return word;
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
    CHECK(native_method(state,0,0x100,(0x3001u<<5)|1,0) &&
        guest_word(0x61C4)==2 && guest_word(0x61C8)==1 && guest_word(0x61CC)==1 &&
        guest_word(0x6174)==1 && guest_word(0x6178)==1 && guest_word(0x617C)==0x3000,73);
    uint8_t first_queued[FUT_SCANOUT_CONTEXT_WORDS*4];memcpy(first_queued,guest_memory+0x6000,sizeof(first_queued));
    CHECK(!native_method(state,0,0x100,(0x4001u<<5)|1,0) &&
        !memcmp(first_queued,guest_memory+0x6000,sizeof(first_queued)),77);
    CHECK(native_method(state,0,0x100,(0x3001u<<5)|1,0) &&
        guest_word(0x61CC)==2 && guest_word(0x6180)==1 && guest_word(0x6184)==2,74);
    uint8_t queued[FUT_SCANOUT_CONTEXT_WORDS*4];memcpy(queued,guest_memory+0x6000,sizeof(queued));
    CHECK(!native_method(state,0,0x100,(0x3001u<<5)|1,0) &&
        !memcmp(queued,guest_memory+0x6000,sizeof(queued)),75);
    CHECK(state->kelvin.flip_read==0 && state->kelvin.flip_write==0 &&
        !memcmp(depth_before,guest_memory+0x4000,sizeof(depth_before)),76);
    connected_interrupt=0;
    return 0;
}
int main(void)
{
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
    ID3D11DeviceContext_Release(state.context); ID3D11Device_Release(state.device);
    puts("Real backend: deferred description validation, WAIT old-plan writeback and independent draw gate OK");
    return 0;
}
