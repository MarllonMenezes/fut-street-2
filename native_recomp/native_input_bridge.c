#include "native_input.h"
#include "recomp_types.h"
#include <stdio.h>
#include <string.h>

/* Exact local SDK entry/class scope. Game polling and mapping remain generated. */
static SRWLOCK handle_lock=SRWLOCK_INIT;
static struct {uint32_t token;unsigned port;} handles[16];
static uint32_t generation,device_mask;
static unsigned rumble_active[4];
static int native_handle(uint32_t token){return (token&0xFFF00000u)==0xF2700000u;}
static int read_guest(uint32_t address,void *out,size_t size)
{
    SIZE_T read=0;
    return g_xbox_mem_offset&&address>=0x10000u&&size<=0x84000000u-address&&address<0x84000000u&&
        ReadProcessMemory(GetCurrentProcess(),(const void*)XBOX_PTR(address),out,size,&read)&&read==size;
}
static int writable(uint32_t address,size_t size)
{
    if(!g_xbox_mem_offset||address<0x10000u||address>=0x84000000u||size>0x84000000u-address)return 0;
    uintptr_t p=(uintptr_t)XBOX_PTR(address),end=p+size;
    while(p<end){MEMORY_BASIC_INFORMATION m;
        if(!VirtualQuery((const void*)p,&m,sizeof(m))||m.State!=MEM_COMMIT||(m.Protect&(PAGE_GUARD|PAGE_NOACCESS))||
           !(m.Protect&(PAGE_READWRITE|PAGE_WRITECOPY|PAGE_EXECUTE_READWRITE|PAGE_EXECUTE_WRITECOPY)))return 0;
        uintptr_t next=(uintptr_t)m.BaseAddress+m.RegionSize;if(next<=p)return 0;p=next;
    }return 1;
}
static int write_guest(uint32_t address,const void *in,size_t size)
{
    SIZE_T written=0;return writable(address,size)&&WriteProcessMemory(GetCurrentProcess(),(void*)XBOX_PTR(address),in,size,&written)&&written==size;
}
static void guest_error(uint32_t error)
{
    uint32_t tls,index,record;
    if(!read_guest(XBOX_FS_BASE+4u,&tls,4)||!read_guest(0x3551FCu,&index,4)||index>4095u||
       tls>0xFFFFFFFCu-index*4u||!read_guest(tls+index*4u,&record,4)||record>0xFFFFFFF3u||!write_guest(record+8u,&error,4)){
        fprintf(stderr,"[NATIVE-INPUT] guest LastError path unavailable NOT_READY\n");ExitProcess(3);
    }
}
int fut_native_input_dispatch(uint32_t site)
{
    if(!fut_native_input_enabled())return 0;
    unsigned argc;
    switch(site){case 0x2A2419u:case 0x2A2168u:argc=1;break;
    case 0x2A243Bu:argc=3;break;case 0x2A2112u:argc=4;break;
    case 0x2A2174u:case 0x2A234Cu:case 0x2A23BFu:argc=2;break;default:return 0;}
    DWORD saved_error=GetLastError();uint32_t stack[5]={0},result=0;
    if(!read_guest(g_esp,stack,4*(argc+1u))){fprintf(stderr,"[NATIVE-INPUT] invalid guest call stack NOT_READY\n");ExitProcess(3);}
    uint32_t *a=stack+1;
    int class_call=site==0x2A2419u||site==0x2A243Bu||site==0x2A2112u;
    if((class_call&&a[0]!=0x2A11B4u)||(!class_call&&!native_handle(a[0]))){SetLastError(saved_error);return 0;}
    AcquireSRWLockExclusive(&handle_lock);
    if(class_call){
        uint32_t mask=0;FutNativeInputSample sample;
        for(unsigned p=0;p<4;++p)if(fut_native_input_sample(p,&sample)==ERROR_SUCCESS)mask|=1u<<p;
        if(site==0x2A2419u){
            static unsigned logged; if(logged++<16)fprintf(stderr,"[NATIVE-INPUT] devices mask=%X SDK-boundary=002A2419 NOT_READY\n",mask);
            device_mask=mask;result=mask;
        }else if(site==0x2A243Bu){
            uint32_t added=mask&~device_mask,removed=device_mask&~mask;
            if(!writable(a[1],4)||!writable(a[2],4))result=0;
            else {if(!write_guest(a[1],&added,4)||!write_guest(a[2],&removed,4))ExitProcess(3);device_mask=mask;result=(added|removed)!=0;}
        }else{
            DWORD error=ERROR_DEVICE_NOT_CONNECTED;
            if(a[1]>=4||a[2]!=0||a[3]!=0)error=ERROR_INVALID_PARAMETER;
            else if(mask&(1u<<a[1])){
                error=ERROR_NO_SYSTEM_RESOURCES;
                for(unsigned i=0;i<16;++i)if(!handles[i].token&&generation<0xFFFFu){
                    result=0xF2700000u|(++generation<<4)|i;handles[i].token=result;handles[i].port=a[1];error=0;
                    fprintf(stderr,"[NATIVE-INPUT] open port=%u handle=%08X NOT_READY\n",a[1],result);break;
                }
            }
            if(error)guest_error(error);
        }
    }else{
        unsigned slot=16;for(unsigned i=0;i<16;++i)if(handles[i].token==a[0]){slot=i;break;}
        result=ERROR_INVALID_HANDLE;
        if(slot<16){
            unsigned port=handles[slot].port;FutNativeInputSample sample;
            if(site==0x2A2168u){
                handles[slot].token=0;unsigned others=0;
                for(unsigned i=0;i<16;++i)if(handles[i].token&&handles[i].port==port)++others;
                if(!others&&rumble_active[port]){fut_native_input_rumble(port,0,0);rumble_active[port]=0;}
                result=0;
            }
            else if((result=fut_native_input_sample(port,&sample))==ERROR_SUCCESS){
                if(site==0x2A2174u)result=write_guest(a[1],sample.caps,25)?0:ERROR_INVALID_PARAMETER;
                else if(site==0x2A234Cu){
                    result=write_guest(a[1],sample.state,22)?0:ERROR_INVALID_PARAMETER;
                    if(!result){
                        static uint32_t previous_packet[4];static unsigned logged;
                        uint32_t packet;memcpy(&packet,sample.state,4);
                        if(packet!=previous_packet[port]){
                            previous_packet[port]=packet;
                            if(logged++<32)fprintf(stderr,
                                "[NATIVE-INPUT] SDK-state-delivered caller=%08X output=%08X handle=%08X packet=%u digital=%04X analogA=%u NOT_READY\n",
                                stack[0],a[1],a[0],packet,
                                (unsigned)(sample.state[4]|sample.state[5]<<8),sample.state[6]);
                        }
                    }
                }
                else{
                    uint8_t feedback[70];
                    if(!read_guest(a[1],feedback,70)||!writable(a[1],4))result=ERROR_INVALID_PARAMETER;
                    else {
                        uint32_t event;memcpy(&event,feedback+4,4);
                        WORD left=(WORD)(feedback[66]|feedback[67]<<8),right=(WORD)(feedback[68]|feedback[69]<<8);
                        if(event||!sample.physical)result=ERROR_NOT_SUPPORTED;
                        else {result=fut_native_input_rumble(port,left,right);if(!result)rumble_active[port]=(left|right)!=0;}
                        if(!write_guest(a[1],&result,4))ExitProcess(3);
                    }
                }
            }
        }
    }
    if(!class_call&&site!=0x2A2168u&&result){
        static unsigned rejected;
        if(rejected++<32)fprintf(stderr,"[NATIVE-INPUT] rejected site=%08X handle=%08X status=%u NOT_READY\n",site,a[0],result);
    }
    ReleaseSRWLockExclusive(&handle_lock);
    g_eax=result;g_esp+=4*(argc+1u);SetLastError(saved_error);return 1;
}
