using System;
using System.IO;
using System.Text.Json;
namespace C64Screensaver;
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
