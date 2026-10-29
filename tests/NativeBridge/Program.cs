using System;
using C64Screensaver.NativeCore;

unsafe
{
    delegate* unmanaged[Cdecl]<int> version = &Exports.Version;
    delegate* unmanaged[Cdecl]<byte*, int, int, ulong> create = &Exports.Create;
    delegate* unmanaged[Cdecl]<ulong, byte*, int, int, int, int, double, int> render = &Exports.Render;
    delegate* unmanaged[Cdecl]<ulong, int> power = &Exports.TestPower;
    delegate* unmanaged[Cdecl]<ulong, void> destroy = &Exports.Destroy;
    void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    Check(version() == 1, "ABI version");
    byte[] invalid = [123];
    fixed (byte* p = invalid) Check(create(p, invalid.Length, 1) == 0, "Reject malformed JSON");
    Check(create(null, 1, 1) == 0, "Reject null JSON");
    ulong first = create(null, 0, 123), second = create(null, 0, 456);
    Check(first != 0 && second != 0 && first != second, "Independent sessions");
    const int w = 384, h = 288, stride = w * 4 + 16;
    byte[] buffer = new byte[stride * h];
    Array.Fill(buffer, (byte)0xCD);
    fixed (byte* p = buffer)
    {
        Check(render(first, p, buffer.Length - 1, w, h, stride, 0) == 0, "Reject short buffer");
        Check(render(first, p, buffer.Length, w, h, w * 4 - 1, 0) == 0, "Reject short stride");
        Check(render(first, p, buffer.Length, w, h, stride, double.NaN) == 0, "Reject invalid time");
        Check(Array.TrueForAll(buffer, b => b == 0xCD), "Validation leaves buffer untouched");
        Check(render(first, p, buffer.Length, w, h, stride, 0) == 1, "Render boot screen");
        Check(Array.Exists(buffer, b => b != 0xCD), "Frame contains pixels");
        for (int y = 0; y < h; y++)
            for (int x = w * 4; x < stride; x++) Check(buffer[y * stride + x] == 0xCD, "Stride padding preserved");
        for (int i = 0; i < 320; i++) Check(render(first, p, buffer.Length, w, h, stride, .25) == 1, "Typing and RUN");
        Check(render(second, p, buffer.Length, w, h, stride, 0) == 1, "Second view still renders");
        Check(power(first) == 1, "Start power test");
        Check(render(first, p, buffer.Length, w, h, stride, .12) == 1, "Render white flash");
        Check(buffer[(h / 2) * stride + (w / 2) * 4] == 255, "White power flash");
        Check(render(first, p, buffer.Length, w, h, stride, .25) == 1, "Collapse vertical beam");
        Check(render(first, p, buffer.Length, w, h, stride, .25) == 1, "Collapse horizontal beam");
        Check(render(first, p, buffer.Length, w, h, stride, .12) == 1, "Render central dot");
        Check(buffer[0] == 0 && buffer[(h / 2) * stride + (w / 2) * 4] > 0, "Dot against black screen");
        for (int i = 0; i < 4; i++) Check(render(first, p, buffer.Length, w, h, stride, .25) == 1, "Power-on reveal");
        destroy(first);
        destroy(first);
        Check(render(first, p, buffer.Length, w, h, stride, 0) == 0, "Destroyed handle rejected");
        Check(render(second, p, buffer.Length, w, h, stride, 0) == 1, "Other session survives destruction");
    }
    destroy(second);
    Console.WriteLine("Native bridge: ABI, JSON, bounds, stride, typing, independent sessions and disposal passed.");
}

unsafe
{
    delegate* unmanaged[Cdecl]<byte*, int, int, ulong> createAmiga = &Exports.Create;
    delegate* unmanaged[Cdecl]<ulong, byte*, int, int, int, int, double, int> renderAmiga = &Exports.Render;
    delegate* unmanaged[Cdecl]<ulong, void> destroyAmiga = &Exports.Destroy;
    byte[] json = System.Text.Encoding.UTF8.GetBytes("""{"AmigaMode":true,"AmigaBoot":0,"ScrollerText":"GRUETZE RETROSAVER","CrtEnabled":false}""");
    ulong id;
    fixed (byte* p = json) id = createAmiga(p, json.Length, 42);
    if (id == 0) throw new Exception("Native Amiga session failed");
    byte[] before = new byte[384 * 288 * 4], after = new byte[before.Length];
    fixed (byte* a = before)
    fixed (byte* b = after)
    {
        if (renderAmiga(id, a, before.Length, 384, 288, 384 * 4, 0) != 1 ||
            renderAmiga(id, b, after.Length, 384, 288, 384 * 4, .25) != 1)
            throw new Exception("Native Amiga render failed");
    }
    if (System.Linq.Enumerable.SequenceEqual(before, after)) throw new Exception("Native Amiga frame did not advance");
    destroyAmiga(id);
    Console.WriteLine("Native Amiga: animated frames through the shared ABI passed.");
}

unsafe
{
    delegate* unmanaged[Cdecl]<byte*, int, int, ulong> create = &Exports.Create;
    delegate* unmanaged[Cdecl]<ulong, byte*, int, int, int, int, double, int> render = &Exports.Render;
    delegate* unmanaged[Cdecl]<ulong, void> destroy = &Exports.Destroy;
    foreach (int boot in new[] { 1, 2, 3 })
    {
        byte[] json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { AmigaMode = true, AmigaBoot = boot, AmigaDiskActivity = false, CrtEnabled = false, PowerTransition = false });
        ulong id;
        fixed (byte* p = json) id = create(p, json.Length, 42);
        if (id == 0) throw new Exception("Original Kickstart native session failed");
        byte[] before = new byte[384 * 288 * 4], after = new byte[before.Length];
        fixed (byte* a = before)
        fixed (byte* b = after)
        {
            if (render(id, a, before.Length, 384, 288, 384 * 4, 0) != 1) throw new Exception("Original Kickstart resource missing");
            for (int i = 0; i < 16; i++)
                if (render(id, b, after.Length, 384, 288, 384 * 4, .25) != 1) throw new Exception("Original Kickstart frame failed");
        }
        bool equal = System.Linq.Enumerable.SequenceEqual(before, after);
        if ((boot == 1) != equal) throw new Exception("Original static/animated native playback mismatch");
        destroy(id);
    }
    Console.WriteLine("Native Kickstart: all three embedded captures and original static/animated playback passed.");
}
