namespace ATMR.UI;

using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

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
    private int _messageHistory = 99;
    private int _messageWindowSize;

    //private int offset = 0;

    // Constructor: capture the layout child. Live will be started at program-level.
    public Messages(UI ui)
    {
        _ui = ui;
        _messagePanel = new Panel("") { Expand = true };
        _messageWindow = _ui.RootLayout["Messages"];
        _messageWindow.Update(_messagePanel);
        _messageWindowSize = _messageWindow.Size.GetValueOrDefault() - 2;
    }

    // Append a message and trim history.
    public void Write(string message)
    {
        lock (_messages)
        {
            /*
            // doesnt work because buh width is fucking hard apparently
            // replace size in here with width, as size is relative to the orientation of the layout
            // if message is too large, split into multiple so shi does not break :c
            while (message.Length > _messageWindowSize)
            {
                var subMessage = message.Substring(_messageWindowSize);
                message = message.Substring(0, _messageWindowSize);
                _messages.Add(message);
                message = subMessage;
            }
            */
            _messages.Add($"#{_messages.Count, -2} {message} {_messageWindowSize}");
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
        _messageWindowSize = _messageWindow.Size.GetValueOrDefault() - 2;
    }
}
