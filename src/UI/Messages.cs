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
    private int _offset = 0;

    // Constructor: capture the layout child. Live will be started at program-level.
    public Messages(UI ui)
    {
        _ui = ui;
        _messagePanel = new Panel("") { Expand = true };
        _messageWindow = _ui.RootLayout["Messages"];
        _messageWindow.Update(_messagePanel);
        _messageWindowSize = _messageWindow.Size.GetValueOrDefault() - 2;
    }

    /// <summary>
    /// queue a message for writing
    /// </summary>
    /// <param name="message">hmm</param>
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
            _messages.Add($"#{_messages.Count, -2} {message}");
            if (_messages.Count > _messageHistory)
                _messages.RemoveRange(0, _messages.Count - _messageHistory);
        }
    }

    /// <summary>
    /// Gets the window of messages to show
    /// </summary>
    /// <returns>The window as a string</returns>
    private string GetMessageString()
    {
        var start = Math.Max(0, _messages.Count - _messageWindowSize - _offset);
        lock (_messages)
        {
            if (_messages.Count == 0)
                return string.Empty;
            return string.Join('\n', _messages.Skip(start).Take(_messageWindowSize));
        }
    }

    // update the panel, plz work
    public void RefreshPanel()
    {
        var panel = new Panel(new Markup(GetMessageString())) { Expand = true };
        _messageWindow.Update(panel);
        _messageWindowSize = _messageWindow.Size.GetValueOrDefault() - 2;
    }

    public void OffsetUp()
    {
        _offset++;
        var maxOffset = Math.Max(0, _messages.Count - _messageWindowSize);
        _offset = Math.Min(_offset, maxOffset);
        RefreshPanel();
    }

    public void OffsetDown()
    {
        _offset--;
        var maxOffset = Math.Max(0, _messages.Count - _messageWindowSize);
        // Clamp offset to [0, maxOffset]
        if (_offset < 0)
            _offset = 0;
        _offset = Math.Min(_offset, maxOffset);
        RefreshPanel();
    }
}
