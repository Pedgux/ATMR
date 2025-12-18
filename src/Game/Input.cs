namespace ATMR.Input;

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Arch.Core;
using Arch.Core.Extensions;
using ATMR.Components;
using ATMR.Game;
using ATMR.Networking;
using ATMR.Tick;

public static class Input
{
    // Batches keystrokes for a short window so multiple players' inputs
    // can be processed together in a single game tick.
    private static readonly TimeSpan TickCoalesceWindow = TimeSpan.FromMilliseconds(50);

    // Central, thread-safe pipeline of input events coming from local or network sources.
    // Tuple payload: (playerId, key pressed). Single reader (the tick pump) with many writers.
    private static readonly Channel<(int playerId, ConsoleKeyInfo keyInfo)> InputEvents =
        Channel.CreateUnbounded<(int playerId, ConsoleKeyInfo keyInfo)>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );

    // Background task that drains the InputEvents channel and emits game ticks.
    private static Task? _tickPump;

    // Ensures only one background pump is started across threads.
    private static readonly object TickPumpLock = new();

    // Start the background poller and return the channel reader
    public static ChannelReader<ConsoleKeyInfo> StartPolling(CancellationToken token = default)
    {
        // This produces a stream of raw ConsoleKeyInfo items by polling Console.
        // Note: This is an unbounded channel; consumers must keep up to avoid memory growth.
        var chan = Channel.CreateUnbounded<ConsoleKeyInfo>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = true }
        );
        _ = Task.Run(
            async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    // Poll the console without blocking so we can honor cancellation.
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        await chan.Writer.WriteAsync(key, token);
                    }
                    else
                    {
                        // Back off a bit when there's no input to avoid busy-waiting.
                        await Task.Delay(10, token);
                    }
                }
                // Signal to readers that no more keys will arrive.
                chan.Writer.TryComplete();
            },
            token
        );
        return chan.Reader;
    }

    private static void EnsureTickPumpStarted(CancellationToken token = default)
    {
        lock (TickPumpLock)
        {
            // Start the single tick pump lazily on first input.
            _tickPump ??= Task.Run(() => TickPump(token), token);
        }
    }

    private static async Task TickPump(CancellationToken token)
    {
        // Single consumer that coalesces inputs into a single game tick.
        var reader = InputEvents.Reader;

        while (await reader.WaitToReadAsync(token))
        {
            if (!reader.TryRead(out var first))
            {
                continue;
            }

            // Collect the first event and then coalesce additional inputs
            // for a short window so the tick sees a snapshot for all players.
            var inputs = new Dictionary<int, ConsoleKeyInfo> { [first.playerId] = first.keyInfo };
            var deadline = DateTime.UtcNow + TickCoalesceWindow;

            while (true)
            {
                while (reader.TryRead(out var next))
                {
                    // For each player, keep only the latest key within the window.
                    inputs[next.playerId] = next.keyInfo;
                }

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                // Wait for either more input or the coalescing timeout to elapse.
                var waitForMore = reader.WaitToReadAsync(token).AsTask();
                var timeout = Task.Delay(remaining, token);
                var completed = await Task.WhenAny(waitForMore, timeout);
                if (completed == timeout)
                {
                    break;
                }
            }

            try
            {
                // Advance the game by one tick with the snapshot of inputs.
                await Tick.CreateAsync(inputs, GameState.Level0);
            }
            catch
            {
                // Swallow errors until the world/player is initialized.
            }
        }
    }

    private static void EnqueueInput(int playerId, ConsoleKeyInfo keyInfo, CancellationToken token)
    {
        // Ensure the tick pump is running before writing; write is non-blocking here.
        EnsureTickPumpStarted(token);
        InputEvents.Writer.TryWrite((playerId, keyInfo));
    }

    private static char KeyCharFromConsoleKey(ConsoleKey key)
    {
        // Convert A-Z and 0-9 ConsoleKey values to their char representation.
        // For non-alphanumeric keys, return NUL (no meaningful char).
        if (key >= ConsoleKey.A && key <= ConsoleKey.Z)
        {
            return (char)('a' + (key - ConsoleKey.A));
        }

        if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9)
        {
            return (char)('0' + (key - ConsoleKey.D0));
        }

        return '\0';
    }

    public static Task ReceiveInput(string message)
    {
        // receive
        // Handles inputs arriving over the network as small text messages.
        // Special cases:
        //  - "iup"   : scroll message window up
        //  - "idown" : scroll message window down
        // General format for keystrokes: i{playerId}{ConsoleKey}
        //   e.g., "i2UpArrow" means player 2 pressed UpArrow.
        if (message == "iup")
        {
            GameState.MessageWindow.OffsetUp();
            return Task.CompletedTask;
        }

        if (message == "idown")
        {
            GameState.MessageWindow.OffsetDown();
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(message) || message[0] != 'i')
            return Task.CompletedTask;

        // Expect: i{playerId}{ConsoleKey}, e.g., i2UpArrow
        int index = 1;
        while (index < message.Length && char.IsDigit(message[index]))
        {
            index++;
        }

        if (index == 1 || index >= message.Length)
        {
            return Task.CompletedTask;
        }

        if (!int.TryParse(message.AsSpan(1, index - 1), out int playerId))
        {
            return Task.CompletedTask;
        }

        string keyText = message.Substring(index);
        if (!Enum.TryParse<ConsoleKey>(keyText, ignoreCase: false, out var key))
        {
            return Task.CompletedTask;
        }

        // Build a ConsoleKeyInfo with our derived char (if any) and no modifiers.
        var keyInfo = new ConsoleKeyInfo(KeyCharFromConsoleKey(key), key, false, false, false);
        EnqueueInput(playerId, keyInfo, CancellationToken.None);
        return Task.CompletedTask;
    }

    // Example consumer that maps keys to handlers
    public static async Task RunConsumer(
        ChannelReader<ConsoleKeyInfo> reader,
        CancellationToken token = default
    )
    {
        // Drain the provided reader (from StartPolling or elsewhere) and route
        // keys either to UI controls (PageUp/PageDown) or into the input pipeline.
        await foreach (var keyInfo in reader.ReadAllAsync(token))
        {
            if (keyInfo.Key == ConsoleKey.PageUp)
            {
                try
                {
                    GameState.MessageWindow.OffsetUp();
                }
                catch { }

                continue;
            }

            if (keyInfo.Key == ConsoleKey.PageDown)
            {
                try
                {
                    GameState.MessageWindow.OffsetDown();
                }
                catch { }

                continue;
            }

            try
            {
                var level = GameState.Level0;
                var playerId = Lobby.PlayerNumber;
                if (UdpTransport.connected)
                {
                    GameState.MessageWindow.Write($"{playerId}");
                    // Mirror local input to peers: "i{playerId}{ConsoleKey}".
                    var message = $"i{playerId}{keyInfo.Key}";
                    await UdpTransport.SendMessage(message);
                }
                // Always feed local input into the authoritative pipeline.
                EnqueueInput(playerId, keyInfo, token);
            }
            catch
            {
                // Ignore input until world/player is initialized.
            }
        }
    }
}
