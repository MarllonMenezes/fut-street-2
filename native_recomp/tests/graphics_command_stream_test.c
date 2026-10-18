#include "graphics_command_stream.h"

#include <stdio.h>
#include <string.h>

typedef struct {
    unsigned count;
    unsigned reject_at;
    uint32_t methods[8], values[8];
    unsigned channels[8];
} Recording;

static int receive(void *context, unsigned channel, uint32_t method,
                   uint32_t value, size_t offset)
{
    Recording *record = context;
    if (record->count == record->reject_at)
        return 0;
    if (record->count >= 8 || offset % 4)
        return 0;
    const unsigned index = record->count++;
    record->channels[index] = channel;
    record->methods[index] = method;
    record->values[index] = value;
    return 1;
}

static int inspect(void *context, unsigned channel, uint32_t method,
                   uint32_t value, size_t offset)
{
    unsigned *release_count = context;
    if (method == 0)
        printf("binding sub=%u handle=%08X offset=%zu\n", channel, value, offset);
    if (channel == 0 && method == 0x1D70) {
        ++*release_count;
        printf("release word=%08X offset=%zu\n", value, offset);
    }
    return 1; /* Decoding a supplied capture, not executing its methods. */
}

typedef struct {
    uint8_t bytes[24];
    Recording record;
    unsigned reads, fail_read, change_header;
} LiveCommands;

static int live_read(void *context, size_t offset, uint8_t *bytes, size_t size)
{
    LiveCommands *live = context;
    if (++live->reads == live->fail_read || offset > sizeof(live->bytes) ||
        size > sizeof(live->bytes) - offset) return 0;
    memcpy(bytes, live->bytes + offset, size);
    if (live->change_header && live->reads == 2) bytes[0] ^= 4;
    return 1;
}

static int patch_later_packets(void *context, unsigned channel, uint32_t method,
                              uint32_t value, size_t offset)
{
    LiveCommands *live = context;
    if (!receive(&live->record, channel, method, value, offset)) return 0;
    if (live->record.count == 1) {
        /* CPU arms the observed WAIT_IDLE / software notification5 packets
         * while an earlier method executes. Header and parameter both change. */
        live->bytes[8] = 0x10; live->bytes[9] = 0x01;
        live->bytes[16] = 0x00; live->bytes[17] = 0x01;
        live->bytes[20] = 5;
    }
    return 1;
}

static int test_live(void)
{
    const uint8_t initial[24] = {
        0x70,0x1D,4,0, 0x73,0,0,0,
        0x90,0x1D,4,0, 0,0,0,0,
        0x90,0x1D,4,0, 0,0,0,0
    };
    LiveCommands live = {.record.reject_at=99};
    memcpy(live.bytes, initial, sizeof(initial));
    FutGraphicsDecodeResult result;
    if (fut_graphics_decode_live(sizeof(initial), live_read, &live,
            patch_later_packets, &live, &result) != FUT_GRAPHICS_DECODED ||
        result.packets != 3 || live.record.count != 3 ||
        live.record.methods[1] != 0x110 || live.record.methods[2] != 0x100 ||
        live.record.values[2] != 5) return 0;
    Recording snapshot = {.reject_at=99};
    if (fut_graphics_decode(initial,sizeof(initial),receive,&snapshot,&result) != FUT_GRAPHICS_DECODED ||
        snapshot.methods[1] != 0x1D90 || snapshot.methods[2] != 0x1D90 || snapshot.values[2]) return 0;
    for (unsigned fail=1; fail<=6; ++fail) {
        memset(&live,0,sizeof(live)); memcpy(live.bytes,initial,sizeof(initial));
        live.record.reject_at=99; live.fail_read=fail;
        if (fut_graphics_decode_live(sizeof(initial),live_read,&live,receive,&live.record,&result)
                != FUT_GRAPHICS_READ_FAILED || result.decoded_bytes != ((fail-1)/2)*8 ||
            live.record.count != (fail-1)/2) return 0;
    }
    memset(&live,0,sizeof(live)); memcpy(live.bytes,initial,sizeof(initial));
    live.record.reject_at=99; live.change_header=1;
    if (fut_graphics_decode_live(24,live_read,&live,receive,&live.record,&result)
            != FUT_GRAPHICS_PACKET_CHANGED || live.record.count || result.decoded_bytes) return 0;
    live.change_header=0; live.reads=0;
    if (fut_graphics_decode_live(7,live_read,&live,receive,&live.record,&result)
            != FUT_GRAPHICS_TRUNCATED || live.record.count || live.reads != 1) return 0;
    live.reads=0; live.record.reject_at=1;
    if (fut_graphics_decode_live(24,live_read,&live,receive,&live.record,&result)
            != FUT_GRAPHICS_HANDLER_REJECTED || result.decoded_bytes != 8 || result.blocked_offset != 12) return 0;
    return 1;
}

