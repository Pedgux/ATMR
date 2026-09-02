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
        var teleportables = new QueryDescription().WithAll<Position, Teleport>();

        world.Query(
            in teleportables,
            (Entity entity, ref Position pos, ref Teleport tp) =>
            {
                if (tp.X == 0 && tp.Y == 0)
                    return;

                bool isSolid = world.Has<Position, Solid>(entity);
                bool canTeleport = isSolid
                    ? CollisionSystem.TryMoveSolid(entity, pos.X, pos.Y, tp.X, tp.Y)
                    : !CollisionSystem.IsBlocked(tp.X, tp.Y);

                if (!canTeleport)
                {
                    tp.X = 0;
                    tp.Y = 0;
                    return;
                }

                // replace the last cell the entity was in, so no duplicates appear
                GameState.GridWindow.RestoreBaseTile(pos.X, pos.Y);

                // teleport em
                pos.X = tp.X;
                pos.Y = tp.Y;

                tp.X = 0;
                tp.Y = 0;
            }
        );
    }
}
