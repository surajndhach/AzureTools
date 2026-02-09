using Azure.Core;
using Azure.Identity;
using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;
using EventGridReceiver;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

internal class Program
{
    private static async Task Main(string[] args)
    {
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(c =>
            {
                c.AddJsonFile("appsettings.json", optional: false);
                c.AddEnvironmentVariables();
            })
            .Build();

        var config = host.Services.GetRequiredService<IConfiguration>();

        var options = config.GetSection("EventGridReciever").Get<EventGridRecieverOptions>()!;

        RecieverClient receiver = new RecieverClient();

        // Receive the published CloudEvents. 
        var client = receiver.RecieveEventWithServicePrincipleAsync(options);

        var result = await client.ReceiveAsync(10);

        Console.WriteLine("Received Response");
        Console.WriteLine("-----------------");
        Console.WriteLine($"Response Status: {result.GetRawResponse().Status}");
        Console.WriteLine($"Response Count: {result.Value.Details.Count}");

        // handle received messages. Define these variables on the top.

        var toRelease = new List<string>();
        var toAcknowledge = new List<string>();
        var toReject = new List<string>();

        // Iterate through the results and collect the lock tokens for events we want to release/acknowledge/result

        foreach (ReceiveDetails detail in result.Value.Details)
        {
            CloudEvent @event = detail.Event;
            BrokerProperties brokerProperties = detail.BrokerProperties;
            Console.WriteLine($"Recieved event - {@event.Data?.ToString()}");

            var instrumentEventBodyString = @event.Data is not null && @event.Data.Length > 0
                    ? Encoding.UTF8.GetString(@event.Data)
                    : "{}";

            try
            {
                var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

                if (@event.Type.Contains("InstrumentManifest."))
                {
                    var instrumentManifest = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentManifest>(instrumentEventBodyString);
                    Console.WriteLine($"Deserialized event - {@instrumentManifest.ToString()}");
                }
                else if (@event.Type.Contains("Instrument."))
                {
                    var instrument = parser.Parse<ONE.Models.CSharp.Instrument.Instrument>(instrumentEventBodyString);
                    Console.WriteLine($"Deserialized event - {@instrument.ToString()}");
                }               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read event body: {ex.Message}");
            }

            // The lock token is used to acknowledge, reject or release the event
            Console.WriteLine(brokerProperties.LockToken);
            Console.WriteLine();

            // If the event is from the "employee_source" and the name is "Bob", we are not able to acknowledge it yet, so we release it
            if (@event.Source == "Claros.IoT.Registry")
            {
                toAcknowledge.Add(brokerProperties.LockToken);
            }
            // reject all other events
            else
            {
                toReject.Add(brokerProperties.LockToken);
            }
        }

        // Release/acknowledge/reject the events

        if (toRelease.Count > 0)
        {
            ReleaseResult releaseResult = await client.ReleaseAsync(toRelease);

            // Inspect the Release result
            Console.WriteLine($"Failed count for Release: {releaseResult.FailedLockTokens.Count}");
            foreach (FailedLockToken failedLockToken in releaseResult.FailedLockTokens)
            {
                Console.WriteLine($"Lock Token: {failedLockToken.LockToken}");
                Console.WriteLine($"Error Code: {failedLockToken.Error}");
                Console.WriteLine($"Error Description: {failedLockToken.ToString()}");
            }

            Console.WriteLine($"Success count for Release: {releaseResult.SucceededLockTokens.Count}");
            foreach (string lockToken in releaseResult.SucceededLockTokens)
            {
                Console.WriteLine($"Lock Token: {lockToken}");
            }
            Console.WriteLine();
        }

        if (toAcknowledge.Count > 0)
        {
            AcknowledgeResult acknowledgeResult = await client.AcknowledgeAsync(toAcknowledge);

            // Inspect the Acknowledge result
            Console.WriteLine($"Failed count for Acknowledge: {acknowledgeResult.FailedLockTokens.Count}");
            foreach (FailedLockToken failedLockToken in acknowledgeResult.FailedLockTokens)
            {
                Console.WriteLine($"Lock Token: {failedLockToken.LockToken}");
                Console.WriteLine($"Error Code: {failedLockToken.Error}");
                Console.WriteLine($"Error Description: {failedLockToken.ToString}");
            }

            Console.WriteLine($"Success count for Acknowledge: {acknowledgeResult.SucceededLockTokens.Count}");
            foreach (string lockToken in acknowledgeResult.SucceededLockTokens)
            {
                Console.WriteLine($"Lock Token: {lockToken}");
            }
            Console.WriteLine();
        }

        if (toReject.Count > 0)
        {
            RejectResult rejectResult = await client.RejectAsync(toReject);

            // Inspect the Reject result
            Console.WriteLine($"Failed count for Reject: {rejectResult.FailedLockTokens.Count}");
            foreach (FailedLockToken failedLockToken in rejectResult.FailedLockTokens)
            {
                Console.WriteLine($"Lock Token: {failedLockToken.LockToken}");
                Console.WriteLine($"Error Code: {failedLockToken.Error}");
                Console.WriteLine($"Error Description: {failedLockToken.ToString}");
            }

            Console.WriteLine($"Success count for Reject: {rejectResult.SucceededLockTokens.Count}");
            foreach (string lockToken in rejectResult.SucceededLockTokens)
            {
                Console.WriteLine($"Lock Token: {lockToken}");
            }
            Console.WriteLine();
        }
    }
}

public class TestModel
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class RecieverClient
{
    public EventGridReceiverClient RecieveEventWithServicePrincipleAsync(EventGridRecieverOptions options)
    {
        var spCredential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
        return CreateEventGridRecieverClient(spCredential, options);
    }

    private EventGridReceiverClient CreateEventGridRecieverClient(TokenCredential credential, EventGridRecieverOptions options)
    {
        var topicEndpoint = new Uri(options.TopicEndpoint);

        // Create EventGridSenderClient with full endpoint
        return new EventGridReceiverClient(topicEndpoint, options.TopicName, options.SubscriptionName, credential);
    }
}