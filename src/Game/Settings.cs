namespace ATMR.Game;

using System.IO;
using Tomlyn;
using Tomlyn.Model;

public static class Settings
{
    private static readonly string FilePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "settings.toml")
    );

    public static string Preset { get; set; } = "hjkl";
    public static bool LocalMode { get; set; } = false;
    public static string StunServer { get; set; } = "stun.l.google.com";
    public static int StunPort { get; set; } = 19302;

    // settings loading
    public static void Load()
    {
        if (!File.Exists(FilePath))
        {
            Save();
            return;
        }

        var toml = File.ReadAllText(FilePath);
        var model = Toml.ToModel(toml);

        if (model.TryGetValue("preset", out var preset))
            Preset = (string)preset;

        if (model.TryGetValue("localMode", out var local))
            LocalMode = (bool)local;

        if (model.TryGetValue("network", out var networkObj) && networkObj is TomlTable network)
        {
            if (network.TryGetValue("stunServer", out var server))
                StunServer = (string)server;

            if (network.TryGetValue("stunPort", out var port))
                StunPort = (int)(long)port;
        }
    }

    public static void Save()
    {
        var toml = $"""
            preset = "{Preset}"
            localMode = {LocalMode.ToString().ToLower()}

            [network]
            stunServer = "{StunServer}"
            stunPort = {StunPort}
            """;

        File.WriteAllText(FilePath, toml);
    }
}
