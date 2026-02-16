namespace ATMR.Helpers;

using Arch.Core;
using ATMR.Components;
using ATMR.Game;

/// <summary>
/// Computes a deterministic hash of the world state for desync detection.
/// </summary>
public static class WorldChecksum
{
    /// <summary>
    /// Hashes all Player entity positions and IDs into a single int.
    /// Deterministic as long as Arch query iteration order is stable.
    /// </summary>
    public static int Compute(World world)
    {
        int hash = 17;
        var query = new QueryDescription().WithAll<Position, Player, Velocity>();
        world.Query(
            in query,
            (ref Position pos, ref Player p, ref Velocity vel) =>
            {
                hash = unchecked(hash * 31 + p.ID);
                hash = unchecked(hash * 31 + pos.X);
                hash = unchecked(hash * 31 + pos.Y);
                hash = unchecked(hash * 31 + vel.X);
                hash = unchecked(hash * 31 + vel.Y);
            }
        );
        return hash;
    }

    /// <summary>
    /// Logs the checksum for a given tick to the message window.
    /// </summary>
    public static void LogTick(World world, int tickNumber, string label = "tick")
    {
        var checksum = Compute(world);
        GameState.MessageWindow.Write($"[yellow]chk {label} t{tickNumber}: {checksum:X8}[/]");
    }
}
