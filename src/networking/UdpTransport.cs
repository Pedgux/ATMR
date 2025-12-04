namespace ATMR.Networking;

using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
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

        await Puncher.Punch(peerEndpoint);

        // start receiving packets & if punching succeeds sending keepalives
        await ReceiveLoop(peerEndpoint);

        // TODO:
        // after game is over, each user deletes their own node and last user deletes lobby
        // unless firebase auto deletes empty JSON Objects. This was true while testing with manual deletion
    }

    public static async Task ReceiveLoop(IPEndPoint peer)
    {
        bool connected = false;
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

                if (result.Buffer.Length == 1 && result.Buffer[0] == 0x01)
                {
                    AnsiConsole.MarkupLine("[blue]alive[/]");
                }

                if (message == "poke")
                {
                    if (!connected)
                    {
                        AnsiConsole.MarkupLine("[green]Got a connection![/]");
                        connected = true;
                        await KeepAliveLoop(peer);
                    }
                    continue;
                }
                //Console.WriteLine($"Got {result.Buffer.Length} bytes from {result.RemoteEndPoint}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Receive error: {ex}[/]");
            }
        }
    }

    private static async Task KeepAliveLoop(IPEndPoint peer)
    {
        byte[] poke = { 0x01 };
        Timer timer = new(30000) { AutoReset = true, Enabled = true };

        // buh lambda. Needs the two params because the += delegate expects them. Named _ because unused
        timer.Elapsed += async (_, __) =>
        {
            try
            {
                await Udp.SendAsync(poke, poke.Length, peer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KeepAlive send error to {peer}: {ex.Message}");
            }
        };
    }
}
