#define COBJMACROS
#include <windows.h>
#include <d3d11.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

#include "xbox_memory_layout.h"
#include "graphics_command_stream.h"
#include "graphics_native_backend.h"

static HANDLE backend_stop;
static HANDLE backend_thread;

static int read_guest(uint32_t va, void *data, size_t size)
{
    SIZE_T copied = 0;
    if (size > UINT32_MAX || va > UINT32_MAX - size)
        return 0;
    return ReadProcessMemory(GetCurrentProcess(),
        (const void *)((uintptr_t)xbox_GetMemoryOffset() + va), data, size, &copied)
        && copied == size;
}

static int read32(uint32_t va, uint32_t *value)
{
    return read_guest(va, value, sizeof(*value));
}

static int write32(uint32_t va, uint32_t value)
{
    SIZE_T written = 0;
    return WriteProcessMemory(GetCurrentProcess(),
        (void *)((uintptr_t)xbox_GetMemoryOffset() + va), &value, sizeof(value), &written)
        && written == sizeof(value);
}

static void fail_backend(const char *reason)
{
    fprintf(stderr, "[GRAPHICS] %s\n", reason);
    fflush(NULL);
    _Exit(3);
}

static int native_method(void *context, unsigned subchannel, uint32_t method,
                         uint32_t value, size_t offset)
{
    (void)context;
    /* Until the object/resource mapping is implemented, no command may
     * authorize progress. This is the renderer dispatch boundary, not a
     * successful no-op implementation of every missing method. */
    fprintf(stderr, "[GRAPHICS] native handler missing: sub=%u method=%04X "
            "value=%08X offset=%zu\n", subchannel, method, value, offset);
    return 0;
}

static DWORD WINAPI native_worker(void *unused)
{
    (void)unused;
    ID3D11Device *device = NULL;
    ID3D11DeviceContext *context = NULL;
    D3D_FEATURE_LEVEL feature_level;
    HRESULT hr = D3D11CreateDevice(NULL, D3D_DRIVER_TYPE_HARDWARE, NULL, 0,
        NULL, 0, D3D11_SDK_VERSION, &device, &feature_level, &context);
    if (FAILED(hr)) {
        fprintf(stderr, "[GRAPHICS] hardware device unavailable: HRESULT=%08lX; trying WARP\n", hr);
        hr = D3D11CreateDevice(NULL, D3D_DRIVER_TYPE_WARP, NULL, 0,
            NULL, 0, D3D11_SDK_VERSION, &device, &feature_level, &context);
    }
    if (FAILED(hr))
        fail_backend("native D3D11 device initialization failed");
    fprintf(stderr, "[GRAPHICS] native D3D11 device initialized: feature_level=%04X\n",
            (unsigned)feature_level);

    uint32_t ring_start = 0, ring_end = 0, channel = 0, cursor = 0;
    while (WaitForSingleObject(backend_stop, 5) == WAIT_TIMEOUT) {
        uint32_t title_device = 0, start = 0, end = 0, write_ptr = 0;
        uint32_t title_channel = 0, put = 0;
        if (!read32(0x0029E968u, &title_device) || !title_device ||
            title_device > 0x04000000u - 0x1C28u)
            continue;
        if (!read32(title_device + 0x24u, &start) ||
            !read32(title_device + 0x28u, &end) || !read32(title_device, &write_ptr) ||
            !read32(title_device + 0x1C20u, &title_channel) ||
            title_channel != 0xFD800000u || !read32(title_channel + 0x40u, &put))
            continue;
        if (!cursor) {
            /* Initialize an empty native submission queue. There are no
             * commands between start and PUT, so no work is acknowledged.
             * The title's consumer semaphore is deliberately untouched. */
            if (!fut_graphics_empty_queue(start, end, write_ptr, put))
                continue;
            ring_start = start; ring_end = end; channel = title_channel; cursor = start;
            if (!write32(channel + 0x44u, put))
                fail_backend("cannot initialize empty native submission queue");
            fprintf(stderr, "[GRAPHICS] empty queue initialized: ring=[%08X,%08X) "
                    "cursor=%08X; no commands/fences completed\n", start, end, cursor);
            continue;
        }
        if (start != ring_start || end != ring_end || title_channel != channel)
            fail_backend("submission queue reconfiguration is not implemented");
        uint32_t submitted = put | 0x80000000u;
        if (submitted == cursor)
            continue;
        if (submitted < cursor || submitted > ring_end || (submitted - cursor) % 4u)
            fail_backend("submission wrap or invalid range is not implemented");
        uint8_t bytes[65536];
        size_t size = submitted - cursor;
        if (size > sizeof(bytes) || !read_guest(cursor, bytes, size))
            fail_backend("submission capture exceeds native decoder bounds");
        FutGraphicsDecodeResult decoded;
        FutGraphicsDecodeStatus status = fut_graphics_decode(bytes, size,
            native_method, context, &decoded);
        if (status != FUT_GRAPHICS_DECODED) {
            FILE *capture = fopen("graphics-rejected.bin", "wb");
            if (capture) { fwrite(bytes, 1, size, capture); fclose(capture); }
            fprintf(stderr, "[GRAPHICS] submission blocked: start=%08X end=%08X "
                    "status=%d offset=%zu header=%08X bytes=%zu; completion unchanged\n",
                    cursor, submitted, status, decoded.blocked_offset, decoded.blocked_header, size);
            fail_backend("implement the reported native graphics method before continuing");
        }
        /* No successful method implementation exists yet. A future backend
         * must wait for native GPU completion before updating GET/fences. */
        fail_backend("native completion is not implemented");
    }
    ID3D11DeviceContext_Release(context);
    ID3D11Device_Release(device);
    return 0;
}

int fut_graphics_native_start(void)
{
    backend_stop = CreateEventA(NULL, TRUE, FALSE, NULL);
    if (!backend_stop)
        return 0;
    backend_thread = CreateThread(NULL, 0, native_worker, NULL, 0, NULL);
    if (!backend_thread) {
        CloseHandle(backend_stop); backend_stop = NULL;
        return 0;
    }
    return 1;
}

void fut_graphics_native_stop(void)
{
    if (backend_thread) {
        SetEvent(backend_stop);
        WaitForSingleObject(backend_thread, INFINITE);
        CloseHandle(backend_thread); backend_thread = NULL;
    }
    if (backend_stop) { CloseHandle(backend_stop); backend_stop = NULL; }
}
