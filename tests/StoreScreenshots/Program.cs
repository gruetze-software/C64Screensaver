using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using C64Screensaver;
using SkiaSharp;
using System.Text.Json;

if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException("Capture on macOS for Mac product media.");
const int Width = 1280, Height = 800;
string output = Path.GetFullPath("artifacts/store-screenshots");
Directory.CreateDirectory(output);
string configFile = Path.Combine(output, "isolated-settings.json");
Environment.SetEnvironmentVariable("GRUETZE_RETROSAVER_SETTINGS_PATH", configFile);
File.WriteAllText(configFile, JsonSerializer.Serialize(new SaverSettings()));
return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont()
    .StartWithClassicDesktopLifetime([], lifetime =>
    {
        lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        lifetime.Startup += (_, _) => Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                foreach (var w in lifetime.Windows.ToArray()) w.Hide();
                await Scenes();
                await Editors();
                File.WriteAllText(Path.Combine(output, "README.txt"),
                    "Gruetze RetroSaver " + AppInfo.Version + " — Mac-rendered app views\n" +
                    "1280 x 800 PNG, opaque RGB, English UI. Capture order follows filenames.\n" +
                    "Frames use the product's C64Scene and CRT renderer; editors use the actual companion/monitor views.\n" +
                    "Native Avalonia platform on macOS; offscreen view capture, not a desktop screen recording.\n" +
                    "Isolated settings only; personal settings are not reset. No marketing overlays or Kickstart artwork.\n" +
                    "Recheck these views against the final sandboxed Store build before submission.\n");
                File.Delete(configFile);
                lifetime.Shutdown(0);
            }
            catch (Exception e) { Console.Error.WriteLine(e); lifetime.Shutdown(1); }
        }, DispatcherPriority.Loaded);
    });

SaverSettings Look() => new()
{
    Curvature = 18, Glow = 65, GlowSpread = 55, ScanlineStrength = 18,
    MaskStrength = 5, Softness = 12, Vignette = 15, Flicker = 0,
    KeepAspectRatio = true, PowerTransition = false, NativeRenderQuality = 2,
    AmigaBoot = AmigaBootStyle.None, ScrollerText = "GRUETZE RETROSAVER - WELCOME TO THE AMIGA!", ScrollerWave = 10
};
void Advance(C64Scene scene, double seconds)
{
    for (int f = 0; f < (int)(seconds * 60); f++) scene.Advance(Width, Height, 1.0 / 60);
}
async Task Scenes()
{
    foreach (var (name, demo, seconds) in new[] {
        ("01-c64-basic", (AmigaDemo?)null, 32.0),
        ("02-amiga-runner", (AmigaDemo?)AmigaDemo.Runner, 8.0),
        ("03-amiga-plasma", (AmigaDemo?)AmigaDemo.Plasma, 3.0),
        ("04-amiga-boing", (AmigaDemo?)AmigaDemo.Boing, 3.0) })
    {
        var s = Look();
        s.AmigaMode = demo.HasValue;
        s.AmigaAnimation = demo ?? AmigaDemo.Cycle;
        s.Programs = ProgramSelection.Maze;
        var scene = new C64Scene { Width = Width, Height = Height };
        scene.Initialize(s, false, Width, Height, new Random(64));
        Advance(scene, seconds);
        var window = new Window { Width = Width, Height = Height, WindowDecorations = WindowDecorations.None,
            Background = Brushes.Black, Content = scene };
        window.Show();
        await Task.Delay(200);
        Save(scene, name);
        window.Close(); scene.Clear();
    }
}
async Task Editors()
{
    foreach (bool amiga in new[] { false, true })
    {
        var s = Look(); s.AmigaMode = amiga;
        s.AmigaAnimation = AmigaDemo.Boing;
        var config = new ConfigManager(configFile) { Settings = s };
        var window = new CompanionWindow(config);
        window.Show();
        await Task.Delay(250);
        var scene = window.GetVisualDescendants().OfType<C64Scene>().Single();
        scene.Initialize(s, true, Width, Height, new Random(64));
        Advance(scene, amiga ? 3 : 32);
        Save((Control)window.Content!, amiga ? "06-amiga-companion" : "05-c64-companion");
        window.Close();
    }
    var monitorConfig = new ConfigManager(configFile) { Settings = Look() };
    monitorConfig.Settings.AmigaMode = true;
    monitorConfig.Settings.AmigaAnimation = AmigaDemo.Boing;
    var monitor = new SettingsWindow(monitorConfig);
    monitor.Show();
    await Task.Delay(250);
    var preview = monitor.GetVisualDescendants().OfType<C64Scene>().Single();
    preview.Initialize(monitorConfig.Settings, true, Width, Height, new Random(64));
    Advance(preview, 3);
    Save((Control)monitor.Content!, "07-monitor-effects");
    monitor.Close();
}
void Save(Control view, string name)
{
    var owner = TopLevel.GetTopLevel(view) as Window;
    Control target = view;
    if (view is not C64Scene && owner != null)
    {
        // Render the full client area, including the content's original margin.
        // Capturing the content root alone discards its outer layout offset.
        owner.Content = null;
        target = new Border { Background = owner.Background, Child = view };
        owner.Content = target;
    }
    target.Width = Width; target.Height = Height;
    target.Measure(new Size(Width, Height)); target.Arrange(new Rect(0, 0, Width, Height));
    if (name == "07-monitor-effects")
    {
        var effectsScroll = target.GetVisualDescendants().OfType<ScrollViewer>()
            .Where(s => s.Classes.Contains("settingsScroll"))
            .OrderBy(s => s.TranslatePoint(default, target)!.Value.X).First();
        effectsScroll.ScrollToEnd();
        Dispatcher.UIThread.RunJobs();
        effectsScroll.Arrange(effectsScroll.Bounds);
    }
    using var capture = new RenderTargetBitmap(new PixelSize(Width, Height), new Vector(96, 96));
    capture.Render(target);
    using var bytes = new MemoryStream(); capture.Save(bytes, PngBitmapEncoderOptions.Default);
    using var bitmap = SKBitmap.Decode(bytes.ToArray()) ?? throw new Exception("Empty capture: " + name);
    using var surface = SKSurface.Create(new SKImageInfo(Width, Height, SKColorType.Rgb888x, SKAlphaType.Opaque));
    var background = owner?.Background is ISolidColorBrush brush
        ? new SKColor(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A) : SKColors.Black;
    surface.Canvas.Clear(background); surface.Canvas.DrawBitmap(bitmap, 0, 0);
    using var image = surface.Snapshot(); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
    string path = Path.Combine(output, name + ".png");
    using var file = File.Create(path); png.SaveTo(file);
    Console.WriteLine("Captured " + name + " 1280x800 on macOS");
}
