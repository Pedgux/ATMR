using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using Spectre.Console;

namespace ATMR.Systems;

public static class MovementSystem
{
    public static async Task Run(World world)
    {
        var movables = new QueryDescription().WithAll<Position, Velocity>();
        //var obstacles = new QueryDescription().WithAll<Position, Solid>();

        world.Query(
            in movables,
            (Entity entity, ref Position pos, ref Velocity vel) =>
            {
                // replace the last cell the entity was in, so no duplicates appear
                if (vel.X != 0 || vel.Y != 0)
                {
                    GameState.GridWindow.SetGridCell(pos.X, pos.Y, " ");
                }

                // move the entity with velocity
                pos.X += vel.X;
                pos.Y += vel.Y;
                // reset velocity
                vel.X = 0;
                vel.Y = 0;
            }
        );
    }
}
