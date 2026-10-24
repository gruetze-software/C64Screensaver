using System;
using System.IO;
using System.Text.Json;

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
        Vignette = Percent(Vignette, 15);
        Flicker = Percent(Flicker, 0);
        ProgramDurationSeconds = Math.Clamp(ProgramDurationSeconds, 15, 1800);
        if (!Enum.IsDefined(Programs)) Programs = ProgramSelection.Alternating;
        CrtStrength = Math.Clamp(CrtStrength, MinCount, MaxCount);
        SpeedMultiplier = double.IsFinite(SpeedMultiplier)
            ? Math.Clamp(SpeedMultiplier, MinSpeed, MaxSpeed) : 1;
    }
}

public class ConfigManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public SaverSettings Settings { get; set; } = new();
    public string ConfigPath { get; }
    public string? LastError { get; private set; }

    public ConfigManager(string? configPath = null)
    {
        ConfigPath = configPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "C64Screensaver", "settings.json");
        LoadSettings();
    }

    public void LoadSettings()
    {
        LastError = null;
        Settings = new();
        try
        {
            if (File.Exists(ConfigPath))
                Settings = JsonSerializer.Deserialize<SaverSettings>(File.ReadAllText(ConfigPath)) ?? new();
            Settings.Validate();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { LastError = Tools.GetExcMsg(ex); }
    }

    public bool SaveSettings()
    {
        LastError = null;
        string? temporaryPath = null;
        try
        {
            Settings.Validate();
            string fullPath = Path.GetFullPath(ConfigPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(Settings, JsonOptions));
            File.Move(temporaryPath, fullPath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { LastError = Tools.GetExcMsg(ex); return false; }
        finally
        {
            if (temporaryPath != null)
            {
                try { File.Delete(temporaryPath); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
    }
}
