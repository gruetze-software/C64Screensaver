using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
namespace C64Screensaver;
internal sealed class SpriteAssets : IDisposable
{
    private static SpriteAssets? instance;
    public static SpriteAssets Instance => instance ??= new();
    public Bitmap Logo { get; } = new(AssetLoader.Open(new Uri("avares://C64Screensaver/Assets/app-icon.png")));
    public Bitmap CompanyLogo { get; } = new(AssetLoader.Open(new Uri("avares://C64Screensaver/Assets/logogs.png")));
    public void Dispose() { Logo.Dispose(); CompanyLogo.Dispose(); }
    public static void Release() { instance?.Dispose(); instance = null; }
}
