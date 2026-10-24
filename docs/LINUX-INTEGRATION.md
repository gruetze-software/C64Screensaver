# Linux automatic activation and Snap status

## Community findings — 2026-10-24

A store-wide absence of screensavers has not been established. No evidence of a
Snap Store ban was found. The sources show desktop and sandbox barriers, not
the reasons why every individual publisher chose not to publish a Snap.

- [Snapcraft: Mutter IdleMonitor](https://forum.snapcraft.io/t/interface-for-org-gnome-mutter-idlemonitor/21466)
  documents AppArmor denials and concerns about allowing idle-monitor mutations.
  This historical discussion does not establish today's policy.
- [Current snapd desktop policy](https://github.com/canonical/snapd/blob/master/interfaces/builtin/desktop.go)
  permits Mutter GetIdletime and GNOME/freedesktop ScreenSaver GetActive.
- [screen-inhibit-control policy](https://github.com/canonical/snapd/blob/master/interfaces/builtin/screen_inhibit_control.go)
  permits setting inhibitors, but does not grant SessionManager IsInhibited.
  Adding this plug would not solve reading inhibitors.
- [GNOME discussion](https://discourse.gnome.org/t/idlemonitor-dbus-and-unprivileged-user/1120)
  explains why session activity belongs in a per-user component.
- [Debian's gnome-screensaver removal](https://bugs.debian.org/1132611)
  cites unmaintained, security-sensitive software, archived upstream in 2018.
- [retrosaver](https://github.com/c-premus/retrosaver) is an existing GNOME/Wayland
  idle helper. Its documentation illustrates animation/blanking/locking conflicts.
  C64Screensaver does not change GNOME's idle-delay or take over locking.

## Automatic activation (development version)

The Linux-only **Automatic start** tab offers an opt-in switch and a wait time
of 1–60 minutes (default five). **Check desktop support** queries the session.
Save writes an XDG autostart entry and starts the helper immediately. Disable
the switch and save to remove the entry; running helpers exit. Keep the extracted
application in a permanent folder; disable this feature before moving it.

The helper runs as the desktop user. It queries aggregate idle time, not keys,
pointer coordinates or input devices. It never changes screen-lock, power,
blanking or idle-delay settings. Set its delay shorter than desktop blanking.
The desktop remains responsible for password protection.

It starts one animation per idle period. Input, locking, blanking, inhibition,
unreadable settings or loss of a required interface stops it. Missing information
pauses activation instead of assuming an unlocked session. Starting the helper
while already idle waits for activity before arming. A dismissed or crashed
animation is not repeatedly relaunched during the same idle period.

| Desktop | Idle source | Lock/inhibition checks | Validation |
| --- | --- | --- | --- |
| GNOME X11/Wayland | Mutter GetIdletime | GNOME ScreenSaver / SessionManager | Injected tests and private Linux D-Bus fixture |
| Cinnamon X11 | XScreenSaver extension | Cinnamon ScreenSaver / SessionManager | Injected tests; native testing pending |
| MATE X11 | XScreenSaver extension | MATE ScreenSaver / SessionManager | Injected tests; native testing pending |
| Xfce X11 | XScreenSaver extension | Xfce ScreenSaver / PowerManagement | Injected tests; native testing pending |
| Other desktops / non-GNOME Wayland | Not implemented | Activation paused | Explicit unsupported result |

The Linux D-Bus fixture runs under Ubuntu WSL, not a live GNOME session.
Native desktop compatibility must still be tested. XWayland idle is never
used as whole-Wayland-session idle.

Ubuntu/Mint prerequisites for extracted ZIP packages:

    sudo apt install libglib2.0-bin libxss1
    ./c64screensaver --diagnose-idle
    ./c64screensaver --idle-daemon

The session manager and screen-lock service must be running. Diagnostic exit
codes: 0 = valid sample (possibly locked/inhibited), 3 = unavailable, 2 = non-Linux.
The helper logs availability problems; run from a terminal to inspect them.
A file lock prevents duplicate helpers for the same settings directory.

## Snap preparation — not yet Store-ready

snap/snapcraft.yaml is a strict-confinement, development-grade draft.
It uses a self-contained publish. Prepare the package with .NET 10 and PowerShell:

    pwsh scripts/prepare-snap.ps1 -Runtime linux-x64

On Ubuntu with Snapcraft, from the repository root:

    chmod +x snap/local/publish/c64screensaver snap/local/launcher/launch
    snapcraft --platform amd64

For ARM64, prepare linux-arm64 and build platform arm64 instead. Each target
requires a fresh staging folder; the build checks an architecture marker.
No arguments opens Settings; explicit arguments pass through.
Autostart follows snapd's SNAP_USER_DATA mechanism.

Known blocker: the built-in policies examined do not permit every required
inhibitor query. Under strict confinement the helper pauses on denial.
Manual/windowed/fullscreen modes do not require automatic activation.
Mint-specific lock interfaces also need policy validation.

Before Store publication:

1. Build/install on native Ubuntu and test GNOME 50.1.
2. Check the installed snapd policy and AppArmor denials.
3. Resolve read-only inhibitor access with Snapcraft maintainers; do not silently
   disable checks or switch to classic confinement.
4. Test login autostart, upgrade, removal, videos, manual lock and suspend.
5. Test advertised Mint desktops and architectures on native systems.
6. Register the name and publish edge before stable.

No Snap has been built or uploaded. The Store name and exclusivity are unverified.

References:
[Snap autostart](https://snapcraft.io/docs/reference/development/yaml-schemas/the-snap-format/),
[Snapcraft platforms](https://ubuntu.com/docs/snapcraft/latest/reference/platforms/),
[GNOME 50.1 interface](https://github.com/GNOME/mutter/blob/50.1/data/dbus-interfaces/org.gnome.Mutter.IdleMonitor.xml).

To repeat the private D-Bus integration test on Ubuntu (requires python3-gi):

    dbus-run-session -- python3 tests/Linux/dbus_probe.py /absolute/path/to/c64screensaver

The fixture also verifies real helper singleton behavior, stopping when settings
are disabled and graceful SIGTERM. It does not modify the host session settings
or show fullscreen windows.
