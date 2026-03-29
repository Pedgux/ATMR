using System.Linq;
using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using ATMR.Helpers;

namespace ATMR.Systems;

public static class InputSystem
{
    // eli siis itse inputtien toiminnot.
    public static void Run(World world, Dictionary<int, (char action, string actionInfo)> inputs)
    {
        // ota levelin deterministinen rngstate
        var rngQuery = new QueryDescription().WithAll<RngState>();
        uint currentRngState = 0;
        world.Query(in rngQuery, (ref RngState state) => currentRngState = state.State);
        var moveRng = new DeterministicRng(currentRngState);

        string players = "";
        // Collect dig operations first, then execute after query iteration.
        // This avoids structural world changes (destroy) while iterating entities.
        var digRequests = new List<DigRequest>();
        var pickupRequests = new List<PickupRequest>();
        var dropRequests = new List<DropRequest>();

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
                    if (player.Id == kvp.Key)
                    {
                        players += player.Id + ", ";
                        var action = kvp.Value.action;
                        var actionInfo = kvp.Value.actionInfo;

                        if (action == 'D')
                        {
                            // Directional dig input reached ECS as a resolved action.
                            if (TryCreateDigRequest(position, actionInfo, out var request))
                            {
                                digRequests.Add(request);
                            }
                            GameState.TimeCounter += 20;
                            continue;
                        }

                        if (actionInfo == "T")
                        {
                            teleport.X = moveRng.Range(1, GameState.GridWindow.GridWidth);
                            teleport.Y = moveRng.Range(1, GameState.GridWindow.GridHeight);
                            GameState.TimeCounter += 3;
                            continue;
                        }

                        // ostoskärry pickup
                        if (InputHelper.TryParsePickupListAction(actionInfo, out var pickupCart))
                        {
                            Log.Write(
                                $"[cyan]Processing batch pickup: {pickupCart.Count} items[/]"
                            );
                            foreach (var cartItem in pickupCart.OrderByDescending(x => x.Key))
                            {
                                pickupRequests.Add(
                                    new PickupRequest(entity, cartItem.Value, cartItem.Key)
                                );
                            }
                            GameState.TimeCounter += 5;
                            continue;
                        }

                        // single stack pickup
                        if (
                            InputHelper.TryParsePickupAction(
                                actionInfo,
                                out int pickupAmount,
                                out int itemIndex
                            )
                        )
                        {
                            Log.Write($"Picked up! Amount={pickupAmount}, Index={itemIndex}");
                            pickupRequests.Add(new PickupRequest(entity, pickupAmount, itemIndex));
                            GameState.TimeCounter += 5;
                            continue;
                        }
                        if (actionInfo == "Drop")
                        {
                            dropRequests.Add(new DropRequest(entity, 0));
                            GameState.TimeCounter += 5;
                            continue;
                        }

                        (int dx, int dy) = actionInfo switch
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
                        if (dx != 0 || dy != 0 || actionInfo == "5")
                        {
                            GameState.TimeCounter += 10;
                        }
                    }
                }
            }
        );

        foreach (var request in digRequests)
        {
            // Apply queued dig requests after all input parsing is done.
            ExecuteDig(world, request.TargetX, request.TargetY);
        }

        foreach (var req in pickupRequests)
        {
            ItemSystem.ExecutePickup(world, req.PlayerEntity, req.Amount, req.ItemIndex);
        }

        foreach (var req in dropRequests)
        {
            ItemSystem.ExecuteDrop(world, req.PlayerEntity, req.ItemIndex);
        }

        // päivitä levelin rng
        world.Query(in rngQuery, (ref RngState state) => state.State = moveRng.State);

        //Log.Write($"Processed players: {players}");
    }

    private static bool TryCreateDigRequest(
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

        request = new DigRequest(targetX, targetY);
        return true;
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

    // Lightweight queued dig command for deferred execution.
    private readonly record struct DigRequest(int TargetX, int TargetY);

    public readonly record struct PickupRequest(Entity PlayerEntity, int Amount, int ItemIndex);

    public readonly record struct DropRequest(Entity PlayerEntity, int ItemIndex);
}
