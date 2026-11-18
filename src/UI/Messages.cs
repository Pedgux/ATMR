namespace ATMR.UI;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ATMR.Game;
using ATMR.Helpers;
using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// Handling of the Messages window.
/// Runs a single Live session bound to the Messages layout child and updates it from
/// a background loop. `Write` only mutates the message buffer.
/// </summary>
public sealed class Messages
{
    private Panel _messagePanel;
    private readonly Layout _messageWindow;
    private readonly List<string> _messages = new();
    private int _messageHistory = 999;
    private int _messageWindowSize;
    private int _offset = 0;

    // matches lines of the form "[colour]plain text[/]" with no other brackets
    private static readonly Regex Wrapped = new(
        @"^\[(?<tag>[^\[\]]+)\](?<body>[^\[\]]*)\[/\]$",
        RegexOptions.Compiled
    );

    public Messages()
    {
        _messagePanel = new Panel("") { Expand = true };
        _messageWindow = GameState.Ui.RootLayout["Messages"];
        _messageWindow.Update(_messagePanel);
        _messageWindowSize = _messageWindow.Size.GetValueOrDefault() - 2;
    }

    /// <summary>
    /// Splits a line into chunks of at most `width` plain characters. Every chunk is
    /// valid markup on its own, so a split can never cut a tag in half.
    /// </summary>
    private static IEnumerable<string> SplitMarkupSafe(string message, int width)
    {
        width = Math.Max(10, width); // also prevents an endless loop on tiny consoles

        string? tag = null;
        string text;
        var m = Wrapped.Match(message);
        if (m.Success)
        {
            tag = m.Groups["tag"].Value;
            text = m.Groups["body"].Value;
        }
        else
        {
            try
            {
                text = Markup.Remove(message);
            }
            catch (InvalidOperationException)
            {
                text = message;
            }
        }

        if (text.Length == 0)
        {
            yield return "";
            yield break;
        }

        int i = 0;
        while (i < text.Length)
        {
            int len = Math.Min(width, text.Length - i);

            // if the rest doesn't fit, break after the last space inside the window
            if (i + len < text.Length)
            {
                int space = text.LastIndexOf(' ', i + len - 1, len);
                if (space > i)
                    len = space - i + 1;
            }

            string chunk = Markup.Escape(text.Substring(i, len).TrimEnd());
            yield return tag is null ? chunk : $"[{tag}]{chunk}[/]";

            i += len;
            while (i < text.Length && text[i] == ' ')
                i++;
        }
    }

    /// <summary>
    /// queue a message for writing
    /// </summary>
    public void Write(string message)
    {
        lock (_messages)
        {
            bool first = true;
            // -5 leaves room for the "#NN " prefix
            foreach (var chunk in SplitMarkupSafe(message, GameState.RightWidth - 4 - 5))
            {
                _messages.Add(first ? $"#{_messages.Count, -2} {chunk}" : $"     {chunk}");
                first = false;
            }

            if (_messages.Count > _messageHistory)
                _messages.RemoveRange(0, _messages.Count - _messageHistory);
        }
    }

    /// <summary>
    /// Gets the window of messages to show
    /// </summary>
    private string GetMessageString()
    {
        lock (_messages)
        {
            if (_messages.Count == 0)
                return string.Empty;
            var start = Math.Max(0, _messages.Count - _messageWindowSize - _offset);
            return string.Join('\n', _messages.Skip(start).Take(_messageWindowSize));
        }
    }

    // update the panel, plz work
    public void RefreshPanel()
    {
        string text = GetMessageString();
        IRenderable body;
        try
        {
            body = new Markup(text);
        } // the parser throws here on bad markup
        catch (InvalidOperationException)
        {
            body = new Text(text);
        }

        _messageWindow.Update(new Panel(body) { Expand = true });
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
        if (_offset < 0)
            _offset = 0;
        _offset = Math.Min(_offset, maxOffset);
        RefreshPanel();
    }
}
