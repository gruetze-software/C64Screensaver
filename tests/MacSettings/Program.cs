using C64Screensaver;
if (!OperatingSystem.IsMacOS()) throw new Exception("Run on macOS.");
var config = new ConfigManager();
if (config.LastError != null) throw new Exception(config.LastError);
if (args is ["write"])
{
    config.Settings = new SaverSettings { AmigaMode = true, AmigaBoot = AmigaBootStyle.None,
        AmigaAnimation = AmigaDemo.Runner, Glow = 37, MaskStrength = 21, Scanlines = false,
        KeepAspectRatio = null, FillScreen = true, ScrollerText = "COMPANION TO SYSTEM",
        Flicker = 7, Softness = 14 };
    if (!config.SaveSettings()) throw new Exception(config.LastError);
}
else if (args is ["read"])
{
    var s = config.Settings;
    if (s.AmigaMode || s.Glow != 73 || s.MaskStrength != 21 || s.Flicker != 7 || s.Softness != 14
        || s.ScrollerText != "SYSTEM TO COMPANION" || !s.FillScreen || s.UsesFourThree)
        throw new Exception("Native save did not round-trip through the companion store.");
}
else throw new Exception("Use write or read.");
Console.WriteLine("PASS: companion shared settings " + args[0]);
