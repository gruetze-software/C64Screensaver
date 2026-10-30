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
    private static readonly AmigaDemo[] DemoChoices = [AmigaDemo.Cycle, AmigaDemo.Runner, AmigaDemo.Plasma];
    private readonly ConfigManager config;
    private readonly Button monitorSettingsButton;
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
        var demo = new ComboBox { Name = "AmigaDemo", ItemsSource = new[] { "Alternate Jump & run / Plasma", "Jump & run", "Plasma" }, SelectedIndex = Math.Max(0, Array.IndexOf(DemoChoices, config.Settings.AmigaAnimation)) };
        var duration = new NumericUpDown { Name = "AmigaDuration", Minimum = 15, Maximum = 1800, Value = config.Settings.AmigaDurationSeconds, Increment = 5 };
        var quality = new ComboBox { Name = "RenderQuality", ItemsSource = new[] { "Efficient", "Balanced", "Detailed" }, SelectedIndex = config.Settings.NativeRenderQuality };
        var program = new ComboBox { Name = "ProgramCombo", ItemsSource = new[] { "All BASIC programs", "Endless maze", "Company card", "Twinkling starfield" }, SelectedIndex = (int)config.Settings.Programs };
        var c64Duration = new NumericUpDown { Name = "DurationInput", Minimum = 15, Maximum = 1800, Value = config.Settings.ProgramDurationSeconds, Increment = 15 };
        var c64Speed = new Slider { Name = "SpeedSlider", Minimum = .2, Maximum = 5, Value = config.Settings.SpeedMultiplier, Height = 32 };
        var c64Colors = new CheckBox { Name = "ColorsCheckBox", Content = "Occasional border colour changes", IsChecked = config.Settings.CycleColors, FontSize = 13 };
        var company = new TextBox { Name = "CompanyNameBox", Text = config.Settings.CompanyName, MaxLength = 28 };
        var website = new TextBox { Name = "CompanyWebsiteBox", Text = config.Settings.CompanyWebsite, MaxLength = 28 };
        var tagline = new TextBox { Name = "CompanyTaglineBox", Text = config.Settings.CompanyTagline, MaxLength = 28 };
        var save = new Button { Content = "Save settings" };
        save.Classes.Add("primary");
        var about = new Button { Content = "About" };
        about.Click += async (_, _) => await new AboutWindow().ShowDialog(this);
        var settings = monitorSettingsButton = new Button { Name = "MonitorSettings", Content = "Monitor effects" };
        var run = new Button { Content = "Start fullscreen preview" };
        var actions = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var button in new[] { settings, run, save, about }) { button.Margin = new Thickness(0, 0, 12, 8); actions.Children.Add(button); }
        if (OperatingSystem.IsMacOS())
        {
            var install = new Button { Name = "InstallMacScreensaver", Content = "Install / update system screensaver" };
            install.Margin = new Thickness(0, 0, 12, 8);
            install.Click += (_, _) =>
            {
                ReadEditor();
                if (!config.SaveSettings()) { status.Text = "Could not save: " + config.LastError; return; }
                // The system installer handles installation and update confirmation.
                string saver = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory,
                    "..", "Resources", "C64Screensaver.saver"));
                if (!System.IO.Directory.Exists(saver)) { status.Text = "Use the complete Mac app from the DMG to install the screensaver."; return; }
                try
                {
                    Process.Start(new ProcessStartInfo("/usr/bin/open") { ArgumentList = { saver }, UseShellExecute = false });
                    status.Text = "Finish installation, then choose Gruetze RetroSaver in System Settings → Screen Saver.";
                }
                catch (Exception ex) { status.Text = "Could not open the system installer: " + ex.Message; }
            };
            actions.Children.Insert(0, install);
        }
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
        void Field(string label, Control control, StackPanel? target = null)
        {
            target ??= editor;
            target.Children.Add(new TextBlock { Text = label, FontSize = 13 });
            control.HorizontalAlignment = HorizontalAlignment.Stretch;
            target.Children.Add(control);
        }
        Field("System", system);
        var c64Editor = new StackPanel { Name = "C64Editor", Spacing = 4 };
        var amigaEditor = new StackPanel { Name = "AmigaEditor", Spacing = 4 };
        Field("BASIC program", program, c64Editor);
        Field("Runtime after RUN · seconds", c64Duration, c64Editor);
        Field("C64 animation speed", c64Speed, c64Editor);
        c64Editor.Children.Add(c64Colors);
        Field("Company", company, c64Editor);
        Field("Website", website, c64Editor);
        Field("Extra text", tagline, c64Editor);
        c64Editor.Children.Add(new TextBlock { Text = "Company card: 28 characters per line; displayed in uppercase.", FontSize = 12, TextWrapping = TextWrapping.Wrap });
        Field("Kickstart startup", boot, amigaEditor);
        amigaEditor.Children.Add(activity);
        Field("Amiga animation", demo, amigaEditor);
        Field("Demo runtime · seconds", duration, amigaEditor);
        amigaEditor.Children.Add(showScroller);
        Field("Amiga scrolling text", text, amigaEditor);
        Field("Scrolling speed", speed, amigaEditor);
        Field("Sine wave height", wave, amigaEditor);
        editor.Children.Add(c64Editor);
        editor.Children.Add(amigaEditor);
        Field("Software rendering quality", quality);
        var center = new Grid { ColumnDefinitions = new ColumnDefinitions("*,330"), Margin = new Thickness(0, 18, 0, 18) };
        center.Children.Add(new Border { Background = Brushes.Black, Child = scene, Margin = new Thickness(0, 0, 20, 0) });
        editor.Margin = new Thickness(0, 0, 10, 0);
        var editorScroll = new ScrollViewer { Name = "EditorScroll", Content = editor };
        editorScroll.Classes.Add("settingsScroll");
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
            c64Editor.IsVisible = !config.Settings.AmigaMode;
            amigaEditor.IsVisible = config.Settings.AmigaMode;
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
        program.SelectionChanged += (_, _) => { config.Settings.Programs = (ProgramSelection)Math.Max(0, program.SelectedIndex); Restart(); };
        c64Duration.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(NumericUpDown.Value)) { config.Settings.ProgramDurationSeconds = (int)(c64Duration.Value ?? 100); Restart(); } };
        c64Speed.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) config.Settings.SpeedMultiplier = c64Speed.Value; };
        c64Colors.IsCheckedChanged += (_, _) => config.Settings.CycleColors = c64Colors.IsChecked == true;
        company.TextChanged += (_, _) => config.Settings.CompanyName = company.Text ?? "";
        website.TextChanged += (_, _) => config.Settings.CompanyWebsite = website.Text ?? "";
        tagline.TextChanged += (_, _) => config.Settings.CompanyTagline = tagline.Text ?? "";
        system.SelectionChanged += (_, _) => { config.Settings.AmigaMode = system.SelectedIndex == 1; Restart(); };
        boot.SelectionChanged += (_, _) => { config.Settings.AmigaBoot = (AmigaBootStyle)Math.Max(0, boot.SelectedIndex); Restart(); };
        activity.IsCheckedChanged += (_, _) => { config.Settings.AmigaDiskActivity = activity.IsChecked == true; scene.InvalidateVisual(); };
        demo.SelectionChanged += (_, _) => { config.Settings.AmigaAnimation = DemoChoices[Math.Clamp(demo.SelectedIndex, 0, DemoChoices.Length - 1)]; Restart(); };
        quality.SelectionChanged += (_, _) => { config.Settings.NativeRenderQuality = Math.Max(0, quality.SelectedIndex); scene.InvalidateVisual(); };
        duration.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(NumericUpDown.Value)) { config.Settings.AmigaDurationSeconds = (int)(duration.Value ?? 45); Restart(); } };
        showScroller.IsCheckedChanged += (_, _) => { config.Settings.ShowAmigaScroller = showScroller.IsChecked == true; UpdateAvailability(); scene.InvalidateVisual(); };
        text.TextChanged += (_, _) => { config.Settings.ScrollerText = text.Text ?? ""; config.Settings.Validate(); scene.InvalidateVisual(); };
        speed.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) config.Settings.ScrollerSpeed = speed.Value; };
        wave.PropertyChanged += (_, e) => { if (e.Property.Name == nameof(Slider.Value)) config.Settings.ScrollerWave = wave.Value; };
        void ReadEditor()
        {
            config.Settings.Programs = (ProgramSelection)Math.Max(0, program.SelectedIndex);
            config.Settings.ProgramDurationSeconds = (int)(c64Duration.Value ?? 100);
            config.Settings.SpeedMultiplier = c64Speed.Value;
            config.Settings.CycleColors = c64Colors.IsChecked == true;
            config.Settings.CompanyName = company.Text ?? "";
            config.Settings.CompanyWebsite = website.Text ?? "";
            config.Settings.CompanyTagline = tagline.Text ?? "";
            config.Settings.AmigaMode = system.SelectedIndex == 1;
            config.Settings.AmigaDiskActivity = activity.IsChecked == true;
            config.Settings.AmigaBoot = (AmigaBootStyle)Math.Max(0, boot.SelectedIndex);
            config.Settings.AmigaAnimation = DemoChoices[Math.Clamp(demo.SelectedIndex, 0, DemoChoices.Length - 1)];
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
        // Reload another process's saves when returning to the companion. Clone first,
        // because setting controls fires handlers that update the live settings object.
        DateTime savedStamp = System.IO.File.GetLastWriteTimeUtc(config.ConfigPath);
        Activated += (_, _) =>
        {
            if (!OperatingSystem.IsMacOS() || OwnedWindows.Any(w => w.IsVisible)) return;
            DateTime stamp = System.IO.File.GetLastWriteTimeUtc(config.ConfigPath);
            if (stamp == savedStamp || !System.IO.File.Exists(config.ConfigPath)) return;
            savedStamp = stamp;
            config.LoadSettings();
            if (config.LastError != null) { status.Text = "Could not reload: " + config.LastError; return; }
            var loaded = System.Text.Json.JsonSerializer.Deserialize<SaverSettings>(System.Text.Json.JsonSerializer.Serialize(config.Settings))!;
            program.SelectedIndex = (int)loaded.Programs;
            c64Duration.Value = loaded.ProgramDurationSeconds;
            c64Speed.Value = loaded.SpeedMultiplier; c64Colors.IsChecked = loaded.CycleColors;
            company.Text = loaded.CompanyName; website.Text = loaded.CompanyWebsite; tagline.Text = loaded.CompanyTagline;
            system.SelectedIndex = loaded.AmigaMode ? 1 : 0;
            boot.SelectedIndex = (int)loaded.AmigaBoot;
            activity.IsChecked = loaded.AmigaDiskActivity;
            demo.SelectedIndex = Math.Max(0, Array.IndexOf(DemoChoices, loaded.AmigaAnimation));
            duration.Value = loaded.AmigaDurationSeconds;
            quality.SelectedIndex = loaded.NativeRenderQuality;
            showScroller.IsChecked = loaded.ShowAmigaScroller;
            text.Text = loaded.ScrollerText; speed.Value = loaded.ScrollerSpeed; wave.Value = loaded.ScrollerWave;
            config.Settings = loaded;
            Restart();
        };
        Restart();
        Opened += (_, _) => {
            if (OperatingSystem.IsMacOS() && !System.IO.File.Exists(config.ConfigPath))
                status.Text = config.SaveSettings() ? "Shared screensaver settings are ready." : "Could not prepare shared settings: " + config.LastError;
            running = true; clock.Start(); RequestAnimationFrame(Animate); };
        Closed += (_, _) => { running = false; clock.Stop(); scene.Clear(); foreach (var preview in previews.ToArray()) preview.Close(); };
    }
    public void OpenMonitorSettings()
    {
        var existing = OwnedWindows.FirstOrDefault(w => w.IsVisible);
        if (existing != null) { existing.Activate(); return; }
        monitorSettingsButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    }

}
