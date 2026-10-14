#define COBJMACROS
#include "graphics_native_vertex.h"
#include "graphics_native_constants.h"
#include "graphics_vertex_program.h"
#include <stdio.h>
#include <string.h>
#include <math.h>

int main(void)
{
    /* Synthetic original-format program with independent expected results:
     * R1=v0; paired MOV oD0=v1 / RCP R1.w=1/old(R1.w);
     * R2=R1*c0; oPos=R2+c1. The paired MAC's R1 write is suppressed.
     * It exercises word-1 opcodes, output versus temp masks, source snapshots
     * and the paired ILU R1 destination, rather than just compilation. */
    uint32_t program[4][4] = {
        {0, (1u<<21)|0x1Bu, 2u<<26, (15u<<24)|(1u<<20)},
        {0, (1u<<21)|(2u<<25)|(1u<<9)|0x1Bu, (2u<<26)|0x3FCu,
            (1u<<30)|(1u<<28)|(15u<<24)|(1u<<20)|(1u<<16)|(15u<<12)|0x800u|(3u<<3)},
        {0, (2u<<21)|0x1Bu, (1u<<28)|(1u<<26)|(3u<<11)|(1u<<21)|(2u<<19)|(3u<<17),
            (15u<<24)|(2u<<20)},
        {0, (3u<<21)|(1u<<13)|0x1Bu, (2u<<28)|(1u<<26)|0x6Cu,
            (3u<<28)|(15u<<12)|0x800u|1u}
    };
    char text[16384]; size_t length;
    if (!fut_vertex_program_hlsl(program, 4, text, sizeof(text), &length) || !length) return 1;
    uint32_t invalid[4][4]; memcpy(invalid, program, sizeof(invalid));
    invalid[0][1] = (4u<<21)|0x1Bu;
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
    if (fut_vertex_program_hlsl(invalid, 4, text, sizeof(text), &length)) return 21;
    memcpy(invalid, program, sizeof(invalid));
    invalid[3][1] = (invalid[3][1] & ~(255u<<13)) | (192u<<13);
    if (fut_vertex_program_hlsl(invalid, 4, text, sizeof(text), &length)) return 22;
    length = 99;
    if (fut_vertex_program_hlsl(NULL, 4, text, sizeof(text), &length) || length) return 23;
    ID3D11Device *device = NULL; ID3D11DeviceContext *context = NULL; D3D_FEATURE_LEVEL level;
    if (FAILED(D3D11CreateDevice(NULL, D3D_DRIVER_TYPE_WARP, NULL, 0, NULL, 0,
        D3D11_SDK_VERSION, &device, &level, &context))) return 5;
    ID3D11VertexShader *shader = NULL; ID3DBlob *bytecode = NULL;
    if (FAILED(fut_native_vertex_compile(device, program, 4, &shader, &bytecode))) return 6;
    FutNativeConstants constants;
    if (FAILED(fut_native_constants_init(&constants, device, context))) return 7;
    float uniforms[192][4] = {{0}};
    const float c0[] = {2,3,4,6}, c1[] = {1,2,3,4};
    memcpy(uniforms[0], c0, sizeof(c0)); memcpy(uniforms[1], c1, sizeof(c1));
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
    const float expected[] = {5,14,27,7,0.25f,0.5f,0.75f,1};
    const float *actual = mapped.pData;
    int correct = 1;
    for (unsigned i = 0; i < 8; ++i) {
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
