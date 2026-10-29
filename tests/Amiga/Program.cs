using System.Diagnostics;
using C64Screensaver;
using SkiaSharp;

void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
var output = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "amiga-review");
Directory.CreateDirectory(output);
var settings = new SaverSettings { AmigaMode = true, AmigaBoot = AmigaBootStyle.None, PowerTransition = false };
using var renderer = new AmigaFrameBuffer();
void Save(SKImage image, string name)
{
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    using var file = File.Create(Path.Combine(output, name + ".png"));
    data.SaveTo(file);
}
int Difference(SKImage a, SKImage b)
{
    using var left = SKBitmap.FromImage(a);
    using var right = SKBitmap.FromImage(b);
    return left.Pixels.Zip(right.Pixels).Count(p => p.First != p.Second);
}
foreach (var boot in new[] { AmigaBootStyle.Classic13, AmigaBootStyle.Purple20, AmigaBootStyle.Purple31 })
{
    settings.AmigaBoot = boot;
    using var first = renderer.CaptureFrame(settings, 1);
    using var loading = renderer.CaptureFrame(settings, 4.6);
    Check(Difference(first, loading) > 500, "Startup disk moves and loads: " + boot);
    Save(first, "boot-" + boot);
    using var demo = renderer.CaptureFrame(settings, 6);
    Check(Difference(first, demo) > 10000, "Startup proceeds to demo: " + boot);
}
settings.AmigaBoot = AmigaBootStyle.None;
foreach (var effect in new[] { AmigaDemo.Copper, AmigaDemo.Boing, AmigaDemo.Starflight, AmigaDemo.Plasma })
{
    settings.AmigaAnimation = effect;
    using var before = renderer.CaptureFrame(settings, 2);
    using var after = renderer.CaptureFrame(settings, 3);
    Check(Difference(before, after) > 300, "Effect animates: " + effect);
    Save(after, "demo-" + effect);
    string originalText = settings.ScrollerText;
    settings.ScrollerText = "";
    using var noText = renderer.CaptureFrame(settings, 3);
    Check(Difference(after, noText) > 100, "Scrolling text remains visible: " + effect);
    settings.ScrollerText = originalText;
    long allocation = GC.GetAllocatedBytesForCurrentThread();
    var clock = Stopwatch.StartNew();
    for (int frame = 0; frame < 180; frame++)
    {
        using var image = renderer.CaptureFrame(settings, 2 + frame / 60.0);
    }
    clock.Stop();
    Console.WriteLine($"{effect}: {clock.Elapsed.TotalMilliseconds / 180:F3} ms/frame; managed {(GC.GetAllocatedBytesForCurrentThread() - allocation) / 180} bytes/frame");
}
settings.AmigaAnimation = AmigaDemo.Cycle;
settings.AmigaBoot = AmigaBootStyle.Classic13;
settings.PowerTransition = true;
settings.AmigaDurationSeconds = 15;
for (int i = 0; i < 8; i++)
    Check(AmigaFrameBuffer.CurrentDemo(settings, i * 21.42 + .001) == (AmigaDemo)(1 + i % 4), "Playlist cycles all effects");
Check(AmigaFrameBuffer.PowerTime(settings, 20.5) is > .49 and < .51, "CRT transition occurs after runtime");
Check(AmigaFrameBuffer.PowerTime(settings, 21.43) == null, "CRT transition finishes on next boot");
settings.AmigaAnimation = AmigaDemo.Plasma;
Check(AmigaFrameBuffer.CurrentDemo(settings, 250) == AmigaDemo.Plasma, "Individual effect repeats without changing selection");
settings.AmigaAnimation = (AmigaDemo)999; settings.AmigaBoot = (AmigaBootStyle)999;
settings.AmigaDurationSeconds = 0; settings.NativeRenderQuality = 99;
settings.Validate();
Check(settings.AmigaAnimation == AmigaDemo.Cycle && settings.AmigaBoot == AmigaBootStyle.Classic13 &&
    settings.AmigaDurationSeconds == 15 && settings.NativeRenderQuality == 2, "Validate persisted Amiga options");
using (var first = renderer.CaptureFrame(settings, 1))
using (var original = SKBitmap.FromImage(first))
{
    using var other = renderer.CaptureFrame(settings, 4);
    using var retained = SKBitmap.FromImage(first);
    Check(original.Pixels.SequenceEqual(retained.Pixels), "Snapshots stay immutable while source surface is reused");
}
renderer.Dispose();
bool rejected = false;
try { using var invalid = renderer.CaptureFrame(settings, 0); } catch (ObjectDisposedException) { rejected = true; }
Check(rejected, "Disposed renderer cannot draw");
Console.WriteLine("PASS: three startup motifs, four animated effects, scrolling text in every effect, playlist, transitions, settings validation and snapshot lifetime.");
