/*
Attempt At A Multiplayer Roguelike (ATMR)
Inspired by Nathan Daniel's "Roguelike Theory of Relativity (RTOR)" paper
Built with Spectre.Console for console UI: https://spectreconsole.net/
*/

using ATMR.Game;
using ATMR.Helpers;
using ATMR.Input;
using ATMR.Networking;
using ATMR.UI;
using Spectre.Console;

public static class Program
{
    public static uint runSeed;
    private static readonly string[] GameModeChoices =
    {
        "Multiplayer",
        "Singleplayer",
        "Settings",
        "Exit",
    };

    private static readonly string[] SettingsChoices = { "ControlPreset", "LocalMode", "Back" };
    private static readonly string[] ControlPresetChoices = { "hjkl", "numpad", "arrows" };

    private static (string Choice, int Index) PromptGameMode(int selectedIndex)
    {
        return ChoicePanel.PromptNumberedChoice(
            "[yellow]What you do?[/]",
            GameModeChoices,
            selectedIndex
        );
    }

    private static string GetSettingsChoiceLabel(string choice)
    {
        return choice switch
        {
            "ControlPreset" => $"Control preset: {Settings.Preset}",
            "LocalMode" => $"Local mode: {(Settings.LocalMode ? "On" : "Off")}",
            _ => "Back",
        };
    }

    private static void ShowSettingsMenu()
    {
        int selectedIndex = 0;

        while (true)
        {
            AnsiConsole.Clear();

            var result = ChoicePanel.PromptNumberedChoice(
                "[yellow]Settings[/]",
                SettingsChoices,
                selectedIndex,
                GetSettingsChoiceLabel
            );
            string settingsChoice = result.Choice;
            selectedIndex = result.Index;

            switch (settingsChoice)
            {
                case "ControlPreset":
                    PromptControlPreset();
                    continue;

                case "LocalMode":
                    Settings.LocalMode = !Settings.LocalMode;
                    Settings.Save();
                    continue;

                default:
                    return;
            }
        }
    }

    private static void PromptControlPreset()
    {
        AnsiConsole.Clear();

        int selectedIndex = Array.IndexOf(ControlPresetChoices, Settings.Preset);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        var presetChoice = ChoicePanel.PromptNumberedChoice(
            "[yellow]Pick control preset[/]",
            ControlPresetChoices,
            selectedIndex
        );

        Settings.Preset = presetChoice.Choice;
        Settings.Save();
    }

    public static async Task InitializeNetworking(string lobbyCode)
    {
        await UdpTransport.Initialize(lobbyCode);
    }

    public static async Task<int> Main()
    {
        // I wonder what ths does
        Settings.Load();
        AnsiConsole.Clear();

        string lobbyCode = string.Empty;
        int modeSelectedIndex = 0;

        while (true)
        {
            AnsiConsole.Clear();

            var modeSelection = PromptGameMode(modeSelectedIndex);
            string modeChoice = modeSelection.Choice;
            modeSelectedIndex = modeSelection.Index;

            if (modeChoice == "Settings")
            {
                ShowSettingsMenu();
                continue;
            }

            if (modeChoice == "Exit")
            {
                return 0;
            }

            if (modeChoice == "Singleplayer")
            {
                GameState.Mode = "singleplayer";
                break;
            }

            lobbyCode = AnsiConsole.Prompt(
                new TextPrompt<string>("Type out a lobby code:").PromptStyle("green")
            );
            // TÄSSÄ RUNSEEDI
            runSeed = Hasher.StringHash(lobbyCode);

            Lobby.PlayerAmount = AnsiConsole.Prompt(
                new TextPrompt<int>("How many players? Amount:").PromptStyle("green")
            );
            GameState.Mode = "multiplayer";
            break;
        }

        // Start input polling and consumer ÖÖÖ SIIS TÄSSÄ ON SE ISO ALKU, ensimmäinen osa
        var cts = new CancellationTokenSource();
        var reader = Input.StartPolling(cts.Token);
        _ = Input.RunConsumer(reader, cts.Token);

        // clear the console, so UI fits
        AnsiConsole.Clear();

        // Instantiate all UI parts
        GameState.Ui = new UI();
        var messageWindow = new Messages();
        Log.Initialize(messageWindow);
        GameState.StatsWindow = new Stats();
        GameState.GridWindow = new ATMR.UI.Grid();

        // optional multiplayer
        if (GameState.Mode == "multiplayer")
        {
            // Start networking initialization in background, important to be after UI initialization
            // because how tf do I prompt in messagewindow? Network init needs UI,
            // prompting user does not. This is messy.
            // todo: idk a prompt window? Prompt in messagewindow so we can init UI at start.
            _ = InitializeNetworking(lobbyCode);
        }

        // Start one Live session bound to the root layout and refresh UI
        // Reminder: maybe do a signal based version of this? Instead of polling evert 60ms, what if
        // UI updates were done when needed? Very inefficient currently.
        // todo: idk put this elsewhere? Such as in UI.Initialize? Weird to be here.
        var liveTask = Task.Run(
            () =>
                AnsiConsole
                    .Live(GameState.Ui.RootLayout)
                    .StartAsync(async ctx =>
                    {
                        // initial draw
                        Log.RefreshPanel();
                        GameState.StatsWindow.RefreshPanel();
                        ctx.Refresh();

                        while (!cts.Token.IsCancellationRequested)
                        {
                            //GameState.Ui.Fit();
                            Log.RefreshPanel();
                            GameState.StatsWindow.RefreshPanel();
                            ctx.Refresh();
                            await Task.Delay(60);
                        }
                    }),
            cts.Token
        );
        // testing grounds, level stuff. ISO ALKU toinen osa
        GameState.InitPlayers();

        // keep app alive until cancellation (e.g. ctrl-c or other signal)
        await Task.Delay(Timeout.Infinite, cts.Token);
        return 0;
    }
}
