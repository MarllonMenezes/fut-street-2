#include "graphics_objects.h"
#include <string.h>

static uint32_t le32(const uint8_t *bytes)
{
    return (uint32_t)bytes[0] | ((uint32_t)bytes[1] << 8) |
        ((uint32_t)bytes[2] << 16) | ((uint32_t)bytes[3] << 24);
}

FutGraphicsObjectStatus fut_graphics_resolve_object(FutGraphicsMemoryRead read,
    void *context, uint32_t handle, unsigned channel, FutGraphicsObject *object)
{
    uint8_t config_bytes[4], table[32768], words[16];
    if (!read || !object || channel > 31)
        return FUT_OBJECT_INVALID_TABLE;
    memset(object, 0, sizeof(*object));
    if (!read(context, 0xFD002210u, config_bytes, 4))
        return FUT_OBJECT_UNREADABLE;
    const uint32_t config = le32(config_bytes);
    const uint32_t base = ((config & 0x1F0u) >> 4) << 12;
    const uint32_t size = 4096u << ((config >> 16) & 3u);
    if (base > 0x100000u - size)
        return FUT_OBJECT_INVALID_TABLE;
    if (!read(context, 0xFD700000u + base, table, size))
        return FUT_OBJECT_UNREADABLE;
    unsigned matches = 0;
    FutGraphicsObject found = {0};
    for (uint32_t offset = 0; offset < size; offset += 8) {
        const uint32_t key = le32(table + offset);
        const uint32_t descriptor = le32(table + offset + 4);
        if (key != handle || !(descriptor & 0x80000000u) ||
            ((descriptor >> 24) & 31u) != channel)
            continue;
        found.handle = key; found.descriptor = descriptor;
        found.instance = (descriptor & 0xFFFFu) << 4;
        found.engine = (descriptor >> 16) & 3u;
        found.channel = channel;
        ++matches;
    }
    if (!matches)
        return FUT_OBJECT_NOT_FOUND;
    if (matches != 1)
        return FUT_OBJECT_AMBIGUOUS;
    if (found.instance > 0x100000u - sizeof(words) ||
        !read(context, 0xFD700000u + found.instance, words, sizeof(words)))
        return FUT_OBJECT_UNREADABLE;
    for (unsigned i = 0; i < 4; ++i)
        found.words[i] = le32(words + i * 4);
    *object = found;
    return FUT_OBJECT_RESOLVED;
}

static int graphics_class_supported(unsigned class_id)
{
    return class_id == 0x97u || class_id == 0x39u || class_id == 0x9Fu ||
           class_id == 0x62u || class_id == 0x44u;
}

static int dma_object(const FutGraphicsObject *object)
{
    const unsigned class_id = object->words[0] & 0xFFFu;
    return class_id == 2u || class_id == 3u || class_id == 0x3Du;
}

int fut_graphics_dma_view(const FutGraphicsObject *object, FutGraphicsDmaView *view)
{
    if (!object || !view || !dma_object(object) ||
        (object->words[0] & 0x000FF000u) != 0x0002B000u)
        return 0;
    const uint32_t address = (object->words[2] & 0xFFFFF000u) | (object->words[0] >> 20);
    const uint64_t size = (uint64_t)object->words[1] + 1u;
    if (address >= 0x04000000u || size > 0x04000000u - address)
        return 0;
    view->guest_address = address | 0x80000000u;
    view->size = (uint32_t)size;
    return 1;
}

FutGraphicsBindingStatus fut_graphics_apply_binding(FutGraphicsBindings *state,
    unsigned subchannel, uint32_t method, uint32_t value)
{
    if (!state || subchannel >= 8 || (method & 3u))
        return FUT_BINDING_REJECTED;
    if (method == 0) {
        FutGraphicsObject object;
        if (fut_graphics_resolve_object(state->read, state->memory_context,
                value, 0, &object) != FUT_OBJECT_RESOLVED || object.engine != 1 ||
            !graphics_class_supported(object.words[0] & 0xFFu))
            return FUT_BINDING_REJECTED;
        state->objects[subchannel] = object;
        state->bound_mask |= 1u << subchannel;
        return FUT_BINDING_APPLIED;
    }
    if (!(state->bound_mask & (1u << subchannel)))
        return FUT_BINDING_REJECTED;
    const unsigned class_id = state->objects[subchannel].words[0] & 0xFFu;
    if (class_id == 0x9Fu && method == 0x2FCu) {
        if (value != 2u && value != 3u)
            return FUT_BINDING_REJECTED;
        state->blit_operation[subchannel] = value;
        return FUT_BINDING_APPLIED;
    }
    int is_reference =
        ((class_id == 0x39u || class_id == 0x62u) &&
            (method == 0x180u || method == 0x184u || method == 0x188u)) ||
        (class_id == 0x9Fu && method >= 0x180u && method <= 0x19Cu) ||
        (class_id == 0x97u && ((method >= 0x180u && method <= 0x188u) ||
                             (method >= 0x190u && method <= 0x1A8u)));
    if (!is_reference)
        return FUT_BINDING_NOT_HANDLED;
    FutGraphicsObject object;
    if (fut_graphics_resolve_object(state->read, state->memory_context,
            value, 0, &object) != FUT_OBJECT_RESOLVED)
        return FUT_BINDING_REJECTED;
    /* A null object disables optional 2D contexts. Other references must
     * match the method's type, not merely be present in the table. */
    const unsigned ref_class = object.words[0] & 0xFFu;
    int valid_type = dma_object(&object);
    if (class_id == 0x9Fu && method >= 0x184u) {
        static const unsigned classes[] = {0x57u, 0x19u, 0x44u, 0x43u, 0x12u, 0x72u, 0x62u};
        valid_type = object.engine == 1 &&
            (ref_class == classes[(method - 0x184u) / 4] || ref_class == 0x30u);
    }
    if (!valid_type)
        return FUT_BINDING_REJECTED;
    unsigned slot = (method - 0x180u) / 4;
    state->references[subchannel][slot] = object;
    state->reference_masks[subchannel] |= 1u << slot;
    return FUT_BINDING_APPLIED;
}
