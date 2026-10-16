/* Local diagnostic stream, never a distributable game asset. Versioned records
 * preserve call order and the bytes observed at each read, not a RAM snapshot. */
#ifndef FUT_GRAPHICS_REPLAY_TRACE_H
#define FUT_GRAPHICS_REPLAY_TRACE_H
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
enum { FUT_TRACE_METHOD=1, FUT_TRACE_FINISH, FUT_TRACE_RESULT,
       FUT_TRACE_READ, FUT_TRACE_WRITE, FUT_TRACE_INTERRUPT };
typedef struct { uint32_t kind, a, b, c, size; } FutTraceRecord;
static FILE *fut_trace_file;
static int fut_trace_replaying, fut_trace_active;
static uint64_t fut_trace_bytes;
static void fut_trace_error(void)
{
    fputs("[REPLAY] incomplete, divergent or oversized graphics trace\n",stderr);
    exit(4);
}
static void fut_trace_transfer(void *data, size_t size)
{
    if (size>64u*1024u*1024u || fut_trace_bytes+size>1024ull*1024ull*1024ull)
        fut_trace_error();
    size_t n=fut_trace_replaying?fread(data,1,size,fut_trace_file):fwrite(data,1,size,fut_trace_file);
    if(n!=size) fut_trace_error();
    fut_trace_bytes+=size;
}
static uint32_t fut_trace_event(uint32_t kind,uint32_t a,uint32_t b,
                                uint32_t c,void *data,uint32_t size)
{
    FutTraceRecord r={kind,a,b,c,size};
    fut_trace_transfer(&r,sizeof(r));
    if(r.kind!=kind || r.a!=a || r.b!=b || r.size!=size) fut_trace_error();
    if(size) {
        if(fut_trace_replaying && kind==FUT_TRACE_WRITE) {
            void *expected=malloc(size);
            if(!expected) fut_trace_error();
            fut_trace_transfer(expected,size);
            if(memcmp(expected,data,size)) fut_trace_error();
            free(expected);
        } else fut_trace_transfer(data,size);
    }
    return r.c;
}
static void fut_trace_open(const char *path,int replay)
{
    char magic[8]="FUTGFX1";
    fut_trace_replaying=replay;fut_trace_bytes=0;
    fut_trace_file=fopen(path,replay?"rb":"wb");
    if(!fut_trace_file) fut_trace_error();
    fut_trace_transfer(magic,sizeof(magic));
    if(memcmp(magic,"FUTGFX1",8)) fut_trace_error();
}
#endif
