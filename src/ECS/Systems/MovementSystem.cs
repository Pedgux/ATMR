using Arch.Core;
using ATMR.Components;
using ATMR.Game;

namespace ATMR.Systems;

/// <summary>
/// Moves entities.
/// </summary>
public static class MovementSystem
{
    public static void Run(World world)
    {
        var movables = new QueryDescription().WithAll<Position, MovementIntent>();

        world.Query(
            in movables,
            (Entity entity, ref Position pos, ref MovementIntent vel) =>
            {
                int nextX = pos.X + vel.X;
                int nextY = pos.Y + vel.Y;

                bool isSolid = world.Has<Position, Solid>(entity);
                bool canMove;
                if (isSolid)
                    canMove = CollisionSystem.TryMoveSolid(entity, pos.X, pos.Y, nextX, nextY);
                else
                    canMove = !CollisionSystem.IsBlocked(nextX, nextY);

                // replace the last cell the entity was in, so no duplicates appear
                GameState.GridWindow.RestoreBaseTile(pos.X, pos.Y);

                // move the entity with velocity
                pos.X = nextX;
                pos.Y = nextY;
               
            }
        );
        world.Remove<MovementIntent>(in movables);
    }
}
