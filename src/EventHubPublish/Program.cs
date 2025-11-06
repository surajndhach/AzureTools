using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using ONE.Models.CSharp.External;
using ONE.Models.CSharp.Instrument;
using System.Text;

class Program
{
    static async Task Main()
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

        // Add 5 events
        for (var i = 1; i <= 1; i++)
        {

            var json = await File.ReadAllTextAsync("measurement.json");

            var instrumentData = InstrumentData.Parser.ParseJson(json);

            foreach (var instrumentMeasurementData in instrumentData.InstrumentMeasurementDatas.Items)
            {
                instrumentMeasurementData.Measurement.TimestampUtc = DateTime.UtcNow.ToClarosDateTime();
            }

            var jsonObj = JObject.Parse(instrumentData.ToString() ?? throw new InvalidOperationException());

            // 2. Convert serialized event to bytes
            var eventBytes = Encoding.UTF8.GetBytes(jsonObj.ToString());

            var eventData = new EventData(eventBytes);
            eventData.Properties.Add("tenantId", "440ae6c6-f4ad-4ec5-a78f-7b6c716f7270");

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