#ifndef FUT_NATIVE_INPUT_H
#define FUT_NATIVE_INPUT_H
#include <stdint.h>
#include <windows.h>
/* Xbox packet layouts are encoded explicitly, without host struct alignment. */
typedef struct { uint8_t state[22],caps[25]; unsigned physical; } FutNativeInputSample;
int fut_native_input_enabled(void);
DWORD fut_native_input_sample(unsigned port,FutNativeInputSample *sample);
DWORD fut_native_input_rumble(unsigned port,WORD left,WORD right);
/* Called only by the native display's window procedure. */
void fut_native_input_message(UINT message,WPARAM key);
/* Strict bounded script: milliseconds:button:hold, separated by commas.
 * Returns -1 for malformed input, 0 inactive, 1 active at the supplied time. */
int fut_native_input_script(const char *spec,uint64_t elapsed,uint16_t *digital,uint8_t analog[8]);
/* Returns 0 without modifying guest state when this SDK call is not selected. */
int fut_native_input_dispatch(uint32_t site);
#endif
