using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using Spectre.Console;

namespace ATMR.Systems;

public static class InputSystem
{
    public static void Run(World world, Dictionary<int, ConsoleKeyInfo>)
    {
        /*
        world.Query(
            in movables,
            (Entity entity, ref Position pos, ref Velocity vel) =>
            {
                // move the entity to Velocity position
                pos.X += vel.X;
                pos.Y += vel.Y;

                vel.X = 0;
                vel.Y = 0;
            }
        );*/
    }
}
