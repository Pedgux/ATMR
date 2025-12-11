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

//ragma warning disable CS1998

public static class Program
{
    public static async Task Initialize(string lobbyCode)
    {
        // Start input polling and consumer so key events are handled during init
        var cts = new CancellationTokenSource();
        var reader = Input.StartPolling(cts.Token);
        _ = Input.RunConsumer(reader, cts.Token);

        await UdpTransport.Initialize(lobbyCode);
    }

    public static async Task Main()
    {
        var lobbyCode = AnsiConsole.Prompt(new TextPrompt<string>("Type out a lobby code: "));
        AnsiConsole.Clear();

        UiState.Ui = new UI();

        // Create and register the messages panel (no live started here)
        UiState.MessageWindow = new Messages();

        // Start networking initialization in background so UI Live runs immediately
        _ = Initialize(lobbyCode);

        // Start one Live session bound to the root layout and refresh messages inside it.
        await AnsiConsole
            .Live(UiState.Ui.RootLayout)
            .StartAsync(async ctx =>
            {
                while (true)
                {
                    UiState.MessageWindow.RefreshPanel();
                    ctx.Refresh();
                    await Task.Delay(60).ConfigureAwait(false);
                }
            });
    }
}
