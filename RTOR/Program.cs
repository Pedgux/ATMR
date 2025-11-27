/*
Attempt At A Multiplayer Roguelike (ATMR)
Inspired by Nathan Daniel's "Roguelike Theory of Relativity (RTOR)" paper
Built with Spectre.Console for console UI: https://spectreconsole.net/

Docs (local PDF):
    - relative:    ./docs/RTOR.pdf
    - workspace:   docs/RTOR.pdf
    - file URI:    file:///C:/Kod/Cterav%C3%A4/RTOR/docs/RTOR.pdf  (note: special chars percent-encoded)

Tip: Most editors (VS Code) will make the relative path clickable in comments; click or Ctrl+Click to open the PDF.
*/

using Spectre.Console;

public static class Program
{
    public static void Main()
    {
        // Testing if Spectre.Console works
        AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Sand)
            .Start(
                "<-- sand falling",
                ctx =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        ctx.Status($"<~~ sand falling: {i + 1}%");
                        Thread.Sleep(50);
                    }
                    ctx.Status("Done!");
                    Thread.Sleep(500);
                }
            );
    }
}
