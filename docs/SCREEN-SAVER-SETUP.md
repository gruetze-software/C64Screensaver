# Set up Gruetze RetroSaver on Linux and macOS

Gruetze RetroSaver renders the animation and exits fullscreen on input.
It does not monitor desktop inactivity, install a background helper, create
login autostart entries or replace the operating system's password lock.

Launch commands in this guide use an extracted ZIP package. Keep all files
together in a permanent directory. Replace example paths with your real path.
These external integrations are documented from their maintainers' manuals;
they have not been tested on native Linux desktop or macOS hardware.

## Flathub and Snap Store search

Checked on **2026-10-24**, using both store search catalogs and upstream feature
documentation. I could not verify a currently available, ready-to-configure app
from either store that both detects genuine session inactivity and launches an
arbitrary external program on Ubuntu GNOME 50.1 / Wayland.

This is a research result, not proof that no such app exists. A fixed countdown,
a macro schedule and a work-break timer are different from an inactivity trigger.

| Candidate | Store | Finding |
| --- | --- | --- |
| [CrossMacro](https://flathub.org/en/apps/io.github.alper_han.crossmacro) | Flathub | Macro recording and scheduled execution are documented; a general inactivity-to-external-command trigger was not verified. Not recommended for this setup. |
| [Time Switch](https://flathub.org/en/apps/io.github.fsobolev.TimeSwitch) | Formerly Flathub | Current page says unavailable/unmaintained. Older timer instructions are not a current install recommendation. |
| [Stretchly](https://snapcraft.io/stretchly) | Snap Store | A break reminder. Its idle detection pauses breaks when the user is away; it is not documented as an arbitrary-program launcher. |
| [no-more-idle](https://snapcraft.io/no-more-idle) | Snap Store | Prevents inactivity rather than launching a saver when idle. Opposite of the required behavior. |

Store queries included idle, inactivity, automation, xautolock and idlehook.
The last two did not yield matching Snap packages at the time checked.
No compatible GNOME 50.1 / Wayland store-based setup is claimed here.
A similarly named idle API or a program that prevents sleep is not sufficient.

## Linux with an X11 desktop: xautolock

[xautolock](https://manpages.ubuntu.com/manpages/noble/man1/xautolock.1.html)
explicitly supports running any program after inactivity. It is available from
the [Ubuntu package repository](https://packages.ubuntu.com/xautolock), rather
than being a confirmed Flathub/Snap Store recommendation.

Check the current session:

    echo "$XDG_SESSION_TYPE"

Use this route only when the result is x11. It is not a solution for the user's
GNOME 50.1 Wayland session; XWayland is not a substitute for session-wide idle.

On Ubuntu or Mint with X11:

    sudo apt install xautolock

Assuming the application was extracted to /home/alex/Applications/C64Screensaver:

    chmod +x /home/alex/Applications/C64Screensaver/c64screensaver
    xautolock -time 5 -detectsleep -locker '/home/alex/Applications/C64Screensaver/c64screensaver --fullscreen'

This starts the animation after five idle minutes. To include spaces in the
application path, quote the path inside the locker command as well:

    xautolock -time 5 -detectsleep -locker '"/home/alex/My Apps/C64Screensaver/c64screensaver" --fullscreen'

After testing, the same xautolock command can be added manually to the desktop's
Startup Applications settings. C64Screensaver does not create that entry.
To stop the xautolock watcher:

    xautolock -exit

The animation is not a lock screen. Leave the desktop's normal password lock
enabled. Choose an animation delay earlier than desktop blanking or locking;
otherwise the animation may be hidden. The basic xautolock example does not
promise that every video player's inhibition request is honored.

[xidlehook](https://github.com/jD91mZM2/xidlehook) is another X11-only option,
with switches to avoid fullscreen or audio playback. Its store availability
was not confirmed; it is not a GNOME Wayland solution.

## KDE Plasma: built-in Run Script

KDE's [Power Management documentation](https://docs.kde.org/stable_kf6/en/powerdevil/kcontrol/powerdevil/)
explicitly supports a custom script after inactivity. No separate store utility
is needed when this desktop is already installed.

1. Open System Settings → Power Management / Energy Saving.
2. Find Run Script or the corresponding custom-command setting.
3. Choose the inactivity trigger and a delay, for example five minutes.
4. Set the command to the absolute application path followed by --fullscreen.

Example command:

    /home/alex/Applications/C64Screensaver/c64screensaver --fullscreen

Names and placement depend on the Plasma version. Configure the relevant power
profile. This documents PowerDevil's feature, not a native KDE compatibility
test of this application's package. Keep the normal screen lock enabled.

## macOS: native system screensaver

Gruetze RetroSaver now has a native `.saver` embedded in its companion app.
The Mac developer DMG is built separately; public GitHub releases contain only
Windows and Linux packages. App Store distribution is planned and is not live yet.

1. Move **Gruetze RetroSaver.app** from the DMG to Applications and open it.
2. Choose C64 or Amiga, configure playback and monitor effects, then save.
3. Click **Install / update system screensaver** and complete Apple's installer.
4. Select Gruetze RetroSaver in the macOS Screen Saver settings.
5. Options opens the companion when available, with a native fallback editor.

The system handles inactivity and password locking. The app and screensaver
share settings, and the screensaver reloads saved changes. After replacing an
older native build, restart the Mac so the host releases the previous code.
Developer builds require signing/notarization for normal distribution; see the
[Mac guide](../macos/README.md) and [Developer ID guide](../macos/APPLE-DEVELOPER-DE.md).

## If an earlier 1.1 preview enabled automatic start

Before replacing that preview, turn off its Automatic start option and save.
If it is already replaced, remove only the autostart entry created by that
preview from the current user's config directory:
C64Screensaver's file was named c64screensaver-idle.desktop under autostart.
Restart the desktop session to end a helper still running from the old preview.
The current application has no --idle-daemon mode and does not create this file.
