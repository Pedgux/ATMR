namespace ATMR.UI;

using System.Collections.Generic;
using Arch.Core.Extensions;
using ATMR.Components;
using ATMR.Game;
using ATMR.Networking;
using Spectre.Console;
using Spectre.Console.Cli.Help;

/// <summary>
/// I wonder what.
/// </summary>
public sealed class Stats
{
    private Panel _statsPanel;
    private readonly Layout _statsWindow;

    // work in progress, idea is to later put strings there like:
    // $"str: {int}", or ping for example $"ping: {int} ms"
    // in a dynamic way, so it's automatically slotted onto the stats panel.
    // Instead of manually putting them in the code, allowing more customization and eases the whole thing.
    // But uhh future me problem, don't even have stats yet. Just ideas.
    // also gotta figure out how to detach the {var} part from the string.
    public static List<string> StatEntries = [];

    // Constructor: capture the layout child. Live will be started at program-level.
    public Stats()
    {
        _statsPanel = new Panel("") { Expand = true };
        _statsWindow = GameState.Ui.RootLayout["Stats"];
        _statsWindow.Update(_statsPanel);
    }

    // todo / reminder:
    // Use breakdownchart to represent player health, easily visualizing:
    // incoming damage (poison, fire),
    // different types of health (armor, shields)

    // update the panel, plz work
    public void RefreshPanel()
    {
        try
        {
            RefreshPanelInternal();
        }
        catch { }
    }

    private void RefreshPanelInternal()
    {
        var world = GameState.Level0.World;
        string playerPositionText = "dead";

        // Provide defaults in case the player is dead or transitioning
        int currentHp = 0;
        int maxHp = 10;

        if (GameState.LocalPlayer.IsAlive() && world.Has<Position>(GameState.LocalPlayer))
        {
            playerPositionText = world.Get<Position>(GameState.LocalPlayer).ToString();
        }

        if (GameState.LocalPlayer.IsAlive() && world.Has<Health>(GameState.LocalPlayer))
        {
            var health = world.Get<Health>(GameState.LocalPlayer);
            currentHp = health.Amount;
            maxHp = health.MaxAmount;
        }

        var hp = new BreakdownChart()
            .ShowTags(false)
            .AddItem("HP", Math.Max(0, currentHp), Color.Green)
            .AddItem(string.Empty, Math.Max(0, maxHp - currentHp), Color.Grey);
        long medianPing = 0;
        if (GameState.PingList.Count > 0)
        {
            var tempList = new List<long>(GameState.PingList);
            tempList.Sort();
            medianPing = tempList[tempList.Count / 2];
        }

        var panel = new Panel(
            new Rows(
                new Markup($"[green]HP: {currentHp}[/]"),
                hp,
                new Markup(
                    $"Median ping: {medianPing} ms       Local tick: {GameState.TickNumber}       Player number: {Lobby.PlayerNumber}      Player {playerPositionText}     Time:{GameState.TimeCounter}"
                )
            )
        )
        {
            Expand = true,
        };
        _statsWindow.Update(panel);
    }
}
