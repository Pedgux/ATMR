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

                bool isPathClear = !GameState.Level0.Spatial.IsBlocked(nextX, nextY);
                if (isPathClear)
                {
                    var from = (pos.X, pos.Y);
                    var to = (nextX, nextY);
                    bool moveSucceeded = GameState.Level0.Spatial.TryMoveOccupancy(
                        entity,
                        from,
                        to
                    );

                    if (moveSucceeded)
                    {
                        GameState.GridWindow.RestoreBaseTile(pos.X, pos.Y);
                        pos.X = nextX;
                        pos.Y = nextY;
                    }
                }
            }
        );
        world.Remove<MovementIntent>(in movables);
    }
}
