#import <Foundation/Foundation.h>
#import <Metal/Metal.h>
#import <dlfcn.h>
#import <stdint.h>
#import <time.h>
#import <string.h>
#import <stdlib.h>
typedef uint64_t (*CreateFn)(const uint8_t *, int, int);
typedef int (*RenderFn)(uint64_t, uint8_t *, int, int, int, int, double);
static double clockSeconds(void) { struct timespec t; clock_gettime(CLOCK_MONOTONIC, &t); return t.tv_sec+t.tv_nsec/1e9; }
int main(int argc, char **argv) {
    @autoreleasepool {
        if (argc != 3) return 2;
        void *module = dlopen(argv[1], RTLD_NOW | RTLD_LOCAL);
        if (!module) return 3;
        int (*load)(const uint8_t *,int) = dlsym(module,"c64_load_skia");
        int (*metal)(void *,void *) = dlsym(module,"c64_set_metal");
        int (*backend)(uint64_t) = dlsym(module,"c64_render_backend");
        CreateFn create = dlsym(module,"c64_create");
        RenderFn render = dlsym(module,"c64_render");
        void (*destroy)(uint64_t) = dlsym(module,"c64_destroy");
        if (!load || !metal || !backend || !create || !render || !destroy || !load((const uint8_t *)argv[2],(int)strlen(argv[2]))) return 4;
        id<MTLDevice> device = MTLCreateSystemDefaultDevice();
        __attribute__((objc_precise_lifetime)) id<MTLCommandQueue> queue = [device newCommandQueue];
        if (!device || !queue) { NSLog(@"SKIP: no Metal device in this runner; native CPU fallback remains covered."); return 0; }
        const int width=960,height=540,stride=width*4,bytes=stride*height;
        uint8_t *pixels = malloc(bytes);
        if (!pixels) return 5;
        NSArray<NSNumber *> *demos = @[@5,@4];
        double cpuTimes[2];
        // Measure both CPU sessions before globally registering a Metal device.
        for (NSUInteger index=0;index<demos.count;index++) {
            NSDictionary *settings = @{@"AmigaMode":@YES,@"AmigaBoot":@0,@"AmigaAnimation":demos[index],@"PowerTransition":@NO};
            NSData *json = [NSJSONSerialization dataWithJSONObject:settings options:0 error:nil];
            uint64_t cpu = create(json.bytes,(int)json.length,64);
            if (!cpu || !render(cpu,pixels,bytes,width,height,stride,1.0/60)) return 6;
            double begin=clockSeconds();
            for(int frame=0;frame<24;frame++) if (!render(cpu,pixels,bytes,width,height,stride,1.0/60)) return 7;
            cpuTimes[index]=(clockSeconds()-begin)*1000/24;
            destroy(cpu);
        }
        if (!metal((__bridge void *)device,(__bridge void *)queue)) return 8;
        for (NSUInteger index=0;index<demos.count;index++) {
            NSDictionary *settings = @{@"AmigaMode":@YES,@"AmigaBoot":@0,@"AmigaAnimation":demos[index],@"PowerTransition":@NO};
            NSData *json = [NSJSONSerialization dataWithJSONObject:settings options:0 error:nil];
            uint64_t gpu = create(json.bytes,(int)json.length,64);
            if (!gpu) return 9;
            for(int frame=0;frame<5;frame++) if (!render(gpu,pixels,bytes,width,height,stride,1.0/60)) return 10;
            if (backend(gpu)!=1) { NSLog(@"FAIL: Metal device exists, but the renderer did not use it."); return 11; }
            double begin=clockSeconds();
            for(int frame=0;frame<24;frame++) if (!render(gpu,pixels,bytes,width,height,stride,1.0/60)) return 12;
            double gpuMs=(clockSeconds()-begin)*1000/24;
            BOOL colored=NO;
            for(int i=0;i<bytes;i+=4) if(pixels[i]||pixels[i+1]||pixels[i+2]) { colored=YES;break; }
            if(!colored) return 13;
            NSLog(@"PASS: demo %@ CPU %.2f ms/frame; Metal %.2f ms/frame including BGRA readback (%@).",demos[index],cpuTimes[index],gpuMs,device.name);
            destroy(gpu);
        }
        free(pixels);
    }
    return 0;
}
