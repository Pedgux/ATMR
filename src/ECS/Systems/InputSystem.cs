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
        GameState.MessageWindow.Write($"grid rng: {Program.runSeed + 234}");
        string players = "";
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
        //GameState.MessageWindow.Write($"Processed players: {players}");
    }
}
