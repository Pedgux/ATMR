using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using ATMR.Helpers;
using Spectre.Console;

namespace ATMR.Systems;

public static class InputSystem
{
    // eli siis itse inputtien toiminnot.
    public static async Task Run(World world, Dictionary<int, ConsoleKeyInfo> inputs)
    {
        string players = "";
        var query = new QueryDescription().WithAll<Player>();
        world.Query(
            in query,
            (Entity entity, ref Player player) =>
            {
                foreach (var kvp in inputs)
                {
                    if (player.ID == kvp.Key)
                    {
                        players += player.ID + ", ";
                        ref var velocity = ref world.Get<Velocity>(entity);

                        (int dx, int dy) = InputHelper.GetActionInfoWithKey(kvp.Value.Key) switch
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
        GameState.MessageWindow.Write($"Processed players: {players}");
    }
}
