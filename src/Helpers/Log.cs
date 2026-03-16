namespace ATMR.Helpers;

public static class Log
{
    private static Action<string> _write = _ => { };

    public static void Initialize(Action<string> write)
    {
        _write = write ?? throw new ArgumentNullException(nameof(write));
    }

    public static void Write(string message)
    {
        _write(message);
    }
}
