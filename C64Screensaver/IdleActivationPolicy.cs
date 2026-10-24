using System;

namespace C64Screensaver;

public enum IdleAction { None, Start, Stop }
public readonly record struct IdleSample(long Milliseconds, bool Locked, bool Inhibited);
public sealed class IdleActivationPolicy
{
    private long? previous;
    private bool armed;
    public IdleAction Evaluate(IdleSample? sample, bool enabled, int delaySeconds, bool running)
    {
        if (sample is not { } current)
        {
            previous = null;
            armed = false;
            return running ? IdleAction.Stop : IdleAction.None;
        }
        bool activity = current.Milliseconds < 2000 ||
            (previous.HasValue && current.Milliseconds + 1000 < previous.Value);
        if (!previous.HasValue) armed = current.Milliseconds < delaySeconds * 1000L;
        previous = current.Milliseconds;
        if (activity) armed = true;
        if (!enabled || current.Locked || current.Inhibited || activity)
            return running ? IdleAction.Stop : IdleAction.None;
        if (!running && armed && current.Milliseconds >= delaySeconds * 1000L)
        {
            armed = false;
            return IdleAction.Start;
        }
        return IdleAction.None;
    }
}
