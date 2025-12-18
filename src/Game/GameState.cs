namespace ATMR.Game;

using System;
using System.Collections.Generic;
using Arch.Core;
using ATMR.Components;
using ATMR.Networking;

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
    // and uhh not +1 when on desktop. fuck
    public static int RightWidth = (int)
        Math.Floor(
            ConsoleWidth / 10.0 * 4
        ) /*+ 1*/
    ;

    // 2/10 of the Console
    // more weird rounding raah
    public static int LeftBottom = (int)Math.Floor(ConsoleHeight / 10.0 * 2) + 1;

    // 8/10 of the Console
    public static int LeftTop = (int)Math.Floor(ConsoleHeight / 10.0 * 8);

    // holds pings. wow.
    public static List<long> PingList = new List<long>();

    // temporary thing to hold level 0 for testing
    public static Level Level0 = new(0);

    // player entity holders
    public static Entity Player1 { get; private set; }
    public static Entity Player2 { get; private set; }
    public static string Mode { get; set; } = null!;

    public static void InitPlayers()
    {
        while (true)
        {
            if (Lobby.PlayerNumber != 0 || Mode == "singleplayer")
            {
                Player1 = Level0.World.Create(
                    new Position(4, 8),
                    new Glyph('@', "[white]"),
                    new Player(Lobby.PlayerNumber),
                    new Velocity(0, 0)
                );
                if (Lobby.PlayerNumber == 1)
                {
                    Player2 = Level0.World.Create(
                        new Position(4, 8),
                        new Glyph('@', "[blue]"),
                        new Player(2),
                        new Velocity(0, 0)
                    );
                }
                else if (Lobby.PlayerNumber == 2)
                {
                    Player2 = Level0.World.Create(
                        new Position(4, 8),
                        new Glyph('@', "[blue]"),
                        new Player(1),
                        new Velocity(0, 0)
                    );
                }
                break;
            }
        }
    }
}
