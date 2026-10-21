/* Captured final software-mixer PCM output, not a DSP/APU implementation.
 * Windows API contract: waveOutOpen / waveOutWrite / waveOutGetPosition.
 * Experimental cursor lead and speaker mapping are not Xbox-validated. */
#include <windows.h>
#include <mmsystem.h>
#include <mmreg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "recomp_types.h"

enum { PCM_RATE = 48000, PCM_CHANNELS = 6, PCM_BLOCK = 12,
       PCM_RING = 2400, PCM_CHUNK = 480, PCM_LEAD = 32 };
static HWAVEOUT output;
static WAVEHDR headers[2];
static unsigned char packets[2][PCM_CHUNK * PCM_BLOCK];
static uint32_t ring_address, submitted, played, query_count, packet_count;
static uint32_t nonzero_packets;
static DWORD owner;
static int configured, started;

static void pcm_stop(const char *reason, unsigned long code)
{
    fprintf(stderr, "[NATIVE-PCM] stopped: %s code=%lu submitted=%u played=%u; NOT_READY\n",
            reason, code, submitted, played);
    fflush(NULL);
    _Exit(3);
}

static int pcm_enabled(void)
{
    const char *option = getenv("RECOMP_NATIVE_PCM_OUTPUT");
    if (!option || strcmp(option, "0") == 0) return 0;
    if (strcmp(option, "1") != 0) pcm_stop("invalid opt-in", 0);
    return 1;
}

static void pcm_check_thread(void)
{
    if (!owner) owner = GetCurrentThreadId();
    if (owner != GetCurrentThreadId()) pcm_stop("output thread changed", 0);
}

static void pcm_close(void)
{
    if (!output) return;
    waveOutReset(output);
    for (unsigned i = 0; i < 2; ++i)
        if (headers[i].dwFlags & WHDR_PREPARED)
            waveOutUnprepareHeader(output, &headers[i], sizeof(headers[i]));
    waveOutClose(output);
    output = NULL;
}

void fut_native_pcm_configure(uint32_t format_address)
{
    if (!pcm_enabled()) return;
    if (configured || format_address < 0x10000u || format_address > 0x9FFFFFEDu)
        pcm_stop("configuration scope", format_address);
    if (MEM16(format_address) != WAVE_FORMAT_PCM ||
        MEM16(format_address + 2u) != PCM_CHANNELS ||
        MEM32(format_address + 4u) != PCM_RATE ||
        MEM32(format_address + 8u) != PCM_RATE * PCM_BLOCK ||
        MEM16(format_address + 12u) != PCM_BLOCK ||
        MEM16(format_address + 14u) != 16u || MEM16(format_address + 16u) != 0u)
        pcm_stop("unsupported original WAVEFORMATEX", format_address);
    configured = 1;
    fprintf(stderr, "[NATIVE-PCM] captured original PCM: 48000Hz/6ch/16bit; output opt-in; DSP-executed=0 NOT_READY\n");
}

