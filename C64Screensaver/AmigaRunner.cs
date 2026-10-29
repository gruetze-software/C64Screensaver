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
    private static readonly Lazy<SKImage[]> Terrain = new(CreateTerrain);
    private static readonly SKSamplingOptions Nearest = new(SKFilterMode.Nearest);
    private long ticks;
    private double velocity;
    public double WorldX { get; private set; } = 56;
    public double FootY { get; private set; } = 198;
    public bool Grounded { get; private set; } = true;
    public int Jumps { get; private set; }
    public int Crystals { get; private set; }
    public int Respawns { get; private set; }

    public AmigaRunner() { _ = RunnerArtwork.Sprites.Value; _ = RunnerArtwork.Panorama.Value; _ = Terrain.Value; _ = RunnerArtwork.Crystals.Value; }
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
        double backgroundScroll = camera * .18;
        int firstBackground = (int)Math.Floor(backgroundScroll / 768);
        for (int i = firstBackground; i <= firstBackground + 1; i++)
        {
            int saved = canvas.Save();
            canvas.Translate((float)(i * 768 - backgroundScroll), 0);
            // Mirrored joins keep the panorama continuous without a hard seam.
            if ((i & 1) != 0) { canvas.Translate(768, 0); canvas.Scale(-1, 1); }
            canvas.DrawImage(RunnerArtwork.Panorama.Value, new SKRect(0, 0, 768, 288), Nearest, paint);
            canvas.RestoreToCount(saved);
        }
        int firstCycle = (int)Math.Floor(camera / Period);
        for (int cycle = firstCycle; cycle <= firstCycle + 1; cycle++)
        for (int index = 0; index < Platforms.Length; index++)
        {
            var platform = Platforms[index];
            float left = (float)(cycle * Period + platform.Start - camera);
            float right = (float)(cycle * Period + platform.End - camera);
            float top = (float)platform.Top;
            if (right < 0 || left > 384) continue;
            paint.Color = SKColors.White;
            canvas.DrawImage(Terrain.Value[index], left, top, paint);
            double crystalWorld = cycle * Period + (platform.Start + platform.End) / 2;
            if (crystalWorld < WorldX - 10) continue;
            float cx = (float)(crystalWorld - camera), cy = top - 24 + (float)Math.Sin(time * 4 + crystalWorld) * 2;
            paint.Color = SKColors.White;
            var crystal = RunnerArtwork.Crystals.Value[(int)(time * 7 + cycle + index) & 3];
            canvas.DrawImage(crystal, cx - 7, cy - 8, paint);
        }
        if (GroundAt(WorldX) is double shadowY)
        {
            paint.Color = new SKColor(0, 0, 0, 65);
            canvas.DrawOval(96, (float)shadowY + 2, 12, 2, paint);
        }
        paint.Color = SKColors.White;
        int frame = Grounded ? (int)(time * 12) % 6 : velocity < 0 ? 6 : 7;
        canvas.DrawImage(RunnerArtwork.Sprites.Value[frame], new SKRect(76, (float)FootY - 44, 116, (float)FootY), Nearest, paint);
    }
    private static SKImage[] CreateTerrain()
    {
        var result = new SKImage[Platforms.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = RunnerArtwork.Platform((int)(Platforms[i].End - Platforms[i].Start),
                288 - (int)Platforms[i].Top, i);
        return result;
    }
}
