#ifndef FUT_GRAPHICS_VERTEX_FORMAT_H
#define FUT_GRAPHICS_VERTEX_FORMAT_H
#include <stdint.h>
/* SIZE=0 disables the established float array descriptor. Its retained
 * record stride cannot cause a DMA read or define a shader input. */
static inline int fut_graphics_vertex_disabled(uint32_t format)
{
    return (format&255u)==0x02u;
}
/* Only established attribute conversions. Stride is record spacing, not
 * readable payload size; every fetch independently validates its DMA range. */
static inline unsigned fut_graphics_vertex_payload(uint32_t format)
{
    unsigned bytes;
    switch(format&255u) {
    case 0x32:bytes=12;break;
    case 0x22:bytes=8;break;
    case 0x42:bytes=16;break;
    case 0x40:case 0x16:bytes=4;break;
    case 0x15:bytes=2;break;
    default:return 0;
    }
    return (format>>8)>=bytes?bytes:0;
}
#endif
