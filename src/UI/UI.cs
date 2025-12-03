namespace ATMR.UI;

using Spectre.Console;

public static class Messages
{
    public static void Initialize()
    {
        var panel = new Panel("Hello World");
        panel.Expand = true;
    }
}
