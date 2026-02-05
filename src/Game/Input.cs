namespace ATMR.Input;

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using ATMR.Game;
using ATMR.Helpers;
using ATMR.Networking;
using ATMR.Tick;

public static class Input
{
    // Batches keystrokes for a short window so multiple players' inputs
    // can be processed together in a single game tick for multiplayer.
    private const int TickDelayMs = 1000;
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

    // Synchronizes access to inputList in multiplayer mode to prevent race conditions.
    private static readonly object InputListLock = new();

    // Rate-limit singleplayer ticks to smooth out OS keyboard repeat floods.
    private static DateTime LastTickTime = DateTime.MinValue;
    private static DateTime _previousTime = DateTime.UtcNow;
    private static int NextLocalTickNumber = -10;
    private static readonly object NextLocalTickLock = new();

    // 1 is newest, 3 oldest. Used to send older inputs too.
    private static string input1 = ""; // now
    private static string input2 = ""; // yesterday
    private static string input3 = ""; // eldest

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

    private static async Task ReadReaderAsync(
        ChannelReader<(int playerId, ConsoleKeyInfo keyInfo, int tickNumber)> reader,
        CancellationToken token,
        List<Dictionary<int, Dictionary<int, ConsoleKeyInfo>>> inputList
    )
    {
        while (await reader.WaitToReadAsync(token))
        {
            GameState.MessageWindow.Write("pääsekö tänne?");
            // Collect the first event and then coalesce additional inputs
            // for a short window so the tick sees a snapshot for all players. öö EI
            while (reader.TryRead(out var first))
            {
                GameState.MessageWindow.Write(
                    $"[green]Added a input: for tick {first.tickNumber} PID: {first.playerId}[/]"
                );
                lock (InputListLock)
                {
                    inputList.Add(
                        new Dictionary<int, Dictionary<int, ConsoleKeyInfo>>
                        {
                            {
                                first.tickNumber,
                                new Dictionary<int, ConsoleKeyInfo>
                                {
                                    [first.playerId] = first.keyInfo,
                                }
                            },
                        }
                    );
                }
            }
        }
    }

    private static async Task<bool> WaitForNextTickInputAsync(
        List<Dictionary<int, Dictionary<int, ConsoleKeyInfo>>> inputList,
        CancellationToken token
    )
    {
        while (!token.IsCancellationRequested)
        {
            lock (InputListLock)
            {
                if (inputList.Any(dict => dict.ContainsKey(GameState.TickNumber + 1)))
                {
                    GameState.MessageWindow.Write("[green]-true-[/])");
                    return true;
                }
            }
            // Yield to let other tasks run, but wake up instantly if data arrives
            await Task.Yield();
        }
        GameState.MessageWindow.Write("[green]-false-[/])");
        return false;
    }

    private static async Task TickPumpMultiplayer(CancellationToken token)
    {
        // Multiplayer: coalesce inputs within a window for synchronization.
        ChannelReader<(int playerId, ConsoleKeyInfo keyInfo, int tickNumber)> reader =
            InputEvents.Reader;
        // Put all inputs from reader here, to enable multiple inputs in 1 tick.
        // Holds all inputs from all players, discards them when read later
        // format is: <ticknumber, <playernumber, consolekeyinfo>>
        var inputList = new List<Dictionary<int, Dictionary<int, ConsoleKeyInfo>>>();
        _ = Task.Run(() => ReadReaderAsync(reader, token, inputList), token);

        while (await WaitForNextTickInputAsync(inputList, token))
        {
            GameState.MessageWindow.Write("[yellow]Tapahtuu[/]");
            // Find all dictionaries containing the current tick
            List<Dictionary<int, Dictionary<int, ConsoleKeyInfo>>> relevantDicts1;
            lock (InputListLock)
            {
                relevantDicts1 = inputList
                    .Where(dict => dict.ContainsKey(GameState.TickNumber + 1))
                    .ToList();
            }

            bool localPlayerInput = false;

            // For each player, take inputs
            foreach (var tickDict in relevantDicts1)
            {
                foreach (var kvp in tickDict[GameState.TickNumber + 1])
                {
                    int playerId = kvp.Key;
                    if (playerId == Lobby.PlayerNumber)
                    {
                        localPlayerInput = true;
                    }
                }
            }

            if (localPlayerInput)
            {
                var time = DateTime.UtcNow;
                var delta = time - _previousTime;
                if (delta > TickWaitWindow)
                {
                    delta = TickWaitWindow;
                }

                //GameState.MessageWindow.Write("Delta: " + delta.ToString(@"mm\:ss\.fff"));
                //GameState.MessageWindow.Write($"Times: {time:mm:ss.fff} {_previousTime:mm:ss.fff}");

                var deadline = time + delta;
                _previousTime = time + delta;

                while (true)
                {
                    var remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                    {
                        break;
                    }

                    // Wait for either more input or the coalescing timeout to elapse.
                    //var waitForMore = reader.WaitToReadAsync(token).AsTask();
                    var timeout = Task.Delay(remaining, token);
                    var completed = await Task.WhenAny( /*waitForMore,*/
                        timeout
                    );
                    if (completed == timeout)
                    {
                        break;
                    }
                }
            }

            try
            {
                List<Dictionary<int, Dictionary<int, ConsoleKeyInfo>>> relevantDicts;
                var inputs = new Dictionary<int, ConsoleKeyInfo>();

                lock (InputListLock)
                {
                    // Find all dictionaries containing the current tick
                    relevantDicts = inputList
                        .Where(dict => dict.ContainsKey(GameState.TickNumber + 1))
                        .ToList();

                    // For each player, take inputs
                    foreach (var tickDict in relevantDicts)
                    {
                        foreach (var kvp in tickDict[GameState.TickNumber + 1])
                        {
                            int playerId = kvp.Key;
                            ConsoleKeyInfo keyInfo = kvp.Value;
                            // Oh no scenario
                            if (inputs.ContainsKey(playerId))
                            {
                                GameState.MessageWindow.Write(
                                    "[red]!!! Double inputs detected !!![/]"
                                );
                            }
                            inputs[playerId] = keyInfo;
                        }
                    }

                    // Remove all processed dictionaries from inputList to avoid reprocessing
                    foreach (var tickDict in relevantDicts)
                    {
                        inputList.Remove(tickDict);
                    }
                }
                // Advance the game by one tick with the snapshot of inputs.
                GameState.MessageWindow.Write(
                    $"[yellow]Starting tick: {GameState.TickNumber + 1}[/]"
                );
                await Tick.CreateAsync(inputs, GameState.Level0, GameState.TickNumber + 1);
                // Advance the global tick by 1.
                GameState.TickNumber++;
            }
            catch
            {
                // Swallow errors until the world/player is initialized.
            }
        }
    }

    private static void EnqueueInput(
        int playerId,
        ConsoleKeyInfo keyInfo,
        CancellationToken token,
        int tickNumber
    )
    {
        // Ensure the tick pump is running before writing; write is non-blocking here.
        EnsureTickPumpStarted(token);
        InputEvents.Writer.TryWrite((playerId, keyInfo, tickNumber));
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

    public static Task ReceiveInput(string bigMessage)
    {
        // receive
        // Handles inputs arriving over the network as small text messages.
        // Special cases:
        //  - "iup"   : scroll message window up
        //  - "idown" : scroll message window down
        // General format for keystrokes: i{playerId}{action}{actionInfo}t{tickNumber}
        //   e.g., "i2M6t42" means player 2 performed action M with info 6 on tick 42.
        GameState.MessageWindow.Write($"[blue]Received: {bigMessage}[/]");

        string[] messages = bigMessage.Split(',');

        foreach (var message in messages)
        {
            // get 1 message out of the 3
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

            // Expect: i{playerId}{action}{actionInfo}t{tickNumber}
            // Parse player ID: extract digits after 'i'
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
                return Task.CompletedTask;
            }

            string actionInfo = message.Substring(actionInfoStart, index - actionInfoStart);
            index++; // skip the 't'

            // Parse tick number (remaining part)
            if (index >= message.Length)
            {
                return Task.CompletedTask;
            }

            if (!int.TryParse(message.AsSpan(index), out int tickNumber))
            {
                return Task.CompletedTask;
            }

            // For now, map actionInfo to a ConsoleKey for EnqueueInput
            // actionInfo contains direction numbers like "6", "4", "8", "2", etc.
            // Map these back to movement keys or handle appropriately
            ConsoleKey mappedKey = Keybinds.ActionInfoToConsoleKey(actionInfo);
            if (mappedKey == ConsoleKey.NoName)
            {
                return Task.CompletedTask;
            }

            var keyInfo = new ConsoleKeyInfo(
                KeyCharFromConsoleKey(mappedKey),
                mappedKey,
                false,
                false,
                false
            );

            // mega check here to have em put to like tick storage idk
            // only enque inputs that have not been yet done? idk

            EnqueueInput(playerId, keyInfo, CancellationToken.None, tickNumber);
        }

        return Task.CompletedTask;
    }

    // Example consumer that maps keys to handlers
    public static async Task RunConsumer(
        ChannelReader<ConsoleKeyInfo> reader,
        CancellationToken token = default
    )
    {
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
                if (UdpTransport.connected)
                {
                    //GameState.MessageWindow.Write($"{playerId}");
                    var action = "M";
                    var actionInfo = Keybinds.GetActionWithKey(keyInfo.Key);
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
                    input3 = input2;
                    input2 = input1;
                    input1 = $"i{playerId}{action}{actionInfo}t{tickNumber}";

                    // Mirror local input to peers: "i{playerId}{ConsoleKey}".
                    var message = $"{input1},{input2},{input3}";

                    await UdpTransport.SendMessage(message);
                    //GameState.MessageWindow.Write($"input sent: {DateTime.UtcNow:mm:ss.fff}");
                }
                // Always feed local input into the authoritative pipeline.
                EnqueueInput(playerId, keyInfo, token, tickNumber);
            }
            catch
            {
                // Ignore input if error
            }
        }
    }
}
