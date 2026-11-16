using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using ATMR.Helpers;

namespace ATMR.Systems;

public static class DigSystem
{
    public static void Run(World world)
    {
        var query = new QueryDescription().WithAll<DigIntent, Position>();

        world.Query(
            in query,
            (Entity entity, ref Position position, ref DigIntent intent) =>
            {
                int targetX = position.X + intent.X;
                int targetY = position.Y + intent.Y;

                Log.Write(" kaivetaan");
                GameState.Level0.Spatial.DamageWall(targetX, targetY);
            }
        );
        world.Remove<DigIntent>(in query);
    }
}
