#import <ScreenSaver/ScreenSaver.h>
#import <QuartzCore/QuartzCore.h>
#import <Metal/Metal.h>
#import <dlfcn.h>
#import <stdint.h>
#import <unistd.h>
#import <sys/stat.h>

typedef int (*VersionFn)(void);
typedef int (*LoadFn)(const uint8_t *, int);
typedef uint64_t (*CreateFn)(const uint8_t *, int, int);
typedef int (*RenderFn)(uint64_t, uint8_t *, int, int, int, int, double);
typedef void (*DestroyFn)(uint64_t);
static CreateFn createSession;
static RenderFn renderSession;
static DestroyFn destroySession;
static int (*testPower)(uint64_t);
static int (*renderBackend)(uint64_t);
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
        renderBackend = (int (*)(uint64_t))dlsym(module, "c64_render_backend");
        int (*setMetal)(void *, void *) = (int (*)(void *, void *))dlsym(module, "c64_set_metal");
        // These Objective-C objects outlive every NativeAOT session in this process.
        static id<MTLDevice> device;
        static id<MTLCommandQueue> queue;
        if (ready && setMetal) {
            device = MTLCreateSystemDefaultDevice();
            queue = [device newCommandQueue];
            if (device && queue) setMetal((__bridge void *)device, (__bridge void *)queue);
        }
    });
    return ready;
}

@interface C64SettingsBackground : NSView
@end
@implementation C64SettingsBackground
- (void)drawRect:(NSRect)dirty { [NSColor.windowBackgroundColor setFill]; NSRectFill(dirty); }
@end

@interface C64HomepageButton : NSButton
@end
@implementation C64HomepageButton
- (void)resetCursorRects { [self addCursorRect:self.bounds cursor:NSCursor.pointingHandCursor]; }
@end

@interface C64SaverView : ScreenSaverView {
    uint64_t _session;
    CFTimeInterval _previous, _nextFrame;
    NSInteger _backend;
    NSTimer *_previewTimer;
    dispatch_queue_t _previewQueue;
    NSUInteger _renderGeneration;
    BOOL _renderInFlight;
    NSInteger _renderQuality;
    NSImage *_image;
    BOOL _failed, _stopped;
    NSDictionary *_lastSettings, *_sessionSettings;
    CFTimeInterval _settingsChecked;
    NSTextField *_companionStatus;
    BOOL _companionLaunching;
    NSWindow *_options;
    NSView *_optionsBody;
    NSTabView *_settingsTabs;
    NSArray<NSButton *> *_settingsTabButtons;
    NSPopUpButton *_program, *_quality, *_borderColors, *_system, *_amigaBoot, *_amigaDemo;
    NSButton *_amigaScroller, *_amigaActivity;
    NSTextField *_amigaText, *_amigaDuration, *_amigaSpeed, *_amigaWave;
    NSButton *_aspect, *_border, *_fullScreen, *_power;
    NSSlider *_phosphor, *_glow, *_spread, *_scanlines;
    NSTextField *_company, *_website, *_tagline, *_duration;
}
@end

