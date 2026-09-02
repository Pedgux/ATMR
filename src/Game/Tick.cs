namespace ATMR.Tick;

using Arch.Core;
using ATMR.Game;
using ATMR.Systems;
using System.Diagnostics;
using ATMR.Helpers;

/// <summary>
/// Represents a single game tick that orchestrates the execution of all game systems in a defined order.
/// </summary>
/// <remarks>
/// Each tick processes player input, updates entity movement, and renders the game state sequentially.
/// Ticks are numbered to track game progression and can be created asynchronously to ensure all systems
/// complete their operations before the next tick begins.
/// </remarks>
public class Tick
{
    /// <summary>
    /// Gets the sequential number identifying this tick in the game loop.
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tick"/> class with the specified tick number.
    /// </summary>
    /// <param name="tickNumber">The sequential number for this tick.</param>
    private Tick(int tickNumber)
    {
        Number = tickNumber;
    }

    /// <summary>
    /// Creates and executes a new game tick, processing all systems in order.
    /// </summary>
    /// <param name="input">A dictionary mapping player input by entity ID to console key information.</param>
    /// <param name="level">The current game level containing the world and all entities.</param>
    /// <param name="tickNumber">The sequential number for this tick.</param>
    public static async Task<Tick> CreateAsync(
        Dictionary<int, (char action, string actionInfo)> input,
        Level level,
        int tickNumber,
        bool rollBack
    )
    {
        var tickWatch = Stopwatch.StartNew();

        if (GameState.WorldStorage.TryGetValue(tickNumber, out var oldSnapshot))
        {
            var snapshotDestroyWatch = Stopwatch.StartNew();
            World.Destroy(oldSnapshot);
            //Log.Write($"[grey]tick {tickNumber} snapshot destroy: {snapshotDestroyWatch.ElapsedMilliseconds} ms[/]");
        }

        var snapshotWatch = Stopwatch.StartNew();
        GameState.WorldStorage[tickNumber] = GameState.Level0.GetSnapshot();
        
        // Log.Write($"[grey]tick {tickNumber} snapshot copy: {snapshotWatch.ElapsedMilliseconds} ms[/]");
        // ööö wth is this. joo se
        var tick = new Tick(tickNumber);

        var intents = InputSystem.Run(level.World, input);
        DigSystem.Run(level.World, intents);
        // joskus se incrementtijuttu (et voi interruptaa)
        CollisionSystem.Run(level.World);
        
        MovementSystem.Run(level.World);
        TeleportSystem.Run(level.World);
        FollowSystem.Run(level.World);
        HealthSystem.Run(level.World);
        DestroySystem.Run(level.World);

        
        if (!rollBack)
        {
            var renderWatch = Stopwatch.StartNew();
            RenderSystem.Run(level.World);
            // Log.Write($"[grey]tick {tickNumber} render: {renderWatch.ElapsedMilliseconds} ms[/]");
        }

        // Log.Write($"[grey]tick {tickNumber} total: {tickWatch.ElapsedMilliseconds} ms[/]");
        return tick;
        // miau miau miu mau
    }
}
// NÄÄSILÄLTÄ MORO
