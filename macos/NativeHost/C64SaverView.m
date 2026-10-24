#import <ScreenSaver/ScreenSaver.h>
#import <QuartzCore/QuartzCore.h>
#import <dlfcn.h>
#import <stdint.h>

typedef int (*VersionFn)(void);
typedef int (*LoadFn)(const uint8_t *, int);
typedef uint64_t (*CreateFn)(const uint8_t *, int, int);
typedef int (*RenderFn)(uint64_t, uint8_t *, int, int, int, int, double);
typedef void (*DestroyFn)(uint64_t);
static CreateFn createSession;
static RenderFn renderSession;
static DestroyFn destroySession;
static BOOL loadCore(NSBundle *bundle) {
    static dispatch_once_t once;
    static BOOL ready;
    dispatch_once(&once, ^{
        // NativeAOT stays loaded for the entire host process; never dlclose.
        NSString *path = [bundle.privateFrameworksPath stringByAppendingPathComponent:@"C64Screensaver.NativeCore.dylib"];
        void *module = dlopen(path.fileSystemRepresentation, RTLD_NOW | RTLD_LOCAL);
        if (!module) { NSLog(@"C64Screensaver: %s", dlerror()); return; }
        VersionFn version = (VersionFn)dlsym(module, "c64_abi_version");
        LoadFn load = (LoadFn)dlsym(module, "c64_load_skia");
        createSession = (CreateFn)dlsym(module, "c64_create");
        renderSession = (RenderFn)dlsym(module, "c64_render");
        destroySession = (DestroyFn)dlsym(module, "c64_destroy");
        if (!version || version() != 1 || !load || !createSession || !renderSession || !destroySession) return;
        NSData *skia = [[bundle.privateFrameworksPath stringByAppendingPathComponent:@"libSkiaSharp.dylib"] dataUsingEncoding:NSUTF8StringEncoding];
        ready = load(skia.bytes, (int)skia.length) == 1;
    });
    return ready;
}

@interface C64SaverView : ScreenSaverView {
    uint64_t _session;
    uint8_t *_pixels;
    int _capacity;
    CFTimeInterval _previous;
    NSImage *_image;
    BOOL _failed;
    NSWindow *_options;
    NSPopUpButton *_program;
    NSButton *_aspect, *_border, *_glow;
    NSTextField *_company, *_website, *_tagline, *_duration;
}
@end

