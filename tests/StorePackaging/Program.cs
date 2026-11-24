using C64Screensaver;
string test = Path.Combine(Path.GetFullPath("artifacts/store-packaging-checks"), Guid.NewGuid().ToString("N"));
string source = Path.Combine(test, "source"), output = Path.Combine(test, "exports");
Directory.CreateDirectory(source);
File.WriteAllText(Path.Combine(source, "c64screensaver.exe"), "fake-apphost");
File.WriteAllText(Path.Combine(source, "c64screensaver.runtimeconfig.json"), "{}");
Directory.CreateDirectory(Path.Combine(source, "nested"));
File.WriteAllText(Path.Combine(source, "nested", "resource.bin"), "payload");
string a = WindowsScreensaver.Export(source, output), b = WindowsScreensaver.Export(source, output);
if (a == b || File.ReadAllText(a) != "fake-apphost" || File.ReadAllText(Path.Combine(Path.GetDirectoryName(a)!, "nested", "resource.bin")) != "payload") throw new Exception("Export is incomplete or overwrites an earlier version");
try { WindowsScreensaver.Export(source, Path.Combine(source, "child")); throw new Exception("Nested export accepted"); } catch (IOException) { }
try { WindowsScreensaver.Export(source, source); throw new Exception("Self export accepted"); } catch (IOException) { }
File.Delete(Path.Combine(source, "c64screensaver.runtimeconfig.json"));
try { WindowsScreensaver.Export(source, output); throw new Exception("Incomplete publish accepted"); } catch (IOException) { }
Console.WriteLine("Store screensaver export checks passed. No system settings or registration changed.");

if (args.Length == 1 && OperatingSystem.IsWindows())
{
    string saver = WindowsScreensaver.Export(Path.GetFullPath(args[0]), output);
    var info = new System.Diagnostics.ProcessStartInfo(saver) { RedirectStandardOutput = true, UseShellExecute = false };
    info.ArgumentList.Add("--help");
    using var process = System.Diagnostics.Process.Start(info)!;
    string text = process.StandardOutput.ReadToEnd();
    if (!process.WaitForExit(15000)) { process.Kill(); throw new Exception("Exported apphost did not exit"); }
    if (process.ExitCode != 0 || !text.Contains("Gruetze RetroSaver")) throw new Exception("Exported .scr cannot find its managed runtime");
    Console.WriteLine("Real exported .scr apphost starts with --help; Windows registration unchanged.");
}
