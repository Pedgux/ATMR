namespace ATMR.UI;

using Spectre.Console;

public static class ChoicePanel
{
    public static (string Choice, int Index) PromptNumberedChoice(
        string title,
        IReadOnlyList<string> choices,
        int selectedIndex,
        Func<string, string>? converter = null
    )
    {
        if (choices.Count == 0)
        {
            throw new InvalidOperationException("Choice list cannot be empty.");
        }

        int defaultIndex = ((selectedIndex % choices.Count) + choices.Count) % choices.Count;
        DrawChoicePanel(title, choices, defaultIndex, converter);

        AnsiConsole.MarkupLine(
            $"Press a number key [grey](1-{choices.Count})[/] or [grey]Enter[/] for default ({defaultIndex + 1})."
        );

        int input = ReadNumberChoice(choices.Count, defaultIndex + 1);
        int absoluteIndex = input - 1;
        return (choices[absoluteIndex], absoluteIndex);
    }

    private static void DrawChoicePanel(
        string title,
        IReadOnlyList<string> choices,
        int defaultIndex,
        Func<string, string>? converter
    )
    {
        var lines = new List<string>(choices.Count);

        for (int i = 0; i < choices.Count; i++)
        {
            string rawLabel = converter == null ? choices[i] : converter(choices[i]);
            string label = Markup.Escape(rawLabel);
            string marker = i == defaultIndex ? " [grey](default)[/]" : string.Empty;
            lines.Add($"[aqua]{i + 1}.[/] {label}{marker}");
        }

        string body = string.Join("\n", lines);
        AnsiConsole.Write(
            new Panel(new Markup(body))
            {
                Header = new PanelHeader(title),
                Border = BoxBorder.Rounded,
            }
        );
    }

    private static int ReadNumberChoice(int maxChoice, int defaultChoice)
    {
        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                return defaultChoice;
            }

            if (!char.IsDigit(keyInfo.KeyChar))
            {
                continue;
            }

            int parsed = keyInfo.KeyChar - '0';
            if (parsed >= 1 && parsed <= maxChoice)
            {
                return parsed;
            }
        }
    }
}
