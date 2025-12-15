using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using Spectre.Console;

namespace ATMR.Systems;

public static class MovementSystem
{
    public static void Run(World world)
    {
        var query = new QueryDescription().WithAll<Position, Move>();

        world.Query(
            in query,
            (Entity entity, ref Position position, ref Move move) =>
            {
                position.X = move.X;
                position.Y = move.Y;
            }
        );
    }
}
