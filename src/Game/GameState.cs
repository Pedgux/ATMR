namespace ATMR;

/// <summary>
/// Global application state to bridge data accross stuff
/// </summary>
public static class GameState
{
    public static UI.UI? RootUI { get; set; }
    public static UI.Messages? MessageWindow { get; set; }
}
