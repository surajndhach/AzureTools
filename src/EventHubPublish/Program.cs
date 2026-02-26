using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using ONE.Models.CSharp.External;
using ONE.Models.CSharp.Instrument;
using System.Text;

class Program
{
    public static async Task Main()
    {
        // Load configuration
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var eventHubSection = config.GetSection("EventHub");

        var connectionString = eventHubSection["ConnectionString"];
        var eventHubName = eventHubSection["EventHubName"];

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(eventHubName))
        {
            Console.WriteLine("Missing configuration values. Please check appsettings.json.");
            return;
        }

        Console.WriteLine($"Connecting to Event Hub '{eventHubName}' using connection string...");

        // Create a producer client
        await using var producer = new EventHubProducerClient(connectionString, eventHubName);

        // Create a batch to hold the events
        using var eventBatch = await producer.CreateBatchAsync();

        var test = new MessageSenderClient();

        await test.RunAsync(eventBatch, producer);
    }
}

public class MessageSenderClient()
{
    public async Task RunAsync(EventDataBatch batch, EventHubProducerClient producer)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Select Operation:");
            Console.WriteLine("1 - Publish Measurement");
            Console.WriteLine("2 - Publish Status");
            Console.WriteLine("3 - Publish Events");
            Console.WriteLine("4 - Publish Diagnostics");
            Console.WriteLine("5 - Publish Settings");
            Console.WriteLine("5 - Publish All");
            Console.WriteLine("0 - Exit");
            Console.Write("Enter choice: ");

            var choice = Console.ReadLine();

            if (choice == "0")
                break;

            await ExecuteChoice(choice, batch, producer);

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    private async Task ExecuteChoice(string? choice, EventDataBatch batch, EventHubProducerClient producer)
    {      
        switch (choice)
        {
            case "1":
                await ProcessMeasurement(batch, producer);
                break;

            case "2":
                await ProcessStatus(batch, producer);
                break;

            case "3":
                await ProcessEvents(batch, producer);
                break;

            case "4":
                await ProcessDiagnostics(batch, producer);
                break;

            case "5":
                await ProcessSettings(batch, producer);
                break;

            case "6":
                await ProcessMeasurement(batch, producer);
                await ProcessStatus(batch, producer);
                await ProcessEvents(batch, producer);
                await ProcessDiagnostics(batch, producer);
                await ProcessSettings(batch, producer);
                break;

            default:
                Console.WriteLine("Invalid choice.");
                break;
        }
    }

    private async Task ProcessMeasurement(EventDataBatch eventBatch, EventHubProducerClient producer)
    {
        // Send 1 event
        for (var i = 0; i < 1; i++)
        {

            var json = await File.ReadAllTextAsync("instrumentmeasurementData.json");

            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

            var instrumentData = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentData>(json);

            var timestamp = DateTime.UtcNow.AddHours(-1).AddMinutes(i).ToClarosDateTime();

            //measurement update
            foreach (var instrumentMeasurementData in instrumentData.InstrumentMeasurementDatas.Items)
            {
                instrumentMeasurementData.Measurement.TimestampUtc = timestamp;
            }

            var jsonObj = JObject.Parse(instrumentData.ToString() ?? throw new InvalidOperationException());

            // 2. Convert serialized event to bytes
            var eventBytes = Encoding.UTF8.GetBytes(jsonObj.ToString());

            var eventData = new EventData(eventBytes);
            eventData.Properties.Add("tenantId", instrumentData.TenantId);

            if (!eventBatch.TryAdd(eventData))
            {
                Console.WriteLine($"Event {i} too large for batch — skipping.");
            }

            Console.WriteLine($"Event with timestamp {timestamp}");
        }

        // Send batch
        await producer.SendAsync(eventBatch);
        Console.WriteLine("✅ Successfully sent events to Event Hub!");
    }

    private async Task ProcessStatus(EventDataBatch eventBatch, EventHubProducerClient producer)
    {
        // Send 1 event
        for (var i = 0; i < 1; i++)
        {

            var json = await File.ReadAllTextAsync("instrumentstatusdata.json");

            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

            var instrumentData = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentData>(json);

            var timestamp = DateTime.UtcNow.AddHours(-i).ToClarosDateTime();

            //status update
            foreach (var instrumentStatusData in instrumentData.InstrumentStatuses.Items)
            {
                instrumentStatusData.StatusDateTimeUtc = timestamp;
            }

            var jsonObj = JObject.Parse(instrumentData.ToString() ?? throw new InvalidOperationException());

            // 2. Convert serialized event to bytes
            var eventBytes = Encoding.UTF8.GetBytes(jsonObj.ToString());

            var eventData = new EventData(eventBytes);
            eventData.Properties.Add("tenantId", instrumentData.TenantId);

            if (!eventBatch.TryAdd(eventData))
            {
                Console.WriteLine($"Event {i} too large for batch — skipping.");
            }
        }

        // Send batch
        await producer.SendAsync(eventBatch);
        Console.WriteLine("✅ Successfully sent events to Event Hub!");
    }

