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
static int (*testPower)(uint64_t);
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
        testPower = (int (*)(uint64_t))dlsym(module, "c64_test_power");
        if (!version || version() != 1 || !load || !createSession || !renderSession || !destroySession || !testPower) return;
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
    CGColorSpaceRef _colors;
    NSInteger _renderQuality;
    NSImage *_image;
    BOOL _failed, _stopped;
    NSWindow *_options;
    NSPopUpButton *_program, *_quality;
    NSButton *_aspect, *_border, *_fullScreen, *_power;
    NSSlider *_phosphor, *_glow, *_spread, *_scanlines;
    NSTextField *_company, *_website, *_tagline, *_duration;
}
@end

@implementation C64SaverView
- (instancetype)initWithFrame:(NSRect)frame isPreview:(BOOL)preview {
    self = [super initWithFrame:frame isPreview:preview];
    if (self) {
        self.animationTimeInterval = 1.0 / 30.0;
        _colors = CGColorSpaceCreateDeviceRGB();
    }
    return self;
}
- (BOOL)isOpaque { return YES; }
- (BOOL)isFlipped { return YES; }
- (NSUserDefaults *)preferences {
    return [[NSUserDefaults alloc] initWithSuiteName:@"de.gruetze-software.C64Screensaver.Native"];
}
- (NSDictionary *)settings {
    NSDictionary *saved = [[self preferences] dictionaryForKey:@"Settings"];
    if (saved) {
        // Earlier builds boxed C comparisons as NSNumber(int), producing JSON
        // 0/1 instead of booleans. Repair them before crossing the typed C ABI.
        NSMutableDictionary *normalized = [saved mutableCopy];
        for (NSString *key in @[@"KeepAspectRatio", @"FillScreen", @"CrtEnabled",
                @"Scanlines", @"CycleColors", @"PowerTransition"])
            if (normalized[key]) normalized[key] = [normalized[key] boolValue] ? @YES : @NO;
        return normalized;
    }
    return @{@"Programs": @0, @"ProgramDurationSeconds": @100, @"KeepAspectRatio": @YES,
        @"CompanyName": @"GRUETZE-SOFTWARE", @"CompanyWebsite": @"WWW.GRUETZE-SOFTWARE.DE",
        @"CompanyTagline": @"SOFTWARE MIT CHARAKTER"};
}
- (void)releaseSession {
    if (_session && destroySession) destroySession(_session);
    _session = 0;
    free(_pixels); _pixels = NULL; _capacity = 0;
    _image = nil; _previous = 0;
}
- (BOOL)ensureSession {
    if (_session) return YES;
    if (_failed) return NO;
    _failed = !loadCore([NSBundle bundleForClass:self.class]);
    if (!_failed) {
        NSDictionary *settings = [self settings];
        _renderQuality = MAX(0, MIN(2, [settings[@"NativeRenderQuality"] integerValue]));
        NSData *json = [NSJSONSerialization dataWithJSONObject:settings options:0 error:nil];
        _session = createSession(json.bytes, (int)json.length, (int)arc4random());
        _failed = !_session;
    }
    if (_failed) NSLog(@"C64Screensaver: unable to create rendering session");
    return _session != 0;
}
- (void)startAnimation {
    [super startAnimation];
    _stopped = NO;
    _failed = NO;
    // A host can attach/reparent a preview after startAnimation. Do not discard
    // a valid session merely because start is called again.
    [self animateOneFrame];
}
- (void)stopAnimation {
    [super stopAnimation];
    _stopped = YES;
    [self releaseSession];
}
- (void)viewDidMoveToWindow {
    [super viewDidMoveToWindow];
    if (!self.window) {
        [self releaseSession];
    } else {
        // Reparenting does not necessarily produce another startAnimation call.
        _stopped = NO;
        _previous = 0;
        [self animateOneFrame];
    }
}
- (void)dealloc { [self releaseSession]; if (_colors) CGColorSpaceRelease(_colors); }
- (void)animateOneFrame {
    if (_stopped || self.bounds.size.width <= 0 || self.bounds.size.height <= 0 || ![self ensureSession]) return;
    CFTimeInterval now = CACurrentMediaTime();
    // AppKit may request multiple draws/start callbacks within one timer tick.
    // Retain the last immutable frame rather than recomputing identical images.
    if (_image && _previous && now - _previous < self.animationTimeInterval * .95) return;
    CGFloat scale = self.window.backingScaleFactor ?: 1;
    double w = self.bounds.size.width * scale, h = self.bounds.size.height * scale;
    const double maxWidth[] = {960, 1280, 1920};
    const double maxHeight[] = {540, 720, 1080};
    double limit = fmin(1, fmin(maxWidth[_renderQuality] / w, maxHeight[_renderQuality] / h));
    int width = MAX(1, (int)(w * limit)), height = MAX(1, (int)(h * limit));
    int stride = width * 4, needed = stride * height;
    if (needed != _capacity) {
        void *replacement = realloc(_pixels, needed);
        if (!replacement) { _failed = YES; [self setNeedsDisplay:YES]; return; }
        _pixels = replacement; _capacity = needed;
    }
    double delta = _previous ? fmin(.25, now - _previous) : 0;
    _previous = now;
    if (!renderSession(_session, _pixels, _capacity, width, height, stride, delta)) {
        _failed = YES; [self setNeedsDisplay:YES]; return;
    }
    // Immutable copy: AppKit may retain the frame after the next render starts.
    CFDataRef data = CFDataCreate(kCFAllocatorDefault, _pixels, needed);
    CGDataProviderRef provider = CGDataProviderCreateWithCFData(data);

    CGImageRef cg = CGImageCreate(width, height, 8, 32, stride, _colors,
        kCGBitmapByteOrder32Little | kCGImageAlphaPremultipliedFirst, provider, NULL, false, kCGRenderingIntentDefault);
    _image = cg ? [[NSImage alloc] initWithCGImage:cg size:self.bounds.size] : nil;
    if (cg) CGImageRelease(cg);
    CGDataProviderRelease(provider); CFRelease(data);
    [self setNeedsDisplay:YES];
}
- (void)drawRect:(NSRect)dirty {
    if (!_image && !_stopped) [self animateOneFrame];
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
    // macOS can request options independently of the animation view's window.
    // Keep one sheet alive and perform all AppKit construction on the main thread.
    if (![NSThread isMainThread]) {
        __block NSWindow *sheet;
        dispatch_sync(dispatch_get_main_queue(), ^{ sheet = [self configureSheet]; });
        return sheet;
    }
    if (_options) return _options;
    NSLog(@"C64Screensaver: creating options sheet");
    @try {
        return [self buildConfigureSheet];
    } @catch (NSException *exception) {
        NSLog(@"C64Screensaver: options failed: %@ %@", exception.name, exception.reason);
        _options = nil;
        return nil;
    }
}
- (NSSlider *)slider:(NSString *)label value:(double)value y:(CGFloat)y tag:(NSInteger)tag {
    NSTextField *caption = [NSTextField labelWithString:label];
    caption.frame = NSMakeRect(20,y,145,24); [_options.contentView addSubview:caption];
    NSSlider *slider = [NSSlider sliderWithValue:fmax(0,fmin(100,value)) minValue:0 maxValue:100
        target:self action:@selector(sliderChanged:)];
    slider.frame = NSMakeRect(170,y,245,24); slider.tag = tag;
    slider.continuous = YES; [_options.contentView addSubview:slider];
    NSTextField *percent = [NSTextField labelWithString:[NSString stringWithFormat:@"%.0f%%",slider.doubleValue]];
    percent.frame = NSMakeRect(430,y,50,24); percent.tag = tag + 100;
    [_options.contentView addSubview:percent];
    return slider;
}
- (void)sliderChanged:(NSSlider *)sender {
    NSTextField *percent = (NSTextField *)[_options.contentView viewWithTag:sender.tag + 100];
    percent.stringValue = [NSString stringWithFormat:@"%.0f%%",sender.doubleValue];
}
- (void)fullScreenChanged:(id)sender {
    BOOL full = _fullScreen.state == NSControlStateValueOn;
    _aspect.enabled = !full; _border.enabled = !full;
}
- (NSWindow *)buildConfigureSheet {
    NSDictionary *s = [self settings];
    _options = [[NSPanel alloc] initWithContentRect:NSMakeRect(0,0,500,660)
        styleMask:NSWindowStyleMaskTitled backing:NSBackingStoreBuffered defer:NO];
    _options.releasedWhenClosed = NO;
    NSBundle *bundle = [NSBundle bundleForClass:self.class];
    NSString *version = [bundle objectForInfoDictionaryKey:@"CFBundleShortVersionString"] ?: @"?";
    NSString *build = [bundle objectForInfoDictionaryKey:@"CFBundleVersion"] ?: @"?";
    _options.title = @"C64Screensaver · Settings";
    _program = [[NSPopUpButton alloc] initWithFrame:NSMakeRect(20,575,460,28) pullsDown:NO];
    [_program addItemsWithTitles:@[@"All programs", @"Maze", @"Company card", @"Stars"]];
    [_program selectItemAtIndex:MAX(0, MIN(3, [s[@"Programs"] integerValue]))];
    [_options.contentView addSubview:_program];
    BOOL aspect = s[@"KeepAspectRatio"] ? [s[@"KeepAspectRatio"] boolValue] : YES;
    BOOL border = [s[@"FillScreen"] boolValue];
    _fullScreen = [self check:@"Fill entire screen — no black bars or C64 border" value:!aspect && border y:540];
    _fullScreen.target = self; _fullScreen.action = @selector(fullScreenChanged:);
    _aspect = [self check:@"Original 4:3 proportions (with black bars)" value:aspect y:510];
    _border = [self check:@"Hide C64 border" value:border y:480];
    [self fullScreenChanged:nil];
    _phosphor = [self slider:@"Phosphor mask" value:[s[@"MaskStrength"] ?: @8 doubleValue] y:435 tag:1];
    _glow = [self slider:@"Phosphor glow" value:[s[@"Glow"] ?: @12 doubleValue] y:390 tag:2];
    _spread = [self slider:@"Glow spread" value:[s[@"GlowSpread"] ?: @30 doubleValue] y:345 tag:3];
    _scanlines = [self slider:@"Scanline strength" value:[s[@"ScanlineStrength"] ?: @22 doubleValue] y:300 tag:4];
    _power = [self check:@"CRT power-off / dot / power-on between programs" value:s[@"PowerTransition"] ? [s[@"PowerTransition"] boolValue] : YES y:270];
    _duration = [self field:[(s[@"ProgramDurationSeconds"] ?: @100) stringValue] label:@"Seconds / program" y:245];
    _company = [self field:s[@"CompanyName"] ?: @"GRUETZE-SOFTWARE" label:@"Company" y:205];
    _website = [self field:s[@"CompanyWebsite"] ?: @"WWW.GRUETZE-SOFTWARE.DE" label:@"Website" y:165];
    _tagline = [self field:s[@"CompanyTagline"] ?: @"SOFTWARE MIT CHARAKTER" label:@"Extra text" y:125];
    NSTextField *hint = [NSTextField wrappingLabelWithString:@"Effects: 0–100%. Save applies changes to the next frame. Full-screen fill stretches the picture to cover the monitor."];
    hint.frame = NSMakeRect(20,85,460,30); [_options.contentView addSubview:hint];
    NSTextField *versionLabel = [NSTextField labelWithString:[NSString stringWithFormat:@"Version %@ · Mac preview build %@", version, build]];
    versionLabel.frame = NSMakeRect(20,54,460,22);
    [_options.contentView addSubview:versionLabel];
    NSButton *homepage = [NSButton buttonWithTitle:@"www.gruetze-software.de" target:self action:@selector(openHomepage:)];
    homepage.frame = NSMakeRect(15,20,175,30); homepage.bordered = NO;
    [_options.contentView addSubview:homepage];
    NSButton *powerTest = [NSButton buttonWithTitle:@"Test CRT" target:self action:@selector(testPowerEffect:)];
    powerTest.frame = NSMakeRect(195,20,85,30); [_options.contentView addSubview:powerTest];
    NSButton *save = [NSButton buttonWithTitle:@"Save" target:self action:@selector(saveOptions:)];
    save.frame = NSMakeRect(390,20,90,30); save.keyEquivalent = @"\r"; [_options.contentView addSubview:save];
    NSButton *cancel = [NSButton buttonWithTitle:@"Cancel" target:self action:@selector(cancelOptions:)];
    cancel.frame = NSMakeRect(290,20,90,30); cancel.keyEquivalent = @"\e"; [_options.contentView addSubview:cancel];
    for (NSView *control in _options.contentView.subviews) {
        NSRect frame = control.frame;
        if (frame.origin.y >= 54) { frame.origin.y += 40; control.frame = frame; }
    }
    _quality = [[NSPopUpButton alloc] initWithFrame:NSMakeRect(20,65,460,28) pullsDown:NO];
    [_quality addItemsWithTitles:@[@"Rendering: Efficient — up to 540p (recommended)",
        @"Rendering: Balanced — up to 720p", @"Rendering: Detailed — up to 1080p, higher CPU use"]];
    [_quality selectItemAtIndex:MAX(0,MIN(2,[s[@"NativeRenderQuality"] integerValue]))];
    [_options.contentView addSubview:_quality];
    return _options;
}
- (void)saveOptions:(id)sender {
    BOOL full = _fullScreen.state == NSControlStateValueOn;
    NSMutableDictionary *settings = [[self settings] mutableCopy];
    [settings addEntriesFromDictionary:@{@"Programs": @(_program.indexOfSelectedItem),
        @"ProgramDurationSeconds": @(MAX(15,MIN(1800,_duration.integerValue))),
        @"KeepAspectRatio": !full && _aspect.state == NSControlStateValueOn ? @YES : @NO,
        @"FillScreen": full || _border.state == NSControlStateValueOn ? @YES : @NO,
        @"MaskStrength": @(_phosphor.doubleValue), @"Glow": @(_glow.doubleValue),
        @"GlowSpread": @(_spread.doubleValue), @"ScanlineStrength": @(_scanlines.doubleValue),
        @"Scanlines": @YES, @"CrtEnabled": @YES,
        @"NativeRenderQuality": @(_quality.indexOfSelectedItem),
        @"PowerTransition": _power.state == NSControlStateValueOn ? @YES : @NO,
        @"CompanyName": _company.stringValue, @"CompanyWebsite": _website.stringValue,
        @"CompanyTagline": _tagline.stringValue}];
    [[self preferences] setObject:settings forKey:@"Settings"];
    [self finishOptions:NSModalResponseOK];
    // Apply settings even if the host has not started its animation timer yet.
    [self releaseSession];
    _failed = NO;
    if (!_stopped) [self animateOneFrame];
}
- (void)openHomepage:(id)sender {
    [[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:@"https://www.gruetze-software.de"]];
}
- (void)testPowerEffect:(id)sender {
    [self saveOptions:sender];
    _stopped = NO;
    if ([self ensureSession] && testPower) {
        testPower(_session);
        _previous = 0;
        [self animateOneFrame];
    }
}
- (void)finishOptions:(NSModalResponse)response {
    // self.window is the preview window, which need not be the sheet's parent.
    // NSApplication owns the ScreenSaver framework's document-modal session.
    NSWindow *sheet = _options;
    if (!sheet) return;
    [NSApp endSheet:sheet returnCode:response];
    [sheet orderOut:nil];
    _options = nil;
}
- (void)cancelOptions:(id)sender { [self finishOptions:NSModalResponseCancel]; }
@end
