namespace C64Screensaver;

public static class AppInfo
{
    public const string Name = "Gruetze RetroSaver";
    public static string Version => System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(typeof(AppInfo).Assembly)?.InformationalVersion.Split('+')[0] ?? "1.2.3";
    public static string DisplayName => $"{Name} {Version}";
    public static string AvaloniaVersion => typeof(Avalonia.Application).Assembly.GetName().Version?.ToString(3) ?? "unknown";
    public const string RepositoryUrl = "https://github.com/gruetze-software/C64Screensaver";
    public const string CompanyUrl = "https://www.gruetze-software.de";
    public const string KofiUrl = "https://ko-fi.com/gruetzesoftware";
}
