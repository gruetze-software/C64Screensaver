using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace C64Screensaver;

public partial class SettingsWindow : Window
{
    private readonly ConfigManager config;
    private readonly SaverSettings previewSettings = new();
    private readonly DispatcherTimer timer;
    private bool loading;
    private Window? enlargedPreview;
    private C64Scene? enlargedScene;
    private Slider[] EffectSliders => [CurvatureSlider, ScanlineStrengthSlider, MaskStrengthSlider,
        SoftnessSlider, GlowSlider, GlowSpreadSlider, VignetteSlider, FlickerSlider];
    public SettingsWindow() : this(new ConfigManager()) { }
    public SettingsWindow(ConfigManager configManager)
    {
        InitializeComponent();
        config = configManager;
        previewSettings.AmigaMode = config.Settings.AmigaMode;
        previewSettings.AmigaDiskActivity = config.Settings.AmigaDiskActivity;
        previewSettings.AmigaBoot = config.Settings.AmigaBoot;
        previewSettings.AmigaAnimation = config.Settings.AmigaAnimation;
        previewSettings.AmigaDurationSeconds = config.Settings.AmigaDurationSeconds;
        previewSettings.NativeRenderQuality = config.Settings.NativeRenderQuality;
        previewSettings.ShowAmigaScroller = config.Settings.ShowAmigaScroller;
        previewSettings.ScrollerText = config.Settings.ScrollerText;
        previewSettings.ScrollerSpeed = config.Settings.ScrollerSpeed;
        previewSettings.ScrollerWave = config.Settings.ScrollerWave;
        LogoImage.Source = SpriteAssets.Instance.Logo;
        VersionLabel.Text = AppInfo.DisplayName;
        Apply(config.Settings);
        PresetCombo.SelectedIndex = !config.Settings.CrtEnabled ? 0 : (CrtParameters.From(config.Settings) with { FillScreen = false, KeepAspectRatio = true }) == CrtParameters.From(new SaverSettings()) ? 1 : 3;
        if (config.LastError != null) StatusLabel.Text = "Could not load settings. Defaults are being used.";
        foreach (var slider in EffectSliders)
            slider.PropertyChanged += (_, e) =>
            {
                if (e.Property.Name == nameof(Slider.Value) && !loading)
                { PresetCombo.SelectedIndex = 3; RefreshPreview(); }
            };
        SpeedSlider.PropertyChanged += (_, e) =>
        { if (e.Property.Name == nameof(Slider.Value) && !loading) RefreshPreview(); };
        CrtCheckBox.IsCheckedChanged += (_, _) => { if (!loading) { PresetCombo.SelectedIndex = 3; RefreshPreview(); } };
        ScanlinesCheckBox.IsCheckedChanged += (_, _) => { if (!loading) { PresetCombo.SelectedIndex = 3; RefreshPreview(); } };
        PowerTransitionCheckBox.IsCheckedChanged += (_, _) => { if (!loading) RestartPreview(); };
        ColorsCheckBox.IsCheckedChanged += (_, _) => { if (!loading) RefreshPreview(); };
        AspectCombo.SelectionChanged += (_, _) => { if (!loading) RefreshPreview(); };
        FillScreenCheckBox.IsCheckedChanged += (_, _) => { if (!loading) RefreshPreview(); };
        foreach (var box in new[] { CompanyNameBox, CompanyWebsiteBox, CompanyTaglineBox })
            box.TextChanged += (_, _) =>
            {
                if (!loading && (
                    BasicPrograms.DisplayText(CompanyNameBox.Text) != previewSettings.CompanyName ||
                    BasicPrograms.DisplayText(CompanyWebsiteBox.Text) != previewSettings.CompanyWebsite ||
                    BasicPrograms.DisplayText(CompanyTaglineBox.Text) != previewSettings.CompanyTagline))
                    RestartPreview(true);
            };
        DurationInput.PropertyChanged += (_, e) =>
        { if (!loading && e.Property.Name == nameof(NumericUpDown.Value)) RestartPreview(); };
        ProgramCombo.SelectionChanged += (_, _) => { if (!loading) RestartPreview(); };
        PreviewProgramCheckBox.IsCheckedChanged += (_, _) => { if (!loading) RestartPreview(); };
        PresetCombo.SelectionChanged += (_, _) =>
        {
            if (loading || PresetCombo.SelectedIndex == 3) return;
            var preset = new SaverSettings();
            if (PresetCombo.SelectedIndex == 0) preset.CrtEnabled = false;
            else if (PresetCombo.SelectedIndex == 2)
            {
                preset.Curvature = 45; preset.ScanlineStrength = 55; preset.MaskStrength = 38;
                preset.Softness = 30; preset.Glow = 28; preset.Vignette = 40;
            }
            if (PresetCombo.SelectedIndex == 4)
            {
                preset.Curvature = 25; preset.ScanlineStrength = 25; preset.MaskStrength = 10;
                preset.Softness = 20; preset.Glow = 85; preset.GlowSpread = 65; preset.Vignette = 35;
            }
            loading = true;
            SetEffects(preset);
            loading = false;
            RefreshPreview();
        };
        RestartPreview();
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0 / 30) };
        timer.Tick += (_, _) =>
        {
            bool run = PreviewProgramCheckBox.IsChecked == true;
            PreviewScene.AdvancePreview(1.0 / 30, run);
            enlargedScene?.AdvancePreview(1.0 / 30, run);
        };
        Opened += (_, _) => timer.Start();
        Closed += (_, _) => { timer.Stop(); enlargedPreview?.Close(); PreviewScene.Clear(); };
    }
    private void SetEffects(SaverSettings s)
    {
        CrtCheckBox.IsChecked = s.CrtEnabled;
        CurvatureSlider.Value = s.Curvature;
        ScanlineStrengthSlider.Value = s.ScanlineStrength;
        MaskStrengthSlider.Value = s.MaskStrength;
        SoftnessSlider.Value = s.Softness;
        GlowSlider.Value = s.Glow;
        GlowSpreadSlider.Value = s.GlowSpread;
        VignetteSlider.Value = s.Vignette;
        FlickerSlider.Value = s.Flicker;
        ScanlinesCheckBox.IsChecked = s.Scanlines;
    }
    private void Apply(SaverSettings s)
    {
        loading = true;
        SetEffects(s);
        FillScreenCheckBox.IsChecked = s.FillScreen;
        AspectCombo.SelectedIndex = s.UsesFourThree ? 0 : 1;
        CompanyNameBox.Text = s.CompanyName;
        CompanyWebsiteBox.Text = s.CompanyWebsite;
        CompanyTaglineBox.Text = s.CompanyTagline;
        SpeedSlider.Value = s.SpeedMultiplier;
        ColorsCheckBox.IsChecked = s.CycleColors;
        FpsCheckBox.IsChecked = s.ShowFps;
        PowerTransitionCheckBox.IsChecked = s.PowerTransition;
        ProgramCombo.SelectedIndex = (int)s.Programs;
        DurationInput.Value = s.ProgramDurationSeconds;
        loading = false;
    }
    private void ReadInto(SaverSettings s)
    {
        s.PowerTransition = PowerTransitionCheckBox.IsChecked == true;
        s.FillScreen = FillScreenCheckBox.IsChecked == true;
        s.KeepAspectRatio = AspectCombo.SelectedIndex == 0;
        s.CompanyName = CompanyNameBox.Text ?? "";
        s.CompanyWebsite = CompanyWebsiteBox.Text ?? "";
        s.CompanyTagline = CompanyTaglineBox.Text ?? "";
        s.CrtEnabled = CrtCheckBox.IsChecked == true;
        s.Curvature = CurvatureSlider.Value; s.ScanlineStrength = ScanlineStrengthSlider.Value;
        s.MaskStrength = MaskStrengthSlider.Value; s.Softness = SoftnessSlider.Value;
        s.Glow = GlowSlider.Value; s.GlowSpread = GlowSpreadSlider.Value; s.Vignette = VignetteSlider.Value; s.Flicker = FlickerSlider.Value;
        s.Scanlines = ScanlinesCheckBox.IsChecked == true;
        s.CycleColors = ColorsCheckBox.IsChecked == true;
        s.SpeedMultiplier = SpeedSlider.Value; s.ShowFps = FpsCheckBox.IsChecked == true;
        s.ProgramDurationSeconds = (int)(DurationInput.Value ?? 100m);
        s.Programs = (ProgramSelection)Math.Max(0, ProgramCombo.SelectedIndex);
        s.Validate();
    }
    private void RefreshPreview()
    {
        ReadInto(previewSettings);
        var labels = new[] { CurvatureLabel, ScanlineStrengthLabel, MaskStrengthLabel,
            SoftnessLabel, GlowLabel, GlowSpreadLabel, VignetteLabel, FlickerLabel };
        var sliders = EffectSliders;
        for (int i = 0; i < sliders.Length; i++) labels[i].Text = $"{sliders[i].Value:F0}%";
        SpeedLabel.Text = FormattableString.Invariant($"{SpeedSlider.Value:F1}×");
        PowerPreviewButton.IsEnabled = previewSettings.PowerTransition;
        EffectsPanel.IsEnabled = previewSettings.CrtEnabled;
        CurvatureSlider.IsEnabled = !previewSettings.FillScreen;
        ScanlineStrengthSlider.IsEnabled = previewSettings.Scanlines;
        PreviewHint.Text = !previewSettings.CrtEnabled
            ? "CRT effects are off. Enable them to adjust the picture."
            : previewSettings.FillScreen
                ? "Borderless mode keeps curvature off. Fine CRT detail is enlarged here; use ↗ for full-size."
                : "Fine CRT detail is enlarged here. Use ↗ for the full-size view.";
        PreviewScene.InvalidateVisual();
        enlargedScene?.InvalidateVisual();
    }
    private void RestartPreview(bool companyText = false)
    {
        RefreshPreview();
        var selected = previewSettings.Programs;
        if (companyText) previewSettings.Programs = ProgramSelection.Company;
        PreviewScene.Initialize(previewSettings, false, 384, 288, new Random(64));
        enlargedScene?.Initialize(previewSettings, false, 960, 720, new Random(64));
        previewSettings.Programs = selected;
        if (PreviewProgramCheckBox.IsChecked == true) { PreviewScene.ShowProgramPreview(); enlargedScene?.ShowProgramPreview(); }
    }
    private void PowerPreview_Click(object? sender, RoutedEventArgs e)
    {
        PreviewScene.PreviewPowerTransition();
        enlargedScene?.PreviewPowerTransition();
    }
    private void EnlargePreview_Click(object? sender, RoutedEventArgs e)
    {
        if (enlargedPreview != null) { enlargedPreview.Activate(); return; }
        enlargedScene = new C64Scene();
        enlargedScene.Initialize(previewSettings, false, 960, 720, new Random(64));
        if (PreviewProgramCheckBox.IsChecked == true) enlargedScene.ShowProgramPreview();
        var scene = enlargedScene;
        var window = new Window
        {
            Title = "C64Screensaver · Live CRT preview · F11 fullscreen · Esc close",
            Icon = Icon, Width = 960, Height = 720, MinWidth = 640, MinHeight = 480,
            Background = Avalonia.Media.Brushes.Black,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = scene
        };
        enlargedPreview = window;
        window.KeyDown += (_, key) =>
        {
            if (key.Key == Avalonia.Input.Key.Escape) window.Close();
            else if (key.Key == Avalonia.Input.Key.F11)
                window.WindowState = window.WindowState == WindowState.FullScreen
                    ? WindowState.Normal : WindowState.FullScreen;
        };
        window.Closed += (_, _) => { scene.Clear(); enlargedScene = null; enlargedPreview = null; };
        window.Show(this);
    }
    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        ReadInto(config.Settings);
        if (config.SaveSettings()) Close();
        else StatusLabel.Text = $"Could not save settings: {config.LastError}";
    }
    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
    private async void About_Click(object? sender, RoutedEventArgs e) => await new AboutWindow().ShowDialog(this);
    private void Reset_Click(object? sender, RoutedEventArgs e)
    {
        Apply(new SaverSettings());
        PresetCombo.SelectedIndex = 1;
        RestartPreview();
    }
}
