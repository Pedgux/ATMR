namespace ATMR.Networking;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ATMR.Game;
using ATMR.Helpers;

public static class Lobby
{
    /// <summary> This player's 1-based index in join order (set after polling the lobby). </summary>
    public static int PlayerNumber;

    /// <summary> Total number of players expected in the lobby (set before joining). </summary>
    public static int PlayerAmount;

    // link to database
    private const string BaseUrl =
        "https://p2plobbysignaler-default-rtdb.europe-west1.firebasedatabase.app/";

    // HTTP to GET / PUT data
    public static readonly HttpClient client = new HttpClient();
    private static string webApiKey = "AIzaSyAaOzN7xaYvMgcB7tIseQ6W7K5kjMb-iSA";

    public static FirebaseAuthResponse? Auth { get; private set; }

    public sealed class FirebaseAuthResponse
    {
        [JsonPropertyName("idToken")]
        public string? IdToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("localId")]
        public string? LocalId { get; set; } // This is the Firebase User UID

        [JsonPropertyName("expiresIn")]
        public string? ExpiresIn { get; set; } // Expiration in seconds

        [JsonPropertyName("isNewUser")]
        public bool IsNewUser { get; set; } // Will be true for anonymous sign-up
    }

    // Represents a player's presence in the lobby.
    // "joinedAt" should be a Firebase server timestamp.
    public sealed class PlayerEntry
    {
        [JsonPropertyName("blob")]
        public string? Blob { get; set; }

        [JsonPropertyName("joinedAt")]
        public long? JoinedAt { get; set; }
    }

    /// <summary>
    /// Signs in anonymously via Firebase Auth and returns the auth tokens.
    /// </summary>
    public static async Task<FirebaseAuthResponse?> SignInAnonymouslyAsync()
    {
        // The endpoint for initial anonymous sign-up
        var requestUrl =
            $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={webApiKey}";

        // The request body: we just need to indicate we want a secure token.
        var requestBody = new { returnSecureToken = true };

        // Serialize the body to JSON
        var jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            // Send the POST request
            var response = await client.PostAsync(requestUrl, httpContent);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Read the response content as a string
                string responseBody = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON response into our custom object
                var authResponse = JsonSerializer.Deserialize<FirebaseAuthResponse>(responseBody);
                return authResponse;
            }
            else
            {
                // Log or handle the error
                string errorContent = await response.Content.ReadAsStringAsync();
                GameState.MessageWindow.Write(
                    $"[red]Error signing in anonymously: {response.StatusCode}[/]"
                );
                GameState.MessageWindow.Write($"[red]Error details: {errorContent}[/]");
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            GameState.MessageWindow.Write($"Network error during anonymous sign-in: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            GameState.MessageWindow.Write($"JSON deserialization error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Authenticates anonymously with Firebase. Must be called before Join or GetOtherPlayerBlobs.
    /// </summary>
    public static async Task Initialize()
    {
        Auth = await SignInAnonymouslyAsync();

        if (Auth == null)
            throw new Exception("Anonymous auth failed. Check console output for details.");

        GameState.MessageWindow.Write($"[yellow]Signed in anonymously.[/]");
    }

    /// <summary>
    /// Registers this player in the Firebase lobby under the given lobby code.
    /// Stores an encoded IP:port blob and a server-side timestamp for join ordering.
    /// </summary>
    public static async Task Join(string lobbyCode, string playerId, string ip, int port)
    {
        if (Auth is null)
            throw new InvalidOperationException(
                "Lobby not initialized. Call Lobby.Initialize(apiKey) before Join."
            );

        string idToken =
            Auth.IdToken
            ?? throw new InvalidOperationException(
                "Auth response missing idToken. Call Lobby.Initialize(apiKey) and verify authentication succeeded."
            );

        //need to PUT this into existence (pun intended)
        string url = $"{BaseUrl}lobbies/{lobbyCode}/{playerId}.json?auth={idToken}";
        GameState.MessageWindow.Write($"[purple]Using: {ip}:{port} in blob[/]");
        string blob = IpPortEncoder.Encode(ip, (ushort)port);
        GameState.MessageWindow.Write($"[purple]Encoded blob: {blob}[/]");
        GameState.MessageWindow.Write($"[purple]Decoded blob: {IpPortEncoder.Decode(blob)}[/]");
        // Store an object with the encoded blob and a server-side join timestamp.
        // This allows deterministic ordering of players later.
        var payload = new
        {
            blob,
            joinedAt = new Dictionary<string, string> { [".sv"] = "timestamp" },
        };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        GameState.MessageWindow.Write($"[green]Trying to create lobby...[/]");
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await client.PutAsync(url, content, cts.Token);
            response.EnsureSuccessStatusCode();
            GameState.MessageWindow.Write($"[green]Lobby created![/]");
        }
        catch (TaskCanceledException)
        {
            GameState.MessageWindow.Write($"[red]Lobby creation timed out after 10s[/]");
            GameState.MessageWindow.Write($"[red]URL: {url}[/]");
        }
        catch (Exception ex)
        {
            GameState.MessageWindow.Write($"[red]Lobby creation failed: {ex.Message}[/]");
        }
    }

    /// <summary>
    /// Polls the Firebase lobby every 5 seconds until PlayerAmount players have joined.
    /// Returns the encoded IP:port blobs of all players except the one matching notThisOne.
    /// Also sets PlayerNumber based on join order.
    /// </summary>
    public static async Task<List<string>> GetOtherPlayerBlobs(string lobbyCode, string notThisOne)
    {
        GameState.MessageWindow.Write("[blue]Waiting for players...[/]");
        while (true)
        {
            // pause before retrying to save data heheee
            await Task.Delay(TimeSpan.FromSeconds(5));
            if (Auth is null)
                throw new InvalidOperationException(
                    "Lobby not initialized. Call Lobby.Initialize(apiKey) before Join."
                );

            string idToken =
                Auth.IdToken
                ?? throw new InvalidOperationException(
                    "Auth response missing idToken. Call Lobby.Initialize(apiKey) and verify authentication succeeded."
                );

            string url = $"{BaseUrl}lobbies/{lobbyCode}.json?auth={idToken}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            // Deserialize the lobby as a map of playerId -> PlayerEntry
            Dictionary<string, PlayerEntry>? map = await response.Content.ReadFromJsonAsync<
                Dictionary<string, PlayerEntry>
            >(options);

            if (map == null || map.Count == 0)
            {
                GameState.MessageWindow.Write("Lobby empty or response was 'null'.");
                continue;
            }

            // Sort players by join timestamp to assign deterministic player numbers.
            // Missing timestamps are treated as latest to avoid stealing lower numbers.
            var ordered = new List<(string id, long sortKey, string? blob)>();
            foreach (var (id, entry) in map)
            {
                long sortKey = entry?.JoinedAt ?? long.MaxValue;
                ordered.Add((id, sortKey, entry?.Blob));
            }

            ordered.Sort((a, b) => a.sortKey.CompareTo(b.sortKey));

            // Walk the sorted list to find our own position and collect other players' blobs
            int index = 1;
            var otherBlobs = new List<string>();
            foreach (var item in ordered)
            {
                if (item.id == notThisOne)
                {
                    PlayerNumber = index; // Our 1-based position in join order
                }
                else if (!string.IsNullOrEmpty(item.blob))
                {
                    otherBlobs.Add(item.blob);
                }
                index++;
            }

            // All players present — return the other players' connection blobs
            if (ordered.Count >= PlayerAmount && otherBlobs.Count == PlayerAmount - 1)
            {
                GameState.MessageWindow.Write($"[blue]Found {otherBlobs.Count} other player(s)[/]");
                return otherBlobs;
            }

            GameState.MessageWindow.Write(
                $"[blue]Found {ordered.Count} player(s) so far, waiting for {PlayerAmount}...[/]"
            );
        }
    }
}
