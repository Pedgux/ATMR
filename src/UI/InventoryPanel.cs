namespace ATMR.UI;

using System.Collections.Generic;
using ATMR.Components;
using ATMR.Game;
using ATMR.Helpers;
using Spectre.Console;

public sealed class InventoryPanel
{
    private Panel _inventoryPanel;
    private readonly Layout _inventoryWindow;

    public InventoryPanel()
    {
        _inventoryPanel = new Panel("Empty") { Expand = true };
        _inventoryWindow = GameState.Ui.RootLayout["Inventory"];
        _inventoryWindow.Update(_inventoryPanel);
    }

    public void RefreshPanel()
    {
        try
        {
            RefreshPanelInternal();
        }
        catch (Exception ex)
        {
            Log.Write($"[red]InventoryPanel error: {ex.Message}[/red]");
        }
    }

    private void RefreshPanelInternal()
    {
        var world = GameState.Level0.World;

        // Ensure we have a local player and inventory
        if (
            GameState.LocalPlayer == Arch.Core.Entity.Null
            || !world.IsAlive(GameState.LocalPlayer)
            || !world.Has<Inventory>(GameState.LocalPlayer)
        )
        {
            _inventoryWindow.Update(
                new Panel("No inventory")
                {
                    Expand = true,
                    Header = new PanelHeader("[yellow]Inventory[/]"),
                }
            );
            return;
        }

        var inventory = world.Get<Inventory>(GameState.LocalPlayer);
        var lines = new List<string>();

        lines.Add($"[grey]Capacity:[/] {inventory.Items.Count}/{inventory.Capacity}");
        lines.Add(""); // spacer

        for (int i = 0; i < inventory.Items.Count; i++)
        {
            var itemEntity = inventory.Items[i];

            if (world.IsAlive(itemEntity) && world.Has<Item>(itemEntity))
            {
                var itemInfo = world.Get<Item>(itemEntity);
                string label = $"{i + 1}. {itemInfo.Name}";

                if (world.Has<Stackable>(itemEntity))
                {
                    var stack = world.Get<Stackable>(itemEntity);
                    label += $" (x{stack.Count})";
                }

                lines.Add(label);
            }
            else
            {
                lines.Add($"{i + 1}. [grey]Invalid item[/]");
            }
        }

        if (inventory.Items.Count == 0)
        {
            lines.Add("[grey]Empty[/]");
        }

        var panel = new Panel(new Markup(string.Join("\n", lines)))
        {
            Expand = true,
            Header = new PanelHeader("[yellow]Inventory[/]"),
        };

        _inventoryWindow.Update(panel);
    }
}
