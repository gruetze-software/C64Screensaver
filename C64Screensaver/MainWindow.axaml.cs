using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace C64Screensaver;

public partial class MainWindow : Window
{
    public bool IsPreviewMode { get; }
    public IntPtr ParentWindowHandle { get; }
    private readonly bool _windowed;
    private readonly SaverSettings _settings;
    private readonly Random _random;
    private readonly Action<TimeSpan> _frameCallback;
    private readonly FrameStatistics _frameTimes = new();
    private TimeSpan? _lastFrame;
    private double _nextFrame;
    private double _fpsElapsed;
    private int _frameCount;
    private Point? _initialMouse;
    private readonly System.Diagnostics.Stopwatch _opening = new();
    private bool _running;

    public MainWindow() : this(false, IntPtr.Zero) { }

    public MainWindow(bool isPreview, IntPtr? parentHandle, bool windowed = false, SaverSettings? settings = null, Random? random = null)
    {
        IsPreviewMode = isPreview;
        ParentWindowHandle = parentHandle ?? IntPtr.Zero;
        _windowed = windowed;
        _settings = settings ?? new ConfigManager().Settings;
        _random = random ?? Random.Shared;
        _frameCallback = Animate;
        _settings.Validate();
        InitializeComponent();
        FpsDisplay.IsVisible = _settings.ShowFps;
        if (isPreview)
        {
            WindowState = WindowState.Normal;
            Topmost = false;
            ShowInTaskbar = false;
            Width = 160;
            Height = 120;
        }
        else if (windowed)
        {
            WindowState = WindowState.Normal;
            WindowDecorations = WindowDecorations.Full;
            Topmost = false;
            Width = 960;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        else
        {
            // AppKit cannot toggle a borderless, non-resizable style into native
            // fullscreen. BorderOnly supplies its required native style bits.
            if (OperatingSystem.IsMacOS())
            {
                WindowDecorations = WindowDecorations.BorderOnly;
                Topmost = false;
                CanResize = CanMaximize = true;
            }
            // Position is set by App before entering fullscreen.
            WindowState = WindowState.Normal;
            Cursor = new Cursor(StandardCursorType.None);
            ShowInTaskbar = false;
        }

        PointerMoved += (_, e) =>
        {
            if (IsPreviewMode || _windowed || _opening.Elapsed.TotalSeconds < 1) return;
            Point current = e.GetPosition(this);
            if (_initialMouse is not { } initial) { _initialMouse = current; return; }
            double dx = current.X - initial.X, dy = current.Y - initial.Y;
            if (dx * dx + dy * dy > 100) Close();
        };
        PointerPressed += (_, _) => { if (!IsPreviewMode && !_windowed && _opening.Elapsed.TotalSeconds >= 1) Close(); };
        KeyDown += (_, e) =>
        {
            if (IsPreviewMode) return;
            if (e.Key == Key.F)
            {
                FpsDisplay.IsVisible = !FpsDisplay.IsVisible;
                _frameCount = 0;
                _fpsElapsed = 0;
                _frameTimes.Reset();
            }
            else if (!_windowed || e.Key == Key.Escape) Close();
        };
    }

    private void InitializeObjects()
    {
        double width = IsPreviewMode || Bounds.Width <= 0 ? Width : Bounds.Width;
        double height = IsPreviewMode || Bounds.Height <= 0 ? Height : Bounds.Height;
        Scene.Initialize(_settings, IsPreviewMode, width, height, _random);
    }

    private void Animate(TimeSpan time)
    {
        if (!_running) return;
        if (WindowState == WindowState.Minimized)
        {
            _lastFrame = time;
            RequestAnimationFrame(_frameCallback);
            return;
        }
        double elapsed = _lastFrame.HasValue ? Math.Max((time - _lastFrame.Value).TotalSeconds, 0) : 0;
        if (_lastFrame.HasValue && time.TotalSeconds + .000001 < _nextFrame)
        {
            RequestAnimationFrame(_frameCallback);
            return;
        }
        _nextFrame = time.TotalSeconds - _nextFrame > 1.0 / 60 ? time.TotalSeconds + 1.0 / 60 : _nextFrame + 1.0 / 60;
        double delta = Math.Min(elapsed, 0.1);
        _lastFrame = time;
        AdvanceScene(delta);
        if (FpsDisplay.IsVisible && delta > 0)
        {
            _frameCount++;
            _fpsElapsed += elapsed;
            _frameTimes.Add(elapsed);
            if (_fpsElapsed >= 0.5)
            {
                var timing = _frameTimes.Snapshot();
                FpsDisplay.Text = FormattableString.Invariant(
                    $"FPS: {_frameCount / _fpsElapsed:F1}  |  frame p95: {timing.P95Ms:F1} ms  |  max: {timing.MaxMs:F1} ms");
                _frameCount = 0;
                _fpsElapsed = 0;
            }
        }
        RequestAnimationFrame(_frameCallback);
    }

    internal void AdvanceScene(double seconds)
    {
        Scene.Advance(Bounds.Width, Bounds.Height, seconds);
        FpsDisplay.Opacity = Scene.IsPowerTransitionActive ? 0 : 1;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _opening.Restart();
        if (IsPreviewMode)
        {
            if (!OperatingSystem.IsWindows() || ParentWindowHandle == IntPtr.Zero || !IsWindow(ParentWindowHandle))
            { Close(); return; }
            var handle = TryGetPlatformHandle();
            if (handle == null || !GetClientRect(ParentWindowHandle, out var rect)) { Close(); return; }
            int style = GetWindowLong(handle.Handle, -16);
            SetWindowLong(handle.Handle, -16, (style & ~unchecked((int)0x80000000)) | 0x40000000);
            SetParent(handle.Handle, ParentWindowHandle);
            SetWindowPos(handle.Handle, IntPtr.Zero, 0, 0, rect.Right, rect.Bottom, 0x0014);
            Width = rect.Right / RenderScaling;
            Height = rect.Bottom / RenderScaling;
        }
        else if (!_windowed)
        {
            if (OperatingSystem.IsMacOS())
            {
                // AppKit temporarily suppresses fullscreen during Show. Request it
                // after the native window has actually become visible/active.
                Topmost = false;
                CanResize = CanMaximize = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (IsVisible) { Activate(); WindowState = WindowState.FullScreen; }
                }, Avalonia.Threading.DispatcherPriority.Loaded);
            }
            else WindowState = WindowState.FullScreen;
        }
        InitializeObjects();
        _running = true;
        RequestAnimationFrame(_frameCallback);
    }

    protected override void OnClosed(EventArgs e)
    {
        _running = false;
        Scene.Clear();
        base.OnClosed(e);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out NativeRect rect);
    [DllImport("user32.dll")] private static extern IntPtr SetParent(IntPtr child, IntPtr parent);
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int index);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int index, int value);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after,
        int x, int y, int width, int height, uint flags);
}
