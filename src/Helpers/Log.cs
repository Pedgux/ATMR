namespace ATMR.Helpers;

using ATMR.UI;

public static class Log
{
    private static Messages? _messages;
    private static Action<string> _write = _ => { };

    public static void Initialize(Messages messages)
    {
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        _write = messages.Write;
    }

    public static void Write(string message)
    {
        _write(message);
    }

    public static void RefreshPanel()
    {
        _messages?.RefreshPanel();
    }

    public static void OffsetUp()
    {
        _messages?.OffsetUp();
    }

    public static void OffsetDown()
    {
        _messages?.OffsetDown();
    }
}
