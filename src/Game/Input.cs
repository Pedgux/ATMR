namespace ATMR.Input;

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Arch.Core;
using ATMR.Game;
using ATMR.Helpers;
using ATMR.Networking;
using ATMR.Tick;

public static class Input
{
    // Batches keystrokes for a short window so multiple players' inputs
    // can be processed together in a single game tick for multiplayer.
    private const int TickDelayMs = 50;
    private static TimeSpan TickWaitWindow = TimeSpan.FromMilliseconds(TickDelayMs);

    // Central, thread-safe pipeline of input events coming from local or network sources.
    // Tuple payload: (playerId, key pressed). Single reader (the tick pump) with many writers.
    private static readonly Channel<(
        int playerId,
        ConsoleKeyInfo keyInfo,
        int tickNumber
    )> InputEvents = Channel.CreateUnbounded<(
        int playerId,
        ConsoleKeyInfo keyInfo,
        int tickNumber
    )>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    // Background task that drains the InputEvents channel and emits game ticks.
    private static Task? _tickPump;

    //private static List<> _inputBuffer;

    // Ensures only one background pump is started across threads.
    private static readonly object TickPumpLock = new();

    // Synchronizes access to InputStorage in multiplayer mode to prevent race conditions.
    private static readonly object InputStorageLock = new();

    // Prevents the tick pump and rollback from mutating the world concurrently.
    private static readonly SemaphoreSlim WorldMutex = new(1, 1);

    // Tracks when local input was first stored for each tick.
    // The tick pump waits TickDelayMs after this timestamp before executing,
    // giving the remote player's input time to arrive over the network.
    private static readonly Dictionary<int, DateTime> LocalInputTime = new();

    // Rate-limit singleplayer ticks to smooth out OS keyboard repeat floods.
    private static DateTime LastTickTime = DateTime.MinValue;
    private static DateTime _previousTime = DateTime.UtcNow;
    private static int NextLocalTickNumber = -10;
    private static readonly object NextLocalTickLock = new();

    // 1 is newest, 3 oldest. Used to send older inputs too.
    private static string input1 = ""; // now
    private static string input2 = ""; // yesterday
    private static string input3 = ""; // eldest

