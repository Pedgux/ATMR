namespace ATMR.Input;

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Arch.Core;
using Arch.Core.Extensions;
using ATMR.Components;
using ATMR.Game;
using ATMR.Helpers;
using ATMR.Networking;
using ATMR.Systems;
using ATMR.Tick;

public static class Input
{
    // Batches keystrokes for a short window so multiple players' inputs
    // can be processed together in a single game tick for multiplayer.
    private const int TickDelayMs = 1;

    // Central, thread-safe pipeline of input events coming from local or network sources.
    // Tuple payload: (playerId, action, actionInfo). Single reader (the tick pump) with many writers.
    private static readonly Channel<(
        int playerId,
        char action,
        string actionInfo,
        int tickNumber
    )> InputEvents = Channel.CreateUnbounded<(
        int playerId,
        char action,
        string actionInfo,
        int tickNumber
    )>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    // Background task that drains the InputEvents channel and emits game ticks.
    private static Task? _tickPump;

    // Ensures only one background pump is started across threads.
    private static readonly object TickPumpLock = new();

    // Synchronizes access to InputStorage in multiplayer mode to prevent race conditions.
    private static readonly object InputLock = new();

    // Prevents the tick pump and rollback from mutating the world concurrently.
    private static readonly SemaphoreSlim WorldMutex = new(1, 1);

    // Tracks when local input was first stored for each tick number.
    // The tick pump waits TickDelayMs after this timestamp before executing,
    // giving the remote player's input time to arrive.
    // Accessed under InputLock.
    private static readonly Dictionary<int, DateTime> LocalInputTime = new();

    // Rate-limit singleplayer ticks to smooth out OS keyboard repeat floods.
    private static DateTime LastTickTime = DateTime.MinValue;

    // MIKS TÄÄÄ ON MIINUS KYMMENEN okei ymmärrän (en)
    private static int NextLocalTickNumber = -10;
    private static readonly object NextLocalTickLock = new();
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

    // Prompt shown when entering the 2-step dig flow.
    private const string DigPromptMessage = "[yellow]Which way do you want to dig?[/]";

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

            var inputs = new Dictionary<int, (char action, string actionInfo)>
            {
                [first.playerId] = (first.action, first.actionInfo),
            };

            // Drain all pending inputs in the queue, keeping only the latest per player.
            // This prevents OS keyboard repeat buffer from causing movement overshoot.
            while (reader.TryRead(out var next))
            {
                inputs[next.playerId] = (next.action, next.actionInfo);
            }

            try
            {
                GameState.TickNumber++;
                // Advance the game by one tick with the snapshot of inputs.
                await Tick.CreateAsync(inputs, GameState.Level0, GameState.TickNumber, false);

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
        Dictionary<int, Dictionary<int, (char action, string actionInfo)>> InputStorage,
        CancellationToken token
    )
    {
        while (!token.IsCancellationRequested)
        {
            lock (InputLock)
            {
                if (InputStorage.ContainsKey(GameState.TickNumber + 1))
                {
                    return true;
                }
            }
            // Yield to let other tasks run, but wake up instantly if data arrives
            await Task.Yield();
        }
        return false;
    }

