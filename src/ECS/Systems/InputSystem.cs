using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using ATMR.Helpers;

namespace ATMR.Systems;

public static class InputSystem
{
    private static DeterministicRng MoveRng = new DeterministicRng(
        Hasher.Hash(Program.runSeed + 234)
    );

    // eli siis itse inputtien toiminnot.
    public static void Run(World world, Dictionary<int, ConsoleKeyInfo> inputs)
    {
        string players = "";
        // Collect dig operations first, then execute after query iteration.
        // This avoids structural world changes (destroy) while iterating entities.
        var digRequests = new List<DigRequest>();
        var query = new QueryDescription().WithAll<Player, Velocity, Teleport, Position>();
        world.Query(
            in query,
            (
                Entity entity,
                ref Player player,
                ref Position position,
                ref Velocity velocity,
                ref Teleport teleport
            ) =>
            {
                foreach (var kvp in inputs)
                {
                    if (player.ID == kvp.Key)
                    {
                        players += player.ID + ", ";

                        if (
                            InputHelper.TryParseDirectionalAction(
                                kvp.Value,
                                out var action,
                                out var directionActionInfo
                            )
                            && action == 'D'
                        )
                        {
                            // Directional dig input reached ECS as a resolved action.
                            if (
                                TryCreateDigRequest(
                                    entity,
                                    position,
                                    directionActionInfo,
                                    out var request
                                )
                            )
                            {
                                digRequests.Add(request);
                            }
                            continue;
                        }

                        if (kvp.Value.Key == ConsoleKey.T)
                        {
                            teleport.X = MoveRng.Range(1, GameState.GridWindow.GridWidth);
                            teleport.Y = MoveRng.Range(1, GameState.GridWindow.GridHeight);
                            continue;
                        }

                        (int dx, int dy) = InputHelper.GetActionInfoWithKey(kvp.Value) switch
                        {
                            "4" => (-1, 0),
                            "2" => (0, 1),
                            "8" => (0, -1),
                            "6" => (1, 0),

                            "5" => (0, 0),

                            "7" => (-1, -1),
                            "9" => (1, -1),
                            "1" => (-1, 1),
                            "3" => (1, 1),

                            _ => (0, 0),
                        };
                        velocity.X += dx;
                        velocity.Y += dy;
                    }
                }
            }
        );

        foreach (var request in digRequests)
        {
            // Apply queued dig requests after all input parsing is done.
            ExecuteDig(world, request.Digger, request.TargetX, request.TargetY);
        }

        //Log.Write($"Processed players: {players}");
    }

    private static bool TryCreateDigRequest(
        Entity digger,
        Position diggerPosition,
        string directionInfo,
        out DigRequest request
    )
    {
        request = default;

        if (!InputHelper.TryGetDirectionOffset(directionInfo, out int dx, out int dy))
        {
            return false;
        }

        // Convert direction into one adjacent target tile.
        int targetX = diggerPosition.X + dx;
        int targetY = diggerPosition.Y + dy;

        request = new DigRequest(digger, targetX, targetY);
        return true;
    }

    private static void ExecuteDig(World world, Entity digger, int targetX, int targetY)
    {
        bool foundTarget = false;
        Position targetPosition = default;
        Entity targetEntity = default;

        var targets = new QueryDescription().WithAll<Position>();
        world.Query(in targets, (Entity entity, ref Position position) => { });

        if (!foundTarget)
        {
            return;
        }

        if (world.Has<Position, Solid>(targetEntity))
        {
            // Keep occupancy index in sync before destroying a solid entity.
            //GameState.SolidOccupancy.UnregisterSolid(targetEntity);
        }

        // Reveal underlying terrain tile and then remove the entity.
        GameState.GridWindow.RestoreBaseTile(targetPosition.X, targetPosition.Y);
    }

    // Lightweight queued dig command for deferred execution.
    private readonly record struct DigRequest(Entity Digger, int TargetX, int TargetY);
}
