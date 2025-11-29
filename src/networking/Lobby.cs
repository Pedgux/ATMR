namespace ATMR.Networking;

using System.Net.Http;
using System.Text;
// Firebase packages removed; using plain HTTP PUT for lobby signaling

public static class Lobby
{
    private const string BaseUrl =
        "https://p2plobbysignaler-default-rtdb.europe-west1.firebasedatabase.app/";

    // HTTP to GET / PUT data
    private static readonly HttpClient client = new HttpClient();

    // Firebase client for authorized DB access below instructions
    // https://github.com/step-up-labs/firebase-authentication-dotnet

    public static async Task Join(string lobbyCode, string playerId, string ip, int port)
    {
        //need to PUT this into existence (pun intended)
        string url = $"{BaseUrl}lobbies/{lobbyCode}/{playerId}.json";
        string blob = IpPortEncoder.Encode(ip, (ushort)port);
        var content = new StringContent($"\"{blob}\"", Encoding.UTF8, "application/json");
        var response = await client.PutAsync(url, content);
        response.EnsureSuccessStatusCode();
    }
}
