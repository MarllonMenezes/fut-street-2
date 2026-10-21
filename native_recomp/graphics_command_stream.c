#include "graphics_command_stream.h"

#include <string.h>

int fut_graphics_empty_queue(uint32_t start, uint32_t end, uint32_t write_ptr, uint32_t put)
{
    return start >= 0x80000000u && end > start && end <= 0x84000000u &&
        (start & 3u) == 0u && (end & 3u) == 0u &&
        write_ptr == start && put == (start & 0x0FFFFFFFu);
}

static uint32_t read_le32(const uint8_t *bytes)
{
    return (uint32_t)bytes[0] | ((uint32_t)bytes[1] << 8) |
           ((uint32_t)bytes[2] << 16) | ((uint32_t)bytes[3] << 24);
}

static FutGraphicsDecodeStatus decode_live(size_t size,
    FutGraphicsCommandReader reader, void *reader_context,
    FutGraphicsMethodReceiver receiver, void *context, FutGraphicsDecodeResult *result,
    uint32_t jump_header,int *jumped)
{
    if(jumped) *jumped=0;
    FutGraphicsDecodeResult local;
    if (!result)
        result = &local;
    memset(result, 0, sizeof(*result));
    if (!reader || !receiver)
        return FUT_GRAPHICS_HANDLER_REJECTED;

    size_t cursor = 0;
    while (cursor < size) {
        result->blocked_offset = cursor;
        if (size - cursor < 4)
            return FUT_GRAPHICS_TRUNCATED;
        uint8_t packet[8192]; /* Header plus at most 2047 parameters. */
        if (!reader(reader_context, cursor, packet, 4))
            return FUT_GRAPHICS_READ_FAILED;
        const uint32_t header = read_le32(packet);
        const uint32_t kind = header & 0xE0030003u;
        result->blocked_header = header;
        if(jump_header && header==jump_header) {
            if(!reader(reader_context,cursor,packet,4)) return FUT_GRAPHICS_READ_FAILED;
            if(read_le32(packet)!=header) return FUT_GRAPHICS_PACKET_CHANGED;
            result->decoded_bytes=cursor+4;
            ++result->packets;
            if(jumped) *jumped=1;
            return FUT_GRAPHICS_DECODED;
        }
        /* Incrementing/non-incrementing packets are the forms observed in
         * the captured title init. Control flow is rejected, never skipped. */
        if (kind != 0u && kind != 0x40000000u)
            return FUT_GRAPHICS_UNSUPPORTED_CONTROL;
        const uint32_t count = (header >> 18) & 0x7FFu;
        const uint32_t method = header & 0x1FFCu;
        const unsigned subchannel = (header >> 13) & 7u;
        if (count > (size - cursor - 4) / 4)
            return FUT_GRAPHICS_TRUNCATED;
        if (kind == 0u && count && method + (count - 1u) * 4u > 0x1FFCu)
            return FUT_GRAPHICS_INVALID_METHOD_RANGE;
        const size_t end = cursor + 4 + (size_t)count * 4;
        if (!reader(reader_context, cursor, packet, end - cursor))
            return FUT_GRAPHICS_READ_FAILED;
        if (read_le32(packet) != header)
            return FUT_GRAPHICS_PACKET_CHANGED;
        for (uint32_t i = 0; i < count; ++i) {
            const size_t offset = cursor + 4 + (size_t)i * 4;
            const uint32_t target = method + (kind == 0u ? i * 4u : 0u);
            if (!receiver(context, subchannel, target,
                    read_le32(packet + 4 + (size_t)i * 4), offset)) {
                result->blocked_offset = offset;
                return FUT_GRAPHICS_HANDLER_REJECTED;
            }
            ++result->methods;
        }
        ++result->packets;
        result->decoded_bytes = end;
        cursor = end;
    }
    result->blocked_offset = size;
    result->blocked_header = 0;
    return FUT_GRAPHICS_DECODED;
}

