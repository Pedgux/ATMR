namespace ATMR.UI;

using System.Runtime.InteropServices;
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
    private List<string> _messages = [];
    private int _messageHistory = 100;
    public int offset = 0;

    // Constructor to get the UI referensööri
    public Messages(UI ui)
    {
        _ui = ui;
        _messagePanel = new Panel("") { Expand = true };
        MessageWindow.Update(_messagePanel);
    }

    public void Write(string message)
    {
        _messages.Add(message);
        //offset++;
        if (_messages.Count > _messageHistory)
        {
            // remove the excess message
            _messages.RemoveAt(0);
        }

        _messagePanel = new Panel(new Markup(GetMessageString())) { Expand = true };
        MessageWindow.Update(_messagePanel);
    }

    private string GetMessageString()
    {
        var windowSize = MessageWindow.Size.GetValueOrDefault();
        var result = string.Empty;
        // Don't read past the end of the message list
        var end = Math.Min(_messages.Count, offset + windowSize);
        for (var i = 0; i < _messages.Count; i++)
        {
            result += $"{_messages[i]}\n";
        }

        return result;
    }
}
