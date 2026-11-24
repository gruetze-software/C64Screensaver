using System;
using System.Diagnostics;
using System.IO;
namespace C64Screensaver;

// The system screensaver must outlive a versioned MSIX installation directory.
// Export only on the user's explicit Install button; Windows owns activation.
public static class WindowsScreensaver
{
    public static string Prepare()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GruetzeRetroSaver", "ScreenSavers");
        return Export(AppContext.BaseDirectory, root);
    }
    public static string Export(string source, string root)
    {
        source = Path.GetFullPath(source);
        root = Path.GetFullPath(root);
        if (root.StartsWith(source.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || string.Equals(root.TrimEnd(Path.DirectorySeparatorChar), source.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Screensaver export must be outside the application folder.");
        if (!File.Exists(Path.Combine(source, "c64screensaver.exe")) || !File.Exists(Path.Combine(source, "c64screensaver.runtimeconfig.json")))
            throw new IOException("The complete published Windows application is required.");
        // Unique immutable exports avoid changing files while an older saver runs.
        string folder = Path.Combine(root, AppInfo.Version + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(folder);
        CopyDirectory(source, folder);
        string screensaver = Path.Combine(folder, "Gruetze RetroSaver.scr");
        // A .NET apphost must keep its embedded managed assembly name; copying
        // it to an .scr is supported by the same host used in ZIP releases.
        File.Copy(Path.Combine(folder, "c64screensaver.exe"), screensaver);
        File.WriteAllText(Path.Combine(folder, "RETROSAVER-EXPORT.txt"), "System screensaver export. To remove: select another saver in Windows, close running previews, then delete this folder.\n");
        return screensaver;
    }
    private static void CopyDirectory(string source, string destination)
    {
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0) throw new IOException("Linked folders are not allowed in the screensaver payload.");
        foreach (string file in Directory.GetFiles(source))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new IOException("Linked files are not allowed in the screensaver payload.");
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }
        foreach (string directory in Directory.GetDirectories(source))
        {
            string target = Path.Combine(destination, Path.GetFileName(directory));
            Directory.CreateDirectory(target); CopyDirectory(directory, target);
        }
    }
    public static void OpenInstaller(string path)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        var info = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "rundll32.exe")) { UseShellExecute = true };
        info.ArgumentList.Add("desk.cpl,InstallScreenSaver"); info.ArgumentList.Add(path);
        Process.Start(info);
    }
}
