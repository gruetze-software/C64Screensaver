using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace C64Screensaver.Controls;

// Same font-independent vector approach as BingWallE's support button.
public sealed class ButtonIcons : AvaloniaObject
{
    public static readonly AttachedProperty<Geometry?> IconProperty =
        AvaloniaProperty.RegisterAttached<ButtonIcons, Button, Geometry?>("Icon");
    public static Geometry? GetIcon(Button button) => button.GetValue(IconProperty);
    public static void SetIcon(Button button, Geometry? value) => button.SetValue(IconProperty, value);

    public static Geometry Coffee { get; } = Geometry.Parse("M3 7 H17 V14 A6 6 0 0 1 5 14 V7 M17 8 H19 A3 3 0 0 1 19 14 H17 M3 21 H19 M8 2 V4 M12 2 V4");
    public static Geometry Save { get; } = Geometry.Parse("M4 3 H17 L21 7 V21 H3 V3 Z M7 3 V9 H16 V3 M7 21 V14 H17 V21");
    public static Geometry Close { get; } = Geometry.Parse("M6 6 L18 18 M18 6 L6 18");
    public static Geometry Reset { get; } = Geometry.Parse("M4 10 A8 8 0 1 1 5 18 M4 4 V10 H10");
    public static Geometry Info { get; } = Geometry.Parse("M12 22 A10 10 0 1 0 12 2 A10 10 0 1 0 12 22 M12 11 V17 M12 7 V7.5");
    public static Geometry External { get; } = Geometry.Parse("M14 3 H21 V10 M21 3 L10 14 M10 3 H4 V21 H21 V14");
    public static Geometry Code { get; } = Geometry.Parse("M8 6 L2 12 L8 18 M16 6 L22 12 L16 18 M14 3 L10 21");
}