    private static string input4 = ""; // chap
    private static string input5 = ""; // gran
    private static string input6 = "";
    private static string input7 = "";
    private static string input8 = "";
    private static string input9 = "";
    private static string input10 = "";

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
                        //GameState.MessageWindow.Write($"input pressed: {DateTime.UtcNow:mm:ss.fff}");
                        var key = Console.ReadKey(intercept: true);
                        await chan.Writer.WriteAsync(key, token);
                    }
                    else
                    {
                        // Back off a bit so we don't burn 100% of the thread.
                        await Task.Delay(1, token);
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
            if (string.Equals(GameState.Mode, "singleplayer", StringComparison.OrdinalIgnoreCase))
            {
                _tickPump ??= Task.Run(() => TickPumpSingleplayer(token), token);
            }
            else
            {
                _tickPump ??= Task.Run(() => TickPumpMultiplayer(token), token);
            }
        }
    }

    private static async Task TickPumpSingleplayer(CancellationToken token)
    {
        // Singleplayer: drain all buffered inputs immediately, keep only the latest per player.
        var reader = InputEvents.Reader;

        while (await reader.WaitToReadAsync(token))
        {
            reader.TryRead(out var first);

            var inputs = new Dictionary<int, ConsoleKeyInfo> { [first.playerId] = first.keyInfo };

            // Drain all pending inputs in the queue, keeping only the latest per player.
            // This prevents OS keyboard repeat buffer from causing movement overshoot.
            while (reader.TryRead(out var next))
            {
                inputs[next.playerId] = next.keyInfo;
            }

            try
            {
                // Advance the game by one tick with the snapshot of inputs.
                await Tick.CreateAsync(
                    inputs,
                    GameState.Level0,
                    0 /*change the 0 later to the actual tick number, when tick storage exists*/
                );

                // Rate-limit ticks to smooth out OS keyboard repeat floods.
                var elapsed = DateTime.UtcNow - LastTickTime;
                if (elapsed < TimeSpan.FromMilliseconds(TickDelayMs))
                {
                    await Task.Delay(TickDelayMs - (int)elapsed.TotalMilliseconds);
                }
                LastTickTime = DateTime.UtcNow;
            }
            catch
            {
                // Swallow errors until the world/player is initialized.
            }
        }
    }

    private static async Task<bool> WaitForNextTickInputAsync(
        Dictionary<int, Dictionary<int, ConsoleKeyInfo>> InputStorage,
        CancellationToken token
    )
    {
        while (!token.IsCancellationRequested)
        {
            lock (InputStorageLock)
            {
                if (InputStorage.ContainsKey(GameState.TickNumber + 1))
                {
                    //GameState.MessageWindow.Write("[green]true[/]");
                    return true;
                }
            }
            // Yield to let other tasks run, but wake up instantly if data arrives
            await Task.Yield();
        }
        //GameState.MessageWindow.Write("[green]false[/])");
        return false;
    }

    private static async Task TickPumpMultiplayer(CancellationToken token)
    {
        // muisto hyvistä ajoista
        //_ = Task.Run(() => ReadReaderAsync(reader, token, GameState.InputStorage), token);

        while (await WaitForNextTickInputAsync(GameState.InputStorage, token))
        {
            // Input delay: if the local player has input for this tick,
            // wait TickDelayMs (50ms) from when it was stored before executing.
            // This gives the remote player's input time to cross the network.
            // Not lockstep — always proceeds after the deadline even if
            // the remote input hasn't arrived (rollback handles that).
            int nextTick = GameState.TickNumber + 1;
            int remotePlayer = 3 - Lobby.PlayerNumber;

            DateTime? localTime;
            lock (InputStorageLock)
            {
                LocalInputTime.TryGetValue(nextTick, out var t);
                localTime = t == default ? null : t;
            }

            if (localTime.HasValue)
            {
                // Wait until TickDelayMs after the local input was stored.
                // Break early if the remote input arrives — no need to wait longer.
                var deadline = localTime.Value.AddMilliseconds(TickDelayMs);
                while (DateTime.UtcNow < deadline)
                {
                    bool hasRemote;
                    lock (InputStorageLock)
                    {
                        var tickInputs = GameState.InputStorage.GetValueOrDefault(nextTick);
                        hasRemote = tickInputs != null && tickInputs.ContainsKey(remotePlayer);
                    }
                    if (hasRemote)
                        break;
                    await Task.Delay(1, token);
                }
            }
            // If no local input for this tick (remote-only), proceed immediately —
            // local player is idle and the remote player drives the tick.

            try
            {
                await WorldMutex.WaitAsync(token);
                try
                {
                    // Read inputs inside WorldMutex so that
                    // copy-inputs + execute + advance-tick is atomic.
                    var inputs = new Dictionary<int, ConsoleKeyInfo>();
                    lock (InputStorageLock)
                    {
                        inputs = new Dictionary<int, ConsoleKeyInfo>(
                            GameState.InputStorage[GameState.TickNumber + 1]
                        );
                    }
                    await Tick.CreateAsync(inputs, GameState.Level0, GameState.TickNumber + 1);
                    // Advance the global tick by 1.
                    GameState.TickNumber++;
                }
                finally
                {
                    WorldMutex.Release();
                }
            }
            catch
            {
                // Swallow errors until the world/player is initialized.
            }
        }
    }

    public static async Task ReceiveInput(string bigMessage)
    {
        // receive
        // Handles inputs arriving over the network as small text messages.
        // Special cases:
        //  - "iup"   : scroll message window up
        //  - "idown" : scroll message window down
        // General format for keystrokes: i{playerId}{action}{actionInfo}t{tickNumber}
        //   e.g., "i2M6t42" means player 2 performed action M with info 6 on tick 42.
        //GameState.MessageWindow.Write($"[blue]Received: {bigMessage}[/]");

        string[] messages = bigMessage.Split(',');

        foreach (var message in messages)
        {
            // get 1 message out of the 3
            if (message == "iup")
            {
                GameState.MessageWindow.OffsetUp();
                continue;
            }

            if (message == "idown")
            {
                GameState.MessageWindow.OffsetDown();
                continue;
            }

            if (string.IsNullOrWhiteSpace(message) || message[0] != 'i')
                continue;

            // Expect: i{playerId}{action}{actionInfo}t{tickNumber}
            // Parse player ID: extract digits after 'i'
            int index = 1;
            while (index < message.Length && char.IsDigit(message[index]))
            {
                index++;
            }

            if (index == 1 || index >= message.Length)
            {
                continue;
            }

            if (!int.TryParse(message.AsSpan(1, index - 1), out int playerId))
            {
                continue;
            }

            // Parse action: single character after player ID
            char action = message[index];
            index++;

            // Parse actionInfo: digits/characters until 't'
            int actionInfoStart = index;
            while (index < message.Length && message[index] != 't')
            {
                index++;
            }

            if (index == actionInfoStart || index >= message.Length)
            {
                continue;
            }

            string actionInfo = message.Substring(actionInfoStart, index - actionInfoStart);
            index++; // skip the 't'

            // Parse tick number (remaining part)
            if (index >= message.Length)
            {
                continue;
            }

            if (!int.TryParse(message.AsSpan(index), out int tickNumber))
            {
                continue;
            }

            // For now, map actionInfo to a ConsoleKey for EnqueueInput
            // actionInfo contains direction numbers like "6", "4", "8", "2", etc.
            // Map these back to movement keys or handle appropriately
            ConsoleKey mappedKey = InputHelper.ActionInfoToConsoleKey(actionInfo);
            if (mappedKey == ConsoleKey.NoName)
            {
                continue;
            }

            var keyInfo = new ConsoleKeyInfo(
                InputHelper.KeyCharFromConsoleKey(mappedKey),
                mappedKey,
                false,
                false,
                false
            );

            // mega check here to have em put to like tick storage idk
            // only enque inputs that have not been yet done? idk
            bool needsRollback = false;
            int rollbackFrom = 0;

            lock (InputStorageLock)
            {
                GameState.InputStorage.TryAdd(tickNumber, new Dictionary<int, ConsoleKeyInfo>());
                if (!GameState.InputStorage[tickNumber].ContainsKey(playerId))
                {
                    //EnqueueInput(playerId, keyInfo, CancellationToken.None, tickNumber);
                    GameState.InputStorage[tickNumber][playerId] = keyInfo;
                    /*
                    GameState.MessageWindow.Write(
                        $"[green]Added a input: for tick {tickNumber} PID: {playerId}[/]"
                    );
                    */

                    // do we need to rollback?
                    if (tickNumber <= GameState.TickNumber)
                    {
                        needsRollback = true;
                        rollbackFrom = tickNumber;
                    }
                }
            }

            if (needsRollback)
            {
                await WorldMutex.WaitAsync();
                try
                {
                    // Re-read TickNumber under WorldMutex — the tick pump may
                    // have advanced since we checked outside the lock (Bug 4/5).
                    int rollbackTo = GameState.TickNumber;

                    GameState.MessageWindow.Write(
                        $"[red]rolling back from {rollbackFrom} to {rollbackTo}[/]"
                    );

                    // Guard: snapshot for rollbackFrom must exist.
                    if (!GameState.WorldStorage.ContainsKey(rollbackFrom))
                    {
                        GameState.MessageWindow.Write(
                            $"[red]No snapshot for tick {rollbackFrom}, skipping rollback[/]"
                        );
                        continue;
                    }

                    var oldWorld = GameState.Level0.World;
                    GameState.Level0.World = GameState.WorldStorage[rollbackFrom];
                    World.Destroy(oldWorld);
                    // Remove the entry so the replay loop won't destroy the now-live world
                    GameState.WorldStorage.Remove(rollbackFrom);
                    for (int i = rollbackFrom; i <= rollbackTo; i++)
                    {
                        Dictionary<int, ConsoleKeyInfo> rollbackInputs;
                        lock (InputStorageLock)
                        {
                            rollbackInputs = new Dictionary<int, ConsoleKeyInfo>(
                                GameState.InputStorage[i]
                            );
                        }
                        GameState.MessageWindow.Write("[red]got rollbackInputs[/]");
                        GameState.MessageWindow.Write($"[red]i: {i} rollbackTo: {rollbackTo}[/]");
                        // I am Zagos
                        if (i == rollbackTo)
                        {
                            await Tick.CreateAsync(rollbackInputs, GameState.Level0, i);
                            GameState.MessageWindow.Write($"[red]last rollback {i}[/]");
                        }
                        else
                        {
                            await Tick.RollBackCreateAsync(rollbackInputs, GameState.Level0, i);
                            GameState.MessageWindow.Write($"[red]rollback {i}[/]");
                        }
                    }
                    GameState.MessageWindow.Write("[red]rolled back[/]");
                }
                finally
                {
                    WorldMutex.Release();
                }
            }
        }
    }

    // Consumer that maps keys to handlers
    public static async Task RunConsumer(
        ChannelReader<ConsoleKeyInfo> reader,
        CancellationToken token = default
    )
    {
        EnsureTickPumpStarted(token);

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
                var tickNumber = GameState.TickNumber;
                var action = "M";
                var actionInfo = InputHelper.GetActionInfoWithKey(keyInfo.Key);
                if (actionInfo == "")
                {
                    continue;
                }
                if (UdpTransport.connected)
                {
                    //GameState.MessageWindow.Write($"{playerId}");

                    lock (NextLocalTickLock)
                    {
                        if (NextLocalTickNumber == -10)
                        {
                            // First input ever: initialize to current global tick
                            NextLocalTickNumber = GameState.TickNumber;
                        }
                        else if (NextLocalTickNumber <= GameState.TickNumber)
                        {
                            // We've fallen behind (multiplayer inputs advanced the global tick)
                            // Resync to the next available tick
                            NextLocalTickNumber = GameState.TickNumber;
                        }
                        tickNumber = NextLocalTickNumber + 1;
                        NextLocalTickNumber++;
                    }
                    input10 = input9;
                    input9 = input8;
                    input8 = input7;
                    input7 = input6;
                    input6 = input5;
                    input5 = input4;
                    input4 = input3;
                    input3 = input2;
                    input2 = input1;
                    input1 = $"i{playerId}{action}{actionInfo}t{tickNumber}";

                    // Mirror local input to peers: "i{playerId}{ConsoleKey}".
                    var message =
                        $"{input1},{input2},{input3},{input4},{input5},{input6},{input7},{input8},{input9},{input10}";

                    await UdpTransport.SendMessage(message);
                    //GameState.MessageWindow.Write($"input sent: {DateTime.UtcNow:mm:ss.fff}");
                }
                // Always feed local input into the authoritative pipeline.
                bool needsLocalRollback = false;
                int localRollbackFrom = 0;
                lock (InputStorageLock)
                {
                    GameState.InputStorage.TryAdd(
                        tickNumber,
                        new Dictionary<int, ConsoleKeyInfo>()
                    );
                    GameState.InputStorage[tickNumber][playerId] = keyInfo;
                    // Record when this local input was stored so the tick pump
                    // can wait TickDelayMs before executing this tick.
                    if (!LocalInputTime.ContainsKey(tickNumber))
                        LocalInputTime[tickNumber] = DateTime.UtcNow;
                    GameState.MessageWindow.Write(
                        $"[green]Added a input: for tick {tickNumber} PID: {playerId}[/]"
                    );
                    // If the tick pump already executed this tick, we need to rollback.
                    if (tickNumber <= GameState.TickNumber)
                    {
                        needsLocalRollback = true;
                        localRollbackFrom = tickNumber;
                    }
                }
                if (needsLocalRollback)
                {
                    await WorldMutex.WaitAsync();
                    try
                    {
                        int rollbackTo = GameState.TickNumber;
                        GameState.MessageWindow.Write(
                            $"[red]Local input rollback from {localRollbackFrom} to {rollbackTo}[/]"
                        );
                        if (!GameState.WorldStorage.ContainsKey(localRollbackFrom))
                        {
                            GameState.MessageWindow.Write(
                                $"[red]No snapshot for tick {localRollbackFrom}, skipping rollback[/]"
                            );
                        }
                        else
                        {
                            var oldWorld = GameState.Level0.World;
                            GameState.Level0.World = GameState.WorldStorage[localRollbackFrom];
                            World.Destroy(oldWorld);
                            GameState.WorldStorage.Remove(localRollbackFrom);
                            for (int i = localRollbackFrom; i <= rollbackTo; i++)
                            {
                                Dictionary<int, ConsoleKeyInfo> rollbackInputs;
                                lock (InputStorageLock)
                                {
                                    rollbackInputs = new Dictionary<int, ConsoleKeyInfo>(
                                        GameState.InputStorage[i]
                                    );
                                }
                                if (i == rollbackTo)
                                {
                                    await Tick.CreateAsync(rollbackInputs, GameState.Level0, i);
                                }
                                else
                                {
                                    await Tick.RollBackCreateAsync(
                                        rollbackInputs,
                                        GameState.Level0,
                                        i
                                    );
                                }
                            }
                            GameState.MessageWindow.Write("[red]local rollback done[/]");
                        }
                    }
                    finally
                    {
                        WorldMutex.Release();
                    }
                }
            }
            catch
            {
                // Ignore input if error
            }
        }
    }
}
