#include "graphics_objects.h"
#include <stdio.h>
#include <string.h>

typedef struct { uint8_t memory[65536]; int fail_instance; } Fixture;
static void put32(uint8_t *data, uint32_t value)
{
    for (unsigned i = 0; i < 4; ++i) data[i] = (uint8_t)(value >> (i * 8));
}
static int read_fixture(void *context, uint32_t address, void *data, size_t size)
{
    Fixture *fixture = context;
    if (address == 0xFD002210u && size == 4) { put32(data, 0x03000000u); return 1; }
    if (address < 0xFD700000u) return 0;
    size_t offset = address - 0xFD700000u;
    if (offset > sizeof(fixture->memory) || size > sizeof(fixture->memory) - offset ||
        (fixture->fail_instance && offset >= 0x4000)) return 0;
    memcpy(data, fixture->memory + offset, size);
    return 1;
}
static void entry(Fixture *fixture, unsigned index, uint32_t handle,
                  uint32_t descriptor, uint32_t class_flags)
{
    put32(fixture->memory + index * 8, handle);
    put32(fixture->memory + index * 8 + 4, descriptor);
    put32(fixture->memory + ((descriptor & 0xFFFFu) << 4), class_flags);
}

int main(void)
{
    Fixture fixture = {0};
    entry(&fixture, 2, 0xD, 0x80010400u, 0x00001097u);
    /* Invalid same-handle entry and another channel are not candidates. */
    entry(&fixture, 3, 0xD, 0x00010410u, 0x62);
    entry(&fixture, 4, 0xD, 0x81010420u, 0x62);
    FutGraphicsObject object;
    if (fut_graphics_resolve_object(read_fixture, &fixture, 0xD, 0, &object) != FUT_OBJECT_RESOLVED ||
        object.instance != 0x4000 || object.engine != 1 || object.words[0] != 0x1097 || object.channel) return 1;
    if (fut_graphics_resolve_object(read_fixture, &fixture, 0xD, 1, &object) != FUT_OBJECT_RESOLVED ||
        object.instance != 0x4200) return 2;
    if (fut_graphics_resolve_object(read_fixture, &fixture, 0xABC, 0, &object) != FUT_OBJECT_NOT_FOUND ||
        object.handle) return 3;
    entry(&fixture, 5, 0xD, 0x80010430u, 0x97);
    if (fut_graphics_resolve_object(read_fixture, &fixture, 0xD, 0, &object) != FUT_OBJECT_AMBIGUOUS || object.handle) return 4;
    memset(fixture.memory + 5 * 8, 0, 8);
    fixture.fail_instance = 1;
    if (fut_graphics_resolve_object(read_fixture, &fixture, 0xD, 0, &object) != FUT_OBJECT_UNREADABLE) return 5;
    fixture.fail_instance = 0;

    entry(&fixture, 6, 0x10, 0x80010440u, 0x9F);
    entry(&fixture, 7, 7, 0x80000450u, 0x0002B03Du);
    entry(&fixture, 8, 0x19, 0x80010460u, 0x30);
    entry(&fixture, 9, 0x11, 0x80010470u, 0x62);
    FutGraphicsBindings bindings = {0};
    bindings.read = read_fixture; bindings.memory_context = &fixture;
    if (fut_graphics_apply_binding(&bindings, 0, 0x180, 7) != FUT_BINDING_REJECTED || bindings.bound_mask) return 6;
    if (fut_graphics_apply_binding(&bindings, 0, 0, 0xD) != FUT_BINDING_APPLIED ||
        fut_graphics_apply_binding(&bindings, 2, 0, 0x10) != FUT_BINDING_APPLIED ||
        bindings.bound_mask != 5) return 7;
    if (fut_graphics_apply_binding(&bindings, 0, 0x180, 7) != FUT_BINDING_APPLIED ||
        bindings.references[0][0].instance != 0x4500 || bindings.reference_masks[0] != 1) return 8;
    /* A present graphics object is still the wrong type for a DMA binding. */
    if (fut_graphics_apply_binding(&bindings, 0, 0x180, 0x11) != FUT_BINDING_REJECTED ||
        bindings.references[0][0].handle != 7) return 9;
    if (fut_graphics_apply_binding(&bindings, 2, 0x184, 0x19) != FUT_BINDING_APPLIED ||
        fut_graphics_apply_binding(&bindings, 2, 0x19C, 0x11) != FUT_BINDING_APPLIED ||
        fut_graphics_apply_binding(&bindings, 2, 0x184, 0x11) != FUT_BINDING_REJECTED) return 10;
    if (fut_graphics_apply_binding(&bindings, 2, 0x2FC, 3) != FUT_BINDING_APPLIED ||
        fut_graphics_apply_binding(&bindings, 2, 0x2FC, 0xFFFFFFFFu) != FUT_BINDING_REJECTED ||
        bindings.blit_operation[2] != 3) return 11;
    if (fut_graphics_apply_binding(&bindings, 0, 0x1D70, 0xF) != FUT_BINDING_NOT_HANDLED ||
        fut_graphics_apply_binding(&bindings, 8, 0, 0xD) != FUT_BINDING_REJECTED ||
        fut_graphics_apply_binding(&bindings, 0, 0x181, 7) != FUT_BINDING_REJECTED) return 12;
    FutGraphicsBindings before = bindings;
    if (fut_graphics_apply_binding(&bindings, 0, 0, 0xBAD) != FUT_BINDING_REJECTED ||
        memcmp(&before, &bindings, sizeof(bindings))) return 13;
    FutGraphicsObject dma = {0};
    dma.words[0] = 0x0402B03Du; dma.words[1] = 31; dma.words[2] = 0x00008003u;
    FutGraphicsDmaView view = {0};
    if (!fut_graphics_dma_view(&dma, &view) || view.guest_address != 0x80008040u || view.size != 32) return 14;
    /* Invalid forms cannot overwrite a previous valid resource view. */
    dma.words[1] = UINT32_MAX;
    if (fut_graphics_dma_view(&dma, &view) || view.guest_address != 0x80008040u || view.size != 32) return 15;
    dma.words[1] = 31; dma.words[0] = 0x0403B03Du;
    if (fut_graphics_dma_view(&dma, &view)) return 16;
    dma.words[0] = 0x0402B03Du; dma.words[2] = 0x04000003u;
    if (fut_graphics_dma_view(&dma, &view)) return 17;
    dma.words[0] = 0x0000B03Du; dma.words[1] = 0x07FFAFFFu; dma.words[2] = 3;
    if (fut_graphics_dma_view(&dma, &view) || !fut_graphics_dma_surface_view(&dma, &view) ||
        view.guest_address != 0x80000000u || view.size != 0x04000000u) return 18;
    dma.words[0] = 0x0001B03Du;
    if (fut_graphics_dma_surface_view(&dma, &view)) return 19; /* Tiled target not guessed. */
    dma.words[0] = 0x0000B03Du; dma.words[2] = 0x04000003;
    if (fut_graphics_dma_surface_view(&dma, &view)) return 20;
    dma.words[2] = 3; dma.words[1] = UINT32_MAX;
    if (fut_graphics_dma_surface_view(&dma, &view)) return 21;
    puts("Exact object resolution, typed references and failure-state preservation OK");
    return 0;
}
