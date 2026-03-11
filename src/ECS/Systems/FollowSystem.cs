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
                GameState.MessageWindow.Write(
                    $"[blue]Followed from ({position.X}, {position.Y} to ({targetPos.X}, {targetPos.Y}))[/]"
                );
                position.X = targetPos.X;
                position.Y = targetPos.Y;
            }
        );
    }
}
