#import <ScreenSaver/ScreenSaver.h>

// Loads the real bundle, creates its options independently of any preview window,
// presents it as a sheet, exercises Cancel and Save, then restores user defaults.
int main(int argc, char **argv) {
    @autoreleasepool {
        if (argc != 2) return 2;
        [NSApplication sharedApplication];
        NSBundle *bundle = [NSBundle bundleWithPath:[NSString stringWithUTF8String:argv[1]]];
        NSError *error = nil;
        if (![bundle loadAndReturnError:&error]) { NSLog(@"%@", error); return 3; }
        ScreenSaverView *view = [[bundle.principalClass alloc] initWithFrame:NSMakeRect(0,0,384,288) isPreview:YES];
        if (!view.hasConfigureSheet) return 4;
        NSWindow *sheet = view.configureSheet;
        if (!sheet || sheet != view.configureSheet || sheet.contentView.subviews.count < 10) return 5;
        if (view.window != nil) return 6; // Reproduce configuration without preview parent.
        NSWindow *parent = [[NSWindow alloc] initWithContentRect:NSMakeRect(0,0,640,480)
            styleMask:NSWindowStyleMaskTitled backing:NSBackingStoreBuffered defer:NO];
        parent.releasedWhenClosed = NO;
        [parent orderFront:nil];
        [parent beginSheet:sheet completionHandler:nil];
        [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:.1]];
        if (sheet.sheetParent != parent || !sheet.isVisible) return 7;
        NSButton *cancel = nil, *save = nil;
        for (NSView *control in sheet.contentView.subviews)
            if ([control isKindOfClass:NSButton.class]) {
                NSButton *button = (NSButton *)control;
                if ([button.title isEqualToString:@"Cancel"]) cancel = button;
            }
        if (!cancel) return 8;
        [cancel performClick:nil];
        [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:.3]];
        if (sheet.isVisible || sheet.sheetParent) return 9;

        NSUserDefaults *defaults = [[NSUserDefaults alloc] initWithSuiteName:@"de.gruetze-software.C64Screensaver.Native"];
        id original = [defaults objectForKey:@"Settings"];
        sheet = view.configureSheet;
        if (!sheet) return 10;
        [parent beginSheet:sheet completionHandler:nil];
        [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:.1]];
        for (NSView *control in sheet.contentView.subviews)
            if ([control isKindOfClass:NSButton.class] && [((NSButton *)control).title isEqualToString:@"Save"])
                save = (NSButton *)control;
        if (!save) return 11;
        [save performClick:nil];
        [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:.3]];
        BOOL passed = !sheet.isVisible && !sheet.sheetParent && [defaults dictionaryForKey:@"Settings"] != nil;
        if (original) [defaults setObject:original forKey:@"Settings"];
        else [defaults removeObjectForKey:@"Settings"];
        [parent orderOut:nil];
        if (!passed) return 12;
        NSLog(@"Options smoke: bundle load, cached sheet, presentation, Cancel, reopening and Save passed.");
    }
    return 0;
}
