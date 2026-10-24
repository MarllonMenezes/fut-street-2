#define COBJMACROS
#include "graphics_native_vertex.h"
#include "graphics_native_constants.h"
#include "graphics_vertex_program.h"
#include <stdio.h>
#include <string.h>
#include <math.h>

/* Golden RCC bits from envytools xf_rcp(v2), including its LUT rounding. */
static const uint32_t rcc_cases[][2]={
    {0x00000000u,0x5F800000u},{0x80000000u,0xDF800000u},
    {0x7F800000u,0x1F800000u},{0xFF800000u,0x9F800000u},
    {0x7FC00000u,0x7FFFFFFFu},{0x40000000u,0x3F000000u},
    {0xC0000000u,0xBF000000u},{0x3FC00000u,0x3F2AAAAAu},
    {0x3F800001u,0x3F7FFFFDu},{0x60000000u,0x1F800000u},
    {0x1F800000u,0x5F800000u},{0x00000001u,0x5F800000u},
    {0x80000001u,0xDF800000u}
};
static int run_fixture(int mode)
{
    int dp4=mode==1,rcc=mode>=2,mad=mode==-2,exponent=mode<=-3 && mode>=-7;
    int addressed=mode<=-8 && mode>=-16,lighting=mode<=-20;
    static const float addresses[]={2.75f,-1.25f,.99999f,95,-96,96,-97,NAN,1000000};
    static const float exp_inputs[]={-1.25f,0.0f,1.5f,-0.0001f,2.0f};
    /* Synthetic original-format program with independent expected results:
     * R1=v0; paired MOV oD0=v1 / RCP R1.w=1/old(R1.w);
     * R2=R1*c0; oPos=R2+c1. The paired MAC's R1 write is suppressed.
     * It exercises word-1 opcodes, output versus temp masks, source snapshots
     * and the paired ILU R1 destination, rather than just compilation. */
    uint32_t program[8][4] = {
        {0, (1u<<21)|0x1Bu, 2u<<26, (15u<<24)|(1u<<20)},
        {0, (1u<<21)|(2u<<25)|(1u<<9)|0x1Bu, (2u<<26)|0x3FCu,
            (1u<<30)|(1u<<28)|(15u<<24)|(1u<<20)|(1u<<16)|(15u<<12)|0x800u|(3u<<3)},
        {0, (2u<<21)|0x1Bu, (1u<<28)|(1u<<26)|(3u<<11)|(1u<<21)|(2u<<19)|(3u<<17),
            (15u<<24)|(2u<<20)},
        {0, (3u<<21)|(1u<<13)|0x1Bu, (2u<<28)|(1u<<26)|0x6Cu,
            (3u<<28)|(15u<<12)|0x800u|1u}
    };
    unsigned count = (dp4 || rcc || mad) ? 5 : 4;
    /* Same paired-source snapshot and R1 destination check, now with RCC. */
    if(mode==-1) program[1][1]=(program[1][1]&~(7u<<25))|(3u<<25);
    if (dp4) {
        program[3][3] &= ~1u;
        /* Independent dot: (5,14,27,7).(1,2,3,4)=142; write x/z only. */
        program[4][1]=(7u<<21)|(1u<<13)|0x1Bu;
        program[4][2]=(12u<<28)|(1u<<26)|(3u<<11)|(1u<<21)|(2u<<19)|(3u<<17);
        program[4][3]=(10u<<12)|0x800u|1u;
    }
    if (rcc) {
        program[3][3]&=~1u;
        /* RCC v2.x -> oPos.xz; other position/color components survive. */
        program[4][1]=(3u<<25)|(2u<<9);
        program[4][3]=(2u<<28)|(10u<<12)|0x800u|4u|1u;
    }
    if(mad) {
        program[3][3]&=~1u;
        /* old oPos*c0 + v2 = (9,40,107,38), written to x/z only. */
        program[4][1]=(4u<<21)|(2u<<9)|0x1Bu;
        program[4][2]=(12u<<28)|(1u<<26)|(3u<<11)|(1u<<21)|(2u<<19)|(3u<<17)|0x6Cu;
        program[4][3]=(2u<<28)|(10u<<12)|0x800u|1u;
    }
    char text[16384]; size_t length;
    if (!fut_vertex_program_hlsl(program, count, text, sizeof(text), &length) || !length) return 1;
    if (dp4) {
        uint32_t only_b[1][4]={{0,(7u<<21)|(3u<<9)|0x1Bu,
            (1u<<26)|(2u<<11)|(1u<<21)|(2u<<19)|(3u<<17),0xF801u}};
        uint16_t mask=0;
        if (!fut_vertex_program_input_mask(only_b,1,&mask) || mask!=(1u<<3)) return 24;
    }
    if(mad) {
        uint32_t probe[1][4]={{0,(4u<<21)|(3u<<9)|0x1Bu,
            (1u<<26)|(2u<<11), (1u<<28)|0xF801u}};
        uint16_t mask=0;
        if(!fut_vertex_program_input_mask(probe,1,&mask) || mask!=(1u<<3)) return 25;
        probe[0][2]=(1u<<26)|(1u<<11);probe[0][3]=(2u<<28)|0xF801u;
        if(!fut_vertex_program_input_mask(probe,1,&mask) || mask!=(1u<<3)) return 26;
    }
    uint32_t invalid[5][4]; memcpy(invalid, program, sizeof(invalid));
    invalid[0][1] = (6u<<21)|0x1Bu;
    if (fut_vertex_program_hlsl(invalid, 4, text, sizeof(text), &length) || length) return 2;
    memcpy(invalid, program, sizeof(invalid)); invalid[3][3] &= ~1u;
    if (fut_vertex_program_hlsl(invalid, 4, text, sizeof(text), &length)) return 3;
    if (fut_vertex_program_hlsl(program, 4, text, 8, &length)) return 4;
    memcpy(invalid, program, sizeof(invalid)); invalid[0][2] = 0; /* Undefined source mux. */
    if (fut_vertex_program_hlsl(invalid, 4, text, sizeof(text), &length) || length) return 18;
    memcpy(invalid, program, sizeof(invalid)); invalid[0][2] = (13u<<28)|(1u<<26);
    if (fut_vertex_program_hlsl(invalid, 4, text, sizeof(text), &length)) return 19;
    memcpy(invalid, program, sizeof(invalid)); invalid[3][3] &= ~0x800u; /* Constant output. */
    if (fut_vertex_program_hlsl(invalid, 4, text, sizeof(text), &length)) return 20;
    memcpy(invalid, program, sizeof(invalid)); invalid[3][3] |= 2u; /* Relative constant input. */
    if (!!fut_vertex_program_hlsl(invalid, 4, text, sizeof(text), &length) != (count==4)) return 21;
    memcpy(invalid, program, sizeof(invalid));
    invalid[3][1] = (invalid[3][1] & ~(255u<<13)) | (192u<<13);
    if (fut_vertex_program_hlsl(invalid, 4, text, sizeof(text), &length)) return 22;
    length = 99;
    if (fut_vertex_program_hlsl(NULL, 4, text, sizeof(text), &length) || length) return 23;
    if(exponent) {
        /* MUL overwrites R2 while EXP must read old R2.x and write paired R1.
         * Position exports all EXP lanes; color exports the independent MUL. */
        memset(program,0,sizeof(program));count=4;
        program[0][1]=(1u<<21)|(2u<<9)|0x1Bu;program[0][2]=2u<<26;
        program[0][3]=(15u<<24)|(2u<<20);
        program[1][1]=(2u<<21)|(5u<<25)|0x1Bu;
        program[1][2]=(2u<<28)|(1u<<26)|(3u<<11)|(1u<<21)|(2u<<19)|(3u<<17);
        program[1][3]=(2u<<30)|(1u<<28)|(15u<<24)|(2u<<20)|(15u<<16);
        program[2][1]=(1u<<21)|0x1Bu;program[2][2]=(1u<<28)|(1u<<26);program[2][3]=0xF800u;
        program[3][1]=(1u<<21)|0x1Bu;program[3][2]=(2u<<28)|(1u<<26);program[3][3]=0xF819u;
        uint16_t mask=0;
        if(!fut_vertex_program_input_mask(program,count,&mask) || mask!=4u)return 27;
    }
    if(addressed) {
        memset(program,0,sizeof(program));count=3;
        /* ARL reads v2.x; paired MOV reads c96 using the OLD initial A0.
         * The next MOV uses the newly written A0. */
        program[0][1]=(13u<<21)|(1u<<25)|(96u<<13)|(2u<<9)|0x1Bu;
        program[0][2]=(2u<<26)|0x6Cu;
        program[0][3]=(3u<<28)|(15u<<16)|2u;
        program[1][1]=(1u<<21)|(96u<<13)|0x1Bu;program[1][2]=3u<<26;program[1][3]=0xF802u;
        program[2][1]=(1u<<21)|0x1Bu;program[2][2]=(1u<<28)|(1u<<26);program[2][3]=0xF819u;
        uint16_t mask=0;
        if(!fut_vertex_program_input_mask(program,count,&mask) || mask!=4u)return 28;
    }
    if(lighting) {
        memset(program,0,sizeof(program));count=6;
        program[0][1]=(5u<<21)|(2u<<9)|0x1Bu;
        program[0][2]=(2u<<26)|(3u<<11)|(1u<<21)|(2u<<19)|(3u<<17);
        program[0][3]=(15u<<24)|(2u<<20);
        program[1][1]=((mode<=-27?12u:10u)<<21)|(1u<<13)|0x1Bu;
        program[1][2]=(2u<<28)|(1u<<26)|(3u<<11)|(1u<<21)|(2u<<19)|(3u<<17);
        program[1][3]=(15u<<24)|(3u<<20);
        program[2][1]=(4u<<25)|(3u<<9);program[2][3]=(2u<<28)|(1u<<20)|(15u<<16);
        program[3][1]=(7u<<25)|(4u<<9);program[3][2]=0x6Cu;program[3][3]=(2u<<28)|0xF804u;
        program[4][1]=(1u<<21)|0x1Bu;program[4][2]=(3u<<28)|(1u<<26);program[4][3]=0xF818u;
        program[5][1]=(1u<<21)|0x1Bu;program[5][2]=(1u<<28)|(1u<<26);program[5][3]=0x8819u;
        uint16_t mask=0;
        if(!fut_vertex_program_input_mask(program,count,&mask) || mask!=((1u<<2)|(1u<<3)|(1u<<4)))return 29;
    }
    ID3D11Device *device = NULL; ID3D11DeviceContext *context = NULL; D3D_FEATURE_LEVEL level;
    if (FAILED(D3D11CreateDevice(NULL, D3D_DRIVER_TYPE_WARP, NULL, 0, NULL, 0,
        D3D11_SDK_VERSION, &device, &level, &context))) return 5;
    ID3D11VertexShader *shader = NULL; ID3DBlob *bytecode = NULL;
    if (FAILED(fut_native_vertex_compile(device, program, count, &shader, &bytecode))) return 6;
    FutNativeConstants constants;
    if (FAILED(fut_native_constants_init(&constants, device, context))) return 7;
    float uniforms[192][4] = {{0}};
    const float c0[] = {2,3,4,6}, c1[] = {1,2,3,4};
    memcpy(uniforms[0], c0, sizeof(c0)); memcpy(uniforms[1], c1, sizeof(c1));
    if(addressed) {
        for(unsigned i=0;i<192;++i){uniforms[i][0]=(float)i;uniforms[i][1]=.25f;uniforms[i][2]=.5f;uniforms[i][3]=1;}
        const float initial_address[]={.1f,.2f,.3f,.4f};memcpy(uniforms[96],initial_address,16);
    }
    if(lighting) {const float maximum[]={.1f,.3f,.15f,.4f};memcpy(uniforms[1],maximum,16);
        if(mode==-28){const float equality[]={0,1,-1,0};memcpy(uniforms[1],equality,16);}}
    if (!fut_native_constants_finish(&constants, (const uint32_t (*)[4])uniforms, 2000, NULL)) return 8;
    D3D11_INPUT_ELEMENT_DESC elements[16] = {0};
    for (unsigned i = 0; i < 16; ++i) {
        elements[i].SemanticName = "ATTR";
        elements[i].SemanticIndex = i;
        elements[i].Format = DXGI_FORMAT_R32G32B32A32_FLOAT;
        elements[i].AlignedByteOffset = i * 16;
        elements[i].InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
    }
    ID3D11InputLayout *layout = NULL;
    HRESULT hr = ID3D11Device_CreateInputLayout(device, elements, 16,
        ID3D10Blob_GetBufferPointer(bytecode), ID3D10Blob_GetBufferSize(bytecode), &layout);
    if (FAILED(hr)) { fprintf(stderr, "input layout failed: %08lX\n", (unsigned long)hr); return 9; }
    D3D11_SO_DECLARATION_ENTRY declaration[] = {
        {0,"SV_POSITION",0,0,4,0}, {0,"COLOR",0,0,4,0}
    };
    UINT output_stride = 32;
    ID3D11GeometryShader *stream_shader = NULL;
    if (FAILED(ID3D11Device_CreateGeometryShaderWithStreamOutput(device,
        ID3D10Blob_GetBufferPointer(bytecode), ID3D10Blob_GetBufferSize(bytecode),
        declaration, 2, &output_stride, 1, D3D11_SO_NO_RASTERIZED_STREAM, NULL, &stream_shader))) return 10;
    float vertex[16][4] = {{2,4,6,2},{0.25f,0.5f,0.75f,1}};
    if(mad) {const float add[]={-1,-2,-1,-4};memcpy(vertex[2],add,sizeof(add));}
    if(rcc) memcpy(&vertex[2][0],&rcc_cases[mode-2][0],4);
    if(exponent) {vertex[2][0]=exp_inputs[-mode-3];vertex[2][1]=vertex[2][2]=vertex[2][3]=0.1f;}
    if(addressed)vertex[2][0]=addresses[-mode-8];
    if(lighting) {
        const float dp[]={.01f,.02f,.03f,100},lit[]={.4f,.5f,0,2};
        memcpy(vertex[2],dp,16);memcpy(vertex[4],lit,16);vertex[3][0]=mode==-21?-4:4;
        if(mode==-22){vertex[3][0]=0;vertex[4][0]=0;}
        if(mode==-23){vertex[3][0]=INFINITY;vertex[4][1]=0;vertex[4][3]=0;}
        if(mode==-24)vertex[4][0]=-.4f;
        if(mode==-25){vertex[4][1]=0;vertex[4][3]=-2;}
        if(mode==-26){vertex[4][1]=.99f;vertex[4][3]=200;}
        if(mode==-28)vertex[2][0]=vertex[2][1]=vertex[2][2]=0;
    }
    D3D11_BUFFER_DESC desc = {0}; desc.ByteWidth = sizeof(vertex);
    desc.Usage = D3D11_USAGE_DEFAULT; desc.BindFlags = D3D11_BIND_VERTEX_BUFFER;
    D3D11_SUBRESOURCE_DATA initial = {vertex,0,0};
    ID3D11Buffer *input = NULL, *output = NULL, *staging = NULL;
    if (FAILED(ID3D11Device_CreateBuffer(device, &desc, &initial, &input))) return 11;
    desc.ByteWidth = output_stride; desc.BindFlags = D3D11_BIND_STREAM_OUTPUT;
    if (FAILED(ID3D11Device_CreateBuffer(device, &desc, NULL, &output))) return 12;
    desc.Usage = D3D11_USAGE_STAGING; desc.BindFlags = 0; desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
    if (FAILED(ID3D11Device_CreateBuffer(device, &desc, NULL, &staging))) return 13;
    UINT stride = sizeof(vertex), offset = 0;
    ID3D11DeviceContext_IASetInputLayout(context, layout);
    ID3D11DeviceContext_IASetVertexBuffers(context, 0, 1, &input, &stride, &offset);
    ID3D11DeviceContext_IASetPrimitiveTopology(context, D3D11_PRIMITIVE_TOPOLOGY_POINTLIST);
    ID3D11DeviceContext_VSSetShader(context, shader, NULL, 0);
    ID3D11DeviceContext_GSSetShader(context, stream_shader, NULL, 0);
    ID3D11DeviceContext_SOSetTargets(context, 1, &output, &offset);
    ID3D11DeviceContext_Draw(context, 1, 0);
    if (!fut_native_constants_finish(&constants, NULL, 2000, NULL)) return 14;
    ID3D11DeviceContext_SOSetTargets(context, 0, NULL, NULL);
    ID3D11DeviceContext_CopyResource(context, (ID3D11Resource *)staging, (ID3D11Resource *)output);
    if (!fut_native_constants_finish(&constants, NULL, 2000, NULL)) return 15;
    D3D11_MAPPED_SUBRESOURCE mapped;
    if (FAILED(ID3D11DeviceContext_Map(context, (ID3D11Resource *)staging, 0, D3D11_MAP_READ, 0, &mapped))) return 16;
    float expected[] = {mad?9:dp4?142:5,14,mad?107:dp4?142:27,7,0.25f,0.5f,0.75f,1};
    if(exponent) {
        float x=exp_inputs[-mode-3],integral=floorf(x);
        expected[0]=exp2f(integral);expected[1]=x-integral;expected[2]=exp2f(x);expected[3]=1;
        expected[4]=fminf(fmaxf(2*x,0),1);expected[5]=0.3f;expected[6]=0.4f;expected[7]=0.6f;
    }
    if(addressed) {
        float x=addresses[-mode-8];
        if(!isfinite(x) || fabsf(x)>65536 || floorf(x)+96<0 || floorf(x)+96>=192)
            expected[0]=expected[1]=expected[2]=expected[3]=NAN;
        else memcpy(expected,uniforms[(int)floorf(x)+96],16);
        memcpy(expected+4,uniforms[96],16);
    }
    if(lighting) {
        float x=fmaxf(vertex[4][0],0),y=fmaxf(vertex[4][1],0),power=fminf(fmaxf(vertex[4][3],-127.99609375f),127.99609375f);
        expected[0]=1;expected[1]=x;expected[2]=x>0?powf(y,power):0;expected[3]=1;
        expected[4]=fminf(1.0f,1.0f/sqrtf(fabsf(vertex[3][0])));expected[5]=.3f;expected[6]=.2f;expected[7]=.4f;
        if(mode<=-27){expected[5]=0;expected[6]=1;expected[7]=mode==-28?1:0;}
    }
    const float *actual = mapped.pData;
    int correct = 1;
    for (unsigned i = 0; i < 8; ++i) {
        if((addressed || lighting) && !isfinite(expected[i])) {
            if(isnan(expected[i])?!isnan(actual[i]):!isinf(actual[i]) || signbit(expected[i])!=signbit(actual[i]))correct=0;
            continue;
        }
        if(rcc && (i==0 || i==2)) {
            uint32_t bits;memcpy(&bits,&actual[i],4);
            if(bits!=rcc_cases[mode-2][1]) {
                fprintf(stderr,"RCC input=%08X got=%08X expected=%08X\n",
                    rcc_cases[mode-2][0],bits,rcc_cases[mode-2][1]);correct=0;
            }
            continue;
        }
        if (!isfinite(actual[i]) || fabsf(actual[i] - expected[i]) > 0.00001f) {
            fprintf(stderr, "shader result[%u]=%g expected=%g\n", i, actual[i], expected[i]); correct = 0;
        }
    }
    ID3D11DeviceContext_Unmap(context, (ID3D11Resource *)staging, 0);
    ID3D11Buffer_Release(staging); ID3D11Buffer_Release(output); ID3D11Buffer_Release(input);
    ID3D11InputLayout_Release(layout); ID3D11GeometryShader_Release(stream_shader);
    ID3D11VertexShader_Release(shader); ID3D10Blob_Release(bytecode);
    fut_native_constants_destroy(&constants);
    ID3D11DeviceContext_Release(context); ID3D11Device_Release(device);
    if (!correct) return 17;
    puts("Original-format shader recompiled and executed: paired issue, reciprocal, masks and outputs verified");
    return 0;
}
static int verify_cache(void)
{
#define CACHE_CHECK(x) do {if(!(x)){fprintf(stderr,"vertex cache contract failed at %d\n",__LINE__);return 40;}}while(0)
    ID3D11Device *device=NULL,*other=NULL;D3D_FEATURE_LEVEL level;
    CACHE_CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,
        D3D11_SDK_VERSION,&device,&level,NULL)));
    CACHE_CHECK(SUCCEEDED(D3D11CreateDevice(NULL,D3D_DRIVER_TYPE_WARP,NULL,0,NULL,0,
        D3D11_SDK_VERSION,&other,&level,NULL)));
    FutNativeVertexCache cache={0};
    /* MOV oPos,c0, then c1: only the complete instruction key distinguishes them. */
    uint32_t a[1][4]={{0,(1u<<21)|0x1Bu,3u<<26,0xF801u}},b[1][4];
    memcpy(b,a,sizeof(b));b[0][1]|=1u<<13;
    ID3D11VertexShader *first=NULL,*shader=NULL;ID3DBlob *first_code=NULL,*code=NULL;
    int hit=99;
    CACHE_CHECK(SUCCEEDED(fut_native_vertex_compile_screen_cached(device,a,1,0,&cache,&first,&first_code,&hit)) && !hit);
    CACHE_CHECK(SUCCEEDED(fut_native_vertex_compile_screen_cached(device,b,1,0,&cache,&shader,&code,&hit)) && !hit);
    ID3D11VertexShader_Release(shader);ID3D10Blob_Release(code);
    CACHE_CHECK(SUCCEEDED(fut_native_vertex_compile_screen_cached(device,a,1,0,&cache,&shader,&code,&hit)) && hit && shader==first && code==first_code);
    ID3D11VertexShader_Release(shader);ID3D10Blob_Release(code);
    CACHE_CHECK(SUCCEEDED(fut_native_vertex_compile_screen_cached(device,a,1,1,&cache,&shader,&code,&hit)) && !hit);
    ID3D11VertexShader_Release(shader);ID3D10Blob_Release(code);
    CACHE_CHECK(SUCCEEDED(fut_native_vertex_compile_screen_cached(other,a,1,0,&cache,&shader,&code,&hit)) && !hit);
    ID3D11Device *owner=NULL;ID3D11VertexShader_GetDevice(shader,&owner);
    CACHE_CHECK(owner==other);ID3D11Device_Release(owner);
    ID3D11VertexShader_Release(shader);ID3D10Blob_Release(code);
    unsigned next=cache.next;
    CACHE_CHECK(SUCCEEDED(fut_native_vertex_compile_screen_cached(device,a,1,0,NULL,&shader,&code,&hit)) && !hit && cache.next==next);
    CACHE_CHECK(ID3D10Blob_GetBufferSize(code)==ID3D10Blob_GetBufferSize(first_code) &&
        !memcmp(ID3D10Blob_GetBufferPointer(code),ID3D10Blob_GetBufferPointer(first_code),ID3D10Blob_GetBufferSize(code)));
    ID3D11VertexShader_Release(shader);ID3D10Blob_Release(code);
    for(unsigned mode=0;mode<6;++mode) {
        uint32_t invalid[1][4];memcpy(invalid,a,sizeof(invalid));
        if(mode==5) invalid[0][1]=(6u<<21)|0x1Bu; /* Unsupported opcode must still fail. */
        CACHE_CHECK(FAILED(fut_native_vertex_compile_screen_cached(mode==0?NULL:device,
            mode==1?NULL:invalid,mode==2?0:mode==3?137:1,mode==4?2:0,&cache,&shader,&code,&hit)) &&
            !shader && !code && !hit && cache.next==next);
    }
    /* Count participates even when a previous successful instruction is a prefix. */
    uint32_t pair[2][4];memcpy(pair[0],a[0],sizeof(a[0]));memcpy(pair[1],b[0],sizeof(b[0]));pair[0][3]&=~1u;
    CACHE_CHECK(SUCCEEDED(fut_native_vertex_compile_screen_cached(device,pair,2,0,&cache,&shader,&code,&hit)) && !hit);
    ID3D11VertexShader_Release(shader);ID3D10Blob_Release(code);
    fut_native_vertex_cache_destroy(&cache);
    /* FIFO fills exactly32 entries, then evicts the oldest. Held caller references survive. */
    for(unsigned i=0;i<=FUT_NATIVE_VERTEX_CACHE_CAPACITY;++i) {
        memcpy(b,a,sizeof(b));b[0][1]|=i<<13;
        CACHE_CHECK(SUCCEEDED(fut_native_vertex_compile_screen_cached(device,b,1,0,&cache,&shader,&code,&hit)) && !hit);
        ID3D11VertexShader_Release(shader);ID3D10Blob_Release(code);
    }
    CACHE_CHECK(cache.next==1);
    CACHE_CHECK(SUCCEEDED(fut_native_vertex_compile_screen_cached(device,b,1,0,&cache,&shader,&code,&hit)) && hit);
    ID3D11VertexShader_Release(shader);ID3D10Blob_Release(code);
    CACHE_CHECK(SUCCEEDED(fut_native_vertex_compile_screen_cached(device,a,1,0,&cache,&shader,&code,&hit)) && !hit);
    ID3D11VertexShader_Release(shader);ID3D10Blob_Release(code);
    fut_native_vertex_cache_destroy(&cache);fut_native_vertex_cache_destroy(&cache);
    CACHE_CHECK(!cache.next && ID3D10Blob_GetBufferSize(first_code)>0);
    ID3D11VertexShader_GetDevice(first,&owner);CACHE_CHECK(owner==device);ID3D11Device_Release(owner);
    for(unsigned i=0;i<FUT_NATIVE_VERTEX_CACHE_CAPACITY;++i)
        CACHE_CHECK(!cache.entries[i].shader && !cache.entries[i].bytecode);
    ID3D11VertexShader_Release(first);ID3D10Blob_Release(first_code);
    ID3D11Device_Release(other);ID3D11Device_Release(device);
    puts("Exact bounded vertex cache: alternation, modes, devices, failure, eviction and ownership verified");
    return 0;
#undef CACHE_CHECK
}
int main(void)
{
    int cache=verify_cache();if(cache)return cache;
    for(int mode=-3;mode>=-7;--mode) {int result=run_fixture(mode);if(result)return result;}
    for(int mode=-8;mode>=-16;--mode){int result=run_fixture(mode);if(result)return result;}
    for(int mode=-20;mode>=-28;--mode){int result=run_fixture(mode);if(result)return result;}
    int mad=run_fixture(-2);
    if(mad) return mad;
    int paired=run_fixture(-1);
    if(paired) return paired;
    for(unsigned mode=0;mode<2+sizeof(rcc_cases)/sizeof(rcc_cases[0]);++mode) {
        int result=run_fixture((int)mode);
        if(result) return result;
    }
    return 0;
}
