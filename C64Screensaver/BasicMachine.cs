using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace C64Screensaver;

// Deliberately bounded BASIC V2 subset: assignments, GOTO, PRINT CHR$, POKE,
// arithmetic, INT and RND. Executes the listing shown on screen.
public sealed class BasicMachine
{
    public static readonly string[] Listing = BasicPrograms.Maze;
    private readonly (int Number, string Code)[] program;
    private readonly Dictionary<string, double> variables = new();
    private readonly Random random;
    private int pc, cursor;
    private double clockSeconds;
    public void AdvanceClock(double seconds) => clockSeconds += Math.Max(0, seconds);
    public byte[] Characters { get; } = new byte[1000];
    public byte[] Colors { get; } = new byte[1000];
    public int Border { get; private set; } = 14;
    public int Background { get; private set; } = 6;
    public int Ink { get; private set; } = 14;
    public long Instructions { get; private set; }
    public int CursorIndex => Math.Min(cursor, 999);

    public BasicMachine(IEnumerable<string>? source = null, int? seed = null)
    {
        random = seed.HasValue ? new Random(seed.Value) : new Random();
        program = (source ?? Listing).Select(line =>
        {
            int split = line.IndexOf(' ');
            return (int.Parse(line[..split], CultureInfo.InvariantCulture), line[(split + 1)..]);
        }).OrderBy(line => line.Item1).ToArray();
        ClearScreen();
    }

    public void ClearScreen() { Array.Fill(Characters, (byte)32); Array.Fill(Colors, (byte)Ink); cursor = 0; }
    public void Write(string text)
    {
        foreach (char ch in text)
        {
            if (ch == '\n') { cursor += 40 - cursor % 40; Scroll(); continue; }
            Scroll();
            Characters[cursor] = (byte)ch;
            Colors[cursor++] = (byte)Ink;
        }
    }
    // The ghost edits only the character immediately before the cursor.
    public void Backspace()
    {
        if (cursor == 0) return;
        Characters[--cursor] = 32;
        Colors[cursor] = (byte)Ink;
    }
    private void Scroll()
    {
        while (cursor >= 1000)
        {
            Array.Copy(Characters, 40, Characters, 0, 960);
            Array.Copy(Colors, 40, Colors, 0, 960);
            Array.Fill(Characters, (byte)32, 960, 40);
            Array.Fill(Colors, (byte)Ink, 960, 40);
            cursor -= 40;
        }
    }
    public void Step()
    {
        if (program.Length == 0) return;
        string code = program[pc].Code.Trim();
        pc = (pc + 1) % program.Length;
        Instructions++;
        Execute(code);
    }
    private void Jump(int number)
    {
        int next = Array.FindIndex(program, line => line.Number == number);
        if (next < 0) throw new InvalidOperationException("UNDEF'D STATEMENT");
        pc = next;
    }
    private void Execute(string code)
    {
        if (code.StartsWith("IF ", StringComparison.Ordinal))
        {
            var match = Regex.Match(code, @"^IF (.+?)(<=|>=|<>|=|<|>)(.+?) THEN (.+)$");
            if (!match.Success) throw new FormatException(code);
            double left = Eval(match.Groups[1].Value), right = Eval(match.Groups[3].Value);
            bool condition = match.Groups[2].Value switch
            {
                "<" => left < right, ">" => left > right, "=" => left == right,
                "<=" => left <= right, ">=" => left >= right, "<>" => left != right,
                _ => false
            };
            if (condition)
            {
                string statement = match.Groups[4].Value;
                if (int.TryParse(statement, out int line)) Jump(line);
                else Execute(statement);
            }
        }
        else if (code.StartsWith("GOTO ", StringComparison.Ordinal)) Jump((int)Eval(code[5..]));
        else if (code.StartsWith("POKE ", StringComparison.Ordinal))
        {
            var parts = code[5..].Split(',', 2);
            int address = (int)Eval(parts[0]), value = (int)Eval(parts[1]);
            if (value is < 0 or > 255) throw new InvalidOperationException("ILLEGAL QUANTITY");
            if (address == 53280) Border = value & 15;
            else if (address == 53281) Background = value & 15;
            else if (address == 646) Ink = value & 15;
            else if (address is >= 1024 and <= 2023)
            {
                // The built-in starfield uses screen codes 32, 42 and 46.
                // Screen codes 0..31 map to the uppercase character set.
                Characters[address - 1024] = (byte)(value < 32 ? value + 64 : value);
            }
            else if (address is >= 55296 and <= 56295) Colors[address - 55296] = (byte)(value & 15);
            else throw new NotSupportedException("Unsupported POKE address");
        }
        else if (code == "PRINT" || code.StartsWith("PRINT ", StringComparison.Ordinal))
        {
            string argument = code.Length == 5 ? "" : code[6..].Trim();
            bool newline = !argument.EndsWith(';');
            if (!newline) argument = argument[..^1].TrimEnd();
            if (argument.StartsWith("CHR$(", StringComparison.Ordinal))
            {
                int close = argument.LastIndexOf(')');
                int value = (int)Eval(argument[5..close]);
                if (value is < 0 or > 255) throw new InvalidOperationException("ILLEGAL QUANTITY");
                if (value == 147) ClearScreen();
                else Write(((char)value).ToString());
            }
            else if (argument.Length >= 2 && argument[0] == '"' && argument[^1] == '"') Write(argument[1..^1]);
            else if (argument.Length > 0) throw new NotSupportedException(code);
            if (newline) Write("\n");
        }
        else
        {
            int equals = code.IndexOf('=');
            if (equals < 1) throw new NotSupportedException(code);
            variables[code[..equals].Trim()] = Eval(code[(equals + 1)..]);
        }
    }
    private double Eval(string expression)
    {
        var tokens = Regex.Matches(expression, @"[A-Z][A-Z0-9]*|\d+(?:\.\d+)?|[()+*/,-]")
            .Select(match => match.Value).ToArray();
        int index = 0;
        double Atom()
        {
            string token = tokens[index++];
            if (token == "TI") return Math.Floor(clockSeconds * 60);
            if (token == "-") return -Atom();
            if (token == "(") { double value = Sum(); Expect(")"); return value; }
            if (token is "INT" or "RND")
            {
                Expect("("); double value = Sum(); Expect(")");
                return token == "INT" ? Math.Floor(value) : random.NextDouble();
            }
            return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double n)
                ? n : variables.GetValueOrDefault(token);
        }
        void Expect(string expected)
        {
            if (index >= tokens.Length || tokens[index++] != expected) throw new FormatException(expression);
        }
        double Product()
        {
            double value = Atom();
            while (index < tokens.Length && tokens[index] is "*" or "/")
            {
                string op = tokens[index++]; double rhs = Atom();
                value = op == "*" ? value * rhs : value / rhs;
            }
            return value;
        }
        double Sum()
        {
            double value = Product();
            while (index < tokens.Length && tokens[index] is "+" or "-")
            {
                string op = tokens[index++]; double rhs = Product();
                value = op == "+" ? value + rhs : value - rhs;
            }
            return value;
        }
        double result = Sum();
        if (index != tokens.Length || !double.IsFinite(result)) throw new FormatException(expression);
        return result;
    }
}
