namespace ATMR.Networking;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Spectre.Console;

public static class Lobby
{
    // link to database
    private const string BaseUrl =
        "https://p2plobbysignaler-default-rtdb.europe-west1.firebasedatabase.app/";

    // HTTP to GET / PUT data
    public static readonly HttpClient client = new HttpClient();
    private static string webApiKey = "AIzaSyAaOzN7xaYvMgcB7tIseQ6W7K5kjMb-iSA";

    public static FirebaseAuthResponse? Auth { get; private set; }

    public class FirebaseAuthResponse
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

    /// <summary>
    /// Performs initial anonymous sign-in to Firebase and returns auth tokens.
    /// </summary>
    /// <returns>A FirebaseAuthResponse object containing idToken, refreshToken, and other details, or null if an error occurs.</returns>
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
                AnsiConsole.MarkupLine(
                    $"[red]Error signing in anonymously: {response.StatusCode}[/]"
                );
                AnsiConsole.MarkupLine($"[red]Error details: {errorContent}[/]");
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network error during anonymous sign-in: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON deserialization error: {ex.Message}");
            return null;
        }
    }

    public static async Task Initialize()
    {
        Auth = await SignInAnonymouslyAsync();

        if (Auth == null)
            throw new Exception("Anonymous auth failed. Check console output for details.");

        AnsiConsole.MarkupLine($"[yellow]Signed in anonymously.[/]");
    }

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
        AnsiConsole.MarkupLine($"[purple]Using: {ip}:{port} in blob[/]");
        string blob = IpPortEncoder.Encode(ip, (ushort)port);
        AnsiConsole.MarkupLine($"[purple]Encoded blob: {blob}[/]");
        AnsiConsole.MarkupLine($"[purple]Decoded blob: {IpPortEncoder.Decode(blob)}[/]");
        var content = new StringContent($"\"{blob}\"", Encoding.UTF8, "application/json");
        AnsiConsole.MarkupLine($"[green]Trying to create lobby...[/]");
        var response = await client.PutAsync(url, content);
        response.EnsureSuccessStatusCode(); //pls do not explode
        AnsiConsole.MarkupLine($"[green]Lobby created![/]");
    }

    public static async Task<string?> GetOtherPlayerBlob(string lobbyCode, string notThisOne)
    {
        bool found = false;

        while (found == false)
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

            string url = $"{BaseUrl}lobbies/{lobbyCode}.json?auth={idToken}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            Dictionary<string, string>? map = await response.Content.ReadFromJsonAsync<
                Dictionary<string, string>
            >(options);

            if (map == null || map.Count == 0)
            {
                Console.WriteLine("Lobby empty or response was 'null'.");
                return null;
            }

            // Find the first player ID that is not the client's ID (notThisOne)
            foreach (var kvp in map)
            {
                if (!string.Equals(kvp.Key, notThisOne, StringComparison.Ordinal))
                {
                    Console.WriteLine($"Found other player ID: {kvp.Key} (blob: {kvp.Value})");
                    return kvp.Value;
                }
            }

            Console.WriteLine("No other player found.");
            return null;
        }
        return null;
    }
}
