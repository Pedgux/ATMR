/*
Attempt At A Multiplayer Roguelike (ATMR)
Inspired by Nathan Daniel's "Roguelike Theory of Relativity (RTOR)" paper
Built with Spectre.Console for console UI: https://spectreconsole.net/
*/

using ATMR;
using ATMR.Input;
using ATMR.Networking;
using ATMR.UI;
using Spectre.Console;

public static class Program
{
    public static async Task InitializeNetworking(string lobbyCode)
    {
        await UdpTransport.Initialize(lobbyCode);
    }

    public static async Task Main()
    {
        string lobbyCode = string.Empty;

        var multiplayer = AnsiConsole.Prompt(
            new TextPrompt<string>("Would you like to play multiplayer? (y/n): ")
        );

        // prompt the user for lobby code, later merge this into the other one below, when prompting works.
        // Input reader is affecting this way of prompting in some weird way, hence it's below. Fix later.
        if (multiplayer == "y")
        {
            lobbyCode = AnsiConsole.Prompt(new TextPrompt<string>("Type out a lobby code: "));
        }

        // Start input polling and consumer
        var cts = new CancellationTokenSource();
        var reader = Input.StartPolling(cts.Token);
        _ = Input.RunConsumer(reader, cts.Token);

        // clear the console, so UI fits
        AnsiConsole.Clear();

        // Instantiate all UI parts
        GameState.Ui = new UI();
        GameState.MessageWindow = new Messages();
        GameState.StatsWindow = new Stats();

        // optional multiplayer
        if (multiplayer == "y")
        {
            // Start networking initialization in background, important to be after UI initialization
            // because how tf do I prompt in messagewindow? Network init needs UI,
            // prompting user does not. This is messy.
            // todo: idk a prompt window? Prompt in messagewindow so we can init UI at start.
            _ = InitializeNetworking(lobbyCode);
        }

        // Start one Live session bound to the root layout and refresh messages inside it.
        // Reminder: maybe do a signal based version of this? Instead of polling evert 60ms, what if
        // UI updates were done when needed? Very inefficient currently.
        // todo: idk put this elsewhere? Such as in UI.Initialize? Weird to be here.
        await AnsiConsole
            .Live(GameState.Ui.RootLayout)
            .StartAsync(async ctx =>
            {
                while (true)
                {
                    GameState.MessageWindow.RefreshPanel();
                    GameState.StatsWindow.RefreshPanel();
                    ctx.Refresh();
                    await Task.Delay(60).ConfigureAwait(false);
                }
            });
    }
}
