using Arch.Core;
using Arch.Core.Extensions;
using ATMR.Components;

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
                if (!follows.Target.IsAlive() || !world.Has<Position>(follows.Target))
                {
                    return;
                }

                var targetPos = world.Get<Position>(follows.Target);
                position.X = targetPos.X;
                position.Y = targetPos.Y;
            }
        );
    }
}
