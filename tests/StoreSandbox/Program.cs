using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using C64Screensaver;

if (!OperatingSystem.IsMacOS()) throw new Exception("Run the signed sandbox probe on macOS.");
return AppBuilder.Configure<ProbeApp>().UsePlatformDetect().WithInterFont().StartWithClassicDesktopLifetime([]);

public sealed class ProbeApp : App
{
    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
        var desktop = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        var window = (CompanionWindow)desktop.MainWindow!;
        window.Opened += (_, _) => {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (_, _) => {
                timer.Stop();
                int code = 0;
                try {
                    if (!window.IsVisible || !window.GetVisualDescendants().OfType<Button>().Any(b => b.Name == "GrantSettingsFolder"))
                        throw new Exception("Store companion did not start with its folder connection control.");
                    var config = new ConfigManager();
                    if (config.SaveSettings() || config.LastError != "Choose the shared settings folder first.")
                        throw new Exception("Saving without user consent was not blocked.");
                    // Check OS enforcement independently of ConfigManager's application guard.
                    string forbidden = "/Users/Shared/retrosaver-sandbox-probe-" + Guid.NewGuid().ToString("N");
                    bool denied = false;
                    try { File.WriteAllText(forbidden, "probe"); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { denied = true; }
                    if (!denied) { File.Delete(forbidden); throw new Exception("OS sandbox did not deny a write outside the container."); }
                    Console.WriteLine("PASS: actual Store companion starts, consent control exists, saving is blocked before permission, OS denies external writes.");
                } catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex); code = 1; }
                desktop.Shutdown(code);
            };
            timer.Start();
        };
    }
}
