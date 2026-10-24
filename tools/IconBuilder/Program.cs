using System.Buffers.Binary;
using System.Text;
using SkiaSharp;

if (args.Length != 2) throw new ArgumentException("Usage: IconBuilder source.png output-directory");
using var bitmap = SKBitmap.Decode(args[0]) ?? throw new IOException("Cannot read source icon");
using var source = SKImage.FromBitmap(bitmap);
int left = bitmap.Width, top = bitmap.Height, right = 0, bottom = 0;
for (int y = 0; y < bitmap.Height; y++)
    for (int x = 0; x < bitmap.Width; x++)
        if (bitmap.GetPixel(x, y).Alpha > 8)
        { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x + 1); bottom = Math.Max(bottom, y + 1); }
if (right <= left || bottom <= top) throw new InvalidDataException("Source icon is empty");
var content = new SKRect(left, top, right, bottom);
byte[] Png(int size)
{
    using var surface = SKSurface.Create(new SKImageInfo(size, size));
    surface.Canvas.Clear(SKColors.Transparent);
    float scale = size * 0.9f / Math.Max(content.Width, content.Height);
    float w = content.Width * scale, h = content.Height * scale;
    var destination = SKRect.Create((size - w) / 2, (size - h) / 2, w, h);
    surface.Canvas.DrawImage(source, content, destination, new SKSamplingOptions(SKCubicResampler.Mitchell));
    using var icon = surface.Snapshot();
    using var data = icon.Encode(SKEncodedImageFormat.Png, 100);
    return data.ToArray();
}
Directory.CreateDirectory(args[1]);
File.WriteAllBytes(Path.Combine(args[1], "app-icon.png"), Png(1024));

var windowsSizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
var windowsImages = windowsSizes.Select(Png).ToArray();
using (var output = new BinaryWriter(File.Create(Path.Combine(args[1], "app-icon.ico"))))
{
    output.Write((ushort)0); output.Write((ushort)1); output.Write((ushort)windowsSizes.Length);
    int offset = 6 + windowsSizes.Length * 16;
    for (int i = 0; i < windowsSizes.Length; i++)
    {
        byte dimension = windowsSizes[i] == 256 ? (byte)0 : (byte)windowsSizes[i];
        output.Write(dimension); output.Write(dimension); output.Write((byte)0); output.Write((byte)0);
        output.Write((ushort)1); output.Write((ushort)32);
        output.Write(windowsImages[i].Length); output.Write(offset);
        offset += windowsImages[i].Length;
    }
    foreach (var data in windowsImages) output.Write(data);
}
var macImages = new[] { ("icp4",16), ("icp5",32), ("icp6",64), ("ic07",128),
    ("ic08",256), ("ic09",512), ("ic10",1024) }.Select(pair => (pair.Item1, Data:Png(pair.Item2))).ToArray();
using (var output = File.Create(Path.Combine(args[1], "app-icon.icns")))
{
    void BigInt(int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value); output.Write(bytes);
    }
    output.Write(Encoding.ASCII.GetBytes("icns"));
    BigInt(8 + macImages.Sum(item => 8 + item.Data.Length));
    foreach (var item in macImages)
    {
        output.Write(Encoding.ASCII.GetBytes(item.Item1));
        BigInt(8 + item.Data.Length); output.Write(item.Data);
    }
}
Console.WriteLine("Generated transparent C64 product icon: PNG, seven-size ICO, seven-size ICNS.");
