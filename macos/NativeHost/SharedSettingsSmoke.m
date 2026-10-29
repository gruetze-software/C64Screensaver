#import <ScreenSaver/ScreenSaver.h>
#import <unistd.h>

static BOOL runManaged(NSString *dotnet, NSString *tool, NSString *operation) {
    NSTask *task = [[NSTask alloc] init];
    task.executableURL = [NSURL fileURLWithPath:dotnet];
    task.arguments = @[tool, operation];
    NSError *error = nil;
    if (![task launchAndReturnError:&error]) { NSLog(@"%@", error); return NO; }
    [task waitUntilExit];
    return task.terminationStatus == 0;
}
static NSView *find(NSView *root, NSInteger tag) {
    if (root.tag == tag) return root;
    for (NSView *child in root.subviews) { NSView *found = find(child, tag); if (found) return found; }
    if ([root isKindOfClass:NSTabView.class])
        for (NSTabViewItem *item in ((NSTabView *)root).tabViewItems) {
            NSView *found = find(item.view,tag); if (found) return found;
        }
    return nil;
}
static NSButton *saveButton(NSView *root) {
    if ([root isKindOfClass:NSButton.class] && [((NSButton *)root).title isEqualToString:@"Save"])
        return (NSButton *)root;
    for (NSView *child in root.subviews) { NSButton *button = saveButton(child); if (button) return button; }
    return nil;
}
int main(int argc, char **argv) {
    @autoreleasepool {
        if (argc != 4) return 2;
        NSString *folder = [NSTemporaryDirectory() stringByAppendingPathComponent:NSUUID.UUID.UUIDString];
        NSString *path = [folder stringByAppendingPathComponent:@"settings.json"];
        setenv("GRUETZE_RETROSAVER_SETTINGS_PATH", path.fileSystemRepresentation, 1);
        NSString *dotnet = @(argv[2]), *tool = @(argv[3]);
        if (!runManaged(dotnet,tool,@"write")) return 3;
        [NSApplication sharedApplication];
        NSBundle *bundle = [NSBundle bundleWithPath:@(argv[1])];
        NSError *error = nil;
        if (![bundle loadAndReturnError:&error]) { NSLog(@"%@",error); return 4; }
        ScreenSaverView *view = [[bundle.principalClass alloc] initWithFrame:NSMakeRect(0,0,384,288) isPreview:YES];
        NSDictionary *settings = [view valueForKey:@"settings"];
        if (![settings[@"AmigaMode"] boolValue] || [settings[@"Glow"] doubleValue] != 37
            || [settings[@"Scanlines"] boolValue] || [settings[@"AmigaAnimation"] integerValue] != 5) return 5;
        NSWindow *sheet = view.configureSheet;
        if (!sheet || ((NSPopUpButton *)find(sheet.contentView,301)).indexOfSelectedItem != 1) return 6;
        [(NSPopUpButton *)find(sheet.contentView,301) selectItemAtIndex:0];
        ((NSSlider *)find(sheet.contentView,2)).doubleValue = 73;
        ((NSTextField *)find(sheet.contentView,306)).stringValue = @"SYSTEM TO COMPANION";
        NSButton *save = saveButton(sheet.contentView);
        if (!save) return 7;
        [save performClick:nil];
        if (!runManaged(dotnet,tool,@"read")) return 8;
        // Another process updates the file while an actual native preview runs.
        NSWindow *window = [[NSWindow alloc] initWithContentRect:NSMakeRect(0,0,384,288)
            styleMask:NSWindowStyleMaskTitled backing:NSBackingStoreBuffered defer:NO];
        window.releasedWhenClosed = NO;
        [window.contentView addSubview:view];
        [view startAnimation];
        [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:.3]];
        if (!runManaged(dotnet,tool,@"write")) return 9;
        NSDate *deadline = [NSDate dateWithTimeIntervalSinceNow:5];
        while (![[view valueForKey:@"sessionSettings"][@"AmigaMode"] boolValue] && deadline.timeIntervalSinceNow > 0)
            [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:.02]];
        if (![[view valueForKey:@"sessionSettings"][@"AmigaMode"] boolValue]) return 10;
        // A torn/invalid save must retain the last good settings and animated image.
        [@"{broken" writeToFile:path atomically:YES encoding:NSUTF8StringEncoding error:nil];
        [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:1.2]];
        if (![[view valueForKey:@"settings"][@"AmigaMode"] boolValue] || ![view valueForKey:@"image"]) return 11;
        [@"{\"Glow\":\"invalid\"}" writeToFile:path atomically:YES encoding:NSUTF8StringEncoding error:nil];
        if ([[view valueForKey:@"settings"][@"Glow"] doubleValue] != 37) return 12;
        for (NSString *invalid in @[@"{\"Glow\":true}", @"{\"AmigaMode\":null}", @"{\"Programs\":1.5}"]) {
            [invalid writeToFile:path atomically:YES encoding:NSUTF8StringEncoding error:nil];
            if ([[view valueForKey:@"settings"][@"Glow"] doubleValue] != 37) return 13;
        }
        [view stopAnimation]; [view removeFromSuperview];
        [NSFileManager.defaultManager removeItemAtPath:folder error:nil];
        NSLog(@"PASS: cross-process companion/native settings, native save, nullable aspect, live reload and corrupt-file fallback.");
    }
    return 0;
}