FutGraphicsDecodeStatus fut_graphics_decode_live(size_t size,
    FutGraphicsCommandReader reader,void *reader_context,
    FutGraphicsMethodReceiver receiver,void *context,FutGraphicsDecodeResult *result)
{
    return decode_live(size,reader,reader_context,receiver,context,result,0,NULL);
}

typedef struct {
    FutGraphicsCommandReader reader;void *context;size_t base;
} OffsetReader;
static int offset_read(void *context,size_t offset,uint8_t *bytes,size_t size)
{
    OffsetReader *r=context;
    return r->reader(r->context,r->base+offset,bytes,size);
}
typedef struct {
    FutGraphicsMethodReceiver receiver;void *context;size_t base;
} OffsetReceiver;
static int offset_receive(void *context,unsigned sub,uint32_t method,uint32_t value,size_t offset)
{
    OffsetReceiver *r=context;
    return r->receiver(r->context,sub,method,value,r->base+offset);
}
FutGraphicsDecodeStatus fut_graphics_decode_wrapped_live(uint32_t start,
    uint32_t end,uint32_t cursor,uint32_t submitted,FutGraphicsCommandReader reader,
    void *reader_context,FutGraphicsMethodReceiver receiver,void *context,
    FutGraphicsDecodeResult *result,size_t *jump_offset)
{
    FutGraphicsDecodeResult local,tail,head;
    if(!result) result=&local;
    memset(result,0,sizeof(*result));
    if(jump_offset) *jump_offset=SIZE_MAX;
    if(!reader || !receiver || start<0x80000000u || end>0x84000000u ||
        start>=end || cursor<start || cursor>=end || submitted<start ||
        submitted>=cursor || ((start|end|cursor|submitted)&3u) ||
        submitted-start>65532u) return FUT_GRAPHICS_UNSUPPORTED_CONTROL;
    size_t head_size=submitted-start,tail_size=end-cursor;
    size_t budget=65536u-head_size;
    if(tail_size>budget) tail_size=budget;
    OffsetReader tail_reader={reader,reader_context,cursor-start};
    int jumped=0;
    /* NV1A+ absolute jump: all target address bits except low01. Format
     * reference: envytools/docs/hw/fifo/dma-pusher.rst. No implicit wrap. */
    uint32_t expected_jump=(start&0x7FFFFFFFu)|1u;
    FutGraphicsDecodeStatus status=decode_live(tail_size,offset_read,&tail_reader,
        receiver,context,&tail,expected_jump,&jumped);
    *result=tail;
    if(status!=FUT_GRAPHICS_DECODED) return status;
    if(!jumped) return FUT_GRAPHICS_UNSUPPORTED_CONTROL;
    if(jump_offset) *jump_offset=tail.decoded_bytes-4;
    OffsetReceiver head_receiver={receiver,context,tail.decoded_bytes};
    status=fut_graphics_decode_live(head_size,reader,reader_context,offset_receive,
        &head_receiver,&head);
    result->decoded_bytes=tail.decoded_bytes+head.decoded_bytes;
    result->packets=tail.packets+head.packets;result->methods=tail.methods+head.methods;
    result->blocked_offset=tail.decoded_bytes+head.blocked_offset;
    result->blocked_header=head.blocked_header;
    return status;
}

static int snapshot_read(void *context, size_t offset, uint8_t *bytes, size_t size)
{
    memcpy(bytes, (const uint8_t *)context + offset, size);
    return 1;
}

FutGraphicsDecodeStatus fut_graphics_decode(const uint8_t *bytes, size_t size,
    FutGraphicsMethodReceiver receiver, void *context, FutGraphicsDecodeResult *result)
{
    if (!bytes && size) {
        if (result) memset(result, 0, sizeof(*result));
        return FUT_GRAPHICS_HANDLER_REJECTED;
    }
    return fut_graphics_decode_live(size, snapshot_read, (void *)bytes,
        receiver, context, result);
}
