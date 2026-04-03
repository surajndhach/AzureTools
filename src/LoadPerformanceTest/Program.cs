using Azure.Identity;
using Azure.Messaging.EventGrid.Namespaces;
using LoadPerformanceTest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(c =>
    {
        c.AddJsonFile("appsettings.json", optional: false);
        c.AddEnvironmentVariables();
    })
    .Build();

var config = host.Services.GetRequiredService<IConfiguration>();
var options = config.GetSection("EventGridSender").Get<EventGridSenderOptions>()!;
var inventoryFilePath = config["DeviceInventoryFilePath"] ?? "Scripts\\device-inventory-tenants2-sensors12.json";

// Step 1: Parse device inventory
var tenants = await DeviceInventoryParser.ParseFromFileAsync(inventoryFilePath);
Console.WriteLine($"Loaded {tenants.Count} tenant(s) from device inventory.");

// Step 2: Build cloud events for all tenants, controllers, and sensors
var cloudEvents = CloudEventBuilder.BuildInstrumentAssignedEvents(tenants);
Console.WriteLine($"Built {cloudEvents.Count} Instrument.Assigned cloud event(s).\n");

// Step 3: Send events to EventGrid
var credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
var client = new EventGridSenderClient(new Uri(options.TopicEndpoint), options.TopicName, credential);

var publisher = new EventGridPublisher(client);
var (successCount, failCount) = await publisher.SendAllAsync(cloudEvents);

Console.WriteLine($"\nCompleted: {successCount} succeeded, {failCount} failed out of {cloudEvents.Count} total events.");
Console.WriteLine("Press any key to exit.");
Console.ReadKey();