namespace ATMR.Tick;

using Arch.Core;
using ATMR.Game;
using ATMR.Systems;

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
        Dictionary<int, ConsoleKeyInfo> input,
        Level level,
        int tickNumber,
        bool rollBack
    )
    {
        if (GameState.WorldStorage.TryGetValue(tickNumber, out var oldSnapshot))
        {
            World.Destroy(oldSnapshot);
        }
        GameState.WorldStorage[tickNumber] = GameState.Level0.GetSnapshot();
        // ööö wth is this
        var tick = new Tick(tickNumber);

        InputSystem.Run(level.World, input);
        // joskus se incrementtijuttu (et voi interruptaa)
        CollisionSystem.Run(level.World);
        MovementSystem.Run(level.World);
        FollowSystem.Run(level.World);
        HealthSystem.Run(level.World);
        DestroySystem.Run(level.World);

        if (!rollBack)
        {
            RenderSystem.Run(level.World);
        }

        return tick;
        // miau miau miu mau
    }
}
// NÄÄSILÄLTÄ MORO
