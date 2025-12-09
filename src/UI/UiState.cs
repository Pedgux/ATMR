namespace ATMR;

/// <summary>
/// Global application state to bridge data accross stuff
/// </summary>
public static class UiState
{
    public static UI.UI? RootUI { get; set; }
    public static UI.Messages? MessageWindow { get; set; }
    public static int ConsoleWidth = Console.WindowWidth;
    public static int ConsoleHeight = Console.WindowHeight;

    // 6/10 of the Console
    public static int leftSize = (int)Math.Floor(ConsoleWidth / 10.0 * 6);

    // 4/10 of the Console
    public static int rightSize = (int)Math.Floor(ConsoleWidth / 10.0 * 4);
}
