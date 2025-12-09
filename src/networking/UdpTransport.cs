namespace ATMR.Networking;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Handles all UDP. heh.
/// </summary>
public static class UdpTransport
{
    private static UdpClient? _udp;
    public static UdpClient Udp
    {
        get => _udp ?? throw new InvalidOperationException("UdpTransport not initialized");
        private set => _udp = value;
    }
    private static List<string> SendBuffer = new();
    private static IPEndPoint? peerEndpoint;

    /// <summary>
    /// Initializes fucking everything
    /// </summary>
    /// <param name="lobbyId"> the name of the lobby</param>
    public static async Task Initialize(string lobbyId)
    {
        _udp = new UdpClient(0);
        UiState.MessageWindow?.Write($"Local UDP bound: {_udp.Client.LocalEndPoint}");
        var (ip, port) = await Stun.GetPublicIPAsync();

        UiState.MessageWindow?.Write($"[green]Address from STUN: {ip}:{port}[/]");

        await Lobby.Initialize();

        string playerId =
            Lobby.Auth?.LocalId
            ?? throw new InvalidOperationException("Lobby.Auth or LocalId is null");
        UiState.MessageWindow?.Write($"[yellow]playerId is: {playerId}[/]");
        await Lobby.Join(lobbyId, playerId, ip, port);

        string? blob = await Lobby.GetOtherPlayerBlob(lobbyId, playerId);
        if (string.IsNullOrWhiteSpace(blob))
            throw new InvalidOperationException(
                "Lobby.GetOtherPlayerBlob returned null or empty blob for the other player in the lobby"
            );

        (string peerIp, ushort peerPort) = IpPortEncoder.Decode(blob);
        peerEndpoint = new IPEndPoint(IPAddress.Parse(peerIp), peerPort);

        _ = Puncher.Punch(peerEndpoint);
        // Start punching in the background so the receive loop can run concurrently
        // start receiving packets & if punching succeeds sending keepalives
        await ReceiveLoop(peerEndpoint);

        // TODO:
        // after game is over, each user deletes their own node and last user deletes lobby
        // unless firebase auto deletes empty JSON Objects. This was true while testing with manual deletion
    }

    /// <summary>
    /// Handles all incoming packets
    /// </summary>
    /// <param name="peer">The peer you're receiving from</param>
    /// <returns>??</returns>
    public static async Task ReceiveLoop(IPEndPoint peer)
    {
        UiState.MessageWindow?.Write("[blue]Starting Receiveloop![/]");
        bool connected = false;
        while (true)
        {
            try
            {
                var result = await Udp.ReceiveAsync();

                // decode bytes to string (UTF-8)
                var message = Encoding.UTF8.GetString(result.Buffer, 0, result.Buffer.Length);
                if (message[0] == 'i')
                {
                    Input.RecieveInput(message);
                }

                if (result.Buffer.Length == 1 && result.Buffer[0] == 0x01)
                {
                    UiState.MessageWindow?.Write("[blue]alive[/]");
                }

                if (message == "poke")
                {
                    if (!connected)
                    {
                        UiState.MessageWindow?.Write("[green]Got a connection![/]");
                        connected = true;
                        await KeepAliveLoop(peer);
                    }
                    continue;
                }
            }
            catch (Exception ex)
            {
                UiState.MessageWindow?.Write($"[red]Receive error: {ex}[/]");
            }
        }
    }

    public static async Task SendMessage(string message)
    {
        UiState.MessageWindow?.Write(SendBuffer[0]);
        SendBuffer.RemoveAt(0);
        byte[] massage = Encoding.UTF8.GetBytes(message);
        try
        {
            await Udp.SendAsync(massage, massage.Length, peerEndpoint);
        }
        catch (Exception ex)
        {
            UiState.MessageWindow?.Write($"KeepAlive send error to {peerEndpoint}: {ex.Message}");
        }
    }

    private static Task KeepAliveLoop(IPEndPoint peer)
    {
        byte[] poke = { 0x01 };

        // Use a background Task loop instead of a Timer so the work isn't
        // garbage-collected when this method returns. The Task runs forever
        // and sends a single-byte keepalive every 30 seconds.
        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await Task.Delay(30000);
                    await Udp.SendAsync(poke, poke.Length, peer);
                }
                catch (Exception ex)
                {
                    UiState.MessageWindow?.Write($"KeepAlive send error to {peer}: {ex.Message}");
                }
            }
        });

        return Task.CompletedTask;
    }
}
