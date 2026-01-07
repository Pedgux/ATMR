using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using Spectre.Console;

namespace ATMR.Systems;

/// <summary>
/// Moves entities either by a velocity or a teleport.
/// </summary>
public static class MovementSystem
{
    public static async Task Run(World world)
    {
        var movables = new QueryDescription().WithAll<Position, Velocity>();
        var teleportables = new QueryDescription().WithAll<Position, Teleport>();
        //var obstacles = new QueryDescription().WithAll<Position, Solid>();

        world.Query(
            in movables,
            (Entity entity, ref Position pos, ref Velocity vel) =>
            {
                if (vel.X == 0 && vel.Y == 0)
                    return;

                // replace the last cell the entity was in, so no duplicates appear
                GameState.GridWindow.SetGridCell(pos.X, pos.Y, " ");

                // todo:
                // check if there is an obstacle in the way, then do something

                // move the entity with velocity
                pos.X += vel.X;
                pos.Y += vel.Y;
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

                // replace the last cell the entity was in, so no duplicates appear
                GameState.GridWindow.SetGridCell(pos.X, pos.Y, " ");

                // todo:
                // check if there is an obstacle in the way, then do something

                // teleport em
                pos.X = tp.X;
                pos.Y = tp.Y;

                tp.X = 0;
                tp.Y = 0;
            }
        );
    }
}
