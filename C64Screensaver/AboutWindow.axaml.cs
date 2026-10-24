using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace C64Screensaver;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        ProductName.Text = AppInfo.DisplayName;
        FrameworkInfo.Text = $"C# · .NET {Environment.Version.Major} · Avalonia {AppInfo.AvaloniaVersion}\nWindows · Linux · macOS";
        ProductLogo.Source = SpriteAssets.Instance.Logo;
        CompanyLogo.Source = SpriteAssets.Instance.CompanyLogo;
    }

    private void CloseDialog(object? sender, RoutedEventArgs e) => Close();

    private async void OpenLink(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string target } ||
            (target != AppInfo.RepositoryUrl && target != AppInfo.CompanyUrl && target != AppInfo.KofiUrl)) return;
        try
        {
            LinkStatus.Text = await Launcher.LaunchUriAsync(new Uri(target))
                ? "" : $"Could not open your browser. Link: {target}";
        }
        catch (Exception)
        {
            LinkStatus.Text = $"Could not open your browser. Link: {target}";
        }
    }
}