    private async Task ProcessEvents(EventDataBatch eventBatch, EventHubProducerClient producer)
    {
        // Send 1 event
        for (var i = 0; i < 1; i++)
        {

            var json = await File.ReadAllTextAsync("instrumenteventdata.json");

            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

            var instrumentData = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentData>(json);

            var timestamp = DateTime.UtcNow.AddHours(-i).ToClarosDateTime();

            //events update
            foreach (var instrumentEventData in instrumentData.InstrumentEventDatas.Items)
            {
                instrumentEventData.EventDateTimeUtc = timestamp;
            }

            var jsonObj = JObject.Parse(instrumentData.ToString() ?? throw new InvalidOperationException());

            // 2. Convert serialized event to bytes
            var eventBytes = Encoding.UTF8.GetBytes(jsonObj.ToString());

            var eventData = new EventData(eventBytes);
            eventData.Properties.Add("tenantId", instrumentData.TenantId);

            if (!eventBatch.TryAdd(eventData))
            {
                Console.WriteLine($"Event {i} too large for batch — skipping.");
            }
        }

        // Send batch
        await producer.SendAsync(eventBatch);
        Console.WriteLine("✅ Successfully sent events to Event Hub!");
    }

    private async Task ProcessDiagnostics(EventDataBatch eventBatch, EventHubProducerClient producer)
    {
        // Send 1 event
        for (var i = 0; i < 1; i++)
        {

            var json = await File.ReadAllTextAsync("instrumentdiagnosticdata.json");

            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

            var instrumentData = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentData>(json);

            var timestamp = DateTime.UtcNow.AddHours(-i).ToClarosDateTime();

            //diagnostics update
            foreach (var instrumentDiagnosticData in instrumentData.InstrumentDiagnostics.Items)
            {
                foreach (var instrumentDiagnosticDatum in instrumentDiagnosticData.Values)
                {
                    instrumentDiagnosticDatum.TimestampUtc = timestamp;
                }
            }

            var jsonObj = JObject.Parse(instrumentData.ToString() ?? throw new InvalidOperationException());

            // 2. Convert serialized event to bytes
            var eventBytes = Encoding.UTF8.GetBytes(jsonObj.ToString());

            var eventData = new EventData(eventBytes);
            eventData.Properties.Add("tenantId", instrumentData.TenantId);

            if (!eventBatch.TryAdd(eventData))
            {
                Console.WriteLine($"Event {i} too large for batch — skipping.");
            }
        }

        // Send batch
        await producer.SendAsync(eventBatch);
        Console.WriteLine("✅ Successfully sent events to Event Hub!");
    }

    private async Task ProcessSettings(EventDataBatch eventBatch, EventHubProducerClient producer)
    {
        // Send 1 event
        for (var i = 0; i < 1; i++)
        {

            var json = await File.ReadAllTextAsync("instrumentsettingdata.json");

            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

            var instrumentData = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentData>(json);

            var timestamp = DateTime.UtcNow.AddHours(-i).ToClarosDateTime();

            //settings update
            instrumentData.InstrumentSettings.SettingsDateTimeUtc = timestamp;
            instrumentData.InstrumentSettings.Settings["name"] = $"Instrument-{DateTime.UtcNow.Ticks}";

            var jsonObj = JObject.Parse(instrumentData.ToString() ?? throw new InvalidOperationException());

            // 2. Convert serialized event to bytes
            var eventBytes = Encoding.UTF8.GetBytes(jsonObj.ToString());

            var eventData = new EventData(eventBytes);
            eventData.Properties.Add("tenantId", instrumentData.TenantId);

            if (!eventBatch.TryAdd(eventData))
            {
                Console.WriteLine($"Event {i} too large for batch — skipping.");
            }
        }

        // Send batch
        await producer.SendAsync(eventBatch);
        Console.WriteLine("✅ Successfully sent events to Event Hub!");
    }
}

internal static class HelperExtensions
{
    public static ClarosJsonTicksDateTime ToClarosDateTime(this DateTime dateTime)
    {
        return new ClarosJsonTicksDateTime
        {
            JsonDateTime = dateTime.ToUtc().ToString("O")
        };
    }

    public static DateTime ToUtc(this DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        return dateTime.ToUniversalTime();
    }
}