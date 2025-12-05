/*
Attempt At A Multiplayer Roguelike (ATMR)
Inspired by Nathan Daniel's "Roguelike Theory of Relativity (RTOR)" paper
Built with Spectre.Console for console UI: https://spectreconsole.net/
*/

using ATMR;
using ATMR.Networking;
using ATMR.UI;
using Spectre.Console;

#pragma warning disable CS1998

public static class Program
{
    public static async Task Initialize(string lobbyCode)
    {
        Input.Poll();
        await UdpTransport.Initialize(lobbyCode);
    }

    public static async Task Main()
    {
        var lobbyCode = AnsiConsole.Prompt(new TextPrompt<string>("Type out a lobby code: "));
        AnsiConsole.Clear();

        var ui = new UI();
        ui.Initialize();
        // expose UI globally for simple access by other modules
        GameState.RootUI = ui;

        // Create and register the messages panel (no live started here)
        Messages messageWindow = new Messages(ui);
        GameState.MessageWindow = messageWindow;

        // Start networking initialization in background so UI Live runs immediately
        _ = Initialize(lobbyCode);

        // Start one Live session bound to the root layout and refresh messages inside it.
        await AnsiConsole
            .Live(ui.RootLayout)
            .StartAsync(async ctx =>
            {
                while (true)
                {
                    messageWindow.RefreshPanel();
                    ctx.Refresh();
                    await Task.Delay(250).ConfigureAwait(false);
                }
            });
    }
}