@implementation C64SaverView
- (instancetype)initWithFrame:(NSRect)frame isPreview:(BOOL)preview {
    self = [super initWithFrame:frame isPreview:preview];
    if (self) self.animationTimeInterval = 1.0 / 30.0;
    return self;
}
- (BOOL)isOpaque { return YES; }
- (BOOL)isFlipped { return YES; }
- (NSUserDefaults *)preferences {
    return [[NSUserDefaults alloc] initWithSuiteName:@"de.gruetze-software.C64Screensaver.Native"];
}
- (NSDictionary *)settings {
    NSDictionary *saved = [[self preferences] dictionaryForKey:@"Settings"];
    return saved ?: @{@"Programs": @0, @"ProgramDurationSeconds": @100, @"KeepAspectRatio": @YES,
        @"CompanyName": @"GRUETZE-SOFTWARE", @"CompanyWebsite": @"WWW.GRUETZE-SOFTWARE.DE",
        @"CompanyTagline": @"SOFTWARE MIT CHARAKTER"};
}
- (void)releaseSession {
    if (_session && destroySession) destroySession(_session);
    _session = 0;
    free(_pixels); _pixels = NULL; _capacity = 0;
    _image = nil; _previous = 0;
}
- (void)startAnimation {
    [super startAnimation];
    [self releaseSession];
    _failed = !loadCore([NSBundle bundleForClass:self.class]);
    if (!_failed) {
        NSData *json = [NSJSONSerialization dataWithJSONObject:[self settings] options:0 error:nil];
        _session = createSession(json.bytes, (int)json.length, (int)arc4random());
        _failed = !_session;
    }
    [self animateOneFrame];
}
- (void)stopAnimation { [super stopAnimation]; [self releaseSession]; }
- (void)viewDidMoveToWindow {
    [super viewDidMoveToWindow];
    if (!self.window) [self releaseSession];
}
- (void)dealloc { [self releaseSession]; }
- (void)animateOneFrame {
    if (!_session || self.bounds.size.width <= 0 || self.bounds.size.height <= 0) return;
    CGFloat scale = self.window.backingScaleFactor ?: 1;
    double w = self.bounds.size.width * scale, h = self.bounds.size.height * scale;
    double limit = fmin(1, fmin(1920.0 / w, 1080.0 / h));
    int width = MAX(1, (int)(w * limit)), height = MAX(1, (int)(h * limit));
    int stride = width * 4, needed = stride * height;
    if (needed != _capacity) {
        void *replacement = realloc(_pixels, needed);
        if (!replacement) { _failed = YES; [self setNeedsDisplay:YES]; return; }
        _pixels = replacement; _capacity = needed;
    }
    CFTimeInterval now = CACurrentMediaTime();
    double delta = _previous ? fmin(.25, now - _previous) : 0;
    _previous = now;
    if (!renderSession(_session, _pixels, _capacity, width, height, stride, delta)) {
        _failed = YES; [self setNeedsDisplay:YES]; return;
    }
    // Immutable copy: AppKit may retain the frame after the next render starts.
    CFDataRef data = CFDataCreate(kCFAllocatorDefault, _pixels, needed);
    CGDataProviderRef provider = CGDataProviderCreateWithCFData(data);
    CGColorSpaceRef colors = CGColorSpaceCreateDeviceRGB();
    CGImageRef cg = CGImageCreate(width, height, 8, 32, stride, colors,
        kCGBitmapByteOrder32Little | kCGImageAlphaPremultipliedFirst, provider, NULL, false, kCGRenderingIntentDefault);
    _image = cg ? [[NSImage alloc] initWithCGImage:cg size:self.bounds.size] : nil;
    if (cg) CGImageRelease(cg);
    CGColorSpaceRelease(colors); CGDataProviderRelease(provider); CFRelease(data);
    [self setNeedsDisplay:YES];
}
- (void)drawRect:(NSRect)dirty {
    [[NSColor blackColor] setFill]; NSRectFill(self.bounds);
    if (_failed) {
        [@"C64Screensaver prototype could not load. See Console." drawAtPoint:NSMakePoint(12, 20)
            withAttributes:@{NSForegroundColorAttributeName: NSColor.whiteColor}];
    } else [_image drawInRect:self.bounds fromRect:NSZeroRect operation:NSCompositingOperationCopy
        fraction:1 respectFlipped:YES hints:@{NSImageHintInterpolation: @(NSImageInterpolationNone)}];
}
- (BOOL)hasConfigureSheet { return YES; }
- (NSTextField *)field:(NSString *)value label:(NSString *)label y:(CGFloat)y {
    NSTextField *caption = [NSTextField labelWithString:label];
    caption.frame = NSMakeRect(20, y, 110, 24); [_options.contentView addSubview:caption];
    NSTextField *field = [[NSTextField alloc] initWithFrame:NSMakeRect(135, y, 345, 24)];
    field.stringValue = value; [_options.contentView addSubview:field]; return field;
}
- (NSButton *)check:(NSString *)label value:(BOOL)value y:(CGFloat)y {
    NSButton *button = [NSButton checkboxWithTitle:label target:nil action:nil];
    button.frame = NSMakeRect(20, y, 460, 24); button.state = value ? NSControlStateValueOn : NSControlStateValueOff;
    [_options.contentView addSubview:button]; return button;
}
- (NSWindow *)configureSheet {
    NSDictionary *s = [self settings];
    _options = [[NSWindow alloc] initWithContentRect:NSMakeRect(0,0,500,370)
        styleMask:NSWindowStyleMaskTitled backing:NSBackingStoreBuffered defer:NO];
    _options.title = @"C64Screensaver · Experimental";
    _program = [[NSPopUpButton alloc] initWithFrame:NSMakeRect(20,325,460,28) pullsDown:NO];
    [_program addItemsWithTitles:@[@"All programs", @"Maze", @"Company card", @"Stars"]];
    [_program selectItemAtIndex:MAX(0, MIN(3, [s[@"Programs"] integerValue]))];
    [_options.contentView addSubview:_program];
    _aspect = [self check:@"Original 4:3 proportions" value:s[@"KeepAspectRatio"] ? [s[@"KeepAspectRatio"] boolValue] : YES y:290];
    _border = [self check:@"Hide C64 border" value:[s[@"FillScreen"] boolValue] y:260];
    _glow = [self check:@"Strong phosphor glow" value:[s[@"Glow"] doubleValue] > 50 y:230];
    _duration = [self field:[(s[@"ProgramDurationSeconds"] ?: @100) stringValue] label:@"Seconds / program" y:190];
    _company = [self field:s[@"CompanyName"] ?: @"GRUETZE-SOFTWARE" label:@"Company" y:150];
    _website = [self field:s[@"CompanyWebsite"] ?: @"WWW.GRUETZE-SOFTWARE.DE" label:@"Website" y:110];
    _tagline = [self field:s[@"CompanyTagline"] ?: @"SOFTWARE MIT CHARAKTER" label:@"Extra text" y:70];
    NSButton *save = [NSButton buttonWithTitle:@"Save" target:self action:@selector(saveOptions:)];
    save.frame = NSMakeRect(390,20,90,30); save.keyEquivalent = @"\r"; [_options.contentView addSubview:save];
    NSButton *cancel = [NSButton buttonWithTitle:@"Cancel" target:self action:@selector(cancelOptions:)];
    cancel.frame = NSMakeRect(290,20,90,30); [_options.contentView addSubview:cancel];
    return _options;
}
- (void)saveOptions:(id)sender {
    BOOL strong = _glow.state == NSControlStateValueOn;
    NSDictionary *settings = @{@"Programs": @(_program.indexOfSelectedItem),
        @"ProgramDurationSeconds": @(MAX(15,MIN(1800,_duration.integerValue))),
        @"KeepAspectRatio": @(_aspect.state == NSControlStateValueOn),
        @"FillScreen": @(_border.state == NSControlStateValueOn),
        @"Glow": strong ? @85 : @12, @"GlowSpread": strong ? @65 : @30,
        @"CompanyName": _company.stringValue, @"CompanyWebsite": _website.stringValue,
        @"CompanyTagline": _tagline.stringValue};
    [[self preferences] setObject:settings forKey:@"Settings"];
    [self.window endSheet:_options]; _options = nil;
    if (self.isAnimating) [self startAnimation];
}
- (void)cancelOptions:(id)sender { [self.window endSheet:_options]; _options = nil; }
@end
