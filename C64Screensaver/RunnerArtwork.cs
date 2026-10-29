using System;
using SkiaSharp;
namespace C64Screensaver;

// Project-owned generated pixel artwork, converted to game-sized surfaces once.
internal static class RunnerArtwork
{
    private static readonly SKSamplingOptions Nearest = new(SKFilterMode.Nearest);
    internal static readonly Lazy<SKImage> Panorama = new(() => Resize("forest-v2", 768, 256));
    internal static readonly Lazy<SKImage[]> Sprites = new(CreateSprites);
    private static readonly Lazy<SKImage[]> Tiles = new(CreateTiles);
    internal static readonly Lazy<SKImage[]> Crystals = new(CreateCrystals);
    private static SKImage Load(string name)
    {
        using var stream = typeof(RunnerArtwork).Assembly.GetManifestResourceStream(
            "GruetzeRetroSaver.Runner." + name + ".png")
            ?? throw new InvalidOperationException("Missing runner artwork: " + name);
        using var data = SKData.Create(stream);
        return SKImage.FromEncodedData(data) ?? throw new InvalidOperationException("Invalid runner artwork.");
    }
    private static SKImage Resize(string name, int width, int height)
    {
        using var input = Load(name);
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.DrawImage(input, new SKRect(0, 0, width, height), Nearest);
        return surface.Snapshot();
    }
    private static SKImage[] CreateTiles()
    {
        using var input = Load("terrain-v2");
        var result = new SKImage[16];
        for (int i = 0; i < result.Length; i++)
        {
            using var tile = SKSurface.Create(new SKImageInfo(32, 32));
            float left = (i % 4) * input.Width / 4f, top = (i / 4) * input.Height / 4f;
            // Ignore atlas dividers; they are not part of the in-game materials.
            var source = new SKRect(left + 3, top + 3, left + input.Width / 4f - 3, top + input.Height / 4f - 3);
            tile.Canvas.DrawImage(input, source, new SKRect(0, 0, 32, 32), Nearest);
            result[i] = tile.Snapshot();
        }
        return result;
    }
    internal static SKImage Platform(int width, int height, int variation)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        using var edge = new SKPath();
        edge.MoveTo(0, 0); edge.LineTo(width, 0);
        for (int y = 8; y < height; y += 8)
            edge.LineTo(width - (y / 8 % 3), y);
        edge.LineTo(width - 2, height); edge.LineTo(2, height);
        for (int y = height - 8; y > 0; y -= 8)
            edge.LineTo(y / 8 % 3, y);
        edge.Close();
        canvas.ClipPath(edge);
        for (int y = 0; y < height; y += 32)
        for (int x = 0; x < width; x += 32)
        {
            int row = y == 0 ? 0 : 1 + (y / 32 + variation) % 3;
            int column = (x / 32 + variation * 3 + y / 32) % 4;
            canvas.DrawImage(Tiles.Value[row * 4 + column], x, y);
        }
        using var shade = SKShader.CreateLinearGradient(new SKPoint(0, 10), new SKPoint(0, height),
            new[] { SKColors.Transparent, new SKColor(9, 11, 26, 110) }, SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = shade };
        canvas.DrawRect(0, 0, width, height, paint);
        return surface.Snapshot();
    }
    private static SKImage[] CreateSprites()
    {
        using var input = Load("explorer-v2");
        using var bitmap = SKBitmap.FromImage(input);
        var bounds = new SKRectI[8];
        var pixels = bitmap.Pixels;
        int cellWidth = input.Width / 4, cellHeight = input.Height / 2;
        int maxWidth = 1, maxHeight = 1;
        for (int i = 0; i < bounds.Length; i++)
        {
            int left = (i % 4) * cellWidth, top = (i / 4) * cellHeight;
            int minX = cellWidth, minY = cellHeight, maxX = 0, maxY = 0;
            for (int y = 0; y < cellHeight; y++)
            for (int x = 0; x < cellWidth; x++)
            {
                if (pixels[(top + y) * bitmap.Width + left + x].Alpha < 170) continue;
                minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x + 1); maxY = Math.Max(maxY, y + 1);
            }
            if (maxX <= minX || maxY <= minY) throw new InvalidOperationException("Empty runner animation frame.");
            bounds[i] = new SKRectI(left + minX, top + minY, left + maxX, top + maxY);
            maxWidth = Math.Max(maxWidth, maxX - minX); maxHeight = Math.Max(maxHeight, maxY - minY);
        }
        float scale = Math.Min(38f / maxWidth, 42f / maxHeight);
        var result = new SKImage[8];
        for (int i = 0; i < result.Length; i++)
        {
            using var frame = SKSurface.Create(new SKImageInfo(40, 44));
            frame.Canvas.Clear(SKColors.Transparent);
            float w = bounds[i].Width * scale, h = bounds[i].Height * scale;
            var source = new SKRect(bounds[i].Left, bounds[i].Top, bounds[i].Right, bounds[i].Bottom);
            frame.Canvas.DrawImage(input, source, new SKRect((40 - w) / 2, 44 - h, (40 + w) / 2, 44), Nearest);
            result[i] = frame.Snapshot();
        }
        return result;
    }
    private static SKImage[] CreateCrystals()
    {
        var result = new SKImage[4];
        int[] widths = [5, 3, 1, 3];
        for (int i = 0; i < result.Length; i++)
        {
            using var surface = SKSurface.Create(new SKImageInfo(14, 16));
            surface.Canvas.Clear(SKColors.Transparent);
            var canvas = surface.Canvas;
            using var paint = new SKPaint { Color = new SKColor(31, 17, 47) };
            using var diamond = new SKPath();
            diamond.MoveTo(7, 1); diamond.LineTo(7 + widths[i], 7);
            diamond.LineTo(7, 15); diamond.LineTo(7 - widths[i], 7); diamond.Close();
            canvas.DrawPath(diamond, paint);
            using var left = new SKPath();
            left.MoveTo(7, 2); left.LineTo(7, 13); left.LineTo(8 - widths[i], 7); left.Close();
            paint.Color = new SKColor(255, 220, 98); canvas.DrawPath(left, paint);
            using var right = new SKPath();
            right.MoveTo(7, 2); right.LineTo(6 + widths[i], 7); right.LineTo(7, 13); right.Close();
            paint.Color = new SKColor(206, 120, 48); canvas.DrawPath(right, paint);
            paint.Color = new SKColor(255, 250, 209); canvas.DrawRect(6, 4, 1, 3, paint);
            result[i] = surface.Snapshot();
        }
        return result;
    }
}
