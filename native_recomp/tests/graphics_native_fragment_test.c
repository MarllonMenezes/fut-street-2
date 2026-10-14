#define COBJMACROS
#include "graphics_native_fragment.h"
#include "graphics_native_constants.h"
#include <math.h>
#include <stdio.h>
#include <string.h>
#define CHECK(expr, code) do { if (!(expr)) { fprintf(stderr,"fragment test failed: %d\n",code); return code; } } while (0)
static void configure(FutGraphicsFragmentState *s,uint32_t color_input,uint32_t alpha_input,uint32_t modes)
{
    memset(s,0,sizeof(*s));
    const uint32_t commands[][2]={{0x1E60,1},{0x1E70,modes},{0x260,alpha_input},{0xAC0,color_input},
        {0xAA0,0xC00},{0x1E40,0xC00},{0x288,0xC},{0x28C,0x1C00}};
    for(unsigned i=0;i<sizeof(commands)/sizeof(commands[0]);++i)
        fut_graphics_fragment_state(s,commands[i][0],commands[i][1]);
}
static int verify_math(ID3D11Device *device,ID3D11DeviceContext *context,FutNativeConstants *completion,
    const FutGraphicsFragmentState *state,const float expected[4])
{
    char math[65536],source[66048];size_t length;
    CHECK(fut_fragment_program_hlsl(state,math,sizeof(math),&length),60);
    int n=snprintf(source,sizeof(source),"%sRWStructuredBuffer<float4> outColor:register(u0);"
        "[numthreads(1,1,1)]void main(){outColor[0]=fut_fragment_math("
        "float4(.25,.5,.75,.5),float4(.5,.25,.125,.75),float4(.125,.25,.5,.875),"
        "float4(.5,.5,.5,.5),float4(.5,.5,.5,.5),float4(.5,.5,.5,.5));}",math);
    CHECK(n>0 && n<(int)sizeof(source),61);
    ID3DBlob *blob=NULL,*errors=NULL;
    HRESULT hr=D3DCompile(source,n,"synthetic_combiner_inputs",NULL,NULL,"main","cs_5_0",D3DCOMPILE_IEEE_STRICTNESS,0,&blob,&errors);
    if(errors){fprintf(stderr,"%.*s",(int)ID3D10Blob_GetBufferSize(errors),(char*)ID3D10Blob_GetBufferPointer(errors));ID3D10Blob_Release(errors);}
    CHECK(SUCCEEDED(hr),62);
    ID3D11ComputeShader *shader=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateComputeShader(device,ID3D10Blob_GetBufferPointer(blob),ID3D10Blob_GetBufferSize(blob),NULL,&shader)),63);
    D3D11_BUFFER_DESC desc={0};desc.ByteWidth=16;desc.Usage=D3D11_USAGE_DEFAULT;
    desc.BindFlags=D3D11_BIND_UNORDERED_ACCESS;desc.MiscFlags=D3D11_RESOURCE_MISC_BUFFER_STRUCTURED;desc.StructureByteStride=16;
    ID3D11Buffer *output=NULL,*staging=NULL;ID3D11UnorderedAccessView *uav=NULL;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&desc,NULL,&output)),64);
    CHECK(SUCCEEDED(ID3D11Device_CreateUnorderedAccessView(device,(ID3D11Resource*)output,NULL,&uav)),65);
    desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.MiscFlags=0;desc.StructureByteStride=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
    CHECK(SUCCEEDED(ID3D11Device_CreateBuffer(device,&desc,NULL,&staging)),66);
    ID3D11DeviceContext_CSSetShader(context,shader,NULL,0);
    ID3D11DeviceContext_CSSetUnorderedAccessViews(context,0,1,&uav,NULL);
    ID3D11DeviceContext_Dispatch(context,1,1,1);
    ID3D11UnorderedAccessView *empty=NULL;ID3D11DeviceContext_CSSetUnorderedAccessViews(context,0,1,&empty,NULL);
    ID3D11DeviceContext_CopyResource(context,(ID3D11Resource*)staging,(ID3D11Resource*)output);
    CHECK(fut_native_constants_finish(completion,NULL,2000,NULL),67);
    D3D11_MAPPED_SUBRESOURCE mapped;
    CHECK(SUCCEEDED(ID3D11DeviceContext_Map(context,(ID3D11Resource*)staging,0,D3D11_MAP_READ,0,&mapped)),68);
    int good=1;
    for(unsigned c=0;c<4;++c) {
        float actual=((float*)mapped.pData)[c];
        if(!isfinite(actual) || fabsf(actual-expected[c])>0.00001f) {
            fprintf(stderr,"fragment channel%u got%f expected%f\n",c,actual,expected[c]);good=0;
        }
    }
    ID3D11DeviceContext_Unmap(context,(ID3D11Resource*)staging,0);ID3D11DeviceContext_ClearState(context);
    ID3D11Buffer_Release(staging);ID3D11UnorderedAccessView_Release(uav);ID3D11Buffer_Release(output);
    ID3D11ComputeShader_Release(shader);ID3D10Blob_Release(blob);return good?0:69;
}
int main(void)
{
    FutGraphicsFragmentState state;
    configure(&state,0x04200000,0x14200000,0);
    FutGraphicsFragmentState before=state;
    CHECK(fut_graphics_fragment_state(&state,0x261,0)==FUT_FRAGMENT_REJECTED &&
        fut_graphics_fragment_state(&state,0x284,0)==FUT_FRAGMENT_NOT_HANDLED && !memcmp(&before,&state,sizeof(state)),1);
    char text[65536];size_t length=999;
    CHECK(fut_fragment_program_hlsl(&state,text,sizeof(text),&length) && length>0,2);
    CHECK(!fut_fragment_program_hlsl(&state,text,4,&length) && !length && !text[0],3);
    state.color_input_valid=0;
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length) && !length && !text[0],4);state=before;
    state.color_input[0]=0x0D200000; /* Initial scratch R1 is not a proven zero. */
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),5);state=before;
    state.color_output[0]=0xC0C; /* Conflicting writes cannot be invented. */
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),6);state=before;
    state.color_output[0]=0x4C00; /* MUX requires fixed-point selector semantics. */
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),7);state=before;
    state.final_words[0]=0xE;
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),8);state=before;
    state.control=9;CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),9);state=before;
    state.stage_program=6;CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),10);state=before;
    ID3D11Device *device=NULL;ID3D11DeviceContext *context=NULL;D3D_FEATURE_LEVEL level;
    CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,D3D11_SDK_VERSION,&device,&level,&context)),11);
    FutNativeConstants completion;CHECK(SUCCEEDED(fut_native_constants_init(&completion,device,context)),12);
    CHECK(verify_math(device,context,&completion,&state,(float[]){.25,.5,.75,.5})==0,13);
    /* Color and alpha read the PREVIOUS stage simultaneously. An in-place
     * RGB write would incorrectly change the subsequent alpha from .75 to .5. */
    fut_graphics_fragment_state(&state,0x1E60,2);
    fut_graphics_fragment_state(&state,0xAC4,0x1C200000);
    fut_graphics_fragment_state(&state,0x264,0x0C200000);
    fut_graphics_fragment_state(&state,0x1E44,0xC00);
    fut_graphics_fragment_state(&state,0xAA4,0xC00);
    CHECK(verify_math(device,context,&completion,&state,(float[]){.5,.5,.5,.75})==0,14);
    const float mapped[8][3]={{.25,.5,.75},{.75,.5,.25},{0,0,.5},{.5,0,0},{0,0,.25},{.25,0,0},{.25,.5,.75},{0,0,0}};
    for(unsigned mapping=0;mapping<8;++mapping) {
        configure(&state,((mapping*32u+4u)<<24)|0x00200000,0x14200000,0);
        float expected[]={mapped[mapping][0],mapped[mapping][1],mapped[mapping][2],.5};
        CHECK(verify_math(device,context,&completion,&state,expected)==0,15);
    }
    const unsigned output_modes[]={0,8,16,24,32,48};
    for(unsigned i=0;i<6;++i) {
        configure(&state,0x04200000,0x14200000,0);state.color_output[0]=(output_modes[i]<<12)|0xC00;
        float expected[4]={0,0,0,.5};
        for(unsigned c=0;c<3;++c) {
            float value=(c+1)*.25f;
            if(output_modes[i]&8)value-=.5f;
            value*=i==4?4:(i==5?.5f:(output_modes[i]&16?2:1));
            expected[c]=value<0?0:(value>1?1:value);
        }
        CHECK(verify_math(device,context,&completion,&state,expected)==0,16);
    }
    configure(&state,0x04050000,0x14200000,0);state.color_output[0]=0x2C00;
    CHECK(verify_math(device,context,&completion,&state,(float[]){.34375,.34375,.34375,.5})==0,17);
    configure(&state,0x01200000,0x14200000,0);
    CHECK(!fut_fragment_program_hlsl(&state,text,sizeof(text),&length),21); /* Missing referenced factor. */
    fut_graphics_fragment_state(&state,0xA60,0xFF204080);
    fut_graphics_fragment_state(&state,0xA64,0xFF804020);
    fut_graphics_fragment_state(&state,0x1E60,0x1002);
    fut_graphics_fragment_state(&state,0xAC4,0x0C010000);
    fut_graphics_fragment_state(&state,0x264,0x14200000);
    fut_graphics_fragment_state(&state,0x1E44,0xC00);fut_graphics_fragment_state(&state,0xAA4,0xC00);
    CHECK(verify_math(device,context,&completion,&state,
        (float[]){32.0f*128/(255*255),64.0f*64/(255*255),128.0f*32/(255*255),.5})==0,22);
    fut_graphics_fragment_state(&state,0x1E60,2);
    CHECK(verify_math(device,context,&completion,&state,
        (float[]){32.0f*32/(255*255),64.0f*64/(255*255),128.0f*128/(255*255),.5})==0,23);
    configure(&state,0x04200000,0x14200000,0);
    for(unsigned stage=1;stage<8;++stage) {
        fut_graphics_fragment_state(&state,0xAC0+stage*4,0x0C200000);
        fut_graphics_fragment_state(&state,0x260+stage*4,0x1C200000);
        fut_graphics_fragment_state(&state,0x1E40+stage*4,0xC00);
        fut_graphics_fragment_state(&state,0xAA0+stage*4,0xC00);
    }
    fut_graphics_fragment_state(&state,0x1E60,8);
    CHECK(verify_math(device,context,&completion,&state,(float[]){.25,.5,.75,.5})==0,24);
    /* First captured game combiner words, but supplied texture/colors are
     * SYNTHETIC. RGB final inputs are zero; alpha comes from diffuse.a. */
    configure(&state,0x08200000,0x14200000,1);state.final_words[0]=0;state.final_words[1]=0x1C80;
    CHECK(verify_math(device,context,&completion,&state,(float[]){0,0,0,.5})==0,18);
    FutGraphicsKelvinState kelvin={0};kelvin.fragment=state;
    FutNativeTexture textures[4]={0};ID3D11PixelShader *shader=NULL;ID3DBlob *bytecode=NULL;
    CHECK(fut_native_fragment_compile(device,&kelvin,textures,&shader,&bytecode)==E_NOTIMPL && !shader && !bytecode,19);
    /* No-texture supported shader adapter must compile to actual PS bytecode. */
    configure(&kelvin.fragment,0x04200000,0x14200000,0);
    CHECK(SUCCEEDED(fut_native_fragment_compile(device,&kelvin,textures,&shader,&bytecode)) && shader && bytecode,20);
    ID3D11PixelShader_Release(shader);ID3D10Blob_Release(bytecode);
    fut_native_constants_destroy(&completion);ID3D11DeviceContext_Release(context);ID3D11Device_Release(device);
    puts("Combiner HLSL: parallel RGB/alpha, input mapping, output bias/scale/dot and strict rejection verified on WARP; no NV2A precision/original draw proof");
    return 0;
}
