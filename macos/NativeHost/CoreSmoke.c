#include <dlfcn.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

// Exercise actual NativeAOT exports in a fresh process, independent of AppKit.
int main(int argc, char **argv) {
    if (argc != 3) return 2;
    void *core = dlopen(argv[1], RTLD_NOW | RTLD_LOCAL);
    if (!core) { fprintf(stderr, "%s\n", dlerror()); return 3; }
    int (*version)(void) = dlsym(core, "c64_abi_version");
    int (*load)(const uint8_t *, int) = dlsym(core, "c64_load_skia");
    uint64_t (*create)(const uint8_t *, int, int) = dlsym(core, "c64_create");
    int (*render)(uint64_t, uint8_t *, int, int, int, int, double) = dlsym(core, "c64_render");
    void (*destroy)(uint64_t) = dlsym(core, "c64_destroy");
    if (!version || !load || !create || !render || !destroy || version() != 1) return 4;
    if (!load((const uint8_t *)argv[2], (int)strlen(argv[2]))) return 5;
    const int width = 384, height = 288, stride = width * 4, size = stride * height;
    uint8_t *pixels = malloc(size);
    if (!pixels) return 6;
    memset(pixels, 0xCD, size);
    uint64_t session = create(NULL, 0, 123);
    if (!session || render(session, pixels, size - 1, width, height, stride, 0)) return 7;
    if (!render(session, pixels, size, width, height, stride, 0)) return 8;
    int changed = 0;
    for (int i = 0; i < size; i++) if (pixels[i] != 0xCD) { changed = 1; break; }
    if (!changed) return 9;
    destroy(session);
    if (render(session, pixels, size, width, height, stride, 0)) return 10;
    free(pixels);
    puts("NativeAOT shared library: exports, Skia load, session, frame, bounds and disposal passed.");
    // Do not dlclose a NativeAOT library.
    return 0;
}