@implementation C64SaverView
- (instancetype)initWithFrame:(NSRect)frame isPreview:(BOOL)preview {
    self = [super initWithFrame:frame isPreview:preview];
    if (self) {
        self.animationTimeInterval = 1.0 / 60.0;
        _previewQueue = dispatch_queue_create("de.gruetze-software.C64Screensaver.preview",
            dispatch_queue_attr_make_with_qos_class(DISPATCH_QUEUE_SERIAL, preview ? QOS_CLASS_UTILITY : QOS_CLASS_USER_INITIATED, 0));
    }
    return self;
}
- (BOOL)isOpaque { return YES; }
- (BOOL)isFlipped { return YES; }
- (NSUserDefaults *)preferences {
    NSString *fixture = NSProcessInfo.processInfo.environment[@"GRUETZE_RETROSAVER_SETTINGS_PATH"];
    NSString *suite = fixture ? [NSString stringWithFormat:@"de.gruetze-software.RetroSaver.Tests.%lu", (unsigned long)fixture.hash]
        : @"de.gruetze-software.C64Screensaver.Native";
    return [[NSUserDefaults alloc] initWithSuiteName:suite];
}
- (NSString *)sharedSettingsPath {
    NSString *override = NSProcessInfo.processInfo.environment[@"GRUETZE_RETROSAVER_SETTINGS_PATH"];
    return override ?: [NSString stringWithFormat:@"/Users/Shared/GruetzeRetroSaver-%u/settings.json", getuid()];
}
- (NSDictionary *)normalizedSettings:(id)value {
    if (![value isKindOfClass:NSDictionary.class]) return nil;
    NSMutableDictionary *settings = [value mutableCopy];
    if (settings[@"KeepAspectRatio"] == NSNull.null) [settings removeObjectForKey:@"KeepAspectRatio"];
    for (NSString *key in @[@"KeepAspectRatio", @"FillScreen", @"CrtEnabled", @"Scanlines", @"CycleColors",
            @"PowerTransition", @"AmigaMode", @"ShowAmigaScroller", @"AmigaDiskActivity", @"ShowFps"])
        if (settings[key]) {
            if (![settings[key] isKindOfClass:NSNumber.class]) return nil;
            settings[key] = [settings[key] boolValue] ? @YES : @NO;
        }
    for (NSString *key in @[@"CompanyName", @"CompanyWebsite", @"CompanyTagline", @"ScrollerText"])
        if (settings[key] && ![settings[key] isKindOfClass:NSString.class]) return nil;
    for (NSString *key in @[@"Programs", @"ProgramDurationSeconds", @"NativeRenderQuality", @"AmigaBoot",
            @"AmigaAnimation", @"AmigaDurationSeconds", @"ScrollerSpeed", @"ScrollerWave", @"CrtStrength",
            @"Curvature", @"ScanlineStrength", @"MaskStrength", @"Softness", @"Glow", @"GlowSpread",
            @"Vignette", @"Flicker", @"SpeedMultiplier"])
        if (settings[key] && (![settings[key] isKindOfClass:NSNumber.class]
            || CFGetTypeID((__bridge CFTypeRef)settings[key]) == CFBooleanGetTypeID())) return nil;
    for (NSString *key in @[@"Programs", @"ProgramDurationSeconds", @"NativeRenderQuality", @"AmigaBoot",
            @"AmigaAnimation", @"AmigaDurationSeconds", @"CrtStrength"])
        if (settings[key] && ([settings[key] doubleValue] != [settings[key] intValue])) return nil;
    return settings;
}
- (BOOL)saveSharedSettings:(NSDictionary *)settings error:(NSError **)error {
    NSString *path = [self sharedSettingsPath];
    NSString *folder = path.stringByDeletingLastPathComponent;
    if (![NSFileManager.defaultManager createDirectoryAtPath:folder withIntermediateDirectories:YES
            attributes:@{NSFilePosixPermissions:@0700} error:error]) return NO;
    struct stat info;
    if (lstat(folder.fileSystemRepresentation, &info) || !S_ISDIR(info.st_mode) || info.st_uid != getuid()) {
        if (error) *error = [NSError errorWithDomain:@"GruetzeRetroSaver" code:1
            userInfo:@{NSLocalizedDescriptionKey:@"The settings folder is not owned by this user."}];
        return NO;
    }
    NSData *json = [NSJSONSerialization dataWithJSONObject:settings options:NSJSONWritingPrettyPrinted error:error];
    if (!json || ![json writeToFile:path options:NSDataWritingAtomic error:error]) return NO;
    _lastSettings = [settings copy];
    [[self preferences] setObject:settings forKey:@"Settings"];
    return YES;
}
- (NSDictionary *)settings {
    NSString *path = [self sharedSettingsPath];
    struct stat info;
    BOOL safeFolder = !lstat(path.stringByDeletingLastPathComponent.fileSystemRepresentation, &info)
        && S_ISDIR(info.st_mode) && info.st_uid == getuid();
    NSData *data = safeFolder ? [NSData dataWithContentsOfFile:path] : nil;
    NSDictionary *shared = data.length <= 1024*1024 && data.length
        ? [self normalizedSettings:[NSJSONSerialization JSONObjectWithData:data options:0 error:nil]] : nil;
    if (shared) {
        _lastSettings = shared;
        return shared;
    }
    if (_lastSettings) return _lastSettings;
    NSDictionary *legacy = [self normalizedSettings:[[self preferences] dictionaryForKey:@"Settings"]];
    _lastSettings = legacy ?: @{@"Programs": @0, @"ProgramDurationSeconds": @100, @"KeepAspectRatio": @YES,
        @"CompanyName": @"GRUETZE-SOFTWARE", @"CompanyWebsite": @"WWW.GRUETZE-SOFTWARE.DE",
        @"CompanyTagline": @"SOFTWARE MIT CHARAKTER"};
    // Existing native preferences seed the common file only if none exists.
    if (!data && legacy) [self saveSharedSettings:legacy error:nil];
    return _lastSettings;
}
- (void)releaseSession {
    // Queued results from an earlier start/stop cycle must never replace a new frame.
    _renderGeneration++;
    _renderInFlight = NO;
    uint64_t retired = _session;
    _session = 0;
    if (retired && destroySession) {
        // Destroy behind outstanding renders, never wait on a managed render lock
        // from AppKit's thread (including detach, stop and configure callbacks).
        dispatch_async(_previewQueue, ^{ destroySession(retired); });
    }
    _image = nil; _previous = _nextFrame = 0; _backend = -1;
}
- (BOOL)ensureSession {
    if (_session) return YES;
    if (_failed) return NO;
    _failed = !loadCore([NSBundle bundleForClass:self.class]);
    if (!_failed) {
        NSDictionary *settings = [self settings];
        _sessionSettings = settings;
        self.animationTimeInterval = [settings[@"AmigaMode"] boolValue] ? 1.0 / 60.0 : 1.0 / 30.0;
        _renderQuality = MAX(0, MIN(2, [settings[@"NativeRenderQuality"] integerValue]));
        NSData *json = [NSJSONSerialization dataWithJSONObject:settings options:0 error:nil];
        _session = createSession(json.bytes, (int)json.length, (int)arc4random());
        _failed = !_session;
    }
    if (_failed) NSLog(@"C64Screensaver: unable to create rendering session");
    return _session != 0;
}
- (void)ensurePreviewTimer {
    if (!self.isPreview || !self.window || _stopped || _previewTimer) return;
    // Some System Settings hosts render a single frame but do not keep calling
    // animateOneFrame after options close. Own a preview-only clock as fallback.
    __weak C64SaverView *weakSelf = self;
    _previewTimer = [NSTimer timerWithTimeInterval:1.0 / 30.0 repeats:YES block:^(NSTimer *timer) {
        C64SaverView *view = weakSelf;
        if (!view || !view.window || view->_stopped) {
            [timer invalidate];
            return;
        }
        // Remote System Settings previews may use an off-screen host window.
        // Window.isVisible is not a reliable indication of preview visibility.
        if (view.isHiddenOrHasHiddenAncestor) return;
        [view animateOneFrame];
    }];
    _previewTimer.tolerance = .003;
    [[NSRunLoop mainRunLoop] addTimer:_previewTimer forMode:NSRunLoopCommonModes];
}
- (void)stopPreviewTimer {
    [_previewTimer invalidate];
    _previewTimer = nil;
}
- (void)startAnimation {
    [super startAnimation];
    _stopped = NO;
    _failed = NO;
    [self ensurePreviewTimer];
    // A host can attach/reparent a preview after startAnimation. Do not discard
    // a valid session merely because start is called again.
    [self animateOneFrame];
}
- (void)stopAnimation {
    [super stopAnimation];
    _stopped = YES;
    [self stopPreviewTimer];
    [self releaseSession];
}
- (void)viewDidMoveToWindow {
    [super viewDidMoveToWindow];
    if (!self.window) {
        [self stopPreviewTimer];
        [self releaseSession];
    } else {
        // Reparenting does not necessarily produce another startAnimation call.
        _stopped = NO;
        _previous = 0;
        [self ensurePreviewTimer];
        [self animateOneFrame];
    }
}
- (void)dealloc { [self stopPreviewTimer]; [self releaseSession]; }
- (void)animateOneFrame {
    if (_renderInFlight || _stopped || self.bounds.size.width <= 0 || self.bounds.size.height <= 0) return;
    CFTimeInterval now = CACurrentMediaTime();
    if (now - _settingsChecked >= 1) {
        _settingsChecked = now;
        if (_session && ![[self settings] isEqualToDictionary:_sessionSettings]) {
            [self releaseSession];
            _failed = NO;
        }
    }
    if (![self ensureSession]) return;
    // AppKit may request multiple draws/start callbacks within one timer tick.
    // Retain the last immutable frame rather than recomputing identical images.
    if (_image && _previous && now + self.animationTimeInterval * .1 < _nextFrame) return;
    _nextFrame = now - _nextFrame > self.animationTimeInterval
        ? now + self.animationTimeInterval : _nextFrame + self.animationTimeInterval;
    CGFloat scale = self.window.backingScaleFactor ?: 1;
    double w = self.bounds.size.width * scale, h = self.bounds.size.height * scale;
    const double maxWidth[] = {960, 1280, 1920};
    const double maxHeight[] = {540, 720, 1080};
    double limit = fmin(1, fmin(maxWidth[_renderQuality] / w, maxHeight[_renderQuality] / h));
    int width = MAX(1, (int)(w * limit)), height = MAX(1, (int)(h * limit));
    {
        // All views render off AppKit, including fullscreen. The worker
        // owns its buffer; stopping the view cannot free memory beneath a render.
        double delta = _previous ? fmin(.25, now - _previous) : 0;
        _previous = now;
        NSUInteger generation = _renderGeneration;
        uint64_t session = _session;
        _renderInFlight = YES;
        __weak C64SaverView *weakSelf = self;
        dispatch_async(_previewQueue, ^{
            int stride = width * 4, bytes = stride * height;
            uint8_t *pixels = malloc(bytes);
            CGImageRef frame = NULL;
            if (pixels && renderSession(session, pixels, bytes, width, height, stride, delta)) {
                // Transfer ownership to CoreGraphics instead of copying the frame.
                CFDataRef data = CFDataCreateWithBytesNoCopy(kCFAllocatorDefault, pixels, bytes, kCFAllocatorMalloc);
                CGDataProviderRef provider = CGDataProviderCreateWithCFData(data);
                CGColorSpaceRef colors = CGColorSpaceCreateDeviceRGB();
                frame = CGImageCreate(width, height, 8, 32, stride, colors,
                    kCGBitmapByteOrder32Little | kCGImageAlphaPremultipliedFirst, provider, NULL, false, kCGRenderingIntentDefault);
                CGColorSpaceRelease(colors);
                CGDataProviderRelease(provider);
                CFRelease(data);
            } else free(pixels);
            int backend = renderBackend ? renderBackend(session) : 0;
            dispatch_async(dispatch_get_main_queue(), ^{
                C64SaverView *view = weakSelf;
                if (view && generation == view->_renderGeneration) {
                    view->_renderInFlight = NO;
                    if (!view->_stopped && frame) {
                        view->_image = [[NSImage alloc] initWithCGImage:frame size:view.bounds.size];
                        if (view->_backend != backend) {
                            view->_backend = backend;
                            NSLog(@"Gruetze RetroSaver: %@ rendering", backend ? @"Metal GPU" : @"CPU fallback");
                        }
                        [view setNeedsDisplay:YES];
                    }
                }
                if (frame) CGImageRelease(frame);
            });
        });
        return;
    }
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
    caption.frame = NSMakeRect(20, y, 130, 24); [_optionsBody addSubview:caption];
    NSTextField *field = [[NSTextField alloc] initWithFrame:NSMakeRect(160, y, 365, 24)];
    field.stringValue = value; [_optionsBody addSubview:field]; return field;
}
- (NSButton *)check:(NSString *)label value:(BOOL)value y:(CGFloat)y {
    NSButton *button = [NSButton checkboxWithTitle:label target:nil action:nil];
    button.frame = NSMakeRect(20, y, 510, 24); button.state = value ? NSControlStateValueOn : NSControlStateValueOff;
    [_optionsBody addSubview:button]; return button;
}
- (NSURL *)companionApplicationURL {
    return [NSWorkspace.sharedWorkspace URLForApplicationWithBundleIdentifier:@"de.gruetze-software.RetroSaver"];
}
- (void)openCompanionURL:(NSURL *)url completion:(void (^)(NSError *))completion {
    NSWorkspaceOpenConfiguration *configuration = [NSWorkspaceOpenConfiguration configuration];
    configuration.activates = YES;
    [NSWorkspace.sharedWorkspace openApplicationAtURL:url configuration:configuration
        completionHandler:^(NSRunningApplication *app, NSError *error) {
            dispatch_async(dispatch_get_main_queue(), ^{
                completion(error ?: (app ? nil : [NSError errorWithDomain:@"GruetzeRetroSaver" code:1 userInfo:nil]));
            });
        }];
}
- (void)launchCompanion:(id)sender {
    if (_companionLaunching) return;
    NSURL *url = [self companionApplicationURL];
    if (!url) { _companionStatus.stringValue = @"Move Gruetze RetroSaver.app to Applications, then try again."; return; }
    _companionLaunching = YES;
    NSWindow *sheet = _options;
    __weak C64SaverView *weakSelf = self;
    [self openCompanionURL:url completion:^(NSError *error) {
        C64SaverView *view = weakSelf;
        if (!view) return;
        view->_companionLaunching = NO;
        if (view->_options != sheet) return;
        if (error) { view->_companionStatus.stringValue = error.localizedDescription; return; }
        // Allow the system host to attach its sheet before ending that modal session.
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, NSEC_PER_SEC / 5), dispatch_get_main_queue(), ^{
            if (view->_options == sheet) [view finishOptions:NSModalResponseOK];
        });
    }];
}
- (NSWindow *)companionConfigureSheet {
    _options = [[NSWindow alloc] initWithContentRect:NSMakeRect(0,0,500,230)
        styleMask:NSWindowStyleMaskTitled backing:NSBackingStoreBuffered defer:NO];
    _options.releasedWhenClosed = NO;
    _options.title = @"Gruetze RetroSaver";
    _options.contentView = [[C64SettingsBackground alloc] initWithFrame:NSMakeRect(0,0,500,230)];
    _optionsBody = _options.contentView;
    [self brandImage:@"app-icon.icns" frame:NSMakeRect(22,136,64,64) tag:201];
    [self label:@"Gruetze RetroSaver" frame:NSMakeRect(105,167,370,30) font:[NSFont boldSystemFontOfSize:22]];
    [self label:@"C64 and Amiga settings" frame:NSMakeRect(105,141,370,24) font:[NSFont systemFontOfSize:14]];
    _companionStatus = [self label:@"Opening the companion app… Saved changes are applied automatically."
        frame:NSMakeRect(25,77,450,50) font:[NSFont systemFontOfSize:13]];
    NSButton *open = [NSButton buttonWithTitle:@"Open app" target:self action:@selector(launchCompanion:)];
    open.frame = NSMakeRect(345,22,130,32); open.keyEquivalent = @"\r"; [_optionsBody addSubview:open];
    NSButton *cancel = [NSButton buttonWithTitle:@"Cancel" target:self action:@selector(cancelOptions:)];
    cancel.frame = NSMakeRect(225,22,110,32); cancel.keyEquivalent = @"\e"; [_optionsBody addSubview:cancel];
    _optionsBody = nil;
    dispatch_async(dispatch_get_main_queue(), ^{ if (self->_options) [self launchCompanion:nil]; });
    return _options;
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
    if ([self companionApplicationURL]) return [self companionConfigureSheet];
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
    caption.frame = NSMakeRect(20,y,155,24); [_optionsBody addSubview:caption];
    NSSlider *slider = [NSSlider sliderWithValue:fmax(0,fmin(100,value)) minValue:0 maxValue:100
        target:self action:@selector(sliderChanged:)];
    slider.frame = NSMakeRect(180,y,270,24); slider.tag = tag;
    slider.continuous = YES; [_optionsBody addSubview:slider];
    NSTextField *percent = [NSTextField labelWithString:[NSString stringWithFormat:@"%.0f%%",slider.doubleValue]];
    percent.frame = NSMakeRect(470,y,50,24); percent.tag = tag + 100;
    [_optionsBody addSubview:percent];
    return slider;
}
- (void)sliderChanged:(NSSlider *)sender {
    NSTextField *percent = (NSTextField *)[sender.superview viewWithTag:sender.tag + 100];
    percent.stringValue = [NSString stringWithFormat:@"%.0f%%",sender.doubleValue];
}
- (void)fullScreenChanged:(id)sender {
    BOOL full = _fullScreen.state == NSControlStateValueOn;
    _aspect.enabled = !full; _border.enabled = !full;
    _borderColors.enabled = !full && _border.state != NSControlStateValueOn;
}
- (NSTextField *)label:(NSString *)text frame:(NSRect)frame font:(NSFont *)font {
    NSTextField *label = [NSTextField wrappingLabelWithString:text];
    label.frame = frame; label.font = font;
    [_optionsBody addSubview:label];
    return label;
}
- (void)brandImage:(NSString *)name frame:(NSRect)frame tag:(NSInteger)tag {
    NSString *path = [[NSBundle bundleForClass:self.class].resourcePath stringByAppendingPathComponent:name];
    NSImageView *image = [[NSImageView alloc] initWithFrame:frame];
    image.image = [[NSImage alloc] initWithContentsOfFile:path];
    image.imageScaling = NSImageScaleProportionallyUpOrDown;
    image.tag = tag;
    image.accessibilityLabel = tag == 201 ? @"C64Screensaver app icon" : @"Grütze-Software logo";
    [_options.contentView addSubview:image];
}
- (NSView *)page:(NSString *)name tabs:(NSTabView *)tabs {
    NSTabViewItem *item = [[NSTabViewItem alloc] initWithIdentifier:name];
    item.label = name;
    item.view = [[NSView alloc] initWithFrame:NSMakeRect(0,0,550,390)];
    [tabs addTabViewItem:item];
    return item.view;
}
- (void)selectSettingsTab:(NSButton *)sender {
    NSInteger index = sender.tag - 501;
    if (index < 0 || index >= _settingsTabs.numberOfTabViewItems) return;
    [_settingsTabs selectTabViewItemAtIndex:index];
    for (NSButton *button in _settingsTabButtons)
        button.state = button == sender ? NSControlStateValueOn : NSControlStateValueOff;
}
- (NSWindow *)buildConfigureSheet {
    NSDictionary *s = [self settings];
    _options = [[NSPanel alloc] initWithContentRect:NSMakeRect(0,0,620,620)
        styleMask:NSWindowStyleMaskTitled backing:NSBackingStoreBuffered defer:NO];
    _options.releasedWhenClosed = NO;
    _options.contentView = [[C64SettingsBackground alloc] initWithFrame:NSMakeRect(0,0,620,620)];
    _options.title = @"Gruetze RetroSaver · Settings";
    NSBundle *bundle = [NSBundle bundleForClass:self.class];
    NSString *version = [bundle objectForInfoDictionaryKey:@"CFBundleShortVersionString"] ?: @"?";
    NSString *build = [bundle objectForInfoDictionaryKey:@"CFBundleVersion"] ?: @"?";
    _optionsBody = _options.contentView;
    [self brandImage:@"app-icon.icns" frame:NSMakeRect(22,532,70,70) tag:201];
    [self brandImage:@"logogs.png" frame:NSMakeRect(520,524,76,80) tag:202];
    [self label:@"Gruetze RetroSaver" frame:NSMakeRect(110,565,390,30) font:[NSFont boldSystemFontOfSize:24]];
    NSTextField *subtitle = [self label:@"Your C64. Your Amiga. Your monitor." frame:NSMakeRect(110,542,390,22) font:[NSFont systemFontOfSize:13]];
    subtitle.textColor = NSColor.secondaryLabelColor;
    NSTextField *versionLabel = [self label:[NSString stringWithFormat:@"Version %@ · Mac preview build %@",version,build]
        frame:NSMakeRect(110,521,390,20) font:[NSFont systemFontOfSize:11]];
    versionLabel.textColor = NSColor.secondaryLabelColor;

    NSTabView *tabs = [[NSTabView alloc] initWithFrame:NSMakeRect(20,75,580,395)];
    tabs.tabViewType = NSNoTabsNoBorder;
    _settingsTabs = tabs;
    NSMutableArray<NSButton *> *navigation = [NSMutableArray array];
    NSArray<NSString *> *titles = @[@"General",@"C64",@"Amiga",@"Monitor effects"];
    for (NSInteger index = 0; index < 4; index++) {
        NSButton *button = [NSButton buttonWithTitle:titles[index] target:self action:@selector(selectSettingsTab:)];
        button.frame = NSMakeRect(20 + index*145,477,140,30);
        button.bezelStyle = NSBezelStyleRounded;
        [button setButtonType:NSButtonTypePushOnPushOff];
        button.state = index == 0 ? NSControlStateValueOn : NSControlStateValueOff;
        button.tag = 501 + index;
        [_options.contentView addSubview:button];
        [navigation addObject:button];
    }
    _settingsTabButtons = navigation;
    tabs.identifier = @"C64SettingsTabs";
    [_options.contentView addSubview:tabs];
    _optionsBody = [self page:@"General" tabs:tabs];
    [self label:@"Active system" frame:NSMakeRect(20,342,510,25) font:[NSFont boldSystemFontOfSize:14]];
    _system = [[NSPopUpButton alloc] initWithFrame:NSMakeRect(20,300,510,30) pullsDown:NO];
    _system.tag = 301;
    [_system addItemsWithTitles:@[@"System: Commodore 64", @"System: Amiga"]];
    [_system selectItemAtIndex:[s[@"AmigaMode"] boolValue] ? 1 : 0];
    [_optionsBody addSubview:_system];
    _quality = [[NSPopUpButton alloc] initWithFrame:NSMakeRect(20,235,510,30) pullsDown:NO];
    [_quality addItemsWithTitles:@[@"Rendering: Efficient — up to 540p (recommended)",
        @"Rendering: Balanced — up to 720p", @"Rendering: Detailed — up to 1080p, higher GPU/CPU use"]];
    [_quality selectItemAtIndex:MAX(0,MIN(2,[s[@"NativeRenderQuality"] integerValue]))];
    [_optionsBody addSubview:_quality];
    _power = [self check:@"CRT power-off / dot / power-on between programs"
        value:s[@"PowerTransition"] ? [s[@"PowerTransition"] boolValue] : YES y:180];
    NSTextField *generalHint = [self label:@"Choose the active system here. C64 and Amiga have their own tabs for programs, timing and text. Monitor effects apply to both systems."
        frame:NSMakeRect(20,65,510,85) font:[NSFont systemFontOfSize:12]];
    generalHint.textColor = NSColor.secondaryLabelColor;

    _optionsBody = [self page:@"C64" tabs:tabs];
    [self label:@"BASIC programs" frame:NSMakeRect(20,342,510,25) font:[NSFont boldSystemFontOfSize:14]];
    _program = [[NSPopUpButton alloc] initWithFrame:NSMakeRect(20,300,510,30) pullsDown:NO];
    [_program addItemsWithTitles:@[@"All programs", @"Maze", @"Company card", @"Stars"]];
    [_program selectItemAtIndex:MAX(0, MIN(3, [s[@"Programs"] integerValue]))];
    [_optionsBody addSubview:_program];
    _duration = [self field:[(s[@"ProgramDurationSeconds"] ?: @100) stringValue] label:@"Seconds / program" y:255];
    _borderColors = [[NSPopUpButton alloc] initWithFrame:NSMakeRect(20,210,510,26) pullsDown:NO];
    _borderColors.tag = 204;
    [_borderColors addItemsWithTitles:@[@"C64 border: Original blue", @"C64 border: Colors from the BASIC program"]];
    [_borderColors selectItemAtIndex:(s[@"CycleColors"] ? [s[@"CycleColors"] boolValue] : YES) ? 1 : 0];
    _borderColors.toolTip = @"The boot screen keeps its original blue border. Program colors start after RUN.";
    [_optionsBody addSubview:_borderColors];
    [self label:@"Company card text" frame:NSMakeRect(20,165,510,25) font:[NSFont boldSystemFontOfSize:14]];
    _company = [self field:s[@"CompanyName"] ?: @"GRUETZE-SOFTWARE" label:@"Company" y:125];
    _website = [self field:s[@"CompanyWebsite"] ?: @"WWW.GRUETZE-SOFTWARE.DE" label:@"Website" y:85];
    _tagline = [self field:s[@"CompanyTagline"] ?: @"SOFTWARE MIT CHARAKTER" label:@"Extra text" y:45];

    _optionsBody = [self page:@"Amiga" tabs:tabs];
    _amigaBoot = [[NSPopUpButton alloc] initWithFrame:NSMakeRect(20,335,510,30) pullsDown:NO];
    _amigaBoot.tag = 302;
    [_amigaBoot addItemsWithTitles:@[@"Skip Kickstart", @"Kickstart 1.3", @"Kickstart 2.0", @"Kickstart 3.1"]];
    [_amigaBoot selectItemAtIndex:MAX(0,MIN(3,s[@"AmigaBoot"] ? [s[@"AmigaBoot"] integerValue] : 1))];
    [_optionsBody addSubview:_amigaBoot];
    _amigaDemo = [[NSPopUpButton alloc] initWithFrame:NSMakeRect(20,290,510,30) pullsDown:NO];
    _amigaDemo.tag = 303;
    [_amigaDemo addItemsWithTitles:@[@"Alternate Jump & run / Plasma", @"Jump & run", @"Plasma"]];
    NSInteger selectedDemo = [s[@"AmigaAnimation"] integerValue];
    [_amigaDemo selectItemAtIndex:selectedDemo == 5 ? 1 : selectedDemo == 4 ? 2 : 0];
    [_optionsBody addSubview:_amigaDemo];
    _amigaDuration = [self field:[(s[@"AmigaDurationSeconds"] ?: @45) stringValue] label:@"Seconds / demo" y:245];
    _amigaScroller = [self check:@"Show scrolling text" value:s[@"ShowAmigaScroller"] ? [s[@"ShowAmigaScroller"] boolValue] : YES y:205];
    _amigaScroller.tag = 304;
    _amigaActivity = [self check:@"Disk activity reflection" value:s[@"AmigaDiskActivity"] ? [s[@"AmigaDiskActivity"] boolValue] : YES y:172];
    _amigaActivity.tag = 305;
    _amigaText = [self field:s[@"ScrollerText"] ?: @"GRUETZE RETROSAVER - WWW.GRUETZE-SOFTWARE.DE" label:@"Scrolling text" y:122];
    _amigaText.tag = 306;
    _amigaSpeed = [self field:[(s[@"ScrollerSpeed"] ?: @48) stringValue] label:@"Scroll speed" y:72];
    _amigaWave = [self field:[(s[@"ScrollerWave"] ?: @20) stringValue] label:@"Wave height" y:22];

    _optionsBody = [self page:@"Monitor effects" tabs:tabs];
    BOOL aspect = s[@"KeepAspectRatio"] ? [s[@"KeepAspectRatio"] boolValue] : ![s[@"FillScreen"] boolValue];
    BOOL border = [s[@"FillScreen"] boolValue];
    _fullScreen = [self check:@"Fill entire screen — no black bars or border" value:!aspect && border y:335];
    _fullScreen.target = self; _fullScreen.action = @selector(fullScreenChanged:);
    _aspect = [self check:@"Original 4:3 proportions (with black bars)" value:aspect y:300];
    _border = [self check:@"Borderless picture" value:border y:265];
    _border.target = self; _border.action = @selector(fullScreenChanged:);
    [self fullScreenChanged:nil];
    _phosphor = [self slider:@"Phosphor mask" value:[s[@"MaskStrength"] ?: @8 doubleValue] y:190 tag:1];
    _glow = [self slider:@"Phosphor glow" value:[s[@"Glow"] ?: @12 doubleValue] y:145 tag:2];
    _spread = [self slider:@"Glow spread" value:[s[@"GlowSpread"] ?: @30 doubleValue] y:100 tag:3];
    _scanlines = [self slider:@"Scanline strength" value:[s[@"ScanlineStrength"] ?: @22 doubleValue] y:55 tag:4];
    NSTextField *monitorHint = [self label:@"Fine-tune the tube from a clear picture to a softly glowing CRT. All effects range from 0 to 100%."
        frame:NSMakeRect(20,5,510,35) font:[NSFont systemFontOfSize:12]];
    monitorHint.textColor = NSColor.secondaryLabelColor;

    C64HomepageButton *homepage = [[C64HomepageButton alloc] initWithFrame:NSMakeRect(20,20,235,30)];
    homepage.title = @"www.gruetze-software.de";
    homepage.target = self; homepage.action = @selector(openHomepage:); homepage.bordered = NO;
    homepage.alignment = NSTextAlignmentLeft;
    homepage.attributedTitle = [[NSAttributedString alloc] initWithString:homepage.title attributes:@{
        NSForegroundColorAttributeName: NSColor.linkColor,
        NSUnderlineStyleAttributeName: @(NSUnderlineStyleSingle),
        NSFontAttributeName: [NSFont systemFontOfSize:12]}];
    homepage.toolTip = @"Open https://www.gruetze-software.de in your browser";
    [_options.contentView addSubview:homepage];
    NSButton *powerTest = [NSButton buttonWithTitle:@"Test CRT" target:self action:@selector(testPowerEffect:)];
    powerTest.frame = NSMakeRect(295,20,90,30); [_options.contentView addSubview:powerTest];
    NSButton *cancel = [NSButton buttonWithTitle:@"Cancel" target:self action:@selector(cancelOptions:)];
    cancel.frame = NSMakeRect(395,20,90,30); cancel.keyEquivalent = @"\e"; [_options.contentView addSubview:cancel];
    NSButton *save = [NSButton buttonWithTitle:@"Save" target:self action:@selector(saveOptions:)];
    save.frame = NSMakeRect(495,20,105,30); save.keyEquivalent = @"\r"; [_options.contentView addSubview:save];
    _optionsBody = nil;
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
        @"NativeRenderQuality": @(_quality.indexOfSelectedItem),
        @"CycleColors": _borderColors.indexOfSelectedItem == 1 ? @YES : @NO,
        @"PowerTransition": _power.state == NSControlStateValueOn ? @YES : @NO,
        @"CompanyName": _company.stringValue, @"CompanyWebsite": _website.stringValue,
        @"CompanyTagline": _tagline.stringValue}];
    [settings addEntriesFromDictionary:@{
        @"AmigaMode": _system.indexOfSelectedItem == 1 ? @YES : @NO,
        @"AmigaBoot": @(_amigaBoot.indexOfSelectedItem),
        @"AmigaAnimation": @(_amigaDemo.indexOfSelectedItem == 1 ? 5 : _amigaDemo.indexOfSelectedItem == 2 ? 4 : 0),
        @"AmigaDurationSeconds": @(MAX(15,MIN(1800,_amigaDuration.integerValue))),
        @"ShowAmigaScroller": _amigaScroller.state == NSControlStateValueOn ? @YES : @NO,
        @"AmigaDiskActivity": _amigaActivity.state == NSControlStateValueOn ? @YES : @NO,
        @"ScrollerText": [_amigaText.stringValue substringToIndex:MIN(4096,_amigaText.stringValue.length)],
        @"ScrollerSpeed": @(MAX(8,MIN(160,_amigaSpeed.doubleValue))),
        @"ScrollerWave": @(MAX(0,MIN(48,_amigaWave.doubleValue)))}];
    NSError *saveError = nil;
    if (![self saveSharedSettings:settings error:&saveError]) {
        NSAlert *alert = [[NSAlert alloc] init];
        alert.messageText = @"Settings could not be saved";
        alert.informativeText = saveError.localizedDescription ?: @"Open the companion app and save your settings there.";
        [alert beginSheetModalForWindow:_options completionHandler:nil];
        return;
    }
    [self finishOptions:NSModalResponseOK];
    // Apply settings even if the host has not started its animation timer yet.
    [self releaseSession];
    _failed = NO;
    if (self.isPreview && self.window) _stopped = NO;
    [self ensurePreviewTimer];
    if (!_stopped) [self animateOneFrame];
}
- (void)openHomepageURL:(NSURL *)url completion:(void (^)(NSError *))completion {
    NSWorkspaceOpenConfiguration *configuration = [NSWorkspaceOpenConfiguration configuration];
    configuration.activates = YES;
    [[NSWorkspace sharedWorkspace] openURL:url configuration:configuration completionHandler:^(NSRunningApplication *app, NSError *error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            completion(error ?: (app ? nil : [NSError errorWithDomain:@"C64Screensaver" code:1 userInfo:nil]));
        });
    }];
}
- (void)openHomepage:(id)sender {
    NSURL *url = [NSURL URLWithString:@"https://www.gruetze-software.de"];
    __weak C64SaverView *weakSelf = self;
    [self openHomepageURL:url completion:^(NSError *error) {
        C64SaverView *view = weakSelf;
        if (!error || !view) return;
        NSLog(@"C64Screensaver: browser launch failed: %@",error);
        NSAlert *alert = [[NSAlert alloc] init];
        alert.messageText = @"The website could not be opened";
        alert.informativeText = @"macOS did not open the browser from the screensaver settings. You can copy the address and open it in your browser.";
        [alert addButtonWithTitle:@"Copy address"];
        [alert addButtonWithTitle:@"Cancel"];
        NSWindow *parent = view->_options;
        if (parent) [alert beginSheetModalForWindow:parent completionHandler:^(NSModalResponse response) {
            if (response == NSAlertFirstButtonReturn) {
                [NSPasteboard.generalPasteboard clearContents];
                [NSPasteboard.generalPasteboard setString:url.absoluteString forType:NSPasteboardTypeString];
            }
        }];
    }];
}
- (void)testPowerEffect:(id)sender {
    [self saveOptions:sender];
    _stopped = NO;
    if ([self ensureSession] && testPower) {
        uint64_t session = _session;
        NSUInteger generation = _renderGeneration;
        __weak C64SaverView *weakSelf = self;
        dispatch_async(_previewQueue, ^{
            testPower(session);
            dispatch_async(dispatch_get_main_queue(), ^{
                C64SaverView *view = weakSelf;
                if (!view || generation != view->_renderGeneration || view->_stopped) return;
                view->_previous = view->_nextFrame = 0;
                [view ensurePreviewTimer];
                [view animateOneFrame];
            });
        });
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
