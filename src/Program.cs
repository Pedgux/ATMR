/*
Attempt At A Multiplayer Roguelike (ATMR)
Inspired by Nathan Daniel's "Roguelike Theory of Relativity (RTOR)" paper
Built with Spectre.Console for console UI: https://spectreconsole.net/
*/

using ATMR.Networking;
using ATMR.UI;
using Spectre.Console;

public static class Program
{
    public static async Task Initialize(string lobbyCode)
    {
        Input.Poll();
        await UdpTransport.Initialize(lobbyCode);
    }

    public static async Task Main()
    {
        AnsiConsole.Clear();

        // Create and initialize instance-based UI
        var ui = new UI();
        ui.Initialize();
        // Optionally create the Messages handler that references the UI
        var messages = new Messages(ui);
        // Render the UI
        ui.Render();

        //var lobbyCode = AnsiConsole.Prompt(new TextPrompt<string>("Type out a lobby code: "));
        //await Initialize(lobbyCode);

        while (true) { }
    }

    // placeholder ig
    public static void GameLoop() { }
}
