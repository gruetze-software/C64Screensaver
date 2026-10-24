using System;
using System.Diagnostics;
using System.IO;


using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace C64Screensaver;

public static class LinuxAutostart
{
    public const string FileName = "c64screensaver-idle.desktop";
    public static ProcessStartInfo StartInfo(params string[] arguments)
    {
        string executable = Environment.ProcessPath ?? throw new IOException("Application path is unavailable.");
        var result = new ProcessStartInfo(executable) { UseShellExecute = false };
        if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            result.ArgumentList.Add(typeof(LinuxAutostart).Assembly.Location);
        foreach (string argument in arguments) result.ArgumentList.Add(argument);
        return result;
    }
    public static string DesktopQuote(string value)
    {
        if (value.Contains('\n') || value.Contains('\r') || value.Contains('\0'))
            throw new IOException("Application path contains an unsupported character.");
        // Desktop-file escaping is separate from shell and JSON escaping.
        return "\"" + value.Replace("\\", "\\\\\\\\").Replace("\"", "\\\\\"")
            .Replace("$", "\\\\$").Replace(((char)96).ToString(), "\\\\" + (char)96)
            .Replace("%", "%%") + "\"";
    }
    public static string BuildEntry(ProcessStartInfo command)
    {
        string exec = DesktopQuote(command.FileName);
        foreach (string arg in command.ArgumentList) exec += " " + DesktopQuote(arg);
        return "[Desktop Entry]\nType=Application\nName=C64Screensaver automatic activation\n" +
            "Comment=Show the C64 animation after inactivity; the desktop retains its screen lock\n" +
            "Exec=" + exec + "\nTerminal=false\nX-GNOME-Autostart-enabled=true\n";
    }
    public static string EntryPath
    {
        get
        {
            string? snapData = Environment.GetEnvironmentVariable("SNAP_USER_DATA");
            string config = !string.IsNullOrEmpty(snapData) ? Path.Combine(snapData, ".config") :
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(config, "autostart", FileName);
        }
    }
    public static void Apply(bool enabled)
    {
        string target = EntryPath;
        if (!enabled) { if (File.Exists(target)) File.Delete(target); return; }
        var command = StartInfo("--idle-daemon");
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SNAP")))
        {
            // snap userd maps this to the app wrapper declared by autostart.
            command = new ProcessStartInfo("c64screensaver");
            command.ArgumentList.Add("--idle-daemon");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string temporary = target + ".tmp";
        File.WriteAllText(temporary, BuildEntry(command));
        File.Move(temporary, target, true);
        using var process = Process.Start(StartInfo("--idle-daemon"));
    }
}

public static class LinuxIdleDaemon
{
    public static async Task<int> RunAsync(bool diagnose)
    {
        if (!OperatingSystem.IsLinux())
        { Console.Error.WriteLine("Automatic idle activation is available on Linux only."); return 2; }
        var probe = new LinuxSessionProbe();
        if (diagnose)
        {
            var result = await probe.ReadAsync();
            Console.WriteLine(result.Message);
            return result.Sample.HasValue ? 0 : 3;
        }
        var config = new ConfigManager();
        string directory = Path.GetDirectoryName(config.ConfigPath)!;
        Directory.CreateDirectory(directory);
        FileStream singleton;
        try { singleton = new FileStream(Path.Combine(directory, "idle-daemon.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException) { return 0; }
        using (singleton)
        using (var cancellation = new CancellationTokenSource())
        using (var terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => { context.Cancel = true; cancellation.Cancel(); }))
        using (var interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, context => { context.Cancel = true; cancellation.Cancel(); }))
        {
            var policy = new IdleActivationPolicy();
            Process? animation = null;
            string? lastMessage = null;
            void Stop()
            {
                if (animation == null) return;
                try { if (!animation.HasExited) { animation.Kill(); animation.WaitForExit(2000); } }
                catch (InvalidOperationException) { }
                animation.Dispose();
                animation = null;
            }
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    config.LoadSettings();
                    if (!config.Settings.AutoStartEnabled) break;
                    var result = config.LastError == null ? await probe.ReadAsync() :
                        new LinuxProbeResult(null, "Settings could not be read; automatic activation is paused.");
                    bool running = animation != null && !animation.HasExited;
                    switch (policy.Evaluate(result.Sample, true, config.Settings.IdleDelaySeconds, running))
                    {
                        case IdleAction.Stop: Stop(); break;
                        case IdleAction.Start:
                            Stop();
                            try { animation = Process.Start(LinuxAutostart.StartInfo("--fullscreen")); }
                            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
                            { result = new(null, "Could not start animation: " + ex.Message); }
                            break;
                    }
                    if (lastMessage != result.Message && (result.Sample == null || lastMessage == null))
                        Console.WriteLine(result.Message);
                    lastMessage = result.Message;
                    await Task.Delay(1000, cancellation.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally { Stop(); }
        }
        return 0;
    }
}
