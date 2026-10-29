using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace C64Screensaver;

public sealed class CompanyInfoWindow : Window
{
    public CompanyInfoWindow()
    {
        Title = "Grütze-Software";
        Classes.Add("dialog");
        Width = 460;
        MinWidth = 380;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        using (var iconStream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://C64Screensaver/Assets/app-icon.ico")))
            Icon = new WindowIcon(iconStream);

        var content = new StackPanel { Spacing = 12, Margin = new Thickness(28) };
        content.Children.Add(new Image
        {
            Source = SpriteAssets.Instance.CompanyLogo,
            Width = 88, Height = 88, HorizontalAlignment = HorizontalAlignment.Center
        });
        content.Children.Add(new TextBlock
        {
            Text = "Grütze-Software", FontSize = 24, FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        content.Children.Add(new TextBlock
        {
            Text = AppInfo.DisplayName, HorizontalAlignment = HorizontalAlignment.Center
        });
        content.Children.Add(new TextBlock
        {
            Text = "© 2026 Grütze-Software", HorizontalAlignment = HorizontalAlignment.Center
        });
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Orange };
        Button Link(string label, string address)
        {
            var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Stretch };
            button.Click += async (_, _) =>
            {
                try
                {
                    status.Text = await Launcher.LaunchUriAsync(new Uri(address))
                        ? "" : "Could not open your browser: " + address;
                }
                catch (Exception) { status.Text = "Could not open your browser: " + address; }
            };
            return button;
        }
        content.Children.Add(Link("www.gruetze-software.de", AppInfo.CompanyUrl));
        var coffee = Link("Support on Ko-fi ☕", AppInfo.KofiUrl);
        coffee.Classes.Add("primary");
        content.Children.Add(coffee);
        content.Children.Add(status);
        var close = new Button
        {
            Content = "Close", IsDefault = true, IsCancel = true,
            HorizontalAlignment = HorizontalAlignment.Center, MinWidth = 110
        };
        close.Click += (_, _) => Close();
        content.Children.Add(close);
        Content = content;
    }
}
