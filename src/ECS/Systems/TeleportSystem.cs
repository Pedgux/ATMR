using Arch.Core;
using ATMR.Components;
using ATMR.Game;

namespace ATMR.Systems;

/// <summary>
/// Teleports entities.
/// </summary>
public static class TeleportSystem
{
    public static void Run(World world)
    {
        var teleportables = new QueryDescription().WithAll<Position, TeleportIntent>();

        world.Query(
            in teleportables,
            (Entity entity, ref Position pos, ref TeleportIntent tp) =>
            {
                bool canTeleport = !GameState.Level0.Spatial.IsBlocked(tp.X, tp.Y);
                if (canTeleport)
                {
                    bool moveSucceeded = GameState.Level0.Spatial.TryMoveOccupancy(
                        entity,
                        (pos.X, pos.Y),
                        (tp.X, tp.Y)
                    );
                    if (moveSucceeded)
                    {
                        // replace the last cell the entity was in, so no duplicates appear
                        GameState.GridWindow.RestoreBaseTile(pos.X, pos.Y);

                        // teleport em
                        pos.X = tp.X;
                        pos.Y = tp.Y;
                    }
                }
            }
        );
        world.Remove<TeleportIntent>(in teleportables);
    }
}
