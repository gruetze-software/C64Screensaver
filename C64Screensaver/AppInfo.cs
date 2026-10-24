namespace C64Screensaver;

public static class AppInfo
{
    public const string Name = "C64Screensaver";
    public static string Version => typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    public static string DisplayName => $"{Name} {Version}";
    public static string AvaloniaVersion => typeof(Avalonia.Application).Assembly.GetName().Version?.ToString(3) ?? "unknown";
    public const string RepositoryUrl = "https://github.com/gruetze-software/C64Screensaver";
    public const string CompanyUrl = "https://www.gruetze-software.de";
    public const string KofiUrl = "https://ko-fi.com/gruetzesoftware";
}
