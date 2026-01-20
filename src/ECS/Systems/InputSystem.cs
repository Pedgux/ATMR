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
        var query = new QueryDescription().WithAll<Player>();
        world.Query(
            in query,
            (Entity entity, ref Player player) =>
            {
                foreach (var kvp in inputs)
                    if (player.ID == kvp.Key)
                    {
                        ref var velocity = ref world.Get<Velocity>(entity);

                        (int dx, int dy) = Keybinds.GetActionWithKey(kvp.Value.Key) switch
                        {
                            "1" => (1, 1),
                            "2" => (1, 0),
                            "4" => (0, -1),
                            "6" => (-1, 0),
                            "8" => (0, 1),
                            "5" => (-1, -1),
                            "3" => (1, -1),
                            "7" => (-1, 1),

                            _ => (0, 0),
                        };
                        velocity.X += dx;
                        velocity.Y += dy;
                    }
            }
        );
    }
}