    private static async Task TickPumpMultiplayer(CancellationToken token)
    {
        while (await WaitForNextTickInputAsync(GameState.InputStorage, token))
        {
            int nextTick = GameState.TickNumber + 1;
            int remotePlayer = 3 - Lobby.PlayerNumber;

            // Wait TickDelayMs from when this tick's local input was stored.
            // If only remote input exists (local player idle), execute immediately.
            DateTime deadline;
            lock (InputLock)
            {
                deadline = LocalInputTime.TryGetValue(nextTick, out var t)
                    ? t.AddMilliseconds(TickDelayMs)
                    : DateTime.MinValue; // no local input → no delay
            }

            while (DateTime.UtcNow < deadline)
            {
                bool hasRemote;
                lock (InputLock)
                {
                    var tickInputs = GameState.InputStorage.GetValueOrDefault(nextTick);
                    hasRemote = tickInputs != null && tickInputs.ContainsKey(remotePlayer);
                }
                if (hasRemote)
                    break;
                await Task.Delay(1, token);
            }

            try
            {
                await WorldMutex.WaitAsync(token);
                try
                {
                    // Read inputs inside WorldMutex so that
                    // copy-inputs + execute + advance-tick is atomic.
                    int executingTick = GameState.TickNumber + 1;
                    Dictionary<int, (char action, string actionInfo)> inputs;
                    lock (InputLock)
                    {
                        inputs = new Dictionary<int, (char action, string actionInfo)>(
                            GameState.InputStorage[executingTick]
                        );
                    }
                    await Tick.CreateAsync(inputs, GameState.Level0, executingTick, false);
                    GameState.TickNumber = executingTick;

                    // Guard against inputs that arrived during execution.
                    // Between reading InputStorage and completing execution,
                    // ReceiveInput or RunConsumer may have stored new entries
                    // for this tick. Because TickNumber hadn't been incremented
                    // yet, those code paths saw it as a future tick and skipped
                    // rollback. Re-check now and replay if inputs changed.
                    bool needsReplay;
                    Dictionary<int, (char action, string actionInfo)> updatedInputs;
                    lock (InputLock)
                    {
                        var current = GameState.InputStorage[executingTick];
                        needsReplay = current.Count > inputs.Count;
                        updatedInputs = needsReplay
                            ? new Dictionary<int, (char action, string actionInfo)>(current)
                            : inputs;
                    }
                    if (needsReplay && GameState.WorldStorage.ContainsKey(executingTick))
                    {
                        Log.Write($"[yellow]Re-exec tick {executingTick} (late input)[/]");
                        var oldWorld = GameState.Level0.World;
                        GameState.Level0.World = GameState.WorldStorage[executingTick];
                        World.Destroy(oldWorld);
                        GameState.WorldStorage.Remove(executingTick);
                        await Tick.CreateAsync(
                            updatedInputs,
                            GameState.Level0,
                            executingTick,
                            false
                        );
                    }
                }
                finally
                {
                    WorldMutex.Release();
                }
            }
            catch (Exception ex)
            {
                Log.Write($"[red]TickPump error: {ex.Message}[/]");
            }
        }
    }

    private static bool ShouldAcceptLocalInput(char action, string actionInfo)
    {
        // Only movement intents are filtered before send/store.
        if (action != 'M')
        {
            return true;
        }

        if (!InputHelper.TryGetDirectionOffset(actionInfo, out int dx, out int dy))
        {
            return true;
        }

        var world = GameState.Level0.World;
        if (!GameState.LocalPlayer.IsAlive() || !world.Has<Position>(GameState.LocalPlayer))
        {
            return true;
        }

        ref var localPos = ref world.Get<Position>(GameState.LocalPlayer);
        int targetX = localPos.X + dx;
        int targetY = localPos.Y + dy;

        bool isBlocked = CollisionSystem.IsBlocked(targetX, targetY);

        return !isBlocked;
    }

