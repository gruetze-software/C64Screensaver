namespace C64Screensaver;

public enum MonitorPreset { Original, Glow, ClassicCrt, OldTv, Custom }

public static class MonitorPresets
{
    public static SaverSettings Create(MonitorPreset preset)
    {
        var s = new SaverSettings();
        switch (preset)
        {
            case MonitorPreset.Original: s.CrtEnabled = false; break;
            case MonitorPreset.Glow:
                s.Curvature = 0; s.Scanlines = false; s.ScanlineStrength = 0;
                s.MaskStrength = 0; s.Softness = 0; s.Glow = 65; s.GlowSpread = 50; s.Vignette = 0;
                break;
            case MonitorPreset.OldTv:
                s.Curvature = 45; s.ScanlineStrength = 55; s.MaskStrength = 25;
                s.Softness = 22; s.Glow = 45; s.GlowSpread = 50; s.Vignette = 40;
                break;
        }
        // Flicker remains opt-in even in the strongest preset.
        s.Flicker = 0;
        return s;
    }
    public static MonitorPreset Match(SaverSettings settings)
    {
        if (!settings.CrtEnabled) return MonitorPreset.Original;
        var current = CrtParameters.From(settings) with { FillScreen = false, KeepAspectRatio = true };
        foreach (var preset in new[] { MonitorPreset.Glow, MonitorPreset.ClassicCrt, MonitorPreset.OldTv })
            if (current == (CrtParameters.From(Create(preset)) with { FillScreen = false, KeepAspectRatio = true })) return preset;
        return MonitorPreset.Custom;
    }
    public static string Description(MonitorPreset preset) => preset switch
    {
        MonitorPreset.Original => "Original pixels, with all monitor effects off.",
        MonitorPreset.Glow => "Phosphor glow only: flat picture, no scanlines or mask.",
        MonitorPreset.ClassicCrt => "A clear, gently curved CRT with subtle glow and scanlines.",
        MonitorPreset.OldTv => "Strong curvature, visible scanlines and a softer phosphor glow. Flicker stays off.",
        _ => "Your own combination of monitor settings."
    };
}
