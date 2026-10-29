using System;
using SkiaSharp;
namespace C64Screensaver;

// Small deterministic autoplayer with fixed-step gravity and platform collisions.
public sealed class AmigaRunner
{
    private const double Step = 1.0 / 60, Speed = 90, Period = 1024;
    private readonly record struct Platform(double Start, double End, double Top);
    private static readonly Platform[] Platforms =
    [
        new(0,160,198), new(192,288,178), new(320,416,198),
        new(448,560,178), new(592,704,198), new(736,848,174), new(880,1024,198)
    ];
    private static readonly Lazy<SKImage[]> Sprites = new(CreateSprites);
    private static readonly Lazy<SKImage> Sky = new(CreateSky);
    private static readonly Lazy<SKImage> Hills = new(CreateHills);
    private static readonly SKSamplingOptions Nearest = new(SKFilterMode.Nearest);
    private long ticks;
    private double velocity;
    public double WorldX { get; private set; } = 56;
    public double FootY { get; private set; } = 198;
    public bool Grounded { get; private set; } = true;
    public int Jumps { get; private set; }
    public int Crystals { get; private set; }
    public int Respawns { get; private set; }

    public AmigaRunner() { _ = Sprites.Value; _ = Sky.Value; _ = Hills.Value; }
    public static double? GroundAt(double x)
    {
        double local = (x % Period + Period) % Period;
        foreach (var platform in Platforms)
            if (local >= platform.Start && local < platform.End) return platform.Top;
        return null;
    }
    private void Reset()
    {
        ticks = 0; velocity = 0; WorldX = 56; FootY = 198;
        Grounded = true; Jumps = Crystals = Respawns = 0;
    }
    public void AdvanceTo(double seconds)
    {
        long target = (long)Math.Floor(Math.Clamp(double.IsFinite(seconds) ? seconds : 0, 0, 1800) * 60 + .000001);
        if (target < ticks) Reset();
        while (ticks < target) { AdvanceStep(); ticks++; }
    }
    private void AdvanceStep()
    {
        double oldX = WorldX;
        WorldX += Speed * Step;
        double? ground = GroundAt(WorldX);
        if (Grounded)
        {
            if (ground == null) Grounded = false;
            else
            {
                FootY = ground.Value;
                double? ahead = GroundAt(WorldX + 26);
                if (ahead == null || ahead.Value < FootY - .5)
                {
                    velocity = -190; Grounded = false; Jumps++;
                }
            }
        }
        if (!Grounded)
        {
            double before = FootY;
            velocity += 420 * Step;
            FootY += velocity * Step;
            if (ground is double y && velocity >= 0 && before <= y && FootY >= y)
            {
                FootY = y; velocity = 0; Grounded = true;
            }
        }
        if (FootY > 320)
        {
            Respawns++;
            WorldX += 40; FootY = GroundAt(WorldX) ?? 198;
            velocity = 0; Grounded = GroundAt(WorldX).HasValue;
        }
        double cycle = Math.Floor(WorldX / Period) * Period;
        foreach (var platform in Platforms)
        {
            double crystal = cycle + (platform.Start + platform.End) / 2;
            if (oldX < crystal && WorldX >= crystal && FootY >= platform.Top - 28 && FootY - 32 <= platform.Top - 20)
                Crystals++;
        }
    }
    public void Draw(SKCanvas canvas, double time, SKPaint paint)
    {
        AdvanceTo(time);
        double camera = WorldX - 96;
        paint.Color = SKColors.White;
        canvas.DrawImage(Sky.Value, 0, 0, paint);
        float parallax = (float)((camera * .22 % 256 + 256) % 256);
        for (int i = -1; i < 3; i++) canvas.DrawImage(Hills.Value, i * 256 - parallax, 66, paint);
        int firstCycle = (int)Math.Floor(camera / Period);
        for (int cycle = firstCycle; cycle <= firstCycle + 1; cycle++)
        foreach (var platform in Platforms)
        {
            float left = (float)(cycle * Period + platform.Start - camera);
            float right = (float)(cycle * Period + platform.End - camera);
            float top = (float)platform.Top;
            if (right < 0 || left > 384) continue;
            paint.Color = new SKColor(40, 34, 70);
            canvas.DrawRect(left, top, right - left, 288 - top, paint);
            paint.Color = new SKColor(64, 48, 91);
            for (float y = top + 16; y < 288; y += 16) canvas.DrawLine(left, y, right, y, paint);
            for (float x = left; x < right; x += 16) canvas.DrawLine(x, top, x, 288, paint);
            paint.Color = new SKColor(29, 115, 107);
            canvas.DrawRect(left, top, right - left, 7, paint);
            paint.Color = new SKColor(93, 223, 171);
            canvas.DrawRect(left, top, right - left, 3, paint);
            double crystalWorld = cycle * Period + (platform.Start + platform.End) / 2;
            if (crystalWorld < WorldX - 10) continue;
            float cx = (float)(crystalWorld - camera), cy = top - 24 + (float)Math.Sin(time * 4 + crystalWorld) * 2;
            paint.Color = new SKColor(94, 230, 255);
            canvas.DrawRect(cx - 2, cy - 4, 4, 8, paint);
            canvas.DrawRect(cx - 4, cy - 2, 8, 4, paint);
            paint.Color = SKColors.White;
            canvas.DrawRect(cx - 1, cy - 2, 2, 2, paint);
        }
        if (GroundAt(WorldX) is double shadowY)
        {
            paint.Color = new SKColor(0, 0, 0, 65);
            canvas.DrawOval(96, (float)shadowY + 2, 12, 2, paint);
        }
        paint.Color = SKColors.White;
        int frame = Grounded ? (int)(time * 9) % 2 : 2;
        canvas.DrawImage(Sprites.Value[frame], new SKRect(84, (float)FootY - 32, 108, (float)FootY), Nearest, paint);
    }
    private static SKImage CreateSky()
    {
        using var surface = SKSurface.Create(new SKImageInfo(384, 288));
        using var gradient = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, 225),
            new[] { new SKColor(12, 20, 53), new SKColor(76, 43, 99) }, SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = gradient };
        surface.Canvas.DrawRect(0, 0, 384, 288, paint);
        paint.Shader = null; paint.Color = new SKColor(242, 186, 130);
        surface.Canvas.DrawCircle(310, 39, 17, paint);
        paint.Color = new SKColor(242, 186, 130, 65);
        surface.Canvas.DrawRect(288, 53, 44, 2, paint);
        return surface.Snapshot();
    }
    private static SKImage CreateHills()
    {
        using var surface = SKSurface.Create(new SKImageInfo(256, 128));
        surface.Canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { Color = new SKColor(43, 34, 76) };
        using var mountain = new SKPath();
        mountain.MoveTo(0, 100); mountain.LineTo(32, 80); mountain.LineTo(64, 40);
        mountain.LineTo(88, 65); mountain.LineTo(112, 52); mountain.LineTo(160, 120);
        mountain.LineTo(200, 70); mountain.LineTo(256, 100);
        mountain.LineTo(256, 128); mountain.LineTo(0, 128); mountain.Close();
        surface.Canvas.DrawPath(mountain, paint);
        return surface.Snapshot();
    }
    private static SKImage[] CreateSprites()
    {
        string[] body =
        [
            "....RRRR....", "...RRRRRR...", "..RRRRRRRR..", "...SSKSS....",
            "...SSSSS....", "...SSSS.....", "....YY......", "..YYYYYY....",
            ".SYYYYYYS...", ".S.YYYY.S...", "...BBBBB....", "...BBBBB....",
            "...BB.BB....", "...BB.BB....", "..KK..KK....", ".KKK..KKK..."
        ];
        var result = new SKImage[3];
        using var paint = new SKPaint();
        for (int frame = 0; frame < result.Length; frame++)
        {
            using var surface = SKSurface.Create(new SKImageInfo(12, 16));
            surface.Canvas.Clear(SKColors.Transparent);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 12; x++)
            {
                int sourceX = x;
                if (y >= 12 && frame == 1) sourceX = x + (y % 2 == 0 ? 1 : -1);
                if (y >= 13 && frame == 2) sourceX = x + (x < 6 ? -1 : 1);
                char pixel = sourceX is >= 0 and < 12 ? body[y][sourceX] : '.';
                paint.Color = pixel switch
                {
                    'R' => new SKColor(53, 194, 175), 'S' => new SKColor(246, 210, 164),
                    'Y' => new SKColor(255, 179, 79), 'B' => new SKColor(91, 120, 184),
                    'K' => new SKColor(16, 22, 40), _ => SKColors.Transparent
                };
                if (pixel != '.') surface.Canvas.DrawRect(x, y, 1, 1, paint);
            }
            result[frame] = surface.Snapshot();
        }
        return result;
    }
}
