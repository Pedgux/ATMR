using Arch.Core;
using ATMR.Components;
using ATMR.Game;

namespace ATMR.Systems;

/// <summary>
/// Moves entities either by a velocity or a teleport.
/// </summary>
public static class MovementSystem
{
    public static void Run(World world)
    {
        var movables = new QueryDescription().WithAll<Position, Velocity>();
        var teleportables = new QueryDescription().WithAll<Position, Teleport>();

        world.Query(
            in movables,
            (Entity entity, ref Position pos, ref Velocity vel) =>
            {
                if (vel.X == 0 && vel.Y == 0)
                    return;

                int nextX = pos.X + vel.X;
                int nextY = pos.Y + vel.Y;

                bool isSolid = world.Has<Position, Solid>(entity);
                bool canMove = CollisionSystem.IsBlocked(nextX, nextY);

                if (canMove)
                {
                    vel.X = 0;
                    vel.Y = 0;
                    return;
                }

                // replace the last cell the entity was in, so no duplicates appear
                GameState.GridWindow.RestoreBaseTile(pos.X, pos.Y);

                // move the entity with velocity
                pos.X = nextX;
                pos.Y = nextY;
                // reset velocity
                vel.X = 0;
                vel.Y = 0;
            }
        );

        world.Query(
            in teleportables,
            (Entity entity, ref Position pos, ref Teleport tp) =>
            {
                if (tp.X == 0 && tp.Y == 0)
                    return;

                bool isSolid = world.Has<Position, Solid>(entity);
                bool canTeleport = !CollisionSystem.IsBlocked(tp.X, tp.Y);

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
