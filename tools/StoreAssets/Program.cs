using SkiaSharp;
if (args.Length != 2) throw new ArgumentException("Usage: StoreAssets input.png output-directory");
using var bitmap = SKBitmap.Decode(args[0]) ?? throw new IOException("Missing icon");
using var source = SKImage.FromBitmap(bitmap);
Directory.CreateDirectory(args[1]);
foreach (var (name, w, h) in new[] { ("StoreLogo", 50, 50), ("Square44x44Logo", 44, 44), ("Square150x150Logo", 150, 150), ("Square310x310Logo", 310, 310), ("Wide310x150Logo", 310, 150), ("StoreListingLogo", 300, 300) })
{
    using var surface = SKSurface.Create(new SKImageInfo(w, h));
    surface.Canvas.Clear(SKColors.Transparent);
    int side = Math.Min(w, h);
    surface.Canvas.DrawImage(source, SKRect.Create((w - side) / 2f, (h - side) / 2f, side, side), new SKSamplingOptions(SKCubicResampler.Mitchell));
    using var image = surface.Snapshot(); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
    using var file = File.Create(Path.Combine(args[1], name + ".png")); png.SaveTo(file);
}
Console.WriteLine("Generated Windows package and Store listing logos.");
