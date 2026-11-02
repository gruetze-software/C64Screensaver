using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace C64Screensaver;

// ConfigManager uses atomic System.IO writes. Avalonia 12.1.4 activates scopes
// around its own storage operations only; holding an IStorageFolder is insufficient.
// Resolve our own security-scoped bookmark and balance its lifetime explicitly.
internal static class StoreSettingsAccess
{
#if MAC_APP_STORE
    public static bool Required => OperatingSystem.IsMacOS();
#else
    public static bool Required => false;
#endif
    private static IntPtr scope;
    public static bool Granted => !Required || scope != IntPtr.Zero;
    private static string BookmarkPath {
        get {
            var buffer = new byte[4096];
            if (!CFStringGetCString(NSHomeDirectory(), buffer, buffer.Length, 0x08000100))
                throw new IOException("Could not locate the app container.");
            int length = Array.IndexOf(buffer, (byte)0);
            return Path.Combine(Encoding.UTF8.GetString(buffer, 0, length), "Library", "Application Support",
                "GruetzeRetroSaver", "shared-folder.bookmark");
        }
    }

    public static bool IsSettingsFolder(string? path) => path != null &&
        (Path.TrimEndingDirectorySeparator(path) == "/Users/Shared" ||
         Path.TrimEndingDirectorySeparator(path) == Path.GetDirectoryName(ConfigManager.DefaultConfigPath));

    public static async Task<bool> RestoreAsync(Window owner)
    {
        if (Granted) return true;
        if (!File.Exists(BookmarkPath)) return false;
        Activate(Convert.FromBase64String(await File.ReadAllTextAsync(BookmarkPath)));
        return true;
    }

    public static async Task<bool> ChooseAsync(Window owner)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
            Title = "Choose Shared (Macintosh HD → Users → Shared)", AllowMultiple = false });
        if (folders.Count == 0) return false;
        using var folder = folders[0];
        string? path = folder.TryGetLocalPath();
        if (!IsSettingsFolder(path))
            throw new IOException("Choose /Users/Shared, or your GruetzeRetroSaver settings folder inside it.");
        IntPtr text = CFStringCreateWithCString(IntPtr.Zero, path!, 0x08000100);
        IntPtr url = CFURLCreateWithFileSystemPath(IntPtr.Zero, text, 0, 1);
        CFRelease(text);
        if (url == IntPtr.Zero) throw new IOException("Could not create the selected folder URL.");
        try { Activate(CreateBookmark(url)); }
        finally { CFRelease(url); }
        return true;
    }

    private static byte[] CreateBookmark(IntPtr url)
    {
        IntPtr data = CFURLCreateBookmarkData(IntPtr.Zero, url, 1 << 11, IntPtr.Zero, IntPtr.Zero, out var error);
        if (error != IntPtr.Zero) CFRelease(error);
        if (data == IntPtr.Zero) throw new IOException("macOS did not grant persistent folder access.");
        try {
            var bytes = new byte[checked((int)CFDataGetLength(data))];
            Marshal.Copy(CFDataGetBytePtr(data), bytes, 0, bytes.Length);
            return bytes;
        } finally { CFRelease(data); }
    }

    private static void Activate(byte[] bookmark)
    {
        IntPtr data = CFDataCreate(IntPtr.Zero, bookmark, bookmark.Length);
        IntPtr url;
        try { url = CFURLCreateByResolvingBookmarkData(IntPtr.Zero, data, (1 << 10) | (1 << 8),
            IntPtr.Zero, IntPtr.Zero, out _, out var error);
            if (error != IntPtr.Zero) CFRelease(error);
        } finally { CFRelease(data); }
        if (url == IntPtr.Zero) throw new IOException("Select the shared settings folder again.");
        bool active = false;
        try {
            var buffer = new byte[4096];
            if (!CFURLGetFileSystemRepresentation(url, 1, buffer, buffer.Length))
                throw new IOException("Could not resolve the settings folder.");
            int length = Array.IndexOf(buffer, (byte)0);
            if (length < 0 || !IsSettingsFolder(Encoding.UTF8.GetString(buffer, 0, length)))
                throw new IOException("The saved folder is not the shared settings location.");
            active = CFURLStartAccessingSecurityScopedResource(url);
            if (!active) throw new IOException("macOS denied access. Select the shared settings folder again.");
            // Refresh stale bookmarks while the resource is accessible.
            var refreshed = CreateBookmark(url);
            Directory.CreateDirectory(Path.GetDirectoryName(BookmarkPath)!);
            File.WriteAllText(BookmarkPath, Convert.ToBase64String(refreshed));
            Release();
            scope = url;
        }
        catch {
            if (active) CFURLStopAccessingSecurityScopedResource(url);
            CFRelease(url);
            throw;
        }
    }

    public static void Release()
    {
        if (scope == IntPtr.Zero) return;
        CFURLStopAccessingSecurityScopedResource(scope);
        CFRelease(scope);
        scope = IntPtr.Zero;
    }

    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation")] private static extern IntPtr NSHomeDirectory();
    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    [return: MarshalAs(UnmanagedType.I1)] private static extern bool CFStringGetCString(IntPtr text, byte[] buffer, nint size, uint encoding);
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    [DllImport(CoreFoundation)] private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, [MarshalAs(UnmanagedType.LPUTF8Str)] string text, uint encoding);
    [DllImport(CoreFoundation)] private static extern IntPtr CFURLCreateWithFileSystemPath(IntPtr allocator, IntPtr path, nint style, byte directory);
    [DllImport(CoreFoundation)] private static extern IntPtr CFURLCreateBookmarkData(IntPtr allocator, IntPtr url, nuint options, IntPtr properties, IntPtr relative, out IntPtr error);
    [DllImport(CoreFoundation)] private static extern IntPtr CFURLCreateByResolvingBookmarkData(IntPtr allocator, IntPtr data, nuint options, IntPtr relative, IntPtr properties, out byte stale, out IntPtr error);
    [DllImport(CoreFoundation)] private static extern IntPtr CFDataCreate(IntPtr allocator, byte[] bytes, nint length);
    [DllImport(CoreFoundation)] private static extern nint CFDataGetLength(IntPtr data);
    [DllImport(CoreFoundation)] private static extern IntPtr CFDataGetBytePtr(IntPtr data);
    [DllImport(CoreFoundation)] [return: MarshalAs(UnmanagedType.I1)] private static extern bool CFURLGetFileSystemRepresentation(IntPtr url, byte resolve, byte[] buffer, nint length);
    [DllImport(CoreFoundation)] [return: MarshalAs(UnmanagedType.I1)] private static extern bool CFURLStartAccessingSecurityScopedResource(IntPtr url);
    [DllImport(CoreFoundation)] private static extern void CFURLStopAccessingSecurityScopedResource(IntPtr url);
    [DllImport(CoreFoundation)] private static extern void CFRelease(IntPtr value);
}
