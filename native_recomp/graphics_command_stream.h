#ifndef FUT_GRAPHICS_COMMAND_STREAM_H
#define FUT_GRAPHICS_COMMAND_STREAM_H

#include <stddef.h>
#include <stdint.h>

typedef enum {
    FUT_GRAPHICS_DECODED,
    FUT_GRAPHICS_TRUNCATED,
    FUT_GRAPHICS_UNSUPPORTED_CONTROL,
    FUT_GRAPHICS_INVALID_METHOD_RANGE,
    FUT_GRAPHICS_HANDLER_REJECTED,
    FUT_GRAPHICS_READ_FAILED,
    FUT_GRAPHICS_PACKET_CHANGED
} FutGraphicsDecodeStatus;

typedef struct {
    size_t decoded_bytes;
    size_t packets;
    size_t methods;
    size_t blocked_offset;
    uint32_t blocked_header;
} FutGraphicsDecodeResult;

/* Returns nonzero only when the receiver accepts this method. Decoding is
 * independent of GPU execution: neither this function nor decoded_bytes
 * authorizes a DMA pointer or semaphore update. A native renderer must
 * establish completion separately after its submitted work completes. */
typedef int (*FutGraphicsMethodReceiver)(void *context, unsigned subchannel,
    uint32_t method, uint32_t value, size_t word_offset);

FutGraphicsDecodeStatus fut_graphics_decode(const uint8_t *bytes, size_t size,
    FutGraphicsMethodReceiver receiver, void *context, FutGraphicsDecodeResult *result);

/* Fetch each packet when reached, so CPU patches made during earlier GPU
 * work remain visible. Reads are bounded by size; a changed header fails
 * before dispatch. This does not establish GPU completion or atomicity. */
typedef int (*FutGraphicsCommandReader)(void *context, size_t offset,
    uint8_t *bytes, size_t size);
FutGraphicsDecodeStatus fut_graphics_decode_live(size_t size,
    FutGraphicsCommandReader reader, void *reader_context,
    FutGraphicsMethodReceiver receiver, void *context, FutGraphicsDecodeResult *result);

/* An initial cursor may be established only when the title's queue is empty,
 * aligned and inside the 64 MB contiguous view. This is not a consumption test. */
int fut_graphics_empty_queue(uint32_t start, uint32_t end, uint32_t write_ptr, uint32_t put);

#endif
