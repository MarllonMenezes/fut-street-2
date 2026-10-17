/*
 * Copyright (C) 2018 Marcelina Kościelnicka <mwk@0x04.net>
 * All Rights Reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice (including the next
 * paragraph) shall be included in all copies or substantial portions of the
 * Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR
 * OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */
/* HLSL adaptation of envytools nvhw/xf.c xf_rcp, v2 RCC only.
 * https://github.com/envytools/envytools/blob/f102b82381f3f11cee113d16374c87091db039d9/nvhw/xf.c
 * 64-bit integer products are represented by two shader-model-5 uints. */
#ifndef FUT_GRAPHICS_VERTEX_RCC_H
#define FUT_GRAPHICS_VERTEX_RCC_H
#define FUT_VERTEX_RCC_HLSL \
"static const uint fut_rcc_lut[64]={0x7f,0x7d,0x7b,0x79,0x78,0x76,0x74,0x73,0x71,0x6f,0x6e,0x6d,0x6b,0x6a,0x68,0x67,0x66,0x65,0x63,0x62,0x61,0x60,0x5f,0x5e,0x5d,0x5c,0x5b,0x5a,0x59,0x58,0x57,0x56,0x55,0x54,0x53,0x52,0x52,0x51,0x50,0x4f,0x4e,0x4e,0x4d,0x4c,0x4c,0x4b,0x4a,0x49,0x49,0x48,0x48,0x47,0x46,0x46,0x45,0x45,0x44,0x43,0x43,0x42,0x42,0x41,0x41,0x40};\n" \
"void fut_rcc_mul(uint a,uint b,out uint lo,out uint hi){\n" \
"uint a0=a&65535u,a1=a>>16,b0=b&65535u,b1=b>>16;\n" \
"uint u=a0*b0,t=a1*b0+(u>>16),w=t&65535u,k=t>>16;\n" \
"t=a0*b1+w;hi=a1*b1+k+(t>>16);lo=(t<<16)|(u&65535u);}\n" \
"float fut_rcc(float inputValue){\n" \
"uint x=asuint(inputValue),e=(x>>23)&255u,f=x&0x7fffffu;\n" \
"if(e==255u && f!=0u)return asfloat(0x7fffffffu);\n" \
"int er=254-int(e);uint fr=0u;\n" \
"if(f!=0u){\n" \
"f+=0x800000u;uint s0=fut_rcc_lut[(f>>17)&63u];\n" \
"uint lo,hi;fut_rcc_mul(0x80000000u-s0*f,s0,lo,hi);\n" \
"uint s1=(lo>>24)|(hi<<8);\n" \
"// (2^37-s1*f)*s1 >>25; retain exact low64 product.\n" \
"uint pLo,pHi;fut_rcc_mul(s1,f,pLo,pHi);\n" \
"uint dLo=0u-pLo,dHi=32u-pHi-(pLo!=0u?1u:0u);\n" \
"fut_rcc_mul(dLo,s1,lo,hi);hi+=dHi*s1;\n" \
"fr=((lo>>25)|(hi<<7))-0x800000u;er--;\n" \
"}\n" \
"if(er<63){er=63;fr=0u;}if(er>=191){er=191;fr=0u;}\n" \
"return asfloat((x&0x80000000u)|(uint(er)<<23)|fr);\n" \
"}\n"
#endif

