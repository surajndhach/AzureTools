using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalrClient;

internal class Program
{
    private static async Task Main(string[] args)
    {
        const string negotiateUrl = "https://api-feature-us.aquaticinformatics.net/instrument/signalr/negotiate/InstrumentHub";
        const string bearerToken = "23A14E9E5A5BABA689BDEF2935F778CC4EC5ABC8EE0D56F11883E825A8AAD598-1";   // 🔴 Replace with your token
        const string groupName = "440ae6c6-f4ad-4ec5-a78f-7b6c716f7270";

        var http = new HttpClient();

        // 1️. Build POST request with Authorization header
        var request = new HttpRequestMessage(HttpMethod.Post, negotiateUrl);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        
        // 2️. Call negotiate endpoint
        var response = await http.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();

        var negotiate = JsonSerializer.Deserialize<NegotiateResponse>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Console.WriteLine($"Negotiated URL: {negotiate!.Url}");

        // 3️. Build SignalR connection using returned URL + access token
        var connection = new HubConnectionBuilder()
            .WithUrl(negotiate.Url, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(negotiate.AccessToken)!;
            })
            .WithAutomaticReconnect()
            .Build();

        // 4️. Receive messages
        connection.On<string>("ReceiveMessage", message =>
        {
            Console.WriteLine($"📩 Message received: {message}");
        });

        connection.Reconnecting += error =>
        {
            Console.WriteLine("Reconnecting...");
            return Task.CompletedTask;
        };

        connection.Reconnected += id =>
        {
            Console.WriteLine("Reconnected. New connection id: " + id);
            return Task.CompletedTask;
        };

        Console.WriteLine("Connecting...");
        await connection.StartAsync();
        Console.WriteLine($"Connected. ConnectionId = {connection.ConnectionId}");

        // 5️. Call your Function-triggered hub method to join the group
        Console.WriteLine($"Joining group: {groupName}");
        //await connection.InvokeAsync("JoinGroup", groupName);
        await connection.SendAsync("JoinGroup", groupName);


        Console.WriteLine("Joined group. Waiting for messages...");
        Console.ReadLine();
    }

    public class NegotiateResponse
    {
        public string Url { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
    }
}