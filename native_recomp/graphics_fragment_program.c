#include "graphics_fragment_program.h"
#include <stdarg.h>
#include <stdio.h>
#include <string.h>

FutFragmentStatus fut_graphics_fragment_state(FutGraphicsFragmentState *s,uint32_t method,uint32_t value)
{
    if(!s || (method&3)) return FUT_FRAGMENT_REJECTED;
    uint32_t *target=NULL; unsigned *mask=NULL,slot=0;
    if(method>=0x260 && method<0x280) {target=s->alpha_input;mask=&s->alpha_input_valid;slot=(method-0x260)/4;}
    else if(method>=0xA60 && method<0xAA0) {
        unsigned index=(method-0xA60)/4;
        target=s->factors[index/8];mask=&s->factor_valid[index/8];slot=index%8;
    } else if(method>=0xAA0 && method<0xAC0) {target=s->alpha_output;mask=&s->alpha_output_valid;slot=(method-0xAA0)/4;}
    else if(method>=0xAC0 && method<0xAE0) {target=s->color_input;mask=&s->color_input_valid;slot=(method-0xAC0)/4;}
    else if(method>=0x1E40 && method<0x1E60) {target=s->color_output;mask=&s->color_output_valid;slot=(method-0x1E40)/4;}
    else if(method==0x1E20 || method==0x1E24) {target=s->final_factor;mask=&s->final_factor_valid;slot=(method-0x1E20)/4;}
    else {
        switch(method) {
        case 0x1E60: s->control=value;s->valid|=1;break;
        case 0x1E70: s->stage_program=value;s->valid|=2;break;
        case 0x1E74: s->dot_rgbmapping=value;break;
        case 0x288: s->final_words[0]=value;s->valid|=4;break;
        case 0x28C: s->final_words[1]=value;s->valid|=8;break;
        default:return FUT_FRAGMENT_NOT_HANDLED;
        }
    }
    if(target){target[slot]=value;*mask|=1u<<slot;}
    s->dirty=1;return FUT_FRAGMENT_APPLIED;
}
typedef struct {char text[65536];size_t used;int failed;} Writer;
static void emit(Writer *w,const char *format,...)
{
    if(w->failed) return;
    va_list args;va_start(args,format);
    int count=vsnprintf(w->text+w->used,sizeof(w->text)-w->used,format,args);va_end(args);
    if(count<0 || (size_t)count>=sizeof(w->text)-w->used) w->failed=1;
    else w->used+=(size_t)count;
}
static void color(Writer *w,const char *name,uint32_t word)
{
    emit(w,"float4 %s=float4(%.9g,%.9g,%.9g,%.9g);\n",name,
        ((word>>16)&255)/255.0,((word>>8)&255)/255.0,(word&255)/255.0,(word>>24)/255.0);
}
static int writable(unsigned reg) {return reg==0 || reg==4 || reg==5 || (reg>=8 && reg<=13);}
static int input(char *out,size_t capacity,unsigned byte,int alpha,int final,
    const FutGraphicsFragmentState *s,unsigned stage,const unsigned defined[16])
{
    unsigned reg=byte&15,mapping=byte>>5;
    const char *channel=alpha?((byte&16)?"a":"b"):((byte&16)?"aaa":"rgb");
    unsigned channels=alpha?((byte&16)?8u:4u):((byte&16)?8u:7u);
    if(final && mapping>1) return 0;
    char base[64];
    if(reg==0) snprintf(base,sizeof(base),"float4(0,0,0,0)");
    else if(reg==1 || reg==2) {
        unsigned factor=reg-1,index=(s->control & (factor?0x10000u:0x1000u))?stage:0;
        if(final) {if(!(s->final_factor_valid & (1u<<factor))) return 0;}
        else if(!(s->factor_valid[factor] & (1u<<index))) return 0;
        snprintf(base,sizeof(base),"C%u",factor);
    } else {
        /* No made-up fog value, initial scratch RGB, SUM or EF product. */
        if(!writable(reg) || reg==0 || (defined[reg]&channels)!=channels) return 0;
        snprintf(base,sizeof(base),"r[%u]",reg);
    }
    static const char *maps[]={"max((%s).%s,0.0)","(1.0-max((%s).%s,0.0))",
        "(2.0*max((%s).%s,0.0)-1.0)","(1.0-2.0*max((%s).%s,0.0))",
        "(max((%s).%s,0.0)-0.5)","(0.5-max((%s).%s,0.0))","((%s).%s)","(-(%s).%s)"};
    int count=snprintf(out,capacity,maps[mapping],base,channel);
    return count>0 && (size_t)count<capacity;
}
static int portion(Writer *w,const FutGraphicsFragmentState *s,unsigned stage,int alpha,const unsigned defined[16])
{
    uint32_t icw=alpha?s->alpha_input[stage]:s->color_input[stage];
    uint32_t ocw=alpha?s->alpha_output[stage]:s->color_output[stage];
    unsigned flags=ocw>>12,mapping=flags&0x38;
    if((flags&3) && s->dot_rgbmapping) return 0;
    if((ocw & ~0x3FFFFu) || (flags&4) || (alpha && (flags&3)) || mapping==0x28 || mapping==0x38) return 0;
    unsigned dest[3]={(ocw>>4)&15,ocw&15,(ocw>>8)&15};
    for(unsigned i=0;i<3;++i) {
        if(!writable(dest[i])) return 0;
        for(unsigned j=0;j<i;++j) if(dest[i] && dest[i]==dest[j]) return 0;
    }
    const char *tag=alpha?"a":"c",*type=alpha?"float":"float3";
    for(unsigned i=0;i<4;++i) {
        char expression[256];
        if(!input(expression,sizeof(expression),(icw>>(24-i*8))&255,alpha,0,s,stage,defined)) return 0;
        emit(w,"%s %s%c=%s;\n",type,tag,'A'+i,expression);
    }
    emit(w,"%s %sAB=%s; %s %sCD=%s; %s %sSUM=%sAB+%sCD;\n",type,tag,
        flags&2?"dot(cA,cB)":(alpha?"aA*aB":"cA*cB"),type,tag,
        flags&1?"dot(cC,cD)":(alpha?"aC*aD":"cC*cD"),type,tag,tag,tag);
    double bias=(mapping&8)?0.5:0,scale=mapping==0x20?4:(mapping==0x30?0.5:((mapping&0x10)?2:1));
    const char *terms[]={"AB","CD","SUM"};
    for(unsigned i=0;i<3;++i) emit(w,"%s%s=clamp((%s%s-%.1f)*%.1f,-1.0,1.0);\n",tag,terms[i],tag,terms[i],bias,scale);
    return !w->failed;
}
static void assign(Writer *w,uint32_t ocw,int alpha,unsigned defined[16])
{
    const unsigned dest[]={(ocw>>4)&15,ocw&15,(ocw>>8)&15};
    const char *terms[]={"AB","CD","SUM"};
    for(unsigned i=0;i<3;++i) if(dest[i]) {
        emit(w,"r[%u].%s=%s%s;\n",dest[i],alpha?"a":"rgb",alpha?"a":"c",terms[i]);
        defined[dest[i]]|=alpha?8:7;
    }
}
int fut_fragment_program_hlsl(const FutGraphicsFragmentState *s,char *text,size_t capacity,size_t *length)
{
    if(length) *length=0;
    if(text && capacity) text[0]=0;
    if(!s || !text || !capacity || !length || (s->valid&15)!=15 ||
        (s->control & ~0x111FFu) || (s->control&255)>8 || (s->stage_program & ~0xFFFFFu) ||
        (s->final_words[1] & 0x1F)) return 0;
    unsigned count=s->control&255,mask=(1u<<count)-1;
    if((s->color_input_valid&mask)!=mask || (s->alpha_input_valid&mask)!=mask ||
        (s->color_output_valid&mask)!=mask || (s->alpha_output_valid&mask)!=mask) return 0;
    Writer w={0};unsigned defined[16]={0};defined[4]=defined[5]=15;defined[12]=8;
    emit(&w,"float4 fut_fragment_math(float4 v0,float4 v1,float4 t0,float4 t1,float4 t2,float4 t3){\n"
        "float4 r[16];[unroll]for(uint i=0;i<16;i++)r[i]=0; r[4]=saturate(v0);r[5]=saturate(v1);\n");
    for(unsigned stage=0;stage<4;++stage) {
        unsigned mode=(s->stage_program>>(stage*5))&31;
        if(mode!=0 && mode!=1 && mode!=4) return 0;
        if(mode) {emit(&w,"r[%u]=t%u;\n",8+stage,stage);defined[8+stage]=15;}
        else {emit(&w,"r[%u]=float4(0,0,0,1);\n",8+stage);defined[8+stage]=8;}
    }
    emit(&w,"r[12].a=%s;\n",(s->stage_program&31)?"r[8].a":"1.0");
    for(unsigned stage=0;stage<count;++stage) {
        emit(&w,"{\n");
        for(unsigned factor=0;factor<2;++factor) {
            unsigned index=(s->control&(factor?0x10000:0x1000))?stage:0;
            color(&w,factor?"C1":"C0",s->factors[factor][index]);
        }
        if(!portion(&w,s,stage,0,defined) || !portion(&w,s,stage,1,defined)) return 0;
        /* Both portions captured their old inputs before any assignment. */
        assign(&w,s->color_output[stage],0,defined);assign(&w,s->alpha_output[stage],1,defined);
        emit(&w,"}\n");
    }
    if(!s->final_words[0] && !s->final_words[1]) {
        if((defined[12]&15)!=15) return 0;
        emit(&w,"return saturate(r[12]);}\n");
    } else {
        color(&w,"C0",s->final_factor[0]);color(&w,"C1",s->final_factor[1]);
        for(unsigned i=0;i<7;++i) {
            unsigned byte=i<4?(s->final_words[0]>>(24-i*8))&255:(s->final_words[1]>>(24-(i-4)*8))&255;
            char expression[256];
            if(!input(expression,sizeof(expression),byte,i==6,1,s,8,defined)) return 0;
            emit(&w,"%s F%c=%s;\n",i==6?"float":"float3",'A'+i,expression);
        }
        emit(&w,"return saturate(float4(FA*FB+(1.0-FA)*FC+FD,FG));}\n");
    }
    if(w.failed || w.used>=capacity) return 0;
    memcpy(text,w.text,w.used+1);*length=w.used;return 1;
}
