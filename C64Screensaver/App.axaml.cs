using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace C64Screensaver;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var options = Program.Options;
            desktop.Exit += (_, _) => SpriteAssets.Release();
            if (options.Mode == LaunchMode.Settings) desktop.MainWindow = new SettingsWindow();
            else if (options.Mode == LaunchMode.About) desktop.MainWindow = new AboutWindow();
            else if (options.Mode == LaunchMode.Preview)
                desktop.MainWindow = new MainWindow(true, new IntPtr(options.ParentHandle));
            else
            {
                var settings = new ConfigManager().Settings;
                var main = new MainWindow(false, IntPtr.Zero, options.Mode == LaunchMode.Windowed, settings);
                desktop.MainWindow = main;
                if (options.Mode == LaunchMode.Fullscreen)
                {
                    var screens = main.Screens.All.ToArray();
                    var primary = screens.FirstOrDefault(s => s.IsPrimary) ?? screens.FirstOrDefault();
                    if (primary != null) main.Position = primary.Bounds.Position;
                    // Native Wayland does not allow arbitrary window positioning.
                    bool nativeWayland = OperatingSystem.IsLinux() &&
                        (options.Backend == LinuxBackend.Wayland || (options.Backend == LinuxBackend.Auto &&
                        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))));
                    bool shuttingDown = false;
                    void CloseAll(object? sender, EventArgs e)
                    {
                        if (shuttingDown) return;
                        shuttingDown = true;
                        desktop.Shutdown();
                    }
                    main.Closed += CloseAll;
                    main.Opened += (_, _) =>
                    {
                        if (nativeWayland) return;
                        foreach (var screen in screens.Where(s => !Equals(s, primary)))
                        {
                            var extra = new MainWindow(false, IntPtr.Zero, settings: settings)
                            {
                                WindowStartupLocation = WindowStartupLocation.Manual,
                                Position = screen.Bounds.Position
                            };
                            extra.Closed += CloseAll;
                            extra.Show();
                        }
                        main.Activate();
                    };
                }
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
