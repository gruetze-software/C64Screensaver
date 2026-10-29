using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using C64Screensaver;

void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
var machine = new BasicMachine(seed: 64);
for (int i = 0; i < 100_000; i++) machine.Step();
Check(machine.Instructions == 100_000, "Instruction count");
Check(machine.Characters.Count(c => c is 205 or 206) > 900, "Scrolling maze must fill the screen");
Check(machine.Border is >= 0 and < 16 && machine.Ink is > 0 and < 16, "Palette bounds");
Check(machine.Colors.All(c => c < 16), "Colour RAM");
var custom = new BasicMachine(["10 A=INT(3.9)+2*4", "20 POKE 53280,A", "30 GOTO 10"]);
custom.Step(); custom.Step();
Check(custom.Border == 11, "Expression precedence and assignment");
machine.ClearScreen();
Check(machine.Characters.All(c => c == 32), "Clear screen");

var settings = new SaverSettings { CrtStrength = 999, SpeedMultiplier = double.NaN };
settings.Validate();
Check(settings.CrtStrength == 100 && settings.SpeedMultiplier == 1, "Settings validation");
var optionsValid = LaunchOptions.TryParse(["/p", "1234"], true, out var options, out _);
Check(optionsValid && options.Mode == LaunchMode.Preview && options.ParentHandle == 1234, "Windows preview parsing");
Check(!LaunchOptions.TryParse(["/p", "1234"], false, out _, out _), "Preview must be Windows only");

Check(LaunchOptions.TryParse([], true, out var companionStart, out _) && companionStart.Mode == LaunchMode.Companion, "Normal launch opens companion");
Check(LaunchOptions.TryParse(["/s"], true, out var saverStart, out _) && saverStart.Mode == LaunchMode.Fullscreen, "System screensaver still starts directly");
var amigaSettings = new SaverSettings { AmigaMode = true, AmigaBoot = AmigaBootStyle.None, ScrollerText = new string('A', 300), ScrollerSpeed = double.NaN, ScrollerWave = 200 };
amigaSettings.Validate();
Check(amigaSettings.ScrollerText.Length == 300 && amigaSettings.ScrollerSpeed == 48 && amigaSettings.ScrollerWave == 48, "Scroller is independent of 28-character company text and validates numeric settings");
using (var firstAmiga = AmigaFrameBuffer.Capture(amigaSettings, 0))
using (var secondAmiga = AmigaFrameBuffer.Capture(amigaSettings, 1))
using (var firstPixels = SkiaSharp.SKBitmap.FromImage(firstAmiga))
using (var secondPixels = SkiaSharp.SKBitmap.FromImage(secondAmiga))
    Check(firstPixels.Pixels.Zip(secondPixels.Pixels).Count(pair => pair.First != pair.Second) > 1000, "Amiga animation advances");
