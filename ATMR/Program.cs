/*
Attempt At A Multiplayer Roguelike (ATMR)
Inspired by Nathan Daniel's "Roguelike Theory of Relativity (RTOR)" paper
Built with Spectre.Console for console UI: https://spectreconsole.net/
*/

using ATMR.Networking;
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
                        Thread.Sleep(5);
                    }
                    ctx.Status("Done!");
                    Thread.Sleep(500);
                }
            );
        Puncher.Test();
    }
}
