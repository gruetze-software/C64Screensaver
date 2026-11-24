# Use Gruetze RetroSaver as a screensaver on Ubuntu and Linux Mint

The companion app controls playback and monitor effects. Automatic activation
is handled by your desktop or an external idle launcher. The Snap does not
install a background watcher and does not replace your password lock.

## 1. Configure and preview

After installing the Snap, open Gruetze RetroSaver from the application menu or run:

```bash
gruetze-retrosaver
```

Choose C64 or Amiga, adjust the monitor effects and click **Save settings**.
Click **Start fullscreen preview**, or run:

```bash
/snap/bin/gruetze-retrosaver.screensaver
```

Move the mouse, click or press a key to exit fullscreen.

## 2. Check your Ubuntu or Linux Mint session

Open a terminal and run:

```bash
echo "$XDG_SESSION_TYPE"
```

Continue below only if it prints **x11**. If it prints **wayland**, use a feature
supported by your compositor instead. XWayland cannot monitor inactivity across
an entire Wayland session.

## 3. Start after inactivity on X11

Install **xautolock**, an external idle launcher from the Ubuntu repositories:

```bash
sudo apt install xautolock
```

First test fullscreen using the X11 backend:

```bash
/snap/bin/gruetze-retrosaver.screensaver --x11
```

Then test automatic activation after **one minute** without keyboard or mouse input:

```bash
xautolock -time 1 -detectsleep -locker '/snap/bin/gruetze-retrosaver.screensaver --x11'
```

Leave this terminal open during the test. The animation should appear after one
idle minute. To stop the watcher, open another terminal and run:

```bash
xautolock -exit
```

For normal use, change the delay to five minutes:

```bash
xautolock -time 5 -detectsleep -locker '/snap/bin/gruetze-retrosaver.screensaver --x11'
```

If a watcher is already running with another delay, stop it with
`xautolock -exit` before starting the new command.

## 4. Enable at login

Once the test works, add a custom entry in your desktop's startup settings:

- **Linux Mint Cinnamon:** open **Startup Applications** from the application menu and add a custom command.
- **Ubuntu with an X11 session:** open **Startup Applications**. Where available, you can also launch it with `gnome-session-properties`.
- **Other desktop editions:** use the corresponding **Startup Applications**, **Session and Startup** or **Autostart** settings.

Use these values:

- Name: **Gruetze RetroSaver on idle**
- Command: `xautolock -time 5 -detectsleep -locker '/snap/bin/gruetze-retrosaver.screensaver --x11'`

Names vary between desktop editions and versions. This entry is intended for X11 sessions;
remove it if you switch to Wayland. To disable automatic activation, remove the
entry and run `xautolock -exit`.

**Keep your existing password lock enabled.** RetroSaver does not lock the
computer. Start the animation earlier than automatic blanking or locking so it
is visible before the desktop takes over. Do not disable locking to display the
animation. xautolock does not automatically honour every video player's or
presentation tool's inhibition requests.

Some Linux desktops already use XScreenSaver. RetroSaver is currently **not an embeddable
XScreenSaver module** and will not automatically appear in its saver list. This
setup launches a separate fullscreen animation before the normal lock screen.

## Other desktops

| Session | Setup |
| --- | --- |
| Linux Mint Cinnamon, MATE, Xfce or LXQt on X11 | Test the xautolock command above, then use your desktop's startup settings. |
| KDE Plasma | If available, use Power Management's idle/script action with `/snap/bin/gruetze-retrosaver.screensaver`. Options vary by Plasma version. |
| GNOME on Wayland | Manual fullscreen preview works; this app does not integrate with GNOME's native lock screen. |
| Other Wayland compositors | A compositor-specific idle launcher is needed. No universal Wayland setup is claimed here. |

## ZIP packages

Replace the Snap command with the absolute path to the extracted executable,
followed by `--fullscreen --x11`. Keep all package files together in a permanent
folder. Quote the executable path inside the locker command if it contains spaces:

```bash
xautolock -time 5 -detectsleep -locker '"/home/alex/My Apps/RetroSaver/c64screensaver" --fullscreen --x11'
```

These integration steps are based on the tools' documentation. Package builds
and rendering checks have passed; automatic activation still needs testing in
your actual Ubuntu or Linux Mint desktop session.

Sources: [Ubuntu xautolock manual](https://manpages.ubuntu.com/manpages/noble/man1/xautolock.1.html),
[XScreenSaver manual](https://manpages.ubuntu.com/manpages/noble/man1/xscreensaver.1.html),
[additional desktop and macOS setup notes](SCREEN-SAVER-SETUP.md).

Startup settings: [Ubuntu documentation](https://help.ubuntu.com/stable/ubuntu-help/startup-applications.html.en), [Linux Mint Cinnamon user guide](https://www.linuxmint.com/documentation/user-guide/Cinnamon/english_18.0.pdf).
