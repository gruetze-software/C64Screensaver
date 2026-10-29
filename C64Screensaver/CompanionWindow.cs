using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Linq;
namespace C64Screensaver;
public sealed class CompanionWindow : Window
{
    private readonly ConfigManager config;
    private readonly C64Scene scene = new() { MinHeight = 280 };
    private bool running;
    private readonly Stopwatch clock = new();
    private double previous;
    private double nextFrame;
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap };
    public CompanionWindow() : this(new ConfigManager()) { }
    public CompanionWindow(ConfigManager configuration)
    {
        config = configuration;
        Title = "Gruetze RetroSaver";
        Classes.Add("dialog");
        using (var iconStream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://C64Screensaver/Assets/app-icon.ico")))
            Icon = new WindowIcon(iconStream);
        Width = 1160; Height = 860; MinWidth = 880; MinHeight = 620;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var system = new ComboBox { ItemsSource = new[] { "Commodore 64", "Amiga" }, SelectedIndex = config.Settings.AmigaMode ? 1 : 0 };
        var text = new TextBox { Text = config.Settings.ScrollerText, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MaxLength = 4096, MinHeight = 60 };
        var showScroller = new CheckBox { Name = "ShowScroller", Content = "Show scrolling text", IsChecked = config.Settings.ShowAmigaScroller, FontSize = 13 };
        var speed = new Slider { Height = 32, Minimum = 8, Maximum = 160, Value = config.Settings.ScrollerSpeed };
        var wave = new Slider { Height = 32, Minimum = 0, Maximum = 48, Value = config.Settings.ScrollerWave };
        var boot = new ComboBox { Name = "AmigaBoot", ItemsSource = new[] { "Skip startup", "Kickstart 1.3", "Kickstart 2.0", "Kickstart 3.1" }, SelectedIndex = (int)config.Settings.AmigaBoot };
        var activity = new CheckBox { Name = "DiskActivity", Content = "Disk activity reflection", IsChecked = config.Settings.AmigaDiskActivity, FontSize = 13 };
        var demo = new ComboBox { Name = "AmigaDemo", ItemsSource = new[] { "Cycle all demos", "Copper & sine scroller", "Boing ball", "Starflight & wireframe", "Plasma", "Jump & run" }, SelectedIndex = (int)config.Settings.AmigaAnimation };
        var duration = new NumericUpDown { Name = "AmigaDuration", Minimum = 15, Maximum = 1800, Value = config.Settings.AmigaDurationSeconds, Increment = 5 };
        var quality = new ComboBox { Name = "RenderQuality", ItemsSource = new[] { "Efficient", "Balanced", "Detailed" }, SelectedIndex = config.Settings.NativeRenderQuality };
        var save = new Button { Content = "Save settings" };
        save.Classes.Add("primary");
        var about = new Button { Content = "About" };
        about.Click += async (_, _) => await new AboutWindow().ShowDialog(this);
        var settings = new Button { Content = "Monitor effects & C64 settings" };
        var run = new Button { Content = "Start fullscreen preview" };
        var actions = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var button in new[] { settings, run, save, about }) { button.Margin = new Thickness(0, 0, 12, 8); actions.Children.Add(button); }
        var body = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(28) };
        var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("70,*,70") };
        var appLogo = new Image { Source = SpriteAssets.Instance.Logo, Width = 60, Height = 60 };
        var title = new TextBlock { Text = "Gruetze RetroSaver", FontSize = 30, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0) };
        var companyLogo = new Image { Source = SpriteAssets.Instance.CompanyLogo, Width = 60, Height = 60 };
        var companyInfo = new Button
        {
            Name = "CompanyInfo", Content = companyLogo, Width = 70, Height = 70,
            Padding = new Thickness(0), Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        ToolTip.SetTip(companyInfo, "Grütze-Software · Homepage, copyright & Ko-fi");
        Avalonia.Automation.AutomationProperties.SetName(companyInfo, "About Grütze-Software");
        companyInfo.Click += async (_, _) => await new CompanyInfoWindow().ShowDialog(this);
        Grid.SetColumn(title, 1); Grid.SetColumn(companyInfo, 2);
        heading.Children.Add(appLogo); heading.Children.Add(title); heading.Children.Add(companyInfo);
        var header = new StackPanel { Spacing = 8 };
        header.Children.Add(heading);
        header.Children.Add(new TextBlock { Text = "Your C64. Your Amiga. Your monitor." });
        body.Children.Add(header);
        var editor = new StackPanel { Spacing = 4 };
        void Field(string label, Control control)
        {
            editor.Children.Add(new TextBlock { Text = label, FontSize = 13 });
            control.HorizontalAlignment = HorizontalAlignment.Stretch;
            editor.Children.Add(control);
        }
        Field("System", system);
        Field("Kickstart startup", boot);
        editor.Children.Add(activity);
        Field("Amiga animation", demo);
        Field("Demo runtime · seconds", duration);
        Field("Software rendering quality", quality);
        editor.Children.Add(showScroller);
        Field("Amiga scrolling text", text);
        Field("Scrolling speed", speed);
        Field("Sine wave height", wave);
        var center = new Grid { ColumnDefinitions = new ColumnDefinitions("*,330"), Margin = new Thickness(0, 18, 0, 18) };
        center.Children.Add(new Border { Background = Brushes.Black, Child = scene, Margin = new Thickness(0, 0, 20, 0) });
        var editorScroll = new ScrollViewer { Content = editor };
        Grid.SetColumn(editorScroll, 1);
        center.Children.Add(editorScroll);
        Grid.SetRow(center, 1);
        body.Children.Add(center);
        var footer = new StackPanel { Spacing = 6 };
        footer.Children.Add(actions);
        footer.Children.Add(new TextBlock { Text = AppInfo.DisplayName, FontSize = 12 });
        footer.Children.Add(status);
        Grid.SetRow(footer, 2);
        body.Children.Add(footer);
        system.Name = "SystemSelection"; text.Name = "ScrollerText"; speed.Name = "ScrollerSpeed"; wave.Name = "ScrollerWave";
        save.Name = "SaveSettings"; run.Name = "StartPreview";
        Content = body;
        void UpdateAvailability()
        {
            showScroller.IsEnabled = boot.IsEnabled = demo.IsEnabled = duration.IsEnabled = activity.IsEnabled = config.Settings.AmigaMode;
            text.IsEnabled = speed.IsEnabled = wave.IsEnabled = config.Settings.AmigaMode && showScroller.IsChecked == true;
            activity.IsEnabled = config.Settings.AmigaMode && config.Settings.AmigaBoot != AmigaBootStyle.None;
        }
        void Restart()
        {
            config.Settings.Validate();
            UpdateAvailability();
            scene.Initialize(config.Settings, true, 640, 480, Random.Shared);
        }
        system.SelectionChanged += (_, _) => { config.Settings.AmigaMode = system.SelectedIndex == 1; Restart(); };
        boot.SelectionChanged += (_, _) => { config.Settings.AmigaBoot = (AmigaBootStyle)Math.Max(0, boot.SelectedIndex); Restart(); };
        activity.IsCheckedChanged += (_, _) => { config.Settings.AmigaDiskActivity = activity.IsChecked == true; scene.InvalidateVisual(); };
        demo.SelectionChanged += (_, _) => { config.Settings.AmigaAnimation = (AmigaDemo)Math.Max(0, demo.SelectedIndex); Restart(); };
        quality.SelectionChanged += (_, _) => { config.Settings.NativeRenderQuality = Math.Max(0, quality.SelectedIndex); scene.InvalidateVisual(); };
        duration.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(NumericUpDown.Value)) { config.Settings.AmigaDurationSeconds = (int)(duration.Value ?? 45); Restart(); } };
        showScroller.IsCheckedChanged += (_, _) => { config.Settings.ShowAmigaScroller = showScroller.IsChecked == true; UpdateAvailability(); scene.InvalidateVisual(); };
        text.TextChanged += (_, _) => { config.Settings.ScrollerText = text.Text ?? ""; config.Settings.Validate(); scene.InvalidateVisual(); };
        speed.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) config.Settings.ScrollerSpeed = speed.Value; };
        wave.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) config.Settings.ScrollerWave = wave.Value; };
        void ReadEditor()
        {
            config.Settings.AmigaMode = system.SelectedIndex == 1;
            config.Settings.AmigaDiskActivity = activity.IsChecked == true;
            config.Settings.AmigaBoot = (AmigaBootStyle)Math.Max(0, boot.SelectedIndex);
            config.Settings.AmigaAnimation = (AmigaDemo)Math.Max(0, demo.SelectedIndex);
            config.Settings.AmigaDurationSeconds = (int)(duration.Value ?? 45);
            config.Settings.NativeRenderQuality = Math.Max(0, quality.SelectedIndex);
            config.Settings.ShowAmigaScroller = showScroller.IsChecked == true;
            config.Settings.ScrollerText = text.Text ?? "";
            config.Settings.ScrollerSpeed = speed.Value;
            config.Settings.ScrollerWave = wave.Value;
            config.Settings.Validate();
        }
        save.Click += (_, _) => { ReadEditor(); status.Text = config.SaveSettings() ? "Settings saved." : "Could not save: " + config.LastError; };
        settings.Click += async (_, _) => { ReadEditor(); await new SettingsWindow(config).ShowDialog(this); Restart(); };
        var previews = new System.Collections.Generic.HashSet<Window>();
        run.Click += (_, _) =>
        {
            ReadEditor();
            var preview = new MainWindow(false, IntPtr.Zero, settings: System.Text.Json.JsonSerializer.Deserialize<SaverSettings>(System.Text.Json.JsonSerializer.Serialize(config.Settings)));
            previews.Add(preview);
            preview.Closed += (_, _) => previews.Remove(preview);
            preview.Show();
        };
        void Animate(TimeSpan timestamp)
        {
            if (!running) return;
            double now = clock.Elapsed.TotalSeconds;
            if (now + .000001 >= nextFrame)
            {
                nextFrame = now - nextFrame > 1.0 / 60 ? now + 1.0 / 60 : nextFrame + 1.0 / 60;
                double elapsed = Math.Clamp(now - previous, 0, .1);
                previous = now;
                // Modal editors and fullscreen previews get the rendering budget.
                if (WindowState != WindowState.Minimized && IsVisible && previews.Count == 0 && !OwnedWindows.Any(w => w.IsVisible))
                    scene.Advance(640, 480, elapsed);
            }
            RequestAnimationFrame(Animate);
        }
        Restart();
        Opened += (_, _) => { running = true; clock.Start(); RequestAnimationFrame(Animate); };
        Closed += (_, _) => { running = false; clock.Stop(); scene.Clear(); foreach (var preview in previews.ToArray()) preview.Close(); };
    }
}
