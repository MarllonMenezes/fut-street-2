#ifndef FUT_GRAPHICS_SHADER_INTERFACE_H
#define FUT_GRAPHICS_SHADER_INTERFACE_H
/* D3D11 adjacent stages share registers, not a name-based linker. Keep the
 * complete declaration (including unused entries) identical in VS and PS.
 * TEXCOORD6 is the raw NV screen position used by vertex-only validation. */
#define FUT_GRAPHICS_VARYINGS_HLSL(name) \
    "struct " name "{float4 pos:SV_POSITION;float4 screen:TEXCOORD6;" \
    "float4 col0:COLOR0;float4 col1:COLOR1;float fog:FOG;float pts:PSIZE;" \
    "float4 back0:TEXCOORD4;float4 back1:TEXCOORD5;" \
    "float4 t0:TEXCOORD0;float4 t1:TEXCOORD1;float4 t2:TEXCOORD2;float4 t3:TEXCOORD3;};\n"
#endif
