# Animated RetroSaver icon

Offline promotional loop; does not change the application's static icon or runtime.
Uses the approved app-icon.png and the existing C64FrameBuffer boot-screen font.
Eight seconds, 24 fps, 1024×1024 MP4. The cursor blinks every half second;
two soft horizontal CRT bands travel downwards and wrap at the screen edges.
Requires .NET 10 and ffmpeg on PATH.

From the repository root:

```sh
dotnet run --project tools/IconAnimation -c Release -- C64Screensaver/Assets/app-icon.png artifacts/icon-animation tools/IconAnimation/glass-reflection.png
ffmpeg -y -i artifacts/icon-animation/RetroSaver-animated-icon.mp4 -filter_complex "fps=20,scale=640:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=sierra2_4a:diff_mode=rectangle" -loop 0 artifacts/icon-animation/RetroSaver-animated-icon.gif
```

The C64 border is preserved directly from the 384×288 framebuffer (320×200 text area). The static glass reflection plate is composited independently of cursor and rolling bands.
