using System;

namespace C64Screensaver;

// Fixed-size buffers: recording frame times never allocates.
internal sealed class FrameStatistics
{
    private readonly double[] _samples = new double[120];
    private readonly double[] _sorted = new double[120];
    private int _count, _next;

    public void Add(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0) return;
        _samples[_next] = seconds * 1000;
        _next = (_next + 1) % _samples.Length;
        _count = Math.Min(_count + 1, _samples.Length);
    }

    public (double P95Ms, double MaxMs) Snapshot()
    {
        if (_count == 0) return (0, 0);
        Array.Copy(_samples, _sorted, _count);
        Array.Sort(_sorted, 0, _count);
        return (_sorted[(int)Math.Ceiling(_count * 0.95) - 1], _sorted[_count - 1]);
    }

    public void Reset() { _count = 0; _next = 0; }
}
