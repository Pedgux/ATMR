namespace ATMR.UI;

using System.Threading.Tasks;
using Spectre.Console;

/// <summary>
/// Handling of the Messages window.
/// </summary>
public sealed class Messages
{
    private readonly UI _ui;
    private readonly Panel _messagePanel;
    private Layout MessageWindow => _ui.RootLayout["Messages"];
    private string[] _messages = [];

    // Constructor to get the UI referensööri
    public Messages(UI ui)
    {
        _ui = ui;
        _messagePanel = new Panel(new Markup("The [green]goblin[/] strikes you!"))
        {
            Expand = true,
        }.HeaderAlignment(Justify.Left);

        MessageWindow.Update(_messagePanel);
    }

    // Start the update loop (cannot await in constructor)
    public Task StartAsync()
    {
        return AnsiConsole.Live(MessageWindow).StartAsync(ctx => Task.CompletedTask);
    }

    // Convenience async factory: constructs and runs StartAsync
    public static async Task<Messages> CreateAsync(UI ui)
    {
        var m = new Messages(ui);
        await m.StartAsync();
        return m;
    }
}