int fut_native_pcm_position(uint32_t ring, uint32_t frames, uint32_t rate,
                            uint32_t *write_position)
{
    if (!pcm_enabled()) return 0;
    pcm_check_thread();
    if (!configured || frames != PCM_RING || rate != PCM_RATE ||
        ring < 0x10000u || ring > 0x9FFF8F7Fu ||
        (ring_address && ring_address != ring))
        pcm_stop("ring scope", ring);
    ring_address = ring;
    if (!output) {
        WAVEFORMATEXTENSIBLE format = {0};
        const GUID pcm_guid = {1, 0, 0x0010, {0x80,0,0,0xAA,0,0x38,0x9B,0x71}};
        format.Format.wFormatTag = WAVE_FORMAT_EXTENSIBLE;
        format.Format.nChannels = PCM_CHANNELS;
        format.Format.nSamplesPerSec = PCM_RATE;
        format.Format.nAvgBytesPerSec = PCM_RATE * PCM_BLOCK;
        format.Format.nBlockAlign = PCM_BLOCK;
        format.Format.wBitsPerSample = 16;
        format.Format.cbSize = 22;
        format.Samples.wValidBitsPerSample = 16;
        /* Explicit experimental 5.1 mapping; raw channels are not reordered. */
        format.dwChannelMask = 0x3Fu;
        format.SubFormat = pcm_guid;
        MMRESULT result = waveOutOpen(&output, WAVE_MAPPER, &format.Format, 0, 0, CALLBACK_NULL);
        if (result) pcm_stop("waveOutOpen", result);
        atexit(pcm_close);
        if ((result = waveOutPause(output))) pcm_stop("waveOutPause", result);
        for (unsigned i = 0; i < 2; ++i) {
            headers[i].lpData = (LPSTR)packets[i];
            headers[i].dwBufferLength = sizeof(packets[i]);
            if ((result = waveOutPrepareHeader(output, &headers[i], sizeof(headers[i]))))
                pcm_stop("waveOutPrepareHeader", result);
        }
        fprintf(stderr, "[NATIVE-PCM] device accepted 6ch PCM; ring=%08X frames=%u chunks=2x480; mapping/latency unvalidated NOT_READY\n", ring, frames);
    }
    MMTIME time = {0};
    time.wType = TIME_SAMPLES;
    MMRESULT result = waveOutGetPosition(output, &time, sizeof(time));
    if (result) pcm_stop("waveOutGetPosition", result);
    uint32_t position;
    if (time.wType == TIME_SAMPLES) position = time.u.sample;
    else if (time.wType == TIME_BYTES && time.u.cb % PCM_BLOCK == 0)
        position = time.u.cb / PCM_BLOCK;
    else { pcm_stop("unsupported position unit", time.wType); return 0; }
    if (position < played || position > submitted)
        pcm_stop("invalid device playback position", position);
    played = position;
    *write_position = ((played + PCM_LEAD) % PCM_RING) * PCM_BLOCK;
    ++query_count;
    if (query_count <= 8u || (query_count & (query_count - 1u)) == 0u) {
        fprintf(stderr, "[NATIVE-PCM] query=%u device_played=%u submitted=%u write_bytes=%u unit=%u NOT_READY\n",
                query_count, played, submitted, *write_position, time.wType);
        fflush(stderr);
    }
    return 1;
}

void fut_native_pcm_submit(uint32_t ring, uint32_t filled)
{
    if (!pcm_enabled()) return;
    pcm_check_thread();
    if (!output || ring != ring_address || filled >= PCM_RING)
        pcm_stop("refill scope", filled);
    /* Queue in submission order, even when the OS completes both headers. */
    for (unsigned n = 0; n < 2; ++n) {
        const unsigned slot = packet_count % 2u;
        WAVEHDR *header = &headers[slot];
        if (header->dwFlags & WHDR_INQUEUE) break;
        const uint32_t offset = submitted % PCM_RING;
        const uint32_t available = (filled + PCM_RING - offset) % PCM_RING;
        if (available < PCM_CHUNK + PCM_LEAD) break;
        const uint32_t first = PCM_RING - offset < PCM_CHUNK ? PCM_RING - offset : PCM_CHUNK;
        memcpy(packets[slot], (const void *)XBOX_PTR(ring + offset * PCM_BLOCK), first * PCM_BLOCK);
        if (first < PCM_CHUNK)
            memcpy(packets[slot] + first * PCM_BLOCK, (const void *)XBOX_PTR(ring), (PCM_CHUNK - first) * PCM_BLOCK);
        MMRESULT result = waveOutWrite(output, header, sizeof(*header));
        if (result) pcm_stop("waveOutWrite", result);
        submitted += PCM_CHUNK;
        ++packet_count;
        unsigned peak = 0;
        for (unsigned j = 0; j < sizeof(packets[slot]); j += 2) {
            int16_t value; memcpy(&value, packets[slot] + j, sizeof(value));
            unsigned magnitude = value < 0 ? (unsigned)-(int)value : (unsigned)value;
            if (magnitude > peak) peak = magnitude;
        }
        if (peak) ++nonzero_packets;
        if (packet_count <= 8u || (packet_count & (packet_count - 1u)) == 0u ||
            (peak && nonzero_packets == 1u)) {
            fprintf(stderr, "[NATIVE-PCM] packet=%u submitted=%u played=%u source_offset=%u peak=%u nonzero_packets=%u NOT_READY\n",
                    packet_count, submitted, played, offset, peak, nonzero_packets);
        }
    }
    if (!started && submitted) {
        MMRESULT result = waveOutRestart(output);
        if (result) pcm_stop("waveOutRestart", result);
        started = 1;
    }
}
