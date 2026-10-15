#ifndef FUT_GUEST_INTERRUPT_GATE_H
#define FUT_GUEST_INTERRUPT_GATE_H
#include <stdint.h>
/* Host serialization for the existing single guest interrupt-mask domain.
 * Not a scheduler, IRQL model, clock, or interrupt producer. */
void fut_guest_interrupt_cli(void);
void fut_guest_interrupt_sti(void);
uint8_t fut_guest_interrupt_enabled(void);
/* Nonblocking, non-nestable. Success holds the gate with IF cleared.
 * The caller must leave on the same thread, restoring entry IF (enabled),
 * even if the guest routine returned with CLI active. */
int fut_guest_interrupt_try_enter(void);
void fut_guest_interrupt_leave(void);
/* Reentrant serialization of MMIO state, preserving IF. Pair on same thread;
 * safe inside CLI or a host interrupt scope. Does not deliver an interrupt. */
void fut_guest_interrupt_lock_state(void);
void fut_guest_interrupt_unlock_state(void);
#endif
