using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventGrid.Namespaces;
using EventGridSender;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(c =>
            {
                c.AddJsonFile("appsettings.json", optional: false);
                c.AddEnvironmentVariables();
            })
            .Build();

        var config = host.Services.GetRequiredService<IConfiguration>();

        var options = config.GetSection("EventGridSender").Get<EventGridSenderOptions>()!;

        // Publish a batch of CloudEvents.
        SenderClient test = new SenderClient();


        var result = await test.SendEventWithServicePrincipleAsync(options);
        Console.WriteLine(result);

        Console.WriteLine("event have been published to the topic. Press any key to end the application.");
        Console.ReadKey();
    }
}

public class SenderClient
{
    public async Task<bool> SendEventWithServicePrincipleAsync(EventGridSenderOptions options)
    {
        Response? response = null;
        
        var spCredential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);

        var client = CreateEventGridSenderClient(spCredential, options);

        var manifestEvent = EventGridData.GetInstrumentManifestEvent();    

        var instrumentEvent = EventGridData.getInstrumentAssignedEvent();        

        try
        {
            var instrumentManifesteventBodyString = manifestEvent.Data is not null && manifestEvent.Data.Length > 0
            ? Encoding.UTF8.GetString(manifestEvent.Data)
            : "{}";

            var instrumentEventBodyString = instrumentEvent.Data is not null && instrumentEvent.Data.Length > 0
            ? Encoding.UTF8.GetString(instrumentEvent.Data)
            : "{}";

            // Just to parse and test if the json deserialization works
            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

            var instrument = parser.Parse<ONE.Models.CSharp.Instrument.Instrument>(instrumentEventBodyString);
            var instrumentManifest = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentManifest>(instrumentManifesteventBodyString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read event body: {ex.Message}");
        }        

        //Update this method call According to requirement
        response = await client.SendAsync(instrumentEvent);
        Console.WriteLine($"Response: {response.Status}");

        response = await client.SendAsync(manifestEvent);
        Console.WriteLine($"Response: {response.Status}");

        return IsSuccessResponse(response);
    }

    private EventGridSenderClient CreateEventGridSenderClient(TokenCredential credential, EventGridSenderOptions options)
    {
        var topicEndpoint = new Uri(options.TopicEndpoint);

        // Create EventGridSenderClient with full endpoint
        return new EventGridSenderClient(topicEndpoint, options.TopicName, credential);
    }

    private bool IsSuccessResponse(Response response)
    {
        if (response.Status >= 200 && response.Status <= 299)
            return true;
        return false;
    }
}
