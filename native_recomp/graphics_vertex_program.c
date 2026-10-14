#include "graphics_vertex_program.h"
#include <stdio.h>
#include <stdarg.h>
#include <string.h>

typedef struct { char *text; size_t size, used; int failed; } Writer;
static void emit(Writer *out, const char *format, ...)
{
    if (out->failed) return;
    va_list args; va_start(args, format);
    int size = vsnprintf(out->text + out->used, out->size - out->used, format, args);
    va_end(args);
    if (size < 0 || (size_t)size >= out->size - out->used) out->failed = 1;
    else out->used += size;
}
static int source(char *text, size_t size, const uint32_t *w, unsigned operand)
{
    unsigned mux, temp, neg, swizzle[4];
    if (operand == 0) {
        mux = (w[2] >> 26) & 3; temp = w[2] >> 28; neg = (w[1] >> 8) & 1;
        for (unsigned i = 0; i < 4; ++i) swizzle[i] = (w[1] >> (6 - i * 2)) & 3;
    } else if (operand == 1) {
        mux = (w[2] >> 11) & 3; temp = (w[2] >> 13) & 15; neg = (w[2] >> 25) & 1;
        for (unsigned i = 0; i < 4; ++i) swizzle[i] = (w[2] >> (23 - i * 2)) & 3;
    } else {
        mux = (w[3] >> 28) & 3; temp = ((w[2] & 3) << 2) | (w[3] >> 30); neg = (w[2] >> 10) & 1;
        for (unsigned i = 0; i < 4; ++i) swizzle[i] = (w[2] >> (8 - i * 2)) & 3;
    }
    char base[48];
    if (mux == 1 && temp <= 12) snprintf(base, sizeof(base), temp == 12 ? "o[0]" : "r[%u]", temp);
    else if (mux == 2) snprintf(base, sizeof(base), "input.v%u", (w[1] >> 9) & 15);
    else if (mux == 3 && ((w[1] >> 13) & 255) < 192 && !(w[3] & 2))
        snprintf(base, sizeof(base), "c[%u]", (w[1] >> 13) & 255);
    else return 0;
    int written = snprintf(text, size, "%s%s.%c%c%c%c", neg ? "-" : "", base,
        "xyzw"[swizzle[0]], "xyzw"[swizzle[1]], "xyzw"[swizzle[2]], "xyzw"[swizzle[3]]);
    return written > 0 && (size_t)written < size;
}
static void write_mask(Writer *out, const char *destination, unsigned mask, const char *value, int fog)
{
    if (fog && mask) {
        unsigned component = 0; while (!(mask & (8u >> component))) ++component;
        emit(out, "%s.x=%s.%c;\n", destination, value, "xyzw"[component]);
        return;
    }
    for (unsigned i = 0; i < 4; ++i)
        if (mask & (8u >> i)) emit(out, "%s.%c=%s.%c;\n", destination, "xyzw"[i], value, "xyzw"[i]);
}
static int generate(const uint32_t (*words)[4], size_t count,
    char *text, size_t capacity, size_t *length,int screen)
{
    if (!length) return 0;
    *length = 0;
    if (!words || !count || count > 136 || !text || !capacity) return 0;
    text[0] = 0;
    Writer out = {text, capacity, 0, 0};
    if(screen) emit(&out,"cbuffer VertexSurface:register(b3){float4 vertexSurface;};\n");
    emit(&out, "cbuffer Constants:register(b1){float4 c[192];};\nstruct In{\n");
    for (unsigned i = 0; i < 16; ++i) emit(&out, "float4 v%u:ATTR%u;\n", i, i);
    emit(&out, "};struct Out{float4 pos:SV_POSITION;float4 screen:TEXCOORD6;float4 col0:COLOR0;float4 col1:COLOR1;"
        "float fog:FOG;float pts:PSIZE;float4 back0:TEXCOORD4;float4 back1:TEXCOORD5;"
        "float4 t0:TEXCOORD0;float4 t1:TEXCOORD1;float4 t2:TEXCOORD2;float4 t3:TEXCOORD3;};\n"
        "Out main(In input){float4 r[12];float4 o[16];"
        "for(int z=0;z<12;++z)r[z]=0;for(int k=0;k<16;++k)o[k]=float4(0,0,0,1);\n");
    int ended = 0;
    for (size_t index = 0; index < count; ++index) {
        const uint32_t *w = words[index];
        unsigned mac = (w[1] >> 21) & 15, ilu = (w[1] >> 25) & 7;
        /* First exercised program uses MOV/MUL/ADD and MOV/RCP. Other ops
         * require implementations plus semantic tests before acceptance. */
        if (mac > 3 || ilu > 2) return 0;
        char a[96] = "0", b[96] = "0", csrc[96] = "0";
        if ((mac && !source(a, sizeof(a), w, 0)) ||
            (mac == 2 && !source(b, sizeof(b), w, 1)) ||
            ((mac == 3 || ilu) && !source(csrc, sizeof(csrc), w, 2))) return 0;
        emit(&out, "{float4 A=%s;float4 B=%s;float4 C=%s;float4 M=0;float4 I=0;\n", a, b, csrc);
        if (mac == 1) emit(&out, "M=A;\n");
        if (mac == 2) emit(&out, "M=A*B;\n");
        if (mac == 3) emit(&out, "M=A+C;\n");
        if (ilu == 1) emit(&out, "I=C;\n");
        if (ilu == 2) emit(&out, "I=(1.0f/C.x).xxxx;\n");
        unsigned temp = (w[3] >> 20) & 15;
        unsigned mac_mask = (w[3] >> 24) & 15, ilu_mask = (w[3] >> 16) & 15;
        if (ilu && temp == 1) mac_mask = 0;
        /* R12 aliases oPos. Reject concurrent writes until this ordering has
         * an independent execution test rather than guessing a winner. */
        unsigned output_mask = (w[3] >> 12) & 15, output = (w[3] >> 3) & 15;
        if (output_mask && output == 0 &&
            ((mac && mac_mask && temp == 12) || (ilu && ilu_mask && !mac && temp == 12))) return 0;
        char destination[24];
        if (mac && mac_mask) {
            if (temp > 12) return 0;
            snprintf(destination, sizeof(destination), temp == 12 ? "o[0]" : "r[%u]", temp);
            write_mask(&out, destination, mac_mask, "M", 0);
        }
        if (ilu && ilu_mask) {
            unsigned target = mac ? 1 : temp;
            if (target > 12) return 0;
            snprintf(destination, sizeof(destination), target == 12 ? "o[0]" : "r[%u]", target);
            write_mask(&out, destination, ilu_mask, "I", 0);
        }
        if (output_mask) {
            int mux = (w[3] >> 2) & 1;
            if (!(w[3] & 0x800) || output == 1 || output == 2 || output > 12 ||
                (mux ? !ilu : !mac)) return 0;
            snprintf(destination, sizeof(destination), "o[%u]", output);
            write_mask(&out, destination, output_mask, mux ? "I" : "M", output == 5);
        }
        emit(&out, "}\n");
        if (w[3] & 1) { ended = 1; break; }
    }
    if (!ended || out.failed) return 0;
    emit(&out, "Out result;result.pos=o[0];result.screen=o[0];result.col0=saturate(o[3]);result.col1=saturate(o[4]);"
        "result.fog=o[5].x;result.pts=o[6].x;result.back0=saturate(o[7]);result.back1=saturate(o[8]);"
        "result.t0=o[9];result.t1=o[10];result.t2=o[11];result.t3=o[12];\n");
    if(screen) {
        /* NV programmable outputs are already screen coordinates. D3D's
         * viewport flips Y and divides by W: invert both operations here.
         * Only finite positive W in [2^-64,2^64] is admitted by the SO probe;
         * exceptional/clamped W and MSAA sampling remain unsupported. */
        emit(&out,"float2 pixel=trunc(o[0].xy*16.0f)/16.0f;"
            "float2 ndc=(2.0f*pixel-vertexSurface.xy)/vertexSurface.xy;"
            "result.pos=float4(ndc.x,-ndc.y,o[0].z/vertexSurface.z,1)*o[0].w;\n");
    }
    emit(&out,"return result;}\n");
    if (out.failed) return 0;
    *length = out.used;
    return 1;
}

int fut_vertex_program_hlsl(const uint32_t (*words)[4],size_t count,
    char *text,size_t capacity,size_t *length)
{
    return generate(words,count,text,capacity,length,0);
}
int fut_vertex_program_hlsl_screen(const uint32_t (*words)[4],size_t count,
    char *text,size_t capacity,size_t *length)
{
    return generate(words,count,text,capacity,length,1);
}

int fut_vertex_program_input_mask(const uint32_t (*words)[4],size_t count,uint16_t *mask)
{
    if(!mask) return 0;
    *mask=0;
    char text[65536];size_t length;
    if(!fut_vertex_program_hlsl(words,count,text,sizeof(text),&length)) return 0;
    unsigned found=0;
    for(size_t index=0;index<count;++index) {
        const uint32_t *w=words[index];
        unsigned mac=(w[1]>>21)&15,ilu=(w[1]>>25)&7;
        if((mac && ((w[2]>>26)&3)==2) || (mac==2 && ((w[2]>>11)&3)==2) ||
            ((mac==3 || ilu) && ((w[3]>>28)&3)==2)) found|=1u<<((w[1]>>9)&15);
        if(w[3]&1) break;
    }
    *mask=(uint16_t)found;return 1;
}