var editing = new BasicMachine();
editing.Write("POKX");
editing.Backspace();
editing.Write("E");
Check(new string(editing.Characters.Take(4).Select(c => (char)c).ToArray()) == "POKE", "Backspace must erase and replace one cell");
Check(editing.CursorIndex == 4, "Fixed-cell cursor after backspace");
var sequence = new ScreenSession(new Random(64), ProgramSelection.AllPrograms, new SaverSettings { PowerTransition = false });
Check(sequence.Machine.Border == 14 && sequence.Machine.Background == 6, "Classic startup colours");
Check(sequence.Machine.CursorIndex % 40 == 0, "READY cursor must start at the left margin");
Check(new string(sequence.Machine.Characters.Skip(200).Take(40).Select(c => (char)c).ToArray()) == "READY." + new string(' ', 34), "READY occupies six cells at the left margin");
sequence.Advance(2, 1);
Check(sequence.Typing.TypedCharacters == 0, "Startup pause before typing");
sequence.Advance(1, 1);
Check(sequence.Typing.TypedCharacters is > 0 and < 5, "Slow character-by-character typing");
bool sawMistake = false, sawCorrection = false;
int elapsedFrames = 0;
while (!sequence.Executing && elapsedFrames++ < 60 * 120)
{
    sequence.Advance(1.0 / 60, 1);
    Check(sequence.Machine.Border == 14, "No colour change before RUN");
    Check(sequence.Machine.Instructions == 0, "No execution before RUN");
    if (sequence.Typing.Corrections > 0) sawCorrection = true;
    string screen = new(sequence.Machine.Characters.Select(c => (char)c).ToArray());
    if (screen.Contains('X')) sawMistake = true;
}
Check(sequence.Executing && elapsedFrames > 60 * 20, "Typing must take time and eventually finish");
Check(sawMistake, "A typo must be visible before correction");
Check(sawCorrection && sequence.Typing.Corrections == 2, "Two visible typo corrections");
string entered = new(sequence.Machine.Characters.Select(c => (char)c).ToArray());
foreach (var line in BasicMachine.Listing) Check(entered.Contains(line), "Corrected listing: " + line);
Check(entered.Contains("RUN"), "RUN must be typed last");
Check(sequence.Machine.Instructions == 0, "RUN must be entered before the first instruction");
sequence.Advance(1, 1);
Check(sequence.Machine.Instructions > 0, "Execute after RUN");
for (int i = 0; i < 100 * 60; i++) sequence.Advance(1.0 / 60, 1);
Check(!sequence.Executing && sequence.Machine.Border == 14, "Cycle restarts in original colours");
Check(sequence.IsCompany, "Alternating mode switches to the company program");
foreach (double speed in new[] { 0.2, 1.0, 5.0 })
{
    var calm = new BasicMachine(seed: 64);
    int changes = 0, previous = calm.Border;
    double credit = 0;
    for (int frame = 0; frame < 60 * 30; frame++)
    {
        calm.AdvanceClock(1.0 / 60);
        credit += 240 * speed / 60;
        while (credit >= 1) { calm.Step(); credit--; }
        if (calm.Border != previous) { changes++; previous = calm.Border; }
    }
    Check(changes is >= 2 and <= 8, "Border changes only about every four seconds, speed " + speed);
}
var clock = new BasicMachine(["10 IF TI<120 THEN 10", "20 POKE 53280,5", "30 GOTO 10"]);
for (int i = 0; i < 100; i++) clock.Step();
Check(clock.Border == 14, "TI wait holds original colour");
clock.AdvanceClock(2);
clock.Step(); clock.Step();
Check(clock.Border == 5, "TI timer releases IF wait after two seconds");
var company = new BasicMachine(BasicPrograms.Company, seed: 64);
for (int i = 0; i < 2000; i++) { company.AdvanceClock(1.0 / 240); company.Step(); }
string card = new(company.Characters.Select(c => (char)c).ToArray());
Check(card.Contains("GRUETZE-SOFTWARE") && card.Contains("WWW.GRUETZE-SOFTWARE.DE"), "Company and website executed from BASIC PRINT");
Check(BasicPrograms.Company.All(line => line.Length <= 40), "Company listing fits 40 columns");
var limits = new SaverSettings { Curvature = double.NaN, Glow = 200, Flicker = -5, Programs = (ProgramSelection)99 };
limits.Validate();
Check(limits.Curvature == 18 && limits.Glow == 100 && limits.Flicker == 0 &&
    limits.Programs == ProgramSelection.Alternating, "Effect settings validation");
Check(new SaverSettings().Flicker == 0, "No flicker by default");
var companyTyping = new ScreenSession(new Random(64), ProgramSelection.Company);
for (int i = 0; !companyTyping.Executing && i < 60 * 150; i++)
    companyTyping.Advance(1.0 / 60, 1);
