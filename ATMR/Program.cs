/*
Attempt At A Multiplayer Roguelike (ATMR)
Inspired by Nathan Daniel's "Roguelike Theory of Relativity (RTOR)" paper
Built with Spectre.Console for console UI: https://spectreconsole.net/
*/

using ATMR.Networking;
using Spectre.Console;

public static class Program
{
    // Support async entry so we can await long-running listeners
    public static async Task Main(string[] args)
    {
        (string ip_, ushort port_) = IpPortEncoder.Decode("fwAAAROI");
        AnsiConsole.WriteLine($"{ip_}:{port_}");
        // ask the user for mode (input)
        string mode = AnsiConsole.Ask<string>("Type out a mode (send / listen): ");
        int port = int.Parse(AnsiConsole.Ask<string>("Type out a port number: "));

        if (mode == "listen")
        {
            await Puncher.RunListener(port);
            return;
        }

        if (mode == "send")
        {
            string ip = AnsiConsole.Ask<string>("Type out the ip: ");
            //await Lobby.Join("1234", "player1", ip, port);
            var cts = new CancellationTokenSource();

            // Cancel on Ctrl+C and prevent process termination so we can shut down cleanly
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true; // don't let the process terminate immediately
                cts.Cancel();
            };

            AnsiConsole.MarkupLine("[grey]Type '/exit' to quit. Press Ctrl+C to cancel.[/]");
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    // Prompt on the current thread, but read input on a Task that honors cancellation
                    AnsiConsole.Markup("[green]Message> [/]");
                    string? message = await Task.Run(() => Console.ReadLine(), cts.Token);

                    // If cancellation requested while waiting, Task.Run throws; otherwise message may be null at EOF
                    if (message is null)
                        break;
                    message = message.Trim();
                    if (string.Equals(message, "/exit", StringComparison.OrdinalIgnoreCase))
                        break;
                    if (message.Length == 0)
                        continue;

                    await Puncher.Send(ip, port, message);
                }
                catch (OperationCanceledException)
                {
                    // Ctrl+C pressed — exit the loop
                    break;
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    // optionally continue or break depending on policy
                }
            }

            AnsiConsole.MarkupLine("[grey]Exiting send mode.[/]");
        }

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
