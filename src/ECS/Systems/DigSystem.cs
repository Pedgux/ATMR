using Arch.Core;
using Arch.Core.Extensions;
using ATMR.Components;
using ATMR.Game;

namespace ATMR.Systems;

public static class DigSystem
{
    public static void Run(World world, IReadOnlyCollection<ActionIntent> intents)
    {
        if (intents.Count == 0)
        {
            return;
        }

        var digIntents = intents
            .Where(intent => intent.Kind == ActionKind.Dig)
            .GroupBy(intent => intent.PlayerId)
            .ToDictionary(group => group.Key, group => group.Last());

        if (digIntents.Count == 0)
        {
            return;
        }

        var query = new QueryDescription().WithAll<Player, Position>();

        world.Query(
            in query,
            (Entity entity, ref Player player, ref Position position) =>
            {
                if (!digIntents.TryGetValue(player.Id, out var intent))
                {
                    return;
                }

                if (intent.Dx == 0 && intent.Dy == 0)
                {
                    return;
                }

                int targetX = position.X + intent.Dx;
                int targetY = position.Y + intent.Dy;

                ExecuteDig(world, targetX, targetY);
                GameState.TimeCounter += 20;
            }
        );
    }

    private static void ExecuteDig(World world, int targetX, int targetY)
    {
        var targets = new QueryDescription().WithAll<Position, Health>();
        world.Query(
            in targets,
            (Entity entity, ref Position position, ref Health health) =>
            {
                if (position.X == targetX && position.Y == targetY)
                {
                    health.Amount -= 2;
                }
            }
        );
    }
}