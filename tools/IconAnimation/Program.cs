using System.Diagnostics;
using System.Runtime.InteropServices;
using C64Screensaver;
using SkiaSharp;

if (args.Length != 3) throw new ArgumentException("Usage: IconAnimation icon.png output-directory glass-reflection.png");
Directory.CreateDirectory(args[1]);
const int size = 1024, fps = 24, seconds = 8;
using var icon = SKBitmap.Decode(args[0]);
using var reflectionPlate = SKBitmap.Decode(args[2]);
using var frame = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Opaque));
using var canvas = new SKCanvas(frame);
using var glass = new SKPath();
// Match the inner glass silhouette of the approved monitor, leaving the case intact.
glass.MoveTo(250, 226);
glass.CubicTo(375, 207, 672, 209, 755, 227);
glass.CubicTo(805, 234, 823, 259, 830, 311);
glass.CubicTo(843, 425, 842, 564, 824, 635);
glass.CubicTo(815, 673, 795, 686, 747, 692);
glass.CubicTo(603, 711, 377, 704, 264, 688);
glass.CubicTo(216, 682, 199, 659, 193, 619);
glass.CubicTo(179, 511, 179, 390, 192, 309);
glass.CubicTo(199, 260, 215, 235, 250, 226);
glass.Close();
var session = new ScreenSession(new Random(64));
session.Machine.ClearScreen();
session.Machine.Write("\n    **** COMMODORE 64 BASIC V2 ****\n\n 64K RAM SYSTEM  38911 BASIC BYTES FREE\n\nREADY.\n");
var framebuffer = new C64FrameBuffer();
var settings = new SaverSettings { PowerTransition = false, CycleColors = false };
using var cursorOn = framebuffer.Capture(session, settings, 0);
using var cursorOff = framebuffer.Capture(session, settings, 0.6);
var start = new ProcessStartInfo("ffmpeg") { RedirectStandardInput = true, UseShellExecute = false };
foreach (var arg in new[] { "-y", "-hide_banner", "-loglevel", "error", "-f", "rawvideo", "-pixel_format", "bgra", "-video_size", "1024x1024", "-framerate", "24", "-i", "pipe:0", "-an", "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart", Path.Combine(args[1], "RetroSaver-animated-icon.mp4") }) start.ArgumentList.Add(arg);
using var encoder = Process.Start(start) ?? throw new IOException("Cannot start ffmpeg");
var bytes = new byte[size * size * 4];
for (int i = 0; i < fps * seconds; i++)
{
    canvas.DrawBitmap(icon, new SKRect(0, 0, size, size));
    canvas.Save(); canvas.ClipPath(glass, SKClipOperation.Intersect, true);
    // A stationary C64 boot screen; only its cursor and the CRT rolling bands move.
    canvas.DrawImage(i / 12 % 2 == 0 ? cursorOn : cursorOff, new SKRect(180, 213, 840, 706), new SKSamplingOptions(SKFilterMode.Linear));
    using (var tint = new SKPaint { Color = new SKColor(5, 12, 74, 65) }) canvas.DrawPath(glass, tint);
    using (var glow = new SKPaint { BlendMode = SKBlendMode.Screen, Color = new SKColor(60, 110, 255, 26), ImageFilter = SKImageFilter.CreateBlur(2, 2) })
        canvas.DrawImage(i / 12 % 2 == 0 ? cursorOn : cursorOff, new SKRect(180, 213, 840, 706), new SKSamplingOptions(SKFilterMode.Linear), glow);
    using (var scan = new SKPaint { Color = new SKColor(0, 0, 30, 28), StrokeWidth = 1 })
        for (float y = 212; y < 735; y += 3) canvas.DrawLine(175, y, 860, y, scan);
    double phase = (double)i / (fps * seconds);
    for (int band = -1; band < 3; band++)
    {
        float y = (float)(211 + phase * 523 + band * 261.5);
        using var paint = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, y - 18), new SKPoint(0, y + 18), new[] { new SKColor(70, 135, 255, 0), new SKColor(100, 170, 255, 46), new SKColor(70, 135, 255, 0) }, new float[] { 0, .5f, 1 }, SKShaderTileMode.Clamp), BlendMode = SKBlendMode.Screen };
        canvas.DrawRect(175, y - 18, 690, 36, paint);
    }
    // Keep the glass reflection independent of C64 border and cursor animation.
    using (var reflection = new SKPaint { BlendMode = SKBlendMode.Screen, Color = new SKColor(255, 255, 255, 145) })
        canvas.DrawBitmap(reflectionPlate, new SKRect(0, 0, size, size), reflection);
    canvas.Restore(); canvas.Flush();
    if (i == 0) { using var image = SKImage.FromBitmap(frame); using var png = image.Encode(SKEncodedImageFormat.Png, 100); using var file = File.Create(Path.Combine(args[1], "preview.png")); png.SaveTo(file); }
    Marshal.Copy(frame.GetPixels(), bytes, 0, bytes.Length);
    encoder.StandardInput.BaseStream.Write(bytes);
}
encoder.StandardInput.Close(); encoder.WaitForExit();
if (encoder.ExitCode != 0) throw new IOException("Video encoding failed");
Console.WriteLine("Rendered 8-second seamless loop: 1024 × 1024, 24 fps.");
