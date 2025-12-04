namespace ATMR.UI;

using System.Threading.Tasks;
using Spectre.Console;

/// <summary>
/// Handling of the Messages window.
/// </summary>
public sealed class Messages
{
    private readonly UI _ui;
    private Panel _messagePanel;
    private Layout MessageWindow => _ui.RootLayout["Messages"];
    private string[] _messages = System.Array.Empty<string>();

    // Constructor to get the UI referensööri
    public Messages(UI ui)
    {
        _ui = ui;
        _messagePanel = new Panel(
            "" /*replace with the actual messages to show wowooooo*/
        );
        MessageWindow.Update(_messagePanel);
    }

    // Start the update loop (cannot await in constructor)
    public async Task StartAsync()
    {
        await AnsiConsole.Live(MessageWindow).StartAsync(async ctx => { });
    }

    // Convenience async factory: constructs and runs StartAsync
    public static async Task<Messages> CreateAsync(UI ui)
    {
        var m = new Messages(ui);
        await m.StartAsync();
        return m;
    }
}
