namespace ATMR.Tick;

using ATMR.Game;
using ATMR.Systems;

class Tick
{
    private Tick() { }

    // call each system in order with player inputs
    public static async Task<Tick> CreateAsync(Dictionary<int, ConsoleKey> input, Level level)
    {
        var turn = new Tick();
        // await so they fully execute before another starts
        //await InputSystem.Run(input, level.World)
        await RenderSystem.Run(level.World);
        await MovementSystem.Run(level.World);
        return turn;
    }
}
