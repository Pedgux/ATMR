namespace ATMR.Networking;

using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Spectre.Console;

public static class UdpTransport
{
    private static UdpClient? _udp;
    public static UdpClient Udp
    {
        get => _udp ?? throw new InvalidOperationException("UdpTransport not initialized");
        private set => _udp = value;
    }

    /// <summary>
    /// Initializes fucking everything
    /// </summary>
    /// <param name="lobbyId"> the name of the lobby</param>
    public static async Task Initialize(string lobbyId)
    {
        _udp = new UdpClient(0);
        var (ip, port) = await Stun.GetPublicIPAsync();
        AnsiConsole.MarkupLine($"[green]Address from STUN: {ip}:{port}[/]");

        await Lobby.Initialize();

        string playerId =
            Lobby.Auth?.LocalId
            ?? throw new InvalidOperationException("Lobby.Auth or LocalId is null");
        AnsiConsole.MarkupLine($"[yellow]playerId is: {playerId}[/]");
        await Lobby.Join(lobbyId, playerId, ip, port);

        string? blob = await Lobby.GetOtherPlayerBlob(lobbyId, playerId);
        if (string.IsNullOrWhiteSpace(blob))
            throw new InvalidOperationException(
                "Lobby.GetOtherPlayerBlob returned null or empty blob for the other player in the lobby"
            );
        (string peerIp, ushort peerPort) = IpPortEncoder.Decode(blob);
        var peerEndpoint = new IPEndPoint(IPAddress.Parse(peerIp), peerPort);
        AnsiConsole.MarkupLine($"[blue]Punching {peerEndpoint}[/]");
        await Puncher.Punch(peerEndpoint);

        // start receiving packets
        await ReceiveLoop();

        // TODO:
        // gotta subscribe to lobby updates, see if another player joins / is already there
        // then query for their blob, decode and start nat punching via Puncher.cs
        // after nat punching succeeds, each user deletes their own node and last user deletes lobby
        // unless firebase auto deletes empty JSON Objects. This was true while testing with manual deletion
    }

    public static async Task ReceiveLoop()
    {
        while (true)
        {
            try
            {
                var result = await Udp.ReceiveAsync();
                // decode bytes to string (UTF-8)
                var message = System.Text.Encoding.UTF8.GetString(
                    result.Buffer,
                    0,
                    result.Buffer.Length
                );
                AnsiConsole.MarkupLine(
                    $"[green]Message from {result.RemoteEndPoint}:[/] {message}"
                );
                Console.WriteLine($"Got {result.Buffer.Length} bytes from {result.RemoteEndPoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive error: {ex}");
            }
        }
    }
}
