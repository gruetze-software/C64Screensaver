using System;



namespace C64Screensaver;

public class SaverSettings
{
    private static double Percent(double value, double fallback) => double.IsFinite(value) ? Math.Clamp(value, 0, 100) : fallback;
    public const int MinCount = 0, MaxCount = 100;
    public const double MinSpeed = 0.2, MaxSpeed = 5;
    // Kept readable for older settings files.
    public int CrtStrength { get; set; } = 45;
    public bool PowerTransition { get; set; } = true;
    public bool FillScreen { get; set; }
    // Missing in older files: retain their framed or borderless presentation.
    public bool? KeepAspectRatio { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool UsesFourThree => KeepAspectRatio ?? !FillScreen;
    public string CompanyName { get; set; } = "GRUETZE-SOFTWARE";
    public string CompanyWebsite { get; set; } = "WWW.GRUETZE-SOFTWARE.DE";
    public string CompanyTagline { get; set; } = "SOFTWARE MIT CHARAKTER";
    public bool CrtEnabled { get; set; } = true;
    public double Curvature { get; set; } = 18;
    public double ScanlineStrength { get; set; } = 22;
    public double MaskStrength { get; set; } = 8;
    public double Softness { get; set; } = 12;
    public double Glow { get; set; } = 12;
    public double GlowSpread { get; set; } = 30;
    public double Vignette { get; set; } = 15;
    public double Flicker { get; set; }
    public ProgramSelection Programs { get; set; } = ProgramSelection.AllPrograms;
    public int ProgramDurationSeconds { get; set; } = 100;
    public double SpeedMultiplier { get; set; } = 1;
    public bool ShowFps { get; set; }
    public bool Scanlines { get; set; } = true;
    public bool CycleColors { get; set; } = true;

    public void Validate()
    {
        CompanyName = BasicPrograms.DisplayText(CompanyName);
        CompanyWebsite = BasicPrograms.DisplayText(CompanyWebsite);
        CompanyTagline = BasicPrograms.DisplayText(CompanyTagline);
        Curvature = Percent(Curvature, 18);
        ScanlineStrength = Percent(ScanlineStrength, 22);
        MaskStrength = Percent(MaskStrength, 8);
        Softness = Percent(Softness, 12);
        Glow = Percent(Glow, 12);
        GlowSpread = Percent(GlowSpread, 30);
        Vignette = Percent(Vignette, 15);
        Flicker = Percent(Flicker, 0);
        ProgramDurationSeconds = Math.Clamp(ProgramDurationSeconds, 15, 1800);
        if (!Enum.IsDefined(Programs)) Programs = ProgramSelection.Alternating;
        CrtStrength = Math.Clamp(CrtStrength, MinCount, MaxCount);
        SpeedMultiplier = double.IsFinite(SpeedMultiplier)
            ? Math.Clamp(SpeedMultiplier, MinSpeed, MaxSpeed) : 1;
    }
}

