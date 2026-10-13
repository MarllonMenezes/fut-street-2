#include "recomp_types.h"
#include "xbox_memory_layout.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <windows.h>

#define FUT_STREET_2_ENTRY_POINT 0x0024D8BEu

/* These host-runtime declarations match the pinned XboxRecomp kernel.h ABI. */
void xbox_kernel_init(void);
void xbox_kernel_shutdown(void);
void xbox_kernel_bridge_init(void);
void xbox_path_init(const char *game_dir, const char *save_dir);

static int read_xbe(const char *path, void **out_data, size_t *out_size)
{
    FILE *file = fopen(path, "rb");
    long file_size;
    void *data;

    if (file == NULL) {
        fprintf(stderr, "Cannot open XBE: %s\n", path);
        return 0;
    }
    if (fseek(file, 0, SEEK_END) != 0 || (file_size = ftell(file)) <= 0 ||
        fseek(file, 0, SEEK_SET) != 0) {
        fprintf(stderr, "Cannot determine XBE size: %s\n", path);
        fclose(file);
        return 0;
    }

    data = malloc((size_t)file_size);
    if (data == NULL) {
        fclose(file);
        return 0;
    }
    if (fread(data, 1, (size_t)file_size, file) != (size_t)file_size) {
        fprintf(stderr, "Cannot read complete XBE: %s\n", path);
        free(data);
        fclose(file);
        return 0;
    }

    fclose(file);
    *out_data = data;
    *out_size = (size_t)file_size;
    return 1;
}

static int environment_variable_is_set(const char *name)
{
    return getenv(name) != NULL;
}

int main(int argc, char **argv)
{
    void *xbe_data = NULL;
    size_t xbe_size = 0;
    recomp_func_t entry;
    int memory_ready = 0;
    int kernel_ready = 0;
    int result = 1;

    if (argc == 2 && strcmp(argv[1], "--help") == 0) {
        puts("Fut Street 2 static recompilation host");
        puts("Usage: fut-street-2-recompiled --launch <default.xbe> <game-data-directory> <save-directory>");
        puts("This build excludes the XboxRecomp NV2A/APU emulators.");
        return 0;
    }
    if (argc != 5 || strcmp(argv[1], "--launch") != 0) {
        fprintf(stderr,
                "Refusing implicit launch. Use --help or explicitly provide "
                "--launch <default.xbe> <game-data-directory> <save-directory>.\n");
        return 2;
    }
    if (environment_variable_is_set("RECOMP_AC97_READY") ||
        environment_variable_is_set("RECOMP_PB_EXEC")) {
        fprintf(stderr,
                "Refusing emulator opt-in environment variables in the "
                "no-emulation build.\n");
        return 2;
    }
    /* This title constructs dispatcher events directly in guest memory. */
    if (_putenv_s("RECOMP_TITLE_KEVENTS", "1") != 0 ||
        !SetEnvironmentVariableA("RECOMP_TITLE_KEVENTS", "1")) {
        fprintf(stderr, "Cannot enable title-built event compatibility.\n");
        return 1;
    }
    if (!read_xbe(argv[2], &xbe_data, &xbe_size))
        return 1;

    if (!xbox_MemoryLayoutInit(xbe_data, xbe_size)) {
        fprintf(stderr, "Xbox memory layout initialization failed.\n");
        goto cleanup;
    }
    memory_ready = 1;

    xbox_kernel_init();
    kernel_ready = 1;
    /* Require caller-owned storage so guest writes never default into the
     * user's XboxRecomp profile. Bring-up callers should pass an isolated dir. */
    xbox_path_init(argv[3], argv[4]);
    xbox_kernel_bridge_init();

    if (!recomp_dispatch_init())
        fprintf(stderr, "Dispatch acceleration unavailable; using lookup fallback.\n");

    g_esp = XBOX_STACK_TOP;
    entry = recomp_lookup(FUT_STREET_2_ENTRY_POINT);
    if (entry == NULL) {
        fprintf(stderr, "No translated entry point at 0x%08X.\n",
                FUT_STREET_2_ENTRY_POINT);
        goto cleanup;
    }

    fprintf(stderr,
            "Calling translated entry 0x%08X in experimental bring-up; "
            "this is not a validated game runtime.\n",
            FUT_STREET_2_ENTRY_POINT);
    entry();
    result = 0;

cleanup:
    if (kernel_ready)
        xbox_kernel_shutdown();
    if (memory_ready)
        xbox_MemoryLayoutShutdown();
    free(xbe_data);
    return result;
}