    public static async Task ReceiveInput(string bigMessage)
    {
        // Handles inputs arriving over the network as small text messages.
        // General format for keystrokes: i{playerId}{action}{actionInfo}t{tickNumber}
        //   e.g., "i2M6t42" means player 2 performed action M with info 6 on tick 42.

        string[] messages = bigMessage.Split(',');

        // Phase 1: Store ALL new inputs from this packet, track earliest tick
        // that needs rollback. This avoids cascading rollbacks for each message.
        int earliestRollback = int.MaxValue;
        bool anyNewInput = false;

        foreach (var message in messages)
        {
            if (message == "iup")
            {
                Log.OffsetUp();
                continue;
            }

            if (message == "idown")
            {
                Log.OffsetDown();
                continue;
            }

            //------ surkea viesti detectaus alkaa ------
            if (string.IsNullOrWhiteSpace(message) || message[0] != 'i')
                continue;

            // Expect: i{playerId}{action}{actionInfo}t{tickNumber}
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
            //------ surkea viesti detectaus loppuu ------

            // Parse action: single character after player ID
            char action = message[index];
            index++;

            // Parse actionInfo: substring bounded by the start and the last 't'
            int actionInfoStart = index;
            int lastTIndex = message.LastIndexOf('t');

            if (lastTIndex <= actionInfoStart)
            {
                continue;
            }

            string actionInfo = message.Substring(actionInfoStart, lastTIndex - actionInfoStart);

            // Parse tick number (remaining part after the last 't')
            if (lastTIndex + 1 >= message.Length)
            {
                continue;
            }

            if (!int.TryParse(message.AsSpan(lastTIndex + 1), out int tickNumber))
            {
                continue;
            }

            switch (action)
            {
                case 'M':
                    if (string.IsNullOrEmpty(actionInfo))
                    {
                        continue;
                    }
                    break;
                case 'P':
                    if (string.IsNullOrEmpty(actionInfo))
                    {
                        continue;
                    }
                    break;
                case 'D':
                    // 'D' means a directional dig action already resolved by the sender
                    if (!InputHelper.IsDirectionalActionInfo(actionInfo))
                    {
                        continue;
                    }
                    break;
                default:
                    continue;
            }

            // Store the input — track whether it's new and needs rollback.
            lock (InputLock)
            {
                GameState.InputStorage.TryAdd(
                    tickNumber,
                    new Dictionary<int, (char action, string actionInfo)>()
                );
                if (!GameState.InputStorage[tickNumber].ContainsKey(playerId))
                {
                    GameState.InputStorage[tickNumber][playerId] = (action, actionInfo);
                    anyNewInput = true;

                    if (tickNumber <= GameState.TickNumber && tickNumber < earliestRollback)
                    {
                        earliestRollback = tickNumber;
                    }
                }
            }
        }

        // Phase 2: Single rollback from the earliest new past-tick input.
        if (anyNewInput && earliestRollback != int.MaxValue)
        {
            await WorldMutex.WaitAsync();
            try
            {
                int rollbackTo = GameState.TickNumber;

                Log.Write($"[red]rolling back from {earliestRollback} to {rollbackTo}[/]");

                if (!GameState.WorldStorage.ContainsKey(earliestRollback))
                {
                    Log.Write(
                        $"[red]No snapshot for tick {earliestRollback}, skipping rollback[/]"
                    );
                    return;
                }

                var oldWorld = GameState.Level0.World;
                GameState.Level0.World = GameState.WorldStorage[earliestRollback];
                World.Destroy(oldWorld);
                GameState.WorldStorage.Remove(earliestRollback);

                for (int i = earliestRollback; i <= rollbackTo; i++)
                {
                    Dictionary<int, (char action, string actionInfo)> rollbackInputs;
                    lock (InputLock)
                    {
                        rollbackInputs = new Dictionary<int, (char action, string actionInfo)>(
                            GameState.InputStorage[i]
                        );
                    }
                    if (i == rollbackTo)
                    {
                        await Tick.CreateAsync(rollbackInputs, GameState.Level0, i, false);
                    }
                    else
                    {
                        await Tick.CreateAsync(rollbackInputs, GameState.Level0, i, true);
                    }
                }
                Log.Write("[red]rolled back[/]");
            }
            finally
            {
                WorldMutex.Release();
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
                    Log.OffsetUp();
                }
                catch { }

                continue;
            }

            if (keyInfo.Key == ConsoleKey.PageDown)
            {
                try
                {
                    Log.OffsetDown();
                }
                catch { }

                continue;
            }
            try
            {
                var playerId = string.Equals(
                    GameState.Mode,
                    "singleplayer",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? 1
                    : Lobby.PlayerNumber;
                var tickNumber = GameState.TickNumber;
                var action = 'M';
                string actionInfo = "";
                var pendingAction = GameState.PendingDirectionalAction;
                var isMenuOpen = GameState.CurrentMenu;

                // ISO PÖTKÖ
                if (isMenuOpen == MenuType.Pickup)
                {
                    if (keyInfo.Key == ConsoleKey.Escape)
                    {
                        GameState.CurrentMenu = MenuType.None;
                        GameState.MenuAmountBuffer = "";
                        GameState.MenuList.Clear();
                        Log.Write("Pickup cancelled.");
                        continue;
                    }
                    else if (char.IsDigit(keyInfo.KeyChar))
                    {
                        GameState.MenuAmountBuffer += keyInfo.KeyChar;
                        Log.Write($"Amount: {GameState.MenuAmountBuffer}");
                        continue;
                    }
                    else if (keyInfo.KeyChar >= 'a' && keyInfo.KeyChar <= 'z')
                    {
                        int itemIndex = keyInfo.KeyChar - 'a';
                        int amount = -1;
                        if (
                            !string.IsNullOrEmpty(GameState.MenuAmountBuffer)
                            && int.TryParse(GameState.MenuAmountBuffer, out int parsed)
                            && parsed > 0
                        )
                        {
                            amount = parsed;
                        }

                        GameState.MenuList[itemIndex] = amount;
                        Log.Write(
                            $"[green]Added to cart: item {(char)('a' + itemIndex)} (x{(amount == -1 ? "all" : amount)})[/]"
                        );

                        GameState.MenuAmountBuffer = "";
                        continue;
                    }
                    else if (keyInfo.KeyChar == ',')
                    {
                        var itemsAtPos = new List<Entity>();
                        var world = GameState.Level0.World;
                        if (
                            GameState.LocalPlayer != Entity.Null
                            && GameState.LocalPlayer.IsAlive()
                            && world.Has<Position>(GameState.LocalPlayer)
                        )
                        {
                            var playerPos = world.Get<Position>(GameState.LocalPlayer);
                            itemsAtPos = ItemSystem.GetItemsAt(world, playerPos);
                        }

                        for (int i = 0; i < itemsAtPos.Count; i++)
                        {
                            GameState.MenuList[i] = -1;
                        }

                        Log.Write($"[green]Added all {itemsAtPos.Count} items to cart[/]");
                        GameState.MenuAmountBuffer = "";
                        continue;
                    }
                    else if (keyInfo.Key == ConsoleKey.Spacebar)
                    {
                        if (GameState.MenuList.Count == 0)
                        {
                            Log.Write("Cart is empty. Select items first.");
                            continue;
                        }

                        var cartParts = new List<string>();
                        foreach (var kvp in GameState.MenuList.OrderBy(x => x.Key))
                        {
                            cartParts.Add($"{kvp.Key}:{kvp.Value}");
                        }
                        actionInfo = "PickupList_" + string.Join("_", cartParts);
                        action = 'P'; // Use 'P' so server filters it easily

                        GameState.CurrentMenu = MenuType.None;
                        GameState.MenuAmountBuffer = "";
                        GameState.MenuList.Clear();
                    }
                    else
                    {
                        Log.Write(
                            "Type an amount and an item letter (e.g. '5a'), Space to confirm"
                        );
                        continue; // ignore keys until valid pick or escape
                    }
                }
                else if (!string.IsNullOrEmpty(pendingAction))
                {
                    // We are waiting for direction after a prior action key (currently dig).
                    if (!InputHelper.TryGetDirectionActionInfo(keyInfo, out actionInfo))
                    {
                        // Ignore non-direction keys until the user gives a direction.
                        continue;
                    }

                    action = pendingAction[0];
                    GameState.PendingDirectionalAction = null;
                }
                else
                {
                    if (keyInfo.Key == ConsoleKey.F && keyInfo.Modifiers == 0)
                    {
                        // Plain F starts dig targeting mode locally.
                        // Important: we do NOT send/store this press yet.
                        GameState.PendingDirectionalAction = "D";
                        Log.Write(DigPromptMessage);
                        continue;
                    }

                    if (keyInfo.KeyChar == ',' || keyInfo.KeyChar == 'g')
                    {
                        var itemsAtPos = new List<Entity>();
                        var world = GameState.Level0.World;
                        if (
                            GameState.LocalPlayer != Entity.Null
                            && GameState.LocalPlayer.IsAlive()
                            && world.Has<Position>(GameState.LocalPlayer)
                        )
                        {
                            var playerPos = world.Get<Position>(GameState.LocalPlayer);
                            itemsAtPos = ItemSystem.GetItemsAt(world, playerPos);
                        }

                        if (itemsAtPos.Count == 0)
                        {
                            Log.Write("Nothing to pick up here.");
                            continue;
                        }
                        else if (itemsAtPos.Count == 1)
                        {
                            // Instantly pick up the single item (index 0, amount -1)
                            action = 'P';
                            actionInfo = "Pickup-1_0";
                        }
                        else
                        {
                            // Multiple items, show prompt menu
                            GameState.CurrentMenu = MenuType.Pickup;
                            GameState.MenuAmountBuffer = "";
                            GameState.MenuList.Clear();
                            Log.Write("[yellow]Multiple items here:[/]");
                            for (int i = 0; i < itemsAtPos.Count; i++)
                            {
                                char itemKey = (char)('a' + i);
                                var itemEntity = itemsAtPos[i];
                                string name = world.Get<Item>(itemEntity).Name;
                                if (world.Has<Stackable>(itemEntity))
                                {
                                    int count = world.Get<Stackable>(itemEntity).Count;
                                    Log.Write($"  [[{itemKey}]] {name} (x{count})");
                                }
                                else
                                {
                                    Log.Write($"  [[{itemKey}]] {name}");
                                }
                            }
                            Log.Write("Type amount then letter, or just letter");
                            continue;
                        }
                    }
                    else
                    {
                        actionInfo = InputHelper.GetActionInfoWithKey(keyInfo);
                        if (actionInfo == "")
                        {
                            continue;
                        }
                    }
                }

                if (!ShouldAcceptLocalInput(action, actionInfo))
                {
                    Log.Write("[grey]Blocked locally; input dropped[/]");
                    continue;
                }

                if (UdpTransport.connected)
                {
                    //Log.Write($"{playerId}");

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
                }
                else if (
                    string.Equals(
                        GameState.Mode,
                        "singleplayer",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    await InputEvents.Writer.WriteAsync(
                        (playerId, action, actionInfo, tickNumber),
                        token
                    );
                    continue; // Skip the multiplayer rollback logic below since singleplayer pump handles its own execution
                }

                // Always feed local input into the authoritative pipeline.
                bool needsLocalRollback = false;
                int localRollbackFrom = 0;
                lock (InputLock)
                {
                    GameState.InputStorage.TryAdd(
                        tickNumber,
                        new Dictionary<int, (char action, string actionInfo)>()
                    );
                    GameState.InputStorage[tickNumber][playerId] = (action, actionInfo);
                    // Record when local input was first stored for this tick.
                    if (!LocalInputTime.ContainsKey(tickNumber))
                        LocalInputTime[tickNumber] = DateTime.UtcNow;
                    /*Log.Write(
                        $"[green]Added a input: for tick {tickNumber} PID: {playerId}[/]"
                    );*/
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
                        Log.Write(
                            $"[red]Local input rollback from {localRollbackFrom} to {rollbackTo}[/]"
                        );
                        if (!GameState.WorldStorage.ContainsKey(localRollbackFrom))
                        {
                            Log.Write(
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
                                Dictionary<int, (char action, string actionInfo)> rollbackInputs;
                                lock (InputLock)
                                {
                                    rollbackInputs = new Dictionary<
                                        int,
                                        (char action, string actionInfo)
                                    >(GameState.InputStorage[i]);
                                }
                                if (i == rollbackTo)
                                {
                                    await Tick.CreateAsync(
                                        rollbackInputs,
                                        GameState.Level0,
                                        i,
                                        false
                                    );
                                }
                                else
                                {
                                    await Tick.CreateAsync(
                                        rollbackInputs,
                                        GameState.Level0,
                                        i,
                                        true
                                    );
                                }
                            }
                            Log.Write("[red]local rollback done[/]");
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
