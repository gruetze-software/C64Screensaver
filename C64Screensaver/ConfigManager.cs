using System;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
namespace C64Screensaver;
public class ConfigManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public SaverSettings Settings { get; set; } = new();
    public string ConfigPath { get; }
    public string? LastError { get; private set; }

    [DllImport("libSystem.B.dylib", EntryPoint = "getuid")]
    private static extern uint GetUserId();
    public static string DefaultConfigPath => OperatingSystem.IsMacOS()
        ? Environment.GetEnvironmentVariable("GRUETZE_RETROSAVER_SETTINGS_PATH")
            ?? $"/Users/Shared/GruetzeRetroSaver-{GetUserId()}/settings.json"
        : LegacyConfigPath;
    private static string LegacyConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "C64Screensaver", "settings.json");

    public ConfigManager(string? configPath = null)
    {
        ConfigPath = configPath ?? DefaultConfigPath;
        LoadSettings();
        // Migrate the old desktop JSON only when no common settings exist.
        if (configPath == null && OperatingSystem.IsMacOS() && !File.Exists(ConfigPath)
            && File.Exists(LegacyConfigPath))
        {
            try
            {
                Settings = JsonSerializer.Deserialize<SaverSettings>(File.ReadAllText(LegacyConfigPath)) ?? new();
                Settings.Validate();
                SaveSettings();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            { LastError = Tools.GetExcMsg(ex); }
        }
    }

    public void LoadSettings()
    {
        LastError = null;
        try
        {
            if (File.Exists(ConfigPath))
            {
                var loaded = JsonSerializer.Deserialize<SaverSettings>(File.ReadAllText(ConfigPath))
                    ?? throw new JsonException("Settings must be an object.");
                loaded.Validate();
                Settings = loaded;
            }
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
            string directory = Path.GetDirectoryName(fullPath)!;
            if (OperatingSystem.IsMacOS() && fullPath == Path.GetFullPath(DefaultConfigPath))
            {
                if ((File.GetAttributes(Path.GetDirectoryName(directory)!) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Settings parent must not be a symbolic link.");
                Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Settings folder must not be a symbolic link.");
                // Also fails safely when another user pre-created our per-user folder.
                File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            else Directory.CreateDirectory(directory);
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