int main(int argc, char **argv)
{
    if (argc == 2) {
        uint8_t bytes[65536];
        FILE *file = fopen(argv[1], "rb");
        if (!file) return 10;
        size_t size = fread(bytes, 1, sizeof(bytes), file);
        const int oversized = fgetc(file) != EOF;
        const int failed = ferror(file);
        fclose(file);
        if (failed || oversized) return 11;
        unsigned releases = 0;
        FutGraphicsDecodeResult result;
        FutGraphicsDecodeStatus status = fut_graphics_decode(bytes, size, inspect, &releases, &result);
        printf("decode status=%d bytes=%zu packets=%zu methods=%zu releases=%u execution=none\n",
               status, result.decoded_bytes, result.packets, result.methods, releases);
        return status == FUT_GRAPHICS_DECODED ? 0 : 12;
    }
    /* LE bytes: INC subchannel 3, methods 0x100/0x104, then NONINC
     * subchannel 5, method 0x200 with two different parameters. */
    const uint8_t commands[] = {
        0x00,0x61,0x08,0x00, 0x78,0x56,0x34,0x12, 0xEF,0xCD,0xAB,0x90,
        0x00,0xA2,0x08,0x40, 0x01,0x00,0x00,0x00, 0x02,0x00,0x00,0x00
    };
    uint8_t copy[sizeof(commands)];
    memcpy(copy, commands, sizeof(copy));
    Recording record = {.reject_at = 99};
    FutGraphicsDecodeResult result;
    if (fut_graphics_decode(commands, sizeof(commands), receive, &record, &result)
            != FUT_GRAPHICS_DECODED || record.count != 4 || result.packets != 2 ||
        result.decoded_bytes != sizeof(commands) ||
        record.channels[0] != 3 || record.channels[3] != 5 ||
        record.methods[0] != 0x100 || record.methods[1] != 0x104 ||
        record.methods[2] != 0x200 || record.methods[3] != 0x200 ||
        record.values[0] != 0x12345678 || record.values[1] != 0x90ABCDEF ||
        memcmp(copy, commands, sizeof(copy))) return 1;

    /* Reject a partial packet before any method reaches the receiver. */
    record.count = 0;
    if (fut_graphics_decode(commands, 8, receive, &record, &result)
            != FUT_GRAPHICS_TRUNCATED || record.count || result.decoded_bytes) return 2;
    if (fut_graphics_decode(commands, 3, receive, &record, &result)
            != FUT_GRAPHICS_TRUNCATED || record.count) return 3;

    record.reject_at = 2;
    if (fut_graphics_decode(commands, sizeof(commands), receive, &record, &result)
            != FUT_GRAPHICS_HANDLER_REJECTED || record.count != 2 ||
        result.decoded_bytes != 12 || result.blocked_offset != 16) return 4;

    /* Never interpret jump/call/return as payload packets. */
    const uint8_t controls[][4] = {{1,0,0,0}, {2,0,0,0}, {0,0,2,0}, {0,0,0,0x20}};
    for (unsigned i = 0; i < 4; ++i) {
        record.count = 0;
        if (fut_graphics_decode(controls[i], 4, receive, &record, &result)
                != FUT_GRAPHICS_UNSUPPORTED_CONTROL || record.count) return 5;
    }
    const uint8_t overflow[] = {0xFC,0x1F,0x08,0, 0,0,0,0, 0,0,0,0};
    if (fut_graphics_decode(overflow, sizeof(overflow), receive, &record, &result)
            != FUT_GRAPHICS_INVALID_METHOD_RANGE || record.count) return 6;
    const uint8_t zero_count[] = {0,0,0,0};
    if (fut_graphics_decode(zero_count, 4, receive, &record, &result)
            != FUT_GRAPHICS_DECODED || result.methods || result.packets != 1) return 7;
    const uint32_t base = 0x80002000u, limit = 0x80402000u, put = 0x2000u;
    if (!fut_graphics_empty_queue(base, limit, base, put) ||
        fut_graphics_empty_queue(base, limit, base + 4, put) ||
        fut_graphics_empty_queue(base, limit, base, put + 4) ||
        fut_graphics_empty_queue(base, base, base, put) ||
        fut_graphics_empty_queue(base + 1, limit, base + 1, put + 1) ||
        fut_graphics_empty_queue(base, 0x84000004u, base, put)) return 8;
    if (!test_live()) return 9;
    puts("graphics command decoding, live CPU patches, rejection and input preservation OK");
    return 0;
}
