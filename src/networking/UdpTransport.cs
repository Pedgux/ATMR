namespace ATMR.Networking;

using System;
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
        GameState.MessageWindow?.Write($"Local UDP bound: {_udp.Client.LocalEndPoint}");
        var (ip, port) = await Stun.GetPublicIPAsync();
        //AnsiConsole.MarkupLine($"[green]Address from STUN: {ip}:{port}[/]");

        GameState.MessageWindow?.Write($"[green]Address from STUN: {ip}:{port}[/]");

        await Lobby.Initialize();

        string playerId =
            Lobby.Auth?.LocalId
            ?? throw new InvalidOperationException("Lobby.Auth or LocalId is null");
        GameState.MessageWindow?.Write($"[yellow]playerId is: {playerId}[/]");
        await Lobby.Join(lobbyId, playerId, ip, port);

        string? blob = await Lobby.GetOtherPlayerBlob(lobbyId, playerId);
        if (string.IsNullOrWhiteSpace(blob))
            throw new InvalidOperationException(
                "Lobby.GetOtherPlayerBlob returned null or empty blob for the other player in the lobby"
            );

        (string peerIp, ushort peerPort) = IpPortEncoder.Decode(blob);
        var peerEndpoint = new IPEndPoint(IPAddress.Parse(peerIp), peerPort);

        //executes this far
        _ = await Puncher.Punch(peerEndpoint);

        // start receiving packets & if punching succeeds sending keepalives
        _ = await ReceiveLoop(peerEndpoint);

        // TODO:
        // after game is over, each user deletes their own node and last user deletes lobby
        // unless firebase auto deletes empty JSON Objects. This was true while testing with manual deletion
    }

    public static async Task ReceiveLoop(IPEndPoint peer)
    {
        GameState.MessageWindow?.Write("[blue]Starting Receiveloop![/]");
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

                // Debug: always log remote endpoint and raw payload (hex + text)
                try
                {
                    GameState.MessageWindow?.Write($"Recv from {result.RemoteEndPoint}:'");
                    GameState.MessageWindow?.Write("{BitConverter.ToString(result.Buffer)}");
                    GameState.MessageWindow?.Write("/ '{message}");
                }
                catch
                {
                    // best-effort logging — do not throw from receive loop
                }

                if (result.Buffer.Length == 1 && result.Buffer[0] == 0x01)
                {
                    GameState.MessageWindow?.Write("[blue]alive[/]");
                }

                if (message == "poke")
                {
                    if (!connected)
                    {
                        GameState.MessageWindow?.Write("[green]Got a connection![/]");
                        connected = true;
                        await KeepAliveLoop(peer);
                    }
                    continue;
                }
            }
            catch (Exception ex)
            {
                GameState.MessageWindow?.Write($"[red]Receive error: {ex}[/]");
            }
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
                    GameState.MessageWindow?.Write($"KeepAlive send error to {peer}: {ex.Message}");
                }
            }
        });

        return Task.CompletedTask;
    }
}
