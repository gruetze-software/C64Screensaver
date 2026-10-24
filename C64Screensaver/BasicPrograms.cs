namespace C64Screensaver;
public enum ProgramSelection { AllPrograms = 0, Alternating = AllPrograms, Maze = 1, Company = 2, Stars = 3 }
public static class BasicPrograms
{
    public static readonly string[] Maze =
    [
        "10 PRINT CHR$(147)",
        "20 T=TI+240",
        "30 POKE 646,INT(RND(1)*15)+1",
        "40 PRINT CHR$(205.5+RND(1));",
        "50 IF TI<T THEN 30",
        "60 POKE 53280,INT(RND(1)*16)",
        "70 T=TI+240",
        "80 GOTO 30"
    ];
    public static readonly string[] Stars =
    [
        "10 PRINT CHR$(147)",
        "20 T=TI+240",
        "30 P=INT(RND(1)*1000)",
        "40 POKE 1024+P,46",
        "45 POKE 55296+P,14",
        "50 IF RND(1)>0.15 THEN 70",
        "55 POKE 1024+P,42",
        "60 POKE 55296+P,1",
        "70 POKE 1024+INT(RND(1)*1000),32",
        "80 IF RND(1)>0.25 THEN 70",
        "90 IF TI<T THEN 30",
        "100 POKE 53280,INT(RND(1)*16)",
        "110 T=TI+240",
        "120 GOTO 30"
    ];
    public static readonly string[] Company = CreateCompany(new SaverSettings());
    public static string[] CreateCompany(SaverSettings settings) =>
    [
        "10 PRINT CHR$(147)",
        "20 T=TI+240",
        "30 POKE 646,13",
        PrintLine(40, settings.CompanyName),
        "45 POKE 646,3",
        PrintLine(50, settings.CompanyWebsite),
        "55 POKE 646,14",
        PrintLine(60, settings.CompanyTagline),
        "70 PRINT",
        "80 IF TI<T THEN 80",
        "90 POKE 53280,INT(RND(1)*16)",
        "100 T=TI+240",
        "110 GOTO 30"
    ];
    private static string PrintLine(int number, string? value)
    {
        string text = DisplayText(value);
        // Keep the entire typed BASIC line within the visible 40 columns.
        int padding = System.Math.Min((40 - text.Length) / 2, 29 - text.Length);
        return $"{number} PRINT \"{new string(' ', padding)}{text}\"";
    }
    public static string DisplayText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        string source = input.Replace("ä", "AE").Replace("ö", "OE").Replace("ü", "UE")
            .Replace("Ä", "AE").Replace("Ö", "OE").Replace("Ü", "UE").Replace("ß", "SS")
            .ToUpperInvariant().Replace('"', '\'');
        const string allowed = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .,:;!?@&_-+*/()=$<>'";
        var output = new System.Text.StringBuilder();
        foreach (char ch in source)
        {
            char value = allowed.Contains(ch) ? ch : ' ';
            if (value == ' ' && (output.Length == 0 || output[^1] == ' ')) continue;
            output.Append(value);
        }
        string normalized = output.ToString().Trim();
        return normalized.Length > 28 ? normalized[..28].TrimEnd() : normalized;
    }
}