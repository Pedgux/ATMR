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
        Messages.Initialize();
    }

    public static async Task Main(string[] args)
    {
        // Create a table
        var table = new Table()
            .AddColumn("ID")
            .AddColumn("Methods")
            .AddColumn("Purpose")
            .AddRow("1", "Center()", "Initializes a new instance that is center aligned")
            .AddRow("2", "Measure()", "Measures the renderable object")
            .AddRow("3", "Right()", "Initializes a new instance that is right aligned.");

        // Create a panel
        var panel = new Panel(table).Header("Other Align Methods").Border(BoxBorder.Double);

        // Renders the panel in the top-center of the console
        AnsiConsole.Write(new Align(panel, HorizontalAlignment.Center, VerticalAlignment.Top));

        //var lobbyCode = AnsiConsole.Prompt(new TextPrompt<string>("Type out a lobby code: "));
        //await Initialize(lobbyCode);
    }

    // placeholder ig
    public static void GameLoop() { }
}
