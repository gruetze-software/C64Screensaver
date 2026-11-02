using System;
using SkiaSharp;

namespace C64Screensaver;

// Original procedural artwork inspired by the classic grey/purple Boing room.
// Rotation is an immutable, low-resolution sprite cycle, not a per-frame ray tracer.
internal static class AmigaBoing
{
    private const int Size = 112;
    private static readonly Lazy<SKImage[]> Frames = new(CreateFrames);
    private static readonly Lazy<SKImage> Room = new(CreateRoom);
    internal static void Prepare() { _ = Frames.Value; _ = Room.Value; }

    private static SKImage CreateRoom()
    {
        using var surface = SKSurface.Create(new SKImageInfo(384, 288));
        var c = surface.Canvas;
        c.Clear(new SKColor(170, 170, 170));
        using var p = new SKPaint { Color = new SKColor(126, 42, 138), IsAntialias = false, StrokeWidth = 1 };
        // Vertical wall, then a floor with a consistent, flat wall/floor junction.
        for (int x = 0; x < 384; x += 24) c.DrawLine(x, 0, x, 232, p);
        for (int y = 16; y <= 232; y += 24) c.DrawLine(0, y, 384, y, p);
        c.DrawLine(0, 232, 384, 232, p);
        for (int x = -192; x <= 576; x += 32)
        {
            float start = 192 + (x - 192) * .52f;
            c.DrawLine(start, 232, x, 288, p);
        }
        foreach (int y in new[] { 243, 261, 287 }) c.DrawLine(0, y, 384, y, p);
        return surface.Snapshot();
    }

    private static SKImage[] CreateFrames()
    {
        var result = new SKImage[32];
        double tilt = 22 * Math.PI / 180, ct = Math.Cos(tilt), st = Math.Sin(tilt);
        for (int f = 0; f < result.Length; f++)
        {
            using var bitmap = new SKBitmap(Size, Size);
            var pixels = new SKColor[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                double nx = (x + .5 - Size / 2.0) / 54, ny = (y + .5 - Size / 2.0) / 54;
                double square = nx * nx + ny * ny;
                if (square >= 1) continue;
                double nz = Math.Sqrt(1 - square);
                double ax = nx * ct + ny * st, ay = -nx * st + ny * ct;
                double longitude = Math.Atan2(ax, nz) + f * Math.PI * 2 / result.Length;
                double latitude = Math.Acos(Math.Clamp(-ay, -1, 1));
                int check = (int)Math.Floor(longitude * 6 / Math.PI) + (int)Math.Floor(latitude * 8 / Math.PI);
                // A small, stepped palette keeps the colour and lighting distinctly
                // Amiga-like instead of adding modern gloss or reflective highlights.
                double light = Math.Clamp(.48 + (-nx * .35 - ny * .45 + nz * .65) * .55, .28, 1);
                int level = Math.Clamp((int)(light * 4), 1, 4);
                bool red = (check & 1) == 0;
                byte value = (byte)(red ? 85 + level * 38 : 85 + level * 42);
                pixels[y * Size + x] = red ? new SKColor(value, (byte)(level * 4), (byte)(level * 4))
                    : new SKColor(value, value, value);
            }
            bitmap.Pixels = pixels;
            result[f] = SKImage.FromBitmap(bitmap);
        }
        return result;
    }

    internal static void Draw(SKCanvas canvas, double time, SKPaint paint)
    {
        canvas.DrawImage(Room.Value, 0, 0);
        double travel = time / 3.8 % 2;
        bool forward = travel < 1;
        float x = 78 + (float)(forward ? travel : 2 - travel) * 228;
        double bounce = time / 1.55 % 1;
        float y = 176 - (float)(4 * bounce * (1 - bounce)) * 108;
        paint.Color = new SKColor(44, 24, 62, 95);
        paint.IsAntialias = false;
        canvas.DrawCircle(x + 27, y + 21, 53, paint);
        // Contact shadow narrows as the ball rises; the room's grid stays visible.
        paint.Color = new SKColor(44, 24, 62, 45);
        float width = 25 + (y - 68) * .16f;
        canvas.DrawOval(new SKRect(x - width, 230, x + width, 238), paint);
        int frame = (int)(time * 30) % Frames.Value.Length;
        if (!forward) frame = Frames.Value.Length - 1 - frame;
        paint.Color = SKColors.White;
        canvas.DrawImage(Frames.Value[frame], x - Size / 2, y - Size / 2, paint);
    }
}