Check(companyTyping.Executing && companyTyping.Typing.Corrections == 2, "Company listing is typed with corrected mistakes and RUN");
string typedCompany = new(companyTyping.Machine.Characters.Select(c => (char)c).ToArray());
foreach (string line in BasicPrograms.Company) Check(typedCompany.Contains(line), "Typed company line: " + line);
string configPath = Path.Combine(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts")), "smoke-settings.json");
var savedConfig = new ConfigManager(configPath) { Settings = new SaverSettings {
    Curvature = 48, ScanlineStrength = 37, MaskStrength = 27, Softness = 41,
    Glow = 29, Vignette = 33, Flicker = 0, CrtEnabled = false, Programs = ProgramSelection.Company } };
Check(savedConfig.SaveSettings(), "Save effect settings");
var restored = new ConfigManager(configPath).Settings;
Check(restored.Curvature == 48 && restored.MaskStrength == 27 && restored.Softness == 41 &&
    restored.Glow == 29 && restored.Vignette == 33 && restored.Flicker == 0 &&
    !restored.CrtEnabled && restored.Programs == ProgramSelection.Company, "Restore effects and program selection");
var personal = new SaverSettings
{
    FillScreen = true, CompanyName = "Grütze & Söhne", CompanyWebsite = "www.example.de",
    CompanyTagline = "Hallo \"Welt\""
};
personal.Validate();
Check(personal.CompanyName == "GRUETZE & SOEHNE" && personal.CompanyWebsite == "WWW.EXAMPLE.DE" &&
    personal.CompanyTagline == "HALLO 'WELT'", "C64 text normalization");
var personalProgram = BasicPrograms.CreateCompany(personal);
Check(personalProgram.All(line => line.Length <= 40), "Edited BASIC listing fits 40 columns");
var personalMachine = new BasicMachine(personalProgram, 64);
for (int i = 0; i < 2000; i++) { personalMachine.AdvanceClock(1.0 / 240); personalMachine.Step(); }
string personalCard = new(personalMachine.Characters.Select(c => (char)c).ToArray());
Check(personalCard.Contains(personal.CompanyName) && personalCard.Contains(personal.CompanyWebsite) &&
    personalCard.Contains(personal.CompanyTagline), "Edited texts are executed by the BASIC program");
Check(BasicPrograms.DisplayText(new string('A', 100)).Length == 28, "C64 text length limit");
Check(!BasicPrograms.DisplayText("HELLO\n\" : POKE 53280,0").Contains('"'), "User text cannot terminate a BASIC string");
var personalConfig = new ConfigManager(Path.Combine(Path.GetDirectoryName(configPath)!, "personal-settings.json")) { Settings = personal };
Check(personalConfig.SaveSettings(), "Save borderless mode and personal text");
var personalRestored = new ConfigManager(personalConfig.ConfigPath).Settings;
Check(personalRestored.FillScreen && personalRestored.CompanyName == personal.CompanyName &&
    personalRestored.CompanyWebsite == personal.CompanyWebsite &&
    personalRestored.CompanyTagline == personal.CompanyTagline, "Restore borderless mode and all three texts");
var starMachine = new BasicMachine(BasicPrograms.Stars, seed: 64);
for (int i = 0; i < 24_000; i++) { starMachine.AdvanceClock(1.0 / 240); starMachine.Step(); }
int starCount = starMachine.Characters.Count(c => c is 42 or 46);
Check(starCount is > 40 and < 500 && starMachine.Characters.Contains((byte)42) &&
    starMachine.Characters.Contains((byte)46), "BASIC starfield draws and erases stars of two sizes");
Check(starMachine.Colors.Contains((byte)1) && starMachine.Colors.Contains((byte)14), "Star colours are set through colour RAM");
Check(BasicPrograms.Stars.All(line => line.Length <= 40), "Starfield listing fits the screen");
var playlistSettings = new SaverSettings { ProgramDurationSeconds = 15, PowerTransition = false };
var playlist = new ScreenSession(new Random(64), ProgramSelection.AllPrograms, playlistSettings);
foreach (var expected in new[] { ProgramSelection.Maze, ProgramSelection.Company, ProgramSelection.Stars })
{
    Check(playlist.CurrentProgram == expected && !playlist.Executing, "Playlist starts each demo in typing mode");
    int frames = 0;
    while (!playlist.Executing && frames++ < 60 * 240) playlist.Advance(1.0 / 60, 1);
    Check(playlist.Executing && frames > 60 * 15, "Typing does not consume the configured runtime");
    Check(playlist.Machine.Instructions == 0, "Every playlist demo waits until RUN");
    playlist.Advance(14.9, 1);
    Check(playlist.Executing && playlist.CurrentProgram == expected, "Run stays active before the duration expires");
    var previousMachine = playlist.Machine;
    playlist.Advance(0.2, 1);
    Check(!playlist.Executing && playlist.Machine.Instructions == 0 &&
        !ReferenceEquals(previousMachine, playlist.Machine), "Duration resets machine and repeats code entry");
}
Check(playlist.CurrentProgram == ProgramSelection.Maze, "All-program playlist wraps after stars");
var repeat = new ScreenSession(new Random(64), ProgramSelection.Stars, playlistSettings);
repeat.StartPreview();
repeat.Advance(15.1, 5);
Check(repeat.CurrentProgram == ProgramSelection.Stars && !repeat.Executing && repeat.Typing.TypedCharacters == 0,
    "Single selected program also restarts after the configured duration");
var durationLimits = new SaverSettings { ProgramDurationSeconds = -1 };
durationLimits.Validate();
Check(durationLimits.ProgramDurationSeconds == 15, "Minimum program duration");
durationLimits.ProgramDurationSeconds = 99999; durationLimits.Validate();
Check(durationLimits.ProgramDurationSeconds == 1800, "Maximum program duration");
var durationConfig = new ConfigManager(Path.Combine(Path.GetDirectoryName(configPath)!, "duration-settings.json"))
{ Settings = new SaverSettings { Programs = ProgramSelection.Stars, ProgramDurationSeconds = 45 } };
Check(durationConfig.SaveSettings(), "Save duration");
var durationRestored = new ConfigManager(durationConfig.ConfigPath).Settings;
Check(durationRestored.ProgramDurationSeconds == 45 && durationRestored.Programs == ProgramSelection.Stars,
    "Restore duration and starfield selection");
var power = new ScreenSession(new Random(64), ProgramSelection.AllPrograms,
    new SaverSettings { ProgramDurationSeconds = 15, PowerTransition = true });
power.StartPreview();
power.Advance(15.1, 1);
Check(power.TransitionTime == 0 && power.CurrentProgram == ProgramSelection.Maze, "Power-off starts before switching programs");
long stoppedAt = power.Machine.Instructions;
power.Advance(0.75, 1);
Check(power.CurrentProgram == ProgramSelection.Maze && power.Machine.Instructions == stoppedAt,
    "Old program freezes while collapsing to a point");
power.Advance(0.30, 1);
Check(power.CurrentProgram == ProgramSelection.Company && power.Machine.Instructions == 0 &&
    !power.Executing && power.TransitionTime.HasValue, "Next boot is prepared during the black interval");
power.Advance(0.40, 1);
Check(!power.TransitionTime.HasValue && power.Typing.TypedCharacters == 0, "Power-on completes before fresh ghost typing");
var powerConfig = new ConfigManager(Path.Combine(Path.GetDirectoryName(configPath)!, "power-settings.json"))
{ Settings = new SaverSettings { PowerTransition = false } };
Check(powerConfig.SaveSettings() && !new ConfigManager(powerConfig.ConfigPath).Settings.PowerTransition,
    "Power transition can be disabled and remains disabled after reload");
AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .WithInterFont().SetupWithoutStarting();
Check(CrtDrawOperation.ShaderAvailable, "CRT shader compiles: " + CrtDrawOperation.ShaderError);
string output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../docs/images"));
Directory.CreateDirectory(output);
if (args.Contains("--companion-only")) { CompanionUi(); return; }
void Capture(Window window, string name)
{
    window.Show();
    Dispatcher.UIThread.RunJobs();
    AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    Dispatcher.UIThread.RunJobs();
    using var bitmap = window.CaptureRenderedFrame() ?? throw new Exception("No rendered frame: " + name);
    bitmap.Save(Path.Combine(output, name + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    window.Close();
}
void CaptureScene(string name, int frames, SaverSettings? settings = null, bool preview = false, int width = 960, int height = 720)
{
    var scene = new C64Scene();
    scene.Initialize(settings ?? new SaverSettings(), false, width, height, new Random(64));
    if (preview) scene.ShowProgramPreview();
    for (int i = 0; i < frames; i++) scene.Advance(width, height, 1.0 / 60);
    Capture(new Window { Width = width, Height = height, Content = scene }, name);
    scene.Clear();
}
Check(new SaverSettings().UsesFourThree && !new SaverSettings { FillScreen = true }.UsesFourThree,
    "Legacy settings preserve framed 4:3 and borderless stretch");
var aspectRoundTrip = System.Text.Json.JsonSerializer.Deserialize<SaverSettings>(
    System.Text.Json.JsonSerializer.Serialize(new SaverSettings { FillScreen = true, KeepAspectRatio = true }))!;
Check(aspectRoundTrip.FillScreen && aspectRoundTrip.UsesFourThree,
    "Border visibility and proportions persist independently");
CaptureScene("aspect-four-three-borderless", 0, new SaverSettings { FillScreen = true, KeepAspectRatio = true, CrtEnabled = false }, false, 1280, 720);
CaptureScene("aspect-four-three-portrait", 0, new SaverSettings { FillScreen = true, KeepAspectRatio = true, CrtEnabled = false }, false, 720, 1280);
CaptureScene("aspect-stretched-frame", 0, new SaverSettings { FillScreen = false, KeepAspectRatio = false, CrtEnabled = false }, false, 1280, 720);
CaptureScene("aspect-four-three-frame", 0, new SaverSettings { FillScreen = false, KeepAspectRatio = true, CrtEnabled = false }, false, 1280, 720);
bool Colored(SkiaSharp.SKColor c) => c.Red + c.Green + c.Blue > 0;
using (var wide = SkiaSharp.SKBitmap.Decode(Path.Combine(output, "aspect-four-three-borderless.png")))
using (var portrait = SkiaSharp.SKBitmap.Decode(Path.Combine(output, "aspect-four-three-portrait.png")))
using (var stretched = SkiaSharp.SKBitmap.Decode(Path.Combine(output, "aspect-stretched-frame.png")))
using (var framed = SkiaSharp.SKBitmap.Decode(Path.Combine(output, "aspect-four-three-frame.png")))
{
    Check(!Colored(wide.GetPixel(159, 360)) && Colored(wide.GetPixel(160, 360)) &&
          Colored(wide.GetPixel(1119, 360)) && !Colored(wide.GetPixel(1120, 360)),
        "Borderless 4:3 is exactly 960 by 720 centered on wide monitor");
    Check(!Colored(portrait.GetPixel(360, 369)) && Colored(portrait.GetPixel(360, 370)) &&
          Colored(portrait.GetPixel(360, 909)) && !Colored(portrait.GetPixel(360, 910)),
        "Borderless 4:3 is exactly 720 by 540 centered on portrait monitor");
    Check(stretched.Pixels.All(Colored), "Stretched framed picture covers entire monitor");
    Check(stretched.GetPixel(0, 360) != stretched.GetPixel(640, 360),
        "Stretch retains the emulated C64 border when requested");
    Check(!Colored(framed.GetPixel(0, 360)) && Colored(framed.GetPixel(640, 360)),
        "Framed 4:3 keeps its original centered presentation");
}
CaptureScene("startup", 60);
CaptureScene("typing", 60 * 20);
CaptureScene("screensaver", 60 * 70);
CaptureScene("company", 0, new SaverSettings { Programs = ProgramSelection.Company }, true);
CaptureScene("clean", 60, new SaverSettings { CrtEnabled = false });
CaptureScene("classic-crt", 60, new SaverSettings { Curvature = 45, ScanlineStrength = 55, MaskStrength = 38, Softness = 30, Glow = 28, Vignette = 40 });
CaptureScene("borderless", 0, new SaverSettings { FillScreen = true }, true, 1280, 720);
CaptureScene("borderless-crt", 60, new SaverSettings { FillScreen = true, Curvature = 100 }, false, 1280, 720);
CaptureScene("borderless-early", 0, new SaverSettings { FillScreen = true, CrtEnabled = false }, false, 1280, 720);
CaptureScene("borderless-later", 60 * 18, new SaverSettings { FillScreen = true, CrtEnabled = false }, false, 1280, 720);
using (var early = SkiaSharp.SKBitmap.Decode(Path.Combine(output, "borderless-early.png")))
using (var later = SkiaSharp.SKBitmap.Decode(Path.Combine(output, "borderless-later.png")))
using (var crt = SkiaSharp.SKBitmap.Decode(Path.Combine(output, "borderless-crt.png")))
{
    int unchanged = 0;
    for (int y = 0; y < early.Height; y++)
        for (int x = 0; x < early.Width; x++)
            if (early.GetPixel(x, y) == later.GetPixel(x, y)) unchanged++;
    Check(unchanged == 0, "Every output pixel changes over time in borderless mode, including the background; unchanged: " + unchanged);
    for (int x = 0; x < crt.Width; x++)
    {
        Check(crt.GetPixel(x, 0) != SkiaSharp.SKColors.Black, "No black top strip with CRT enabled");
        Check(crt.GetPixel(x, crt.Height - 1) != SkiaSharp.SKColors.Black, "No black bottom strip with CRT enabled");
    }
    for (int y = 0; y < crt.Height; y++)
    {
        Check(crt.GetPixel(0, y) != SkiaSharp.SKColors.Black, "No black left strip with CRT enabled");
        Check(crt.GetPixel(crt.Width - 1, y) != SkiaSharp.SKColors.Black, "No black right strip with CRT enabled");
    }
}
using (var icon = SkiaSharp.SKBitmap.Decode(Path.GetFullPath(Path.Combine(output, "../../C64Screensaver/Assets/app-icon.png"))))
    Check(icon.Width == 1024 && icon.GetPixel(0, 0).Alpha == 0, "Transparent C64 product icon");
CaptureScene("starfield", 0, new SaverSettings { Programs = ProgramSelection.Stars }, true);
CaptureScene("stars-borderless", 0, new SaverSettings { Programs = ProgramSelection.Stars, FillScreen = true }, true, 1280, 720);
Capture(new SettingsWindow(), "settings");
var monitorDialog = new SettingsWindow();
monitorDialog.FindControl<TabControl>("SettingsTabs")!.SelectedIndex = 1;
monitorDialog.Width = 980; monitorDialog.Height = 640;
monitorDialog.Show();
Dispatcher.UIThread.RunJobs();
foreach (string name in new[] { "CurvatureSlider", "ScanlineStrengthSlider", "MaskStrengthSlider", "SoftnessSlider",
    "GlowSlider", "GlowSpreadSlider", "VignetteSlider", "FlickerSlider" })
{
    var slider = monitorDialog.FindControl<Slider>(name)!;
    Check(slider.Bounds.Height >= 52, "Slider has enough vertical space: " + name);
    var thumbs = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(slider)
        .OfType<Avalonia.Controls.Primitives.Thumb>().ToArray();
    Check(thumbs.Length > 0, "Slider thumb is templated: " + name);
    foreach (var thumb in thumbs)
    {
        var origin = thumb.TranslatePoint(default, slider) ?? throw new Exception("No thumb coordinates");
        Check(origin.Y >= -0.5 && origin.Y + thumb.Bounds.Height <= slider.Bounds.Height + 0.5,
            "Slider thumb stays within its allocated height: " + name);
    }
}
Capture(monitorDialog, "monitor-settings-small");
var monitorLarge = new SettingsWindow();
monitorLarge.FindControl<TabControl>("SettingsTabs")!.SelectedIndex = 1;
monitorLarge.Height = 880;
Capture(monitorLarge, "monitor-settings");
Capture(new AboutWindow(), "about");
string configBeforePreview = File.ReadAllText(configPath);
var previewConfig = new ConfigManager(configPath);
var previewDialog = new SettingsWindow(previewConfig);
previewDialog.FindControl<ComboBox>("PresetCombo")!.SelectedIndex = 2;
Check(previewDialog.FindControl<CheckBox>("CrtCheckBox")!.IsChecked == true &&
    previewDialog.FindControl<Slider>("CurvatureSlider")!.Value == 45, "Classic preset applies controls");
previewDialog.FindControl<Slider>("CurvatureSlider")!.Value = 66;
Check(previewDialog.FindControl<ComboBox>("PresetCombo")!.SelectedIndex == 3, "Manual slider adjustment selects Custom");
Check(!previewConfig.Settings.CrtEnabled && previewConfig.Settings.Curvature == 48,
    "Live preview does not mutate saved configuration");
previewDialog.FindControl<NumericUpDown>("DurationInput")!.Value = 45;
Check(previewConfig.Settings.ProgramDurationSeconds == 100, "Unsaved duration does not change persisted settings");
previewDialog.FindControl<CheckBox>("FillScreenCheckBox")!.IsChecked = true;
Check(!previewDialog.FindControl<Slider>("CurvatureSlider")!.IsEnabled, "Borderless mode disables curvature in the UI");
previewDialog.FindControl<TextBox>("CompanyNameBox")!.Text = "My Company";
previewDialog.FindControl<TextBox>("CompanyWebsiteBox")!.Text = "www.example.de";
previewDialog.FindControl<TextBox>("CompanyTaglineBox")!.Text = "Hello World";
Dispatcher.UIThread.RunJobs();
Check(previewConfig.Settings.CompanyWebsite == "WWW.GRUETZE-SOFTWARE.DE" && !previewConfig.Settings.FillScreen,
    "Edited texts and borderless preview do not mutate saved settings");
Capture(previewDialog, "custom-settings");
Check(File.ReadAllText(configPath) == configBeforePreview, "Closing without Save leaves configuration unchanged");
void CaptureTransition(string name, double progress)
{
    var scene = new C64Scene();
    scene.Initialize(new SaverSettings { FillScreen = true, ProgramDurationSeconds = 15, PowerTransition = true },
        false, 640, 480, new Random(64));
    scene.ShowProgramPreview();
    scene.Advance(640, 480, 15.1);
    if (progress > 0) scene.Advance(640, 480, progress);
    Capture(new Window { Width = 640, Height = 480, Content = scene }, name);
    scene.Clear();
}
CaptureTransition("crt-power-black", 0);
CaptureTransition("crt-power-white", 0.12);
CaptureTransition("crt-power-collapse", 0.30);
CaptureTransition("crt-power-line", 0.55);
CaptureTransition("crt-power-point", 0.75);
CaptureTransition("crt-power-on", 1.42);
using (var black = SkiaSharp.SKBitmap.Decode(Path.Combine(output, "crt-power-black.png")))
using (var white = SkiaSharp.SKBitmap.Decode(Path.Combine(output, "crt-power-white.png")))
using (var point = SkiaSharp.SKBitmap.Decode(Path.Combine(output, "crt-power-point.png")))
{
    Check(black.Pixels.All(p => p == SkiaSharp.SKColors.Black), "Power-off begins with a black screen");
    Check(white.Pixels.All(p => p == SkiaSharp.SKColors.White), "Power-off has the requested white flash");
    Check(point.GetPixel(point.Width/2, point.Height/2).Red > 50 &&
        point.GetPixel(0,0) == SkiaSharp.SKColors.Black &&
        point.Pixels.Count(p => p.Red > 0) < 500, "Collapsed image is a small central light point");
}

var probeConfig = new ConfigManager(Path.Combine(Path.GetDirectoryName(configPath)!, "preview-probe.json"));
probeConfig.Settings = new SaverSettings { CrtEnabled = true, FillScreen = false, Curvature = 0,
    ScanlineStrength = 0, MaskStrength = 0, Softness = 0, Glow = 0, Vignette = 0, Flicker = 0, PowerTransition = false };
var probe = new SettingsWindow(probeConfig);
probe.FindControl<TabControl>("SettingsTabs")!.SelectedIndex = 1;
probe.FindControl<CheckBox>("PreviewProgramCheckBox")!.IsChecked = false;
probe.Show();
SkiaSharp.SKColor[] SmallPreviewPixels()
{
    Dispatcher.UIThread.RunJobs();
    AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    Dispatcher.UIThread.RunJobs();
    var scene = probe.FindControl<C64Scene>("PreviewScene")!;
    var origin = scene.TranslatePoint(default, probe) ?? throw new Exception("No preview position");
    using var frame = probe.CaptureRenderedFrame() ?? throw new Exception("Missing settings frame");
    using var buffer = new MemoryStream();
    frame.Save(buffer, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    using var pixels = SkiaSharp.SKBitmap.Decode(buffer.ToArray());
    int w = (int)scene.Bounds.Width, h = (int)scene.Bounds.Height;
    var result = new SkiaSharp.SKColor[w*h];
    for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++) result[y*w+x] = pixels.GetPixel((int)origin.X+x, (int)origin.Y+y);
    return result;
}
var previewDiffs = new Dictionary<string,int>();
foreach (string name in new[] { "CurvatureSlider", "ScanlineStrengthSlider", "MaskStrengthSlider",
    "SoftnessSlider", "GlowSlider", "GlowSpreadSlider", "VignetteSlider", "FlickerSlider" })
{
    var slider = probe.FindControl<Slider>(name)!;
    slider.Value = 0;
    if (name == "GlowSpreadSlider") probe.FindControl<Slider>("GlowSlider")!.Value = 85;
    var before = SmallPreviewPixels();
    slider.Value = 100;
    int changed = 0;
    for (int attempt = 0; attempt < 3; attempt++)
    {
        probe.FindControl<C64Scene>("PreviewScene")!.AdvancePreview(0.12, false);
        var after = SmallPreviewPixels();
        changed = Math.Max(changed, before.Zip(after).Count(pair => pair.First != pair.Second));
    }
    previewDiffs[name] = changed;
    Check(changed > 100, "Settings slider visibly changes small preview: " + name + " (" + changed + " pixels)");
    slider.Value = 0;
    if (name == "GlowSpreadSlider") probe.FindControl<Slider>("GlowSlider")!.Value = 0;
}
probe.FindControl<CheckBox>("ScanlinesCheckBox")!.IsChecked = false;
Check(!probe.FindControl<Slider>("ScanlineStrengthSlider")!.IsEnabled,
    "Scanline slider is disabled when scanlines are switched off");
probe.FindControl<CheckBox>("ScanlinesCheckBox")!.IsChecked = true;
Check(probeConfig.Settings.MaskStrength == 0 && probeConfig.Settings.ScanlineStrength == 0,
    "Preview edits do not alter stored settings");
probe.FindControl<Button>("EnlargePreviewButton")!.RaiseEvent(
    new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
var expanded = probe.OwnedWindows.Single();
Check(expanded.Content is C64Scene expandedScene && !expandedScene.PreviewDetails,
    "Enlarged preview uses real effect spacing");
Dispatcher.UIThread.RunJobs();
AvaloniaHeadlessPlatform.ForceRenderTimerTick();
SkiaSharp.SKColor[] ExpandedPixels()
{
    Dispatcher.UIThread.RunJobs();
    AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    Dispatcher.UIThread.RunJobs();
    using var frame = expanded.CaptureRenderedFrame() ?? throw new Exception("No enlarged frame");
    using var buffer = new MemoryStream();
    frame.Save(buffer, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    using var pixels = SkiaSharp.SKBitmap.Decode(buffer.ToArray());
    return pixels.Pixels;
}
var aspectBefore = ExpandedPixels();
probe.FindControl<ComboBox>("AspectCombo")!.SelectedIndex = 1;
var aspectAfter = ExpandedPixels();
Check(aspectBefore.Zip(aspectAfter).Count(pair => pair.First != pair.Second) > 10_000, "Proportion choice updates enlarged preview immediately");
probe.FindControl<ComboBox>("AspectCombo")!.SelectedIndex = 0;
var expandedBefore = ExpandedPixels();
probe.FindControl<ComboBox>("PresetCombo")!.SelectedIndex = 2;
var expandedAfter = ExpandedPixels();
Check(expandedBefore.Zip(expandedAfter).Count(pair => pair.First != pair.Second) > 10_000,
    "Settings changes update the open enlarged preview live");
Capture(expanded, "enlarged-preview");
Check(!probe.FindControl<Button>("PowerPreviewButton")!.IsEnabled, "Manual effect test is disabled when power transitions are off");
probe.FindControl<CheckBox>("PowerTransitionCheckBox")!.IsChecked = true;
probe.FindControl<Button>("PowerPreviewButton")!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
var testedScene = probe.FindControl<C64Scene>("PreviewScene")!;
Check(testedScene.IsPowerTransitionActive, "Power test button starts the preview effect immediately");
testedScene.AdvancePreview(0.75, false);
Check(testedScene.IsPowerTransitionActive, "Power test animates even with Running program unchecked");
testedScene.AdvancePreview(0.70, false);
Check(!testedScene.IsPowerTransitionActive, "Manual effect test finishes and returns to startup");
Capture(probe, "settings-preview-fixed");
Console.WriteLine("Small-preview changed pixels: " + string.Join(", ", previewDiffs.Select(p => p.Key + "=" + p.Value)));
Console.WriteLine("PASS: eight live slider image comparisons, enlarged live view, black/white/line/point power cycle, manual power test; BASIC, typing, RUN gating, 4-second borders at three speeds, company/website, alternating programs, settings and CRT shader; borderless coverage, editable text, icon, three-program rotation, configurable runtime, repeated typing and unclipped slider thumbs at minimum window size.");
if (args.Contains("--release-screenshots"))
{
    // Explicit opt-in only: ordinary tests never reset the user's configuration.
    var userConfig = new ConfigManager();
    if (File.Exists(userConfig.ConfigPath))
        File.Copy(userConfig.ConfigPath, Path.Combine(Path.GetDirectoryName(configPath)!, "user-settings-before-release.json"), true);
    userConfig.Settings = new SaverSettings();
    Check(userConfig.SaveSettings(), "Reset user settings for release screenshots");
    var reset = new ConfigManager();
    Check(reset.Settings.UsesFourThree && !reset.Settings.FillScreen &&
        reset.Settings.Flicker == 0 && reset.Settings.ProgramDurationSeconds == 100 &&
        reset.Settings.CompanyWebsite == "WWW.GRUETZE-SOFTWARE.DE", "User defaults saved and reloaded");
    var playback = new SettingsWindow(reset) { Height = 960 };
    Capture(playback, "settings-playback");
    var monitor = new SettingsWindow(new ConfigManager()) { Height = 960 };
    monitor.FindControl<TabControl>("SettingsTabs")!.SelectedIndex = 1;
    Capture(monitor, "settings-monitor");
    Check(!string.IsNullOrEmpty(AppInfo.Version), "Release version is populated");
    Console.WriteLine("Release screenshots captured with saved user defaults; previous settings backed up in artifacts.");
}
CaptureScene("glow-off", 60, new SaverSettings { Glow = 0, GlowSpread = 65 });
CaptureScene("glowing-crt", 60, new SaverSettings { Glow = 100, GlowSpread = 65 });
CaptureScene("glowing-stars", 0, new SaverSettings { Glow = 85, GlowSpread = 65, Programs = ProgramSelection.Stars }, true);
using (var plain = SkiaSharp.SKBitmap.Decode(Path.Combine(output, "glow-off.png")))
using (var glowing = SkiaSharp.SKBitmap.Decode(Path.Combine(output, "glowing-crt.png")))
{
    int changed = plain.Pixels.Zip(glowing.Pixels).Count(pair => pair.First != pair.Second);
    Check(changed > 10000, "Glow produces a visible halo on the rendered screen");
    int haloPixels = 0;
    for (int y = 60; y < 300; y++)
    for (int x = 70; x < 890; x++)
    {
        var a = plain.GetPixel(x, y); var b = glowing.GetPixel(x, y);
        if (a.Red < 90 && a.Green < 90 && b.Red + b.Green + b.Blue > a.Red + a.Green + a.Blue + 8) haloPixels++;
    }
    Check(haloPixels > 500, "Bloom spreads light into dark pixels around the text");
}
var glowDialog = new SettingsWindow(new ConfigManager(configPath) { Settings = new SaverSettings() });
glowDialog.FindControl<ComboBox>("PresetCombo")!.SelectedIndex = 4;
Check(glowDialog.FindControl<Slider>("GlowSlider")!.Value == 85 &&
    glowDialog.FindControl<Slider>("GlowSpreadSlider")!.Value == 65 &&
    glowDialog.FindControl<Slider>("FlickerSlider")!.Value == 0, "Glowing CRT preset has broad stable bloom");
glowDialog.FindControl<TabControl>("SettingsTabs")!.SelectedIndex = 1;
glowDialog.Height = 1000;
Capture(glowDialog, "glowing-settings");
Console.WriteLine("PASS: broad bloom, dark-pixel halo and Glowing CRT preset.");

CompanionUi();

void CompanionUi()
{
var companionConfig = new ConfigManager(Path.Combine(Path.GetTempPath(), "retrosaver-test-" + Guid.NewGuid() + ".json"));
var companion = new CompanionWindow(companionConfig);
companion.Show();
Dispatcher.UIThread.RunJobs();
var controls = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(companion).OfType<Control>().ToArray();
var systemChoice = controls.OfType<ComboBox>().Single(c => c.Name == "SystemSelection");
systemChoice.SelectedIndex = 1;
controls.OfType<TextBox>().Single(c => c.Name == "ScrollerText").Text = "LONG SCROLLER " + new string('X', 150);
controls.OfType<Slider>().Single(c => c.Name == "ScrollerSpeed").Value = 80;
controls.OfType<ComboBox>().Single(c => c.Name == "AmigaBoot").SelectedIndex = (int)AmigaBootStyle.Purple31;
controls.OfType<CheckBox>().Single(c => c.Name == "DiskActivity").IsChecked = false;
controls.OfType<CheckBox>().Single(c => c.Name == "ShowScroller").IsChecked = false;
controls.OfType<ComboBox>().Single(c => c.Name == "AmigaDemo").SelectedIndex = 1;
controls.OfType<ComboBox>().Single(c => c.Name == "RenderQuality").SelectedIndex = 1;
controls.OfType<NumericUpDown>().Single(c => c.Name == "AmigaDuration").Value = 20;
Dispatcher.UIThread.RunJobs();
controls.OfType<Button>().Single(c => c.Name == "SaveSettings").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
Check(!controls.OfType<TextBox>().Single(c => c.Name == "ScrollerText").IsEnabled, "Hidden scrolling text disables its editor");
var reloadedCompanion = new ConfigManager(companionConfig.ConfigPath).Settings;
Check(reloadedCompanion.AmigaMode && !reloadedCompanion.ShowAmigaScroller && reloadedCompanion.ScrollerText.Length > 150 && reloadedCompanion.ScrollerSpeed == 80 && reloadedCompanion.AmigaBoot == AmigaBootStyle.Purple31 && !reloadedCompanion.AmigaDiskActivity && reloadedCompanion.AmigaAnimation == AmigaDemo.Runner && reloadedCompanion.AmigaDurationSeconds == 20 && reloadedCompanion.NativeRenderQuality == 1, "Companion controls persist Amiga selection and long text");
companion.Close();
Capture(new CompanionWindow(new ConfigManager(companionConfig.ConfigPath) { Settings = new SaverSettings { AmigaMode = true } }), "retrosaver-companion");
CaptureScene("amiga-plasma-scroller", 180, new SaverSettings { AmigaMode = true, AmigaBoot = AmigaBootStyle.None, AmigaAnimation = AmigaDemo.Plasma });
File.Delete(companionConfig.ConfigPath);
Console.WriteLine("PASS: companion startup, direct system launch, Amiga animation and editable scrolling text persistence.");
}
