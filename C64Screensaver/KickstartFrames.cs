using System;
using SkiaSharp;

namespace C64Screensaver;

// Decode historical screen captures once. Rendering only copies an immutable frame.
internal sealed class KickstartFrames
{
    private readonly SKImage[] frames;
    private readonly int[] ends;
    private readonly int duration;
    private static readonly Lazy<KickstartFrames> Classic = new(() => new("13"));
    private static readonly Lazy<KickstartFrames> Two = new(() => new("20"));
    private static readonly Lazy<KickstartFrames> Three = new(() => new("31"));

    private KickstartFrames(string version)
    {
        using var stream = typeof(KickstartFrames).Assembly.GetManifestResourceStream(
            "GruetzeRetroSaver.Kickstart.kickstart-" + version + ".gif")
            ?? throw new InvalidOperationException("Missing Kickstart screen resource.");
        using var data = SKData.Create(stream);
        using var codec = SKCodec.Create(data) ?? throw new InvalidOperationException("Invalid Kickstart screen.");
        var info = codec.FrameInfo;
        int count = Math.Max(1, info.Length);
        frames = new SKImage[count];
        ends = new int[count];
        using var bitmap = new SKBitmap(new SKImageInfo(codec.Info.Width, codec.Info.Height,
            SKColorType.Bgra8888, SKAlphaType.Premul));
        for (int i = 0; i < count; i++)
        {
            var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels(), new SKCodecOptions(i));
            if (result != SKCodecResult.Success) throw new InvalidOperationException("Could not decode Kickstart frame.");
            frames[i] = SKImage.FromPixelCopy(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes);
            // GIF zero-delay frames use the browser's conventional 100 ms delay.
            int delay = info.Length > i ? info[i].Duration : 100;
            duration += delay > 10 ? delay : 100;
            ends[i] = duration;
        }
    }
    private static KickstartFrames For(AmigaBootStyle style) => style switch
        {
            AmigaBootStyle.Purple20 => Two.Value,
            AmigaBootStyle.Purple31 => Three.Value,
            _ => Classic.Value
        };
    internal static double PlaybackSeconds(AmigaBootStyle style) => For(style).duration / 1000.0;
    internal static SKImage At(AmigaBootStyle style, double seconds)
    {
        var animation = For(style);
        int position = (int)(Math.Max(0, seconds) * 1000 % animation.duration);
        int index = 0;
        while (index < animation.ends.Length - 1 && position >= animation.ends[index]) index++;
        return animation.frames[index]; // Borrowed; owned by this process-lifetime cache.
    }
}
