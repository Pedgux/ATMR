/*
Attempt At A Multiplayer Roguelike (ATMR)
Inspired by Nathan Daniel's "Roguelike Theory of Relativity (RTOR)" paper
Built with Spectre.Console for console UI: https://spectreconsole.net/
*/

using System;
using System.Threading.Tasks;
using ATMR.Networking;
using Spectre.Console;

public static class Program
{
    // Support async entry so we can await long-running listeners
    public static async Task Main(string[] args)
    {
        // ask the user for mode (input)
        string mode = AnsiConsole.Ask<string>("Type out a mode (send / listen): ");

        if (mode == "listen")
        {
            int port = int.Parse(AnsiConsole.Ask<string>("Type out a port number: "));
            await Puncher.RunListener(port);
            return;
        }

        if (mode == "send") { }

        // default behavior
        // GameLoop();
    }

    // gameloop will be here, just a placeholder for now
    public static void GameLoop()
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
    }
}
