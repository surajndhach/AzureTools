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
    public async Task RunAsync(EventGridSenderOptions options)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Select Operation:");
            Console.WriteLine("1 - Assigned (Controller + Sensor)");
            Console.WriteLine("2 - Updated (Controller + Sensor)");
            Console.WriteLine("3 - UnAssigned (Sensor + Controller)");
            Console.WriteLine("4 - Assigned (Manifest)");
            Console.WriteLine("5 - Updated (Manifest)");
            Console.WriteLine("6 - Run All");
            Console.WriteLine("0 - Exit");
            Console.Write("Enter choice: ");

            var choice = Console.ReadLine();

            if (choice == "0")
                break;

            await ExecuteChoice(choice, options);

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    private async Task ExecuteChoice(string? choice, EventGridSenderOptions options)
    {
        var spCredential = new ClientSecretCredential(
            options.TenantId,
            options.ClientId,
            options.ClientSecret);

        var client = CreateEventGridSenderClient(spCredential, options);
        var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

        switch (choice)
        {
            case "1":
                await CreateInstrument(client, parser);
                break;

            case "2":
                await UpdateInstrument(client, parser);
                break;

            case "3":
                await DeleteInstrument(client);
                break;

            case "4":
                await CreateManifest(client, parser);
                break;

            case "5":
                await UpdateManifest(client, parser);
                break;

            case "6":
                await CreateInstrument(client, parser);
                await UpdateInstrument(client, parser);
                await DeleteInstrument(client);
                await CreateManifest(client, parser);
                await UpdateManifest(client, parser);
                break;

            default:
                Console.WriteLine("Invalid choice.");
                break;
        }
    }

    private async Task CreateInstrument(EventGridSenderClient client, JsonParser parser)
    {
        Response? response = null;

        Console.WriteLine("Sending Controller Assigned...");
        // Validation
        var instrumentControllerAssignedEvent = await EventGridData.GetInstrumentControllerAssigned();
        var instrumentControllerEventBodyString = instrumentControllerAssignedEvent.Data is not null && instrumentControllerAssignedEvent.Data.Length > 0
                ? Encoding.UTF8.GetString(instrumentControllerAssignedEvent.Data) : "{}";
        var instrumentController = parser.Parse<ONE.Models.CSharp.Instrument.Instrument>(instrumentControllerEventBodyString);
        response = await client.SendAsync(instrumentControllerAssignedEvent);
        Console.WriteLine($"Response: {response.Status}");

        await Task.Delay(5000);

        Console.WriteLine("Sending Sensor Assigned...");
        //Validation
        var instrumentSensorAssignedEvent = await EventGridData.GetInstrumentSensorAssigned();
        var instrumentSensorEventBodyString = instrumentSensorAssignedEvent.Data is not null && instrumentSensorAssignedEvent.Data.Length > 0
            ? Encoding.UTF8.GetString(instrumentSensorAssignedEvent.Data) : "{}";
        var instrumentSensor = parser.Parse<ONE.Models.CSharp.Instrument.Instrument>(instrumentSensorEventBodyString);
        response = await client.SendAsync(instrumentSensorAssignedEvent);
        Console.WriteLine($"Response: {response.Status}");
    }

    private async Task CreateManifest(EventGridSenderClient client, JsonParser parser)
    {
        Response? response = null;

        Console.WriteLine("Sending Manifest for Controller...");
        //Validation
        var manifestControllerEvent = await EventGridData.GetInstrumentManifestControllerAssignedEvent();
        var instrumentManifesteventBodyString = manifestControllerEvent.Data is not null && manifestControllerEvent.Data.Length > 0 ? Encoding.UTF8.GetString(manifestControllerEvent.Data) : "{}";
        var instrumentManifest = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentManifest>(instrumentManifesteventBodyString);

        response = await client.SendAsync(manifestControllerEvent);
        Console.WriteLine($"Response: {response.Status}");

        await Task.Delay(5000);

        Console.WriteLine("Sending Manifest for Sensor...");
        //Validation
        var manifestSensorEvent = await EventGridData.GetInstrumentManifestSensorAssignedEvent();
        var instrumentManifestSensoreventBodyString = manifestSensorEvent.Data is not null && manifestSensorEvent.Data.Length > 0 ? Encoding.UTF8.GetString(manifestSensorEvent.Data) : "{}";
        var instrumentManifestSensor = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentManifest>(instrumentManifestSensoreventBodyString);

        response = await client.SendAsync(manifestSensorEvent);
        Console.WriteLine($"Response: {response.Status}");
    }

    private async Task UpdateManifest(EventGridSenderClient client, JsonParser parser)
    {
        Response? response = null;

        Console.WriteLine("Sending Manifest for Controller...");
        //Validation
        var manifestControllerEvent = await EventGridData.GetInstrumentManifestControllerUpdatedEvent();
        var instrumentManifesteventBodyString = manifestControllerEvent.Data is not null && manifestControllerEvent.Data.Length > 0 ? Encoding.UTF8.GetString(manifestControllerEvent.Data) : "{}";
        var instrumentManifest = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentManifest>(instrumentManifesteventBodyString);

        response = await client.SendAsync(manifestControllerEvent);
        Console.WriteLine($"Response: {response.Status}");

        await Task.Delay(5000);

        Console.WriteLine("Sending Manifest for Sensor...");
        //Validation
        var manifestSensorEvent = await EventGridData.GetInstrumentManifestSensorUpdatedEvent();
        var instrumentManifestSensoreventBodyString = manifestSensorEvent.Data is not null && manifestSensorEvent.Data.Length > 0 ? Encoding.UTF8.GetString(manifestSensorEvent.Data) : "{}";
        var instrumentManifestSensor = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentManifest>(instrumentManifestSensoreventBodyString);

        response = await client.SendAsync(manifestSensorEvent);
        Console.WriteLine($"Response: {response.Status}");
    }

    private async Task UpdateInstrument(EventGridSenderClient client, JsonParser parser)
    {
        Response? response = null;

        Console.WriteLine("Sending Controller Updated...");
        // Validation
        var instrumentControllerUpdatedEvent = await EventGridData.GetInstrumentControllerUpdated();
        var instrumentControllerUpdatedEventBodyString = instrumentControllerUpdatedEvent.Data is not null && instrumentControllerUpdatedEvent.Data.Length > 0
            ? Encoding.UTF8.GetString(instrumentControllerUpdatedEvent.Data) : "{}";
        var instrumentControllerUpdated = parser.Parse<ONE.Models.CSharp.Instrument.Instrument>(instrumentControllerUpdatedEventBodyString);
        response = await client.SendAsync(instrumentControllerUpdatedEvent);
        Console.WriteLine($"Response: {response.Status}");

        await Task.Delay(5000);

        Console.WriteLine("Sending Sensor Updated...");
        // Validation
        var instrumentSensorUpdatedEvent = await EventGridData.GetInstrumentSensorUpdated();
        var instrumentSensorUpdatedEventBodyString = instrumentSensorUpdatedEvent.Data is not null && instrumentSensorUpdatedEvent.Data.Length > 0
            ? Encoding.UTF8.GetString(instrumentSensorUpdatedEvent.Data) : "{}";
        var instrumentSensorUpdated = parser.Parse<ONE.Models.CSharp.Instrument.Instrument>(instrumentSensorUpdatedEventBodyString);
        response = await client.SendAsync(instrumentSensorUpdatedEvent);
        Console.WriteLine($"Response: {response.Status}");
    }

    private async Task DeleteInstrument(EventGridSenderClient client)
    {
        Response? response = null;

        Console.Write("Enter TenantId: ");
        var tenantId = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            Console.WriteLine("TenantId cannot be empty.");
            return;
        }

        Console.Write("Enter InstrumentId (Guid): ");
        var instrumentIdInput = Console.ReadLine();

        if (!Guid.TryParse(instrumentIdInput, out var instrumentId))
        {
            Console.WriteLine("Invalid Guid format.");
            return;
        }

        Console.WriteLine("Deleting Instrument...");
        response = await client.SendAsync(EventGridData.GetInstrumentUnassigned(
                tenantId, instrumentIdInput));
        Console.WriteLine($"Response: {response.Status}");
    }

    public async Task<bool> SendEventWithServicePrincipleAsync(EventGridSenderOptions options)
    { 
        try
        {
            await RunAsync(options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read event body: {ex.Message}");
        }    

        return false;
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
