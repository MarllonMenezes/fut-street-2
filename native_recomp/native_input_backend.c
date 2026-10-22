#include "native_input.h"
#include <xinput.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

static SRWLOCK sample_lock=SRWLOCK_INIT;
static volatile LONG keys[256];
static uint8_t previous[4][18];
static DWORD packets[4];
static unsigned connected[4];
static ULONGLONG origin;
static int exact_option(const char *name){const char *s=getenv(name);return s&&!strcmp(s,"1");}
int fut_native_input_enabled(void){return exact_option("RECOMP_NATIVE_INPUT");}
static void word(uint8_t *p,uint16_t x){p[0]=(uint8_t)x;p[1]=(uint8_t)(x>>8);}
static void dword(uint8_t *p,uint32_t x){for(unsigned i=0;i<4;++i)p[i]=(uint8_t)(x>>(8*i));}
static int held(unsigned k){return k<256&&InterlockedCompareExchange(keys+k,0,0)!=0;}
void fut_native_input_message(UINT message,WPARAM key)
{
    if(message==WM_KILLFOCUS||message==WM_DESTROY){for(unsigned i=0;i<256;++i)InterlockedExchange(keys+i,0);return;}
    if(!fut_native_input_enabled()||!exact_option("RECOMP_NATIVE_KEYBOARD_INPUT")||key>=256)return;
    if(message==WM_KEYDOWN)InterlockedExchange(keys+key,1);
    if(message==WM_KEYUP)InterlockedExchange(keys+key,0);
}
static int number(const char **p,unsigned *out)
{
    unsigned n=0,count=0;
    while(**p>='0'&&**p<='9'){if(++count>6)return 0;n=n*10+(unsigned)(*(*p)++-'0');}
    *out=n;return count!=0;
}
int fut_native_input_script(const char *spec,uint64_t elapsed,uint16_t *digital,uint8_t analog[8])
{
    if(!spec||!spec[0]||strlen(spec)>512||!digital||!analog)return -1;
    const char *p=spec;uint16_t bits=0;uint8_t pressures[8]={0};unsigned steps=0;int active=0;
    static const struct {const char *name;uint16_t bit;int analog;} buttons[]={
        {"start",0x10,-1},{"back",0x20,-1},{"up",1,-1},{"down",2,-1},
        {"left",4,-1},{"right",8,-1},{"a",0,0},{"b",0,1},{"x",0,2},{"y",0,3}};
    while(*p){
        unsigned at,hold; if(++steps>32||!number(&p,&at)||at>300000||*p++!=':')return -1;
        const char *name=p;while(*p>='a'&&*p<='z')++p;
        size_t length=(size_t)(p-name);int button=-1;
        for(unsigned i=0;i<sizeof(buttons)/sizeof(buttons[0]);++i)
            if(strlen(buttons[i].name)==length&&!memcmp(name,buttons[i].name,length))button=(int)i;
        if(button<0||*p++!=':'||!number(&p,&hold)||!hold||hold>5000)return -1;
        if(elapsed>=at&&elapsed-at<hold){active=1;bits|=buttons[button].bit;if(buttons[button].analog>=0)pressures[buttons[button].analog]=255;}
        if(*p&&*p!=',')return -1;
        if(*p==','&&!*++p)return -1;
    }
    *digital=bits;memcpy(analog,pressures,8);return active;
}
static SHORT axis(unsigned negative,unsigned positive){return (SHORT)((held(positive)-held(negative))*32767);}
DWORD fut_native_input_sample(unsigned port,FutNativeInputSample *out)
{
    if(port>=4||!out)return ERROR_INVALID_PARAMETER;
    XINPUT_STATE host={0};XINPUT_CAPABILITIES caps={0};
    const char *physical=getenv("RECOMP_NATIVE_INPUT_PHYSICAL");
    DWORD status=physical&&!strcmp(physical,"0")?ERROR_DEVICE_NOT_CONNECTED:XInputGetState(port,&host);
    FutNativeInputSample s={0};s.physical=status==ERROR_SUCCESS;
    uint16_t digital=s.physical?(uint16_t)(host.Gamepad.wButtons&0xFF):0;
    uint8_t analog[8]={0};SHORT axes[4]={0};
    if(s.physical){
        const WORD masks[6]={XINPUT_GAMEPAD_A,XINPUT_GAMEPAD_B,XINPUT_GAMEPAD_X,XINPUT_GAMEPAD_Y,XINPUT_GAMEPAD_RIGHT_SHOULDER,XINPUT_GAMEPAD_LEFT_SHOULDER};
        for(unsigned i=0;i<6;++i)analog[i]=(host.Gamepad.wButtons&masks[i])?255:0;
        analog[6]=host.Gamepad.bLeftTrigger;analog[7]=host.Gamepad.bRightTrigger;
        axes[0]=host.Gamepad.sThumbLX;axes[1]=host.Gamepad.sThumbLY;axes[2]=host.Gamepad.sThumbRX;axes[3]=host.Gamepad.sThumbRY;
    }
    int keyboard=port==0&&exact_option("RECOMP_NATIVE_KEYBOARD_INPUT");
    const char *script=port==0?getenv("RECOMP_NATIVE_INPUT_SCRIPT"):NULL;
    AcquireSRWLockExclusive(&sample_lock);
    ULONGLONG now=GetTickCount64();if(!origin)origin=now;
    if(script&&*script){
        uint16_t b;uint8_t a[8];
        if(fut_native_input_script(script,now-origin,&b,a)<0){ReleaseSRWLockExclusive(&sample_lock);fprintf(stderr,"[NATIVE-INPUT] invalid bounded input script NOT_READY\n");exit(3);}
        digital|=b;for(unsigned i=0;i<8;++i)if(a[i]>analog[i])analog[i]=a[i];
    }
    if(!s.physical&&!keyboard&&!(script&&*script)){connected[port]=0;ReleaseSRWLockExclusive(&sample_lock);return status;}
    if(keyboard){
        const unsigned dk[8]={VK_UP,VK_DOWN,VK_LEFT,VK_RIGHT,VK_RETURN,VK_BACK,VK_SHIFT,VK_CONTROL};
        for(unsigned i=0;i<8;++i)if(held(dk[i]))digital|=(uint16_t)(1u<<i);
        const unsigned ak[8]={'Z','X','C','V','E','Q','1','3'};
        for(unsigned i=0;i<8;++i)if(held(ak[i]))analog[i]=255;
        SHORT k[4]={axis('A','D'),axis('S','W'),axis('J','L'),axis('K','I')};
        for(unsigned i=0;i<4;++i)if(k[i])axes[i]=k[i];
    }
    word(s.state+4,digital);memcpy(s.state+6,analog,8);
    for(unsigned i=0;i<4;++i)word(s.state+14+i*2,(uint16_t)axes[i]);
    if(!connected[port]||memcmp(previous[port],s.state+4,18)){
        memcpy(previous[port],s.state+4,18);++packets[port];
        static unsigned logged;
        if(logged++<128)fprintf(stderr,"[NATIVE-INPUT] state port=%u packet=%lu digital=%04X analog=%u/%u/%u/%u source=%s%s%s NOT_READY\n",port,(unsigned long)packets[port],digital,analog[0],analog[1],analog[2],analog[3],s.physical?"xinput":"",keyboard?" keyboard":"",script&&*script?" automated-script":"");
    }
    connected[port]=1;
    dword(s.state,packets[port]);
    /* Original packed caps: subtype, reserved WORD, 18 input bytes, 4 rumble. */
    s.caps[0]=2;word(s.caps+3,0xFF);memset(s.caps+5,255,8);
    for(unsigned i=0;i<4;++i)word(s.caps+13+i*2,0xFFFF);
    if(s.physical&&XInputGetCapabilities(port,0,&caps)==ERROR_SUCCESS){word(s.caps+21,caps.Vibration.wLeftMotorSpeed);word(s.caps+23,caps.Vibration.wRightMotorSpeed);}
    ReleaseSRWLockExclusive(&sample_lock);*out=s;return ERROR_SUCCESS;
}
DWORD fut_native_input_rumble(unsigned port,WORD left,WORD right)
{
    XINPUT_VIBRATION vibration={left,right};return XInputSetState(port,&vibration);
}
