namespace ATMR.UI;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// Handling of the Messages window.
/// Runs a single Live session bound to the Messages layout child and updates it from
/// a background loop. `Write` only mutates the message buffer.
/// </summary>
public sealed class Messages
{
    private readonly UI _ui;
    private Panel _messagePanel;
    private readonly Layout _messageWindow;
    private readonly List<string> _messages = new();
    private int _messageHistory = 100;
    private int _messageWindowSize = 9;

    //private int offset = 0;

    // Constructor: capture the layout child. Live will be started at program-level.
    public Messages(UI ui)
    {
        _ui = ui;
        _messagePanel = new Panel("") { Expand = true };
        _messageWindow = _ui.RootLayout["Messages"];
        _messageWindow.Update(_messagePanel);
    }

    // Append a message and trim history.
    public void Write(string message)
    {
        lock (_messages)
        {
            _messages.Add(message);
            if (_messages.Count > _messageHistory)
                _messages.RemoveRange(0, _messages.Count - _messageHistory);

            //var maxOffset = Math.Max(0, _messages.Count - _messageWindowSize);
            //offset = Math.Min(offset, maxOffset);
        }
    }

    private string GetMessageString()
    {
        var start = Math.Max(0, _messages.Count - _messageWindowSize);
        lock (_messages)
        {
            if (_messages.Count == 0)
                return string.Empty;
            return string.Join('\n', _messages.Skip(start).Take(_messageWindowSize));
        }
    }

    // Rebuild and update the message panel from the current buffer.
    public void RefreshPanel()
    {
        var panel = new Panel(new Markup(GetMessageString())) { Expand = true };
        _messageWindow.Update(panel);
    }
}
