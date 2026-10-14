/* Exercise the real method receiver with synthetic guest memory, no game
 * binary, guest CPU, submission thread or generated code execution. */
#include "../graphics_native_backend.c"

#define CHECK(expr, code) do { if (!(expr)) { fprintf(stderr, "backend lifecycle test failed: %d\n", code); return code; } } while (0)
static uint8_t guest_memory[0x10000];
ptrdiff_t xbox_GetMemoryOffset(void)
{
    return (ptrdiff_t)((uintptr_t)guest_memory - (uintptr_t)0x80000000u);
}
static uint32_t guest_word(unsigned offset)
{
    uint32_t word; memcpy(&word, guest_memory + offset, 4); return word;
}
int main(void)
{
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
    CHECK(!native_method(&state,0,0x17FC,5,0),20); /* SW raster gate stays closed. */
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
    CHECK(!native_method(&state,0,0x17FC,5,0),32); /* Convolution cannot turn into a draw. */
    CHECK(native_method(&state,0,0x1B0C,0,0) && native_prepare_textures(&state),33);
    ID3D11DeviceContext_PSGetShaderResources(state.context,0,1,&bound);CHECK(!bound,34);
    for(unsigned stage=0;stage<4;++stage) fut_native_texture_destroy(&state.textures[stage]);
    fut_native_surfaces_destroy(&state.native_surfaces);
    fut_native_constants_destroy(&state.native_constants);
    ID3D11DeviceContext_Release(state.context); ID3D11Device_Release(state.device);
    puts("Real backend: deferred description validation, WAIT old-plan writeback and independent draw gate OK");
    return 0;
}
