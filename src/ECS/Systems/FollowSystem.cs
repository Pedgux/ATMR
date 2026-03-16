using Arch.Core;
using ATMR.Components;
using ATMR.Game;

namespace ATMR.Systems;

public static class FollowSystem
{
    public static void Run(World world)
    {
        var query = new QueryDescription().WithAll<Position, FollowsEntity>();

        world.Query(
            in query,
            (Entity entity, ref Position position, ref FollowsEntity follows) =>
            {
                var targetPos = world.Get<Position>(follows.Target);
                position.X = targetPos.X;
                position.Y = targetPos.Y;
            }
        );
    }
}
