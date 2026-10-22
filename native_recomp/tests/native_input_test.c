#include "native_input.h"
#include "recomp_types.h"
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
ptrdiff_t g_xbox_mem_offset;
RECOMP_TLS uint32_t g_eax,g_esp,g_fs_base;
#define GP(x) ((void *)XBOX_PTR(x))
#define CHECK(x) do{if(!(x)){fprintf(stderr,"native input failed line=%d\n",__LINE__);return 1;}}while(0)
static uint32_t invoke(uint32_t site,unsigned count,const uint32_t *args)
{
    uint32_t stack[5]={0xDEADBEEFu};memcpy(stack+1,args,count*4);
    memcpy(GP(0x11000u),stack,(count+1)*4);g_esp=0x11000;g_eax=123;
    SetLastError(0x1234);
    if(!fut_native_input_dispatch(site)||g_esp!=0x11000+(count+1)*4||GetLastError()!=0x1234){fprintf(stderr,"SDK stack/error preservation failed\n");ExitProcess(2);}
    return g_eax;
}
int main(void)
{
    _putenv_s("RECOMP_NATIVE_INPUT_SCRIPT","");
    uint16_t digital=0;uint8_t analog[8]={0};
    CHECK(fut_native_input_script("0:start:5000,100:a:200",0,&digital,analog)==1&&digital==0x10&&!analog[0]);
    CHECK(fut_native_input_script("0:start:5000,100:a:200",100,&digital,analog)==1&&digital==0x10&&analog[0]==255);
    CHECK(fut_native_input_script("0:start:5000,100:a:200",300,&digital,analog)==1&&!analog[0]);
    CHECK(fut_native_input_script("0:start:5000,100:a:200",5000,&digital,analog)==0&&!digital);
    const char *bad[]={"","0:unknown:1","0:start:0","0:start:5001","300001:start:2","9999999:start:1","0:start:1,","0:start:1junk",":start:1","0:"};
    for(unsigned i=0;i<sizeof(bad)/sizeof(bad[0]);++i)CHECK(fut_native_input_script(bad[i],0,&digital,analog)==-1);
    _putenv_s("RECOMP_NATIVE_INPUT","0");g_eax=123;g_esp=0xFFFFFFFFu;SetLastError(0x1234);
    CHECK(!fut_native_input_dispatch(0x2A2112)&&g_eax==123&&g_esp==0xFFFFFFFFu&&GetLastError()==0x1234);
    _putenv_s("RECOMP_NATIVE_INPUT","2");CHECK(!fut_native_input_dispatch(0x2A2112));
    _putenv_s("RECOMP_NATIVE_INPUT","1");CHECK(!fut_native_input_dispatch(0xDEADBEEF));
    uint8_t *memory=VirtualAlloc(NULL,0x400000,MEM_COMMIT|MEM_RESERVE,PAGE_READWRITE);
    CHECK(memory);g_xbox_mem_offset=(ptrdiff_t)(uintptr_t)memory-0x10000;
    g_fs_base=0x12000;MEM32(0x12004)=0x13000;MEM32(0x13000)=0x14000;MEM32(0x3551FC)=0;
    uint32_t a[4]={0x2A11B4,0,0,0};CHECK(invoke(0x2A2419,1,a)==1);
    a[1]=0x15000;a[2]=0x15004;MEM32(0x15000)=99;MEM32(0x15004)=99;
    CHECK(invoke(0x2A243B,3,a)==0&&!MEM32(0x15000)&&!MEM32(0x15004));
    a[1]=5;a[2]=a[3]=0;CHECK(invoke(0x2A2112,4,a)==0&&MEM32(0x14008)==ERROR_INVALID_PARAMETER);
    a[1]=0;a[2]=1;CHECK(invoke(0x2A2112,4,a)==0&&MEM32(0x14008)==ERROR_INVALID_PARAMETER);
    a[2]=0;uint32_t handle=invoke(0x2A2112,4,a);CHECK((handle&0xFFF00000u)==0xF2700000u);
    a[0]=handle;a[1]=0x16000;memset(GP(a[1]),0xA5,40);
    CHECK(invoke(0x2A2174,2,a)==0);
    uint8_t expected_caps[25]={2,0,0,255,0,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,0,0,0,0};
    CHECK(!memcmp(GP(a[1]),expected_caps,25)&&MEM8(a[1]+25)==0xA5);
    fut_native_input_message(WM_KEYDOWN,VK_RETURN);fut_native_input_message(WM_KEYDOWN,'Z');
    fut_native_input_message(WM_KEYDOWN,'Q');fut_native_input_message(WM_KEYDOWN,'D');
    CHECK(invoke(0x2A234C,2,a)==0);
    uint8_t expected_state[18]={0x10,0,255,0,0,0,0,255,0,0,255,127,0,0,0,0,0,0};
    CHECK(!memcmp(GP(a[1]+4),expected_state,18)&&MEM8(a[1]+22)==0);
    uint32_t packet=MEM32(a[1]);CHECK(packet!=0);
    MEM8(a[1]+22)=0xA5;CHECK(invoke(0x2A234C,2,a)==0&&MEM32(a[1])==packet&&MEM8(a[1]+22)==0xA5);
    fut_native_input_message(WM_KILLFOCUS,0);CHECK(invoke(0x2A234C,2,a)==0&&MEM32(a[1])!=packet);
    uint8_t zero[18]={0};CHECK(!memcmp(GP(a[1]+4),zero,18));
    DWORD old;CHECK(VirtualProtect(memory+0x3FF000,4096,PAGE_NOACCESS,&old));
    a[1]=0x40F000;CHECK(invoke(0x2A234C,2,a)==ERROR_INVALID_PARAMETER);
    a[1]=0x40EFF8;memset(GP(a[1]),0xA5,8);
    CHECK(invoke(0x2A234C,2,a)==ERROR_INVALID_PARAMETER);
    for(unsigned i=0;i<8;++i)CHECK(MEM8(a[1]+i)==0xA5);
    a[1]=0xFFFFFFFFu;CHECK(invoke(0x2A2174,2,a)==ERROR_INVALID_PARAMETER);
    a[1]=0x16000;memset(GP(a[1]),0,70);MEM32(a[1]+4)=1;
    CHECK(invoke(0x2A23BF,2,a)==ERROR_NOT_SUPPORTED&&MEM32(a[1])==ERROR_NOT_SUPPORTED);
    MEM32(a[1]+4)=0;CHECK(invoke(0x2A23BF,2,a)==ERROR_NOT_SUPPORTED);
    CHECK(invoke(0x2A2168,1,a)==0);CHECK(invoke(0x2A234C,2,a)==ERROR_INVALID_HANDLE);
    a[0]=0x2A11B4;a[1]=a[2]=a[3]=0;uint32_t reopened=invoke(0x2A2112,4,a);CHECK(reopened&&reopened!=handle);
    _putenv_s("RECOMP_NATIVE_KEYBOARD_INPUT","0");
    a[1]=0x15000;a[2]=0x15004;
    CHECK(invoke(0x2A243B,3,a)==1&&!MEM32(0x15000)&&MEM32(0x15004)==1);
    CHECK(invoke(0x2A243B,3,a)==0&&!MEM32(0x15000)&&!MEM32(0x15004));
    a[0]=reopened;a[1]=0x16000;MEM32(a[1])=0xAABBCCDD;
    CHECK(invoke(0x2A234C,2,a)==ERROR_DEVICE_NOT_CONNECTED&&MEM32(a[1])==0xAABBCCDD);
    _putenv_s("RECOMP_NATIVE_KEYBOARD_INPUT","1");
    a[0]=0x2A11B4;a[1]=0x15000;a[2]=0x15004;
    CHECK(invoke(0x2A243B,3,a)==1&&MEM32(0x15000)==1&&!MEM32(0x15004));
    a[0]=reopened;a[1]=0x16000;CHECK(invoke(0x2A234C,2,a)==0);
    CHECK(invoke(0x2A2168,1,a)==0);
    g_esp=0x11000;MEM32(g_esp+4)=0x2A11B8;g_eax=42;
    CHECK(!fut_native_input_dispatch(0x2A2419)&&g_esp==0x11000&&g_eax==42);
    VirtualFree(memory,0,MEM_RELEASE);return 0;
}
