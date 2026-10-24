using System;
using System.Collections.Generic;

namespace C64Screensaver;

public sealed class GhostTyping
{
    private readonly record struct Stroke(char Character, bool Delete, double Pause);
    private readonly List<Stroke> strokes = new();
    private int index;
    private double remaining = 2.8;
    public bool Complete => index == strokes.Count;
    public int Corrections { get; private set; }
    public int TypedCharacters { get; private set; }

    public GhostTyping(Random random, string[]? listing = null)
    {
        var lines = new List<string>(listing ?? BasicMachine.Listing) { "RUN" };
        for (int line = 0; line < lines.Count; line++)
        {
            for (int column = 0; column < lines[line].Length; column++)
            {
                char correct = lines[line][column];
                // Two deliberate mistakes per listing, at different positions.
                if ((line == 1 && column == 8) || (line == 4 && column == Math.Min(15, lines[line].Length - 2)))
                {
                    strokes.Add(new(correct == 'X' ? 'Z' : 'X', false, 0.65));
                    strokes.Add(new('\0', true, 0.32));
                }
                strokes.Add(new(correct, false, 0.12 + random.NextDouble() * 0.14 +
                    (correct is '(' or ',' ? 0.12 : 0)));
            }
            // Read the completed line before pressing Return; linger after Return too.
            strokes.Add(new('\0', false, 0.65 + random.NextDouble() * 0.4));
            strokes.Add(new('\n', false, line == lines.Count - 1 ? 1.1 : 0.8 + random.NextDouble() * 0.8));
        }
        // RUN is visible for a moment before the program takes over.
        strokes.Add(new('\0', false, 0));
    }

    public void Advance(BasicMachine screen, double seconds)
    {
        remaining -= seconds;
        while (!Complete && remaining <= 0)
        {
            var stroke = strokes[index++];
            if (stroke.Delete) { screen.Backspace(); Corrections++; }
            else if (stroke.Character != '\0')
            {
                screen.Write(stroke.Character.ToString());
                TypedCharacters++;
            }
            remaining += stroke.Pause;
        }
    }
}

public sealed class ScreenSession
{
    public BasicMachine Machine { get; private set; } = null!;
    public GhostTyping Typing { get; private set; } = null!;
    public bool Executing { get; private set; }
    public double Elapsed { get; private set; }
    private readonly Random random;
    private readonly ProgramSelection selection;
    private readonly string[] companyProgram;
    private int cycle = -1;
    public ProgramSelection CurrentProgram { get; private set; }
    public bool IsCompany => CurrentProgram == ProgramSelection.Company;
    private readonly int programDuration;
    private readonly bool powerTransition;
    private double transitionTime = -1;
    private bool transitionSwitched;
    public double? TransitionTime => transitionTime >= 0 ? transitionTime : null;
    private double runTime, instructionCredit;
    public ScreenSession(Random? random = null, ProgramSelection selection = ProgramSelection.AllPrograms, SaverSettings? companySettings = null)
    {
        this.random = random ?? new Random();
        this.selection = Enum.IsDefined(selection) ? selection : ProgramSelection.AllPrograms;
        var settings = companySettings ?? new SaverSettings();
        companyProgram = BasicPrograms.CreateCompany(settings);
        powerTransition = settings.PowerTransition;
        programDuration = Math.Clamp(settings.ProgramDurationSeconds, 15, 1800);
        Restart();
    }
    private void Restart()
    {
        cycle++;
        CurrentProgram = selection == ProgramSelection.AllPrograms
            ? (ProgramSelection)(cycle % 3 + 1) : selection;
        var listing = CurrentProgram switch
        {
            ProgramSelection.Company => companyProgram,
            ProgramSelection.Stars => BasicPrograms.Stars,
            _ => BasicPrograms.Maze
        };
        Machine = new BasicMachine(listing, random.Next());
        Machine.Write("\n    **** COMMODORE 64 BASIC V2 ****\n\n 64K RAM SYSTEM  38911 BASIC BYTES FREE\n\nREADY.\n");
        Typing = new GhostTyping(random, listing);
        Executing = false;
        Elapsed = runTime = instructionCredit = 0;
    }
    public void BeginTransitionPreview()
    {
        transitionTime = 0;
        transitionSwitched = false;
    }
    public void StartPreview()
    {
        Executing = true;
        for (int i = 0; i < 24_000; i++) { Machine.AdvanceClock(1.0 / 240); Machine.Step(); }
    }
    public void Advance(double seconds, double speed)
    {
        Elapsed += seconds;
        if (transitionTime >= 0)
        {
            transitionTime += seconds;
            if (!transitionSwitched && transitionTime >= 1.02)
            {
                Restart();
                transitionSwitched = true;
            }
            if (transitionTime >= 1.42) transitionTime = -1;
            return;
        }
        Machine.AdvanceClock(seconds);
        if (!Executing)
        {
            // Typing stays readable even with a high animation-speed setting.
            Typing.Advance(Machine, seconds * Math.Clamp(speed, 0.6, 1.35));
            if (Typing.Complete) Executing = true;
            return;
        }
        runTime += seconds;
        if (runTime >= programDuration)
        {
            if (powerTransition) { transitionTime = 0; transitionSwitched = false; }
            else Restart();
            return;
        }
        instructionCredit = Math.Min(instructionCredit + seconds * 240 * speed, 450);
        while (instructionCredit >= 1) { Machine.Step(); instructionCredit--; }
    }
}
