namespace ATMR.Game;

/// <summary>
/// Global application state to bridge data accross stuff
/// </summary>
public static class GameState
{
    // UI stuff
    public static UI.UI Ui { get; set; } = null!;
    public static UI.Messages MessageWindow { get; set; } = null!;
    public static UI.Stats StatsWindow { get; set; } = null!;
    public static UI.Grid GridWindow { get; set; } = null!;

    // Console stuff
    public static int ConsoleWidth = Console.WindowWidth;
    public static int ConsoleHeight = Console.WindowHeight;

    // 6/10 of the Console
    public static int LeftWidth = (int)Math.Floor(ConsoleWidth / 10.0 * 6);

    // 4/10 of the Console
    // probably some weird rounding error, but this leaves out 1 tile, hence the +1. Investigate later
    public static int RightWidth = (int)Math.Floor(ConsoleWidth / 10.0 * 4) + 1;

    // 2/10 of the Console
    // more weird rounding raah
    public static int LeftBottom = (int)Math.Floor(ConsoleHeight / 10.0 * 2) + 1;

    // 8/10 of the Console
    public static int LeftTop = (int)Math.Floor(ConsoleHeight / 10.0 * 8);

    // holds pings. wow.
    public static List<long> PingList = [];

    // temporary thing to hold level 0 for testing
    public static Level Level = new(0);
}
