#ifndef FUT_GRAPHICS_OBJECTS_H
#define FUT_GRAPHICS_OBJECTS_H

#include <stddef.h>
#include <stdint.h>

typedef int (*FutGraphicsMemoryRead)(void *context, uint32_t address, void *data, size_t size);
typedef enum {
    FUT_OBJECT_RESOLVED, FUT_OBJECT_NOT_FOUND, FUT_OBJECT_AMBIGUOUS,
    FUT_OBJECT_UNREADABLE, FUT_OBJECT_INVALID_TABLE
} FutGraphicsObjectStatus;
typedef struct {
    uint32_t handle, instance, descriptor, words[4];
    unsigned engine, channel;
} FutGraphicsObject;
typedef struct { uint32_t guest_address, size; } FutGraphicsDmaView;

/* The linear PCI/unified-RAM descriptor form observed in the title. Other
 * descriptor forms remain unsupported; this does not perform a DMA transfer. */
int fut_graphics_dma_view(const FutGraphicsObject *object, FutGraphicsDmaView *view);

/* Resolve an exact handle/channel from the title-built RAMHT and retain
 * the original context words. Failure never supplies a guessed object. */
FutGraphicsObjectStatus fut_graphics_resolve_object(FutGraphicsMemoryRead read,
    void *context, uint32_t handle, unsigned channel, FutGraphicsObject *object);

typedef struct {
    FutGraphicsMemoryRead read;
    void *memory_context;
    FutGraphicsObject objects[8], references[8][16];
    unsigned bound_mask, reference_masks[8];
    uint32_t blit_operation[8];
} FutGraphicsBindings;
typedef enum {
    FUT_BINDING_APPLIED, FUT_BINDING_NOT_HANDLED, FUT_BINDING_REJECTED
} FutGraphicsBindingStatus;

/* Applies only object/context association and deferred blit operation state.
 * It neither executes a draw/copy nor publishes any GPU completion. */
FutGraphicsBindingStatus fut_graphics_apply_binding(FutGraphicsBindings *state,
    unsigned subchannel, uint32_t method, uint32_t value);

#endif
