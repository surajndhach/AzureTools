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
var inventoryFilePath = config["DeviceInventoryFilePath"];

// Parse device inventory
var tenants = await DeviceInventoryParser.ParseFromFileAsync(inventoryFilePath);
Console.WriteLine($"Loaded {tenants.Count} tenant(s) from device inventory.\n");

// Set up EventGrid client
var credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
var client = new EventGridSenderClient(new Uri(options.TopicEndpoint), options.TopicName, credential);
var publisher = new EventGridPublisher(client);

bool exit = false;

while (!exit)
{
    Console.WriteLine("Select an option:");
    Console.WriteLine("  1 - Create Tenants");
    Console.WriteLine("  2 - Create Controllers");
    Console.WriteLine("  3 - Create Sensors");
    Console.WriteLine("  4 - Update Instruments");
    Console.WriteLine("  5 - Delete Instruments");
    Console.WriteLine("  Q - Quit");
    Console.Write("\nYour choice: ");

    var input = Console.ReadLine()?.Trim();

    switch (input)
    {
        case "1":
            // TODO: Add tenant creation logic
            Console.WriteLine("Tenants created successfully.\n");
            break;

        case "2":
            var controllerEvents = CloudEventBuilder.BuildControllerAssignedEvents(tenants);
            Console.WriteLine($"Built {controllerEvents.Count} Controller Instrument.Assigned cloud event(s).");

            var (controllerSuccess, controllerFail) = await publisher.SendAllAsync(controllerEvents);
            Console.WriteLine($"Controllers completed: {controllerSuccess} succeeded, {controllerFail} failed out of {controllerEvents.Count} total events.\n");
            break;

        case "3":
            var sensorEvents = CloudEventBuilder.BuildSensorAssignedEvents(tenants);
            Console.WriteLine($"Built {sensorEvents.Count} Sensor Instrument.Assigned cloud event(s).");

            var (sensorSuccess, sensorFail) = await publisher.SendAllAsync(sensorEvents);
            Console.WriteLine($"Sensors completed: {sensorSuccess} succeeded, {sensorFail} failed out of {sensorEvents.Count} total events.\n");
            break;

        case "4":
            var controllerUpdateEvents = CloudEventBuilder.BuildControllerUpdatedEvents(tenants);
            var sensorUpdateEvents = CloudEventBuilder.BuildSensorUpdatedEvents(tenants);
            var allUpdateEvents = new List<Azure.Messaging.CloudEvent>();
            allUpdateEvents.AddRange(controllerUpdateEvents);
            allUpdateEvents.AddRange(sensorUpdateEvents);
            Console.WriteLine($"Built {allUpdateEvents.Count} Instrument.Updated cloud event(s) ({controllerUpdateEvents.Count} controllers, {sensorUpdateEvents.Count} sensors).");

            var (updateSuccess, updateFail) = await publisher.SendAllAsync(allUpdateEvents);
            Console.WriteLine($"Update completed: {updateSuccess} succeeded, {updateFail} failed out of {allUpdateEvents.Count} total events.\n");
            break;

        case "5":
            var sensorUnassignEvents = CloudEventBuilder.BuildSensorUnassignedEvents(tenants);
            var controllerUnassignEvents = CloudEventBuilder.BuildControllerUnassignedEvents(tenants);
            var allDeleteEvents = new List<Azure.Messaging.CloudEvent>();
            allDeleteEvents.AddRange(sensorUnassignEvents);
            allDeleteEvents.AddRange(controllerUnassignEvents);
            Console.WriteLine($"Built {allDeleteEvents.Count} Instrument.Unassigned cloud event(s) ({sensorUnassignEvents.Count} sensors, {controllerUnassignEvents.Count} controllers).");

            var (deleteSuccess, deleteFail) = await publisher.SendAllAsync(allDeleteEvents);
            Console.WriteLine($"Delete completed: {deleteSuccess} succeeded, {deleteFail} failed out of {allDeleteEvents.Count} total events.\n");
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