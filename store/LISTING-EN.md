# Store listing — English

Name: Gruetze RetroSaver
Short description: C64 BASIC and Amiga animations with adjustable CRT glow.
Price: Free on Windows and Linux. The macOS edition is distributed separately through the Mac App Store.
Category: Personalization / Graphics (choose the nearest category offered by the store).
Keywords: C64, Commodore, Amiga, retro, screensaver, BASIC, CRT, plasma, scroller
Website / support: https://www.gruetze-software.de
Issue tracker: https://github.com/gruetze-software/C64Screensaver/issues
Privacy policy: https://github.com/gruetze-software/C64Screensaver/blob/main/PRIVACY.md

## Description

Bring classic home-computer atmosphere to your desktop with Gruetze RetroSaver.

Watch an invisible typist enter C64 BASIC programs, correct mistakes and type RUN. Choose an endless maze, a personalised company card or a twinkling starfield. Switch to Amiga-inspired animation: a woodland Jump & run with layered backgrounds, colourful Plasma or a red-and-white bouncing checker ball. Write your own scrolling message, adjust its speed and wave movement, or hide it.

Make the picture your own with adjustable phosphor glow, scanlines, curvature, softness and picture proportions. Preview everything in the companion app or start a fullscreen animation. No separate .NET installation, account or subscription is required. Settings stay on your device; the app contains no advertising or analytics.

Windows: use “Install / update system screensaver” in the Microsoft Store companion app, then choose your timeout in Windows Screen Saver Settings. Fullscreen preview is also available.

Linux Snap: launch the companion app with `gruetze-retrosaver` or the animation with `gruetze-retrosaver.screensaver`. Automatic activation depends on the desktop and compositor; this snap does not replace the lock screen. GNOME on Wayland does not support third-party system screensavers. Read the setup guide before choosing an idle launcher.

An independent project by Grütze-Software. Not affiliated with Commodore or Amiga. Historical artwork, where included, retains its original rights and credits.

## Microsoft certification notes

No sign-in, server connection, purchase or subscription is required. This is a full-trust .NET/Avalonia desktop graphics application, not a UWP application. runFullTrust is needed for the desktop application and the native Windows screensaver export. No administrator privileges are requested.

1. Start Gruetze RetroSaver from Start. Switch between C64 and Amiga and change monitor effects.
2. Start fullscreen preview. Move the mouse, click or press a key to exit.
3. Click Install / update system screensaver. The app exports its complete self-contained Windows payload to a new per-user folder under %LOCALAPPDATA%\GruetzeRetroSaver\ScreenSavers, then opens the Windows screen saver installer.
4. Select the saver, click Preview and exit it. Settings opens the companion/editor. Choose a timeout and click Apply to activate it.
5. The user-selected screensaver export remains after MSIX uninstall, so Windows never points at a removed package version. To remove it, choose another saver first, close running previews, then delete the exported folder. Updates require clicking Install / update again to select the new export. This is described in the setup guide.

The app does not silently change screen lock, password or idle timeout settings.
