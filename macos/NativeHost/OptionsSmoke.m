#import <ScreenSaver/ScreenSaver.h>

static BOOL hasPicture(ScreenSaverView *view) {
    NSImage *image = [view valueForKey:@"image"];
    if (!image) return NO;
    CGImageRef cg = [image CGImageForProposedRect:NULL context:nil hints:nil];
    if (!cg) return NO;
    CFDataRef data = CGDataProviderCopyData(CGImageGetDataProvider(cg));
    const UInt8 *bytes = CFDataGetBytePtr(data);
    BOOL colored = NO;
    for (CFIndex i = 0; i + 3 < CFDataGetLength(data); i += 4)
        if (bytes[i] || bytes[i+1] || bytes[i+2]) { colored = YES; break; }
    CFRelease(data);
    return colored;
}

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
        [parent.contentView addSubview:view];
        [view startAnimation];
        [view animateOneFrame];
        if (!hasPicture(view)) return 20;
        NSImage *beforeTimer = [view valueForKey:@"image"];
        [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:.65]];
        if ([view valueForKey:@"image"] == beforeTimer) return 28;
        [view removeFromSuperview];
        [parent.contentView addSubview:view];
        [view animateOneFrame];
        if (!hasPicture(view)) return 21;
        [view stopAnimation];
        [view startAnimation];
        if (!hasPicture(view)) return 22;
        [view stopAnimation];
        [view removeFromSuperview];
        [parent beginSheet:sheet completionHandler:nil];
        [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:.1]];
        if (sheet.sheetParent != parent || !sheet.isVisible) return 7;
        NSButton *cancel = nil, *save = nil;
        for (NSView *control in sheet.contentView.subviews)
            if ([control isKindOfClass:NSButton.class]) {
                NSButton *button = (NSButton *)control;
                if ([button.title isEqualToString:@"Cancel"]) cancel = button;
            }
        BOOL hasVersion = NO, hasHomepage = NO, hasPower = NO, hasPowerTest = NO;
        for (NSView *control in sheet.contentView.subviews) {
            if ([control isKindOfClass:NSTextField.class] &&
                [((NSTextField *)control).stringValue hasPrefix:@"Version "]) hasVersion = YES;
            if ([control isKindOfClass:NSButton.class]) {
                NSString *title = ((NSButton *)control).title;
                if ([title isEqualToString:@"www.gruetze-software.de"]) hasHomepage = YES;
                if ([title hasPrefix:@"CRT power-off"]) hasPower = YES;
                if ([title isEqualToString:@"Test CRT"]) hasPowerTest = YES;
            }
        }
        if (!hasVersion || !hasHomepage || !hasPower || !hasPowerTest) return 26;
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
        NSPopUpButton *quality = nil;
        for (NSView *control in sheet.contentView.subviews)
            if ([control isKindOfClass:NSPopUpButton.class] &&
                [[(NSPopUpButton *)control itemTitleAtIndex:0] hasPrefix:@"Rendering:"])
                quality = (NSPopUpButton *)control;
        if (!quality || quality.numberOfItems != 3) return 27;
        [quality selectItemAtIndex:1];
        for (NSInteger tag = 1; tag <= 4; tag++) {
            NSSlider *slider = (NSSlider *)[sheet.contentView viewWithTag:tag];
            if (![slider isKindOfClass:NSSlider.class]) return 23;
            slider.doubleValue = 10 * tag;
            [NSApp sendAction:slider.action to:slider.target from:slider];
            NSTextField *percent = (NSTextField *)[sheet.contentView viewWithTag:tag+100];
            if (![percent.stringValue isEqualToString:[NSString stringWithFormat:@"%ld%%", (long)(10*tag)]]) return 24;
        }
        NSButton *full = nil;
        for (NSView *control in sheet.contentView.subviews)
            if ([control isKindOfClass:NSButton.class] && [((NSButton *)control).title hasPrefix:@"Fill entire screen"])
                full = (NSButton *)control;
        if (!full) return 25;
        full.state = NSControlStateValueOff;
        [full performClick:nil];
        [save performClick:nil];
        [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:.3]];
        BOOL passed = !sheet.isVisible && !sheet.sheetParent && [defaults dictionaryForKey:@"Settings"] != nil;
        NSDictionary *saved = [defaults dictionaryForKey:@"Settings"];
        passed = passed && [saved[@"NativeRenderQuality"] integerValue] == 1 && [saved[@"MaskStrength"] doubleValue] == 10
            && [saved[@"Glow"] doubleValue] == 20 && [saved[@"GlowSpread"] doubleValue] == 30
            && [saved[@"ScanlineStrength"] doubleValue] == 40
            && ![saved[@"KeepAspectRatio"] boolValue] && [saved[@"FillScreen"] boolValue];
        NSData *serialized = [NSJSONSerialization dataWithJSONObject:saved options:0 error:nil];
        NSString *json = [[NSString alloc] initWithData:serialized encoding:NSUTF8StringEncoding];
        passed = passed && [json containsString:@"\"KeepAspectRatio\":false"]
            && [json containsString:@"\"FillScreen\":true"];
        // Simulate settings saved by build 2; creation must recover without reset.
        NSMutableDictionary *legacy = [saved mutableCopy];
        legacy[@"KeepAspectRatio"] = @0; legacy[@"FillScreen"] = @1;
        [defaults setObject:legacy forKey:@"Settings"];
        NSWindow *reopened = view.configureSheet;
        passed = passed && [((NSSlider *)[reopened.contentView viewWithTag:2]) doubleValue] == 20;
        [parent.contentView addSubview:view];
        [view startAnimation];
        view.frame = NSMakeRect(0,0,1920,1080);
        [view stopAnimation]; [view startAnimation];
        passed = passed && hasPicture(view);
        NSImage *capped = [view valueForKey:@"image"];
        CGImageRef cappedCG = [capped CGImageForProposedRect:NULL context:nil hints:nil];
        passed = passed && cappedCG && CGImageGetWidth(cappedCG) == 1280 && CGImageGetHeight(cappedCG) == 720;
        [view stopAnimation];
        [view removeFromSuperview];
        [parent beginSheet:reopened completionHandler:nil];
        NSButton *powerButton = nil;
        for (NSView *control in reopened.contentView.subviews)
            if ([control isKindOfClass:NSButton.class] && [((NSButton *)control).title isEqualToString:@"Test CRT"])
                powerButton = (NSButton *)control;
        if (!powerButton) return 29;
        [parent.contentView addSubview:view];
        [view startAnimation];
        [powerButton performClick:nil];
        NSImage *powerFirst = [view valueForKey:@"image"];
        [[NSRunLoop currentRunLoop] runUntilDate:[NSDate dateWithTimeIntervalSinceNow:1.8]];
        passed = passed && hasPicture(view) && [view valueForKey:@"image"] != powerFirst;
        [view stopAnimation];
        passed = passed && [view valueForKey:@"previewTimer"] == nil;
        [view removeFromSuperview];
        if (original) [defaults setObject:original forKey:@"Settings"];
        else [defaults removeObjectForKey:@"Settings"];
        [parent orderOut:nil];
        if (!passed) return 12;
        NSLog(@"Options smoke: nonblack frames, detach/reattach, stop/start, sheet, CRT sliders, saved values and borderless full-screen passed.");
    }
    return 0;
}
