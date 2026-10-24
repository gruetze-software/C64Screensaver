using System;
using System.Collections.Generic;
using System.Globalization;
namespace C64Screensaver;
// Independently described fixed 8x8 glyphs; no C64 ROM or host font required.
internal static class PixelCharacters
{
    private static readonly Dictionary<byte, byte[]> Glyphs = Create();
    public static byte[]? Rows(byte character) => Glyphs.GetValueOrDefault(character);
    private static Dictionary<byte, byte[]> Create()
    {
        const string data = """
A:183C667E66666600
B:7C66667C66667C00
C:3C66606060663C00
D:786C6666666C7800
E:7E60607860607E00
F:7E60607860606000
G:3C66606E66663C00
H:6666667E66666600
I:3C18181818183C00
J:1E0C0C0C0C6C3800
K:666C7870786C6600
L:6060606060607E00
M:63777F6B63636300
N:66767E7E6E666600
O:3C66666666663C00
P:7C66667C60606000
Q:3C6666666E3C0E00
R:7C66667C786C6600
S:3C66603C06663C00
T:7E18181818181800
U:6666666666663C00
V:66666666663C1800
W:6363636B7F776300
X:66663C183C666600
Y:6666663C18181800
Z:7E060C1830607E00
0:3C666E7666663C00
1:1838181818187E00
2:3C66060C30607E00
3:3C66061C06663C00
4:0C1C3C6C7E0C0C00
5:7E607C0606663C00
6:3C66607C66663C00
7:7E66060C18181800
8:3C66663C66663C00
9:3C66663E06663C00
.:0000000000181800
,:0000000000181830
;:0018180000181830
=:00007E007E000000
<:060C1830180C0600
>:6030180C18306000
+:0018187E18180000
-:0000007E00000000
*:00663C7E3C660000
(:0C18303030180C00
):30180C0C0C183000
$:183E603C067C1800
":6666240000000000
?:3C66060C18001800
':1818100000000000
::0018180018180000
!:1818181818001800
@:3C666E6E60663C00
&:386C386ECCCC7600
_:0000000000007E00
/:00060C1830600000
""";
        var result = new Dictionary<byte, byte[]>();
        foreach (var line in data.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = line.Trim();
            var rows = new byte[8];
            for (int y = 0; y < 8; y++)
                rows[y] = byte.Parse(trimmed.AsSpan(2 + y * 2, 2), NumberStyles.HexNumber);
            result[(byte)trimmed[0]] = rows;
        }
        var slash = new byte[8];
        var backslash = new byte[8];
        for (int y = 0; y < 8; y++)
        {
            slash[y] = (byte)(1 << y);
            backslash[y] = (byte)(128 >> y);
        }
        result[205] = slash;
        result[206] = backslash;
        return result;
    }
}