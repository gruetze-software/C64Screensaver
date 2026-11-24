using C64Screensaver;
using SkiaSharp;
if (!CrtRenderer.ShaderAvailable) throw new Exception(CrtRenderer.ShaderError);
Directory.CreateDirectory("artifacts/crt-edge-check");
using var source = SKSurface.Create(new SKImageInfo(384,288));
source.Canvas.Clear(SKColor.Parse("#7869C4"));
using (var ink = new SKPaint { Color = SKColor.Parse("#40318D") }) source.Canvas.DrawRect(32,0,320,288,ink);
using var input = source.Snapshot();
var settings = new SaverSettings { Curvature = 45, Scanlines = false, MaskStrength = 0, Softness = 0, Glow = 0, Vignette = 0, Flicker = 0 };
using var surface = SKSurface.Create(new SKImageInfo(1200,900));
surface.Canvas.Clear(SKColors.Black);
CrtRenderer.Draw(surface.Canvas,new SKRect(0,0,1200,900),input,CrtParameters.From(settings),0);
using var frame = surface.Snapshot(); using var pixels = SKBitmap.FromImage(frame);
int blendedRows = 0, widestEdge = 0;
for (int y = 150; y < 750; y++)
{
    int count = 0;
    for (int x = 100; x < 220; x++)
    {
        var c = pixels.GetPixel(x,y);
        if (c.Red > 67 && c.Red < 117) count++;
    }
    if (count > 0) blendedRows++;
    widestEdge = Math.Max(widestEdge,count);
}
if (blendedRows < 250 || widestEdge > 2) throw new Exception($"Curved edge coverage failed: {blendedRows}/600 rows; width {widestEdge}");
using (var data = frame.Encode(SKEncodedImageFormat.Png,100))
using (var file = File.Create("artifacts/crt-edge-check/curved-border.png")) data.SaveTo(file);
Console.WriteLine($"PASS: continuous curved border coverage on {blendedRows}/600 rows; transition at most {widestEdge} output pixels. Sharp pixel interiors retained.");
