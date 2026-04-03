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

// Parse device inventory
var tenants = await DeviceInventoryParser.ParseFromFileAsync(inventoryFilePath);
Console.WriteLine($"Loaded {tenants.Count} tenant(s) from device inventory.\n");

// Set up EventGrid client
var credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
var client = new EventGridSenderClient(new Uri(options.TopicEndpoint), options.TopicName, credential);
var publisher = new EventGridPublisher(client);

bool tenantsCreated = false;
bool controllersCreated = false;
bool exit = false;

while (!exit)
{
    Console.WriteLine("Select an option:");
    Console.WriteLine("  1 - Create Tenants");
    Console.WriteLine("  2 - Create Controllers");
    Console.WriteLine("  3 - Create Sensors");
    Console.WriteLine("  Q - Quit");
    Console.Write("\nYour choice: ");

    var input = Console.ReadLine()?.Trim();

    switch (input)
    {
        case "1":
            // TODO: Add tenant creation logic
            tenantsCreated = true;
            Console.WriteLine("Tenants created successfully.\n");
            break;

        case "2":
            if (!tenantsCreated)
            {
                Console.WriteLine("Error: You must create Tenants (option 1) before creating Controllers.\n");
                break;
            }

            var controllerEvents = CloudEventBuilder.BuildControllerAssignedEvents(tenants);
            Console.WriteLine($"Built {controllerEvents.Count} Controller Instrument.Assigned cloud event(s).");

            var (controllerSuccess, controllerFail) = await publisher.SendAllAsync(controllerEvents);
            Console.WriteLine($"Controllers completed: {controllerSuccess} succeeded, {controllerFail} failed out of {controllerEvents.Count} total events.\n");

            controllersCreated = true;
            break;

        case "3":
            if (!tenantsCreated || !controllersCreated)
            {
                Console.WriteLine("Error: You must create Tenants (option 1) and Controllers (option 2) before creating Sensors.\n");
                break;
            }

            var sensorEvents = CloudEventBuilder.BuildSensorAssignedEvents(tenants);
            Console.WriteLine($"Built {sensorEvents.Count} Sensor Instrument.Assigned cloud event(s).");

            var (sensorSuccess, sensorFail) = await publisher.SendAllAsync(sensorEvents);
            Console.WriteLine($"Sensors completed: {sensorSuccess} succeeded, {sensorFail} failed out of {sensorEvents.Count} total events.\n");

            break;

        case "Q":
        case "q":
            exit = true;
            break;

        default:
            Console.WriteLine("Invalid selection. Please try again.\n");
            break;
    }
}

Console.WriteLine("Exiting. Press any key to close.");
Console.ReadKey();