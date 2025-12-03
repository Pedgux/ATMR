/*
Attempt At A Multiplayer Roguelike (ATMR)
Inspired by Nathan Daniel's "Roguelike Theory of Relativity (RTOR)" paper
Built with Spectre.Console for console UI: https://spectreconsole.net/
*/

using System.Threading.Tasks;
using ATMR.Networking;
using Spectre.Console;

public static class Program
{
    public static async Task Initialize(string lobbyCode)
    {
        await UdpTransport.Initialize(lobbyCode);
        Input.Poll();
    }

    public static async Task Main(string[] args)
    {
        var lobbyCode = AnsiConsole.Prompt(new TextPrompt<string>("Type out a lobby code: "));
        await Initialize(lobbyCode);
        /*
        AnsiConsole.WriteLine($"Address: {ip}:{port}");
        AnsiConsole.WriteLine($"encoded blob: {IpPortEncoder.Encode(ip, port)}");
        AnsiConsole.WriteLine(
            $"decoded blob: {IpPortEncoder.Decode(IpPortEncoder.Encode(ip, port))}"
        );
        AnsiConsole.WriteLine($"Player id: {playerId}");
        */

        //await Lobby.Join("1234", playerId, ip, port);
    }

    // placeholder ig
    public static void GameLoop() { }
}
