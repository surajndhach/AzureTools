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
var inventoryFileName = Path.GetFileName(inventoryFilePath);
Console.WriteLine($"Loaded {tenants.Count} tenant(s) from device inventory.\n");
Logger.LogInfo($"Parsed inventory file: {inventoryFileName} — loaded {tenants.Count} tenant(s).");

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
    Console.WriteLine("  6 - Delete Tenants");
    Console.WriteLine("  Q - Quit");
    Console.Write("\nYour choice: ");

    var input = Console.ReadLine()?.Trim();

    switch (input)
    {
        case "1":
            var count = await TenantFacade.CreateTenantsAsync(tenants);
            Console.WriteLine($"Tenant creation completed: {count} out of {tenants.Count} tenants created successfully.\n");
            break;

        case "2":
            try
            {
                var controllerEvents = CloudEventBuilder.BuildControllerAssignedEvents(tenants);
                Console.WriteLine($"Built {controllerEvents.Count} Controller Instrument.Assigned cloud event(s).");
                Logger.LogInfo($"[{inventoryFileName}] Built {controllerEvents.Count} Controller Instrument.Assigned cloud event(s).");

                var (controllerSuccess, controllerFail) = await publisher.SendAllAsync(controllerEvents);
                Console.WriteLine($"Controllers completed: {controllerSuccess} succeeded, {controllerFail} failed out of {controllerEvents.Count} total events.\n");
                Logger.LogInfo($"[{inventoryFileName}] Controllers completed: {controllerSuccess} succeeded, {controllerFail} failed out of {controllerEvents.Count} total events.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating controllers: {ex.Message}\n");
                Logger.LogError($"[{inventoryFileName}] Error creating controllers.", ex);
            }
            break;

        case "3":
            try
            {
                var sensorEvents = CloudEventBuilder.BuildSensorAssignedEvents(tenants);
                Console.WriteLine($"Built {sensorEvents.Count} Sensor Instrument.Assigned cloud event(s).");
                Logger.LogInfo($"[{inventoryFileName}] Built {sensorEvents.Count} Sensor Instrument.Assigned cloud event(s).");

                var (sensorSuccess, sensorFail) = await publisher.SendAllAsync(sensorEvents);
                Console.WriteLine($"Sensors completed: {sensorSuccess} succeeded, {sensorFail} failed out of {sensorEvents.Count} total events.\n");
                Logger.LogInfo($"[{inventoryFileName}] Sensors completed: {sensorSuccess} succeeded, {sensorFail} failed out of {sensorEvents.Count} total events.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating sensors: {ex.Message}\n");
                Logger.LogError($"[{inventoryFileName}] Error creating sensors.", ex);
            }
            break;

        case "4":
            try
            {
                var controllerUpdateEvents = CloudEventBuilder.BuildControllerUpdatedEvents(tenants);
                var sensorUpdateEvents = CloudEventBuilder.BuildSensorUpdatedEvents(tenants);
                var allUpdateEvents = new List<Azure.Messaging.CloudEvent>();
                allUpdateEvents.AddRange(controllerUpdateEvents);
                allUpdateEvents.AddRange(sensorUpdateEvents);
                Console.WriteLine($"Built {allUpdateEvents.Count} Instrument.Updated cloud event(s) ({controllerUpdateEvents.Count} controllers, {sensorUpdateEvents.Count} sensors).");
                Logger.LogInfo($"[{inventoryFileName}] Built {allUpdateEvents.Count} Instrument.Updated cloud event(s) ({controllerUpdateEvents.Count} controllers, {sensorUpdateEvents.Count} sensors).");

                var (updateSuccess, updateFail) = await publisher.SendAllAsync(allUpdateEvents);
                Console.WriteLine($"Update completed: {updateSuccess} succeeded, {updateFail} failed out of {allUpdateEvents.Count} total events.\n");
                Logger.LogInfo($"[{inventoryFileName}] Update completed: {updateSuccess} succeeded, {updateFail} failed out of {allUpdateEvents.Count} total events.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating instruments: {ex.Message}\n");
                Logger.LogError($"[{inventoryFileName}] Error updating instruments.", ex);
            }
            break;

        case "5":
            try
            {
                var sensorUnassignEvents = CloudEventBuilder.BuildSensorUnassignedEvents(tenants);
                var controllerUnassignEvents = CloudEventBuilder.BuildControllerUnassignedEvents(tenants);
                var allDeleteEvents = new List<Azure.Messaging.CloudEvent>();
                allDeleteEvents.AddRange(sensorUnassignEvents);
                allDeleteEvents.AddRange(controllerUnassignEvents);
                Console.WriteLine($"Built {allDeleteEvents.Count} Instrument.Unassigned cloud event(s) ({sensorUnassignEvents.Count} sensors, {controllerUnassignEvents.Count} controllers).");
                Logger.LogInfo($"[{inventoryFileName}] Built {allDeleteEvents.Count} Instrument.Unassigned cloud event(s) ({sensorUnassignEvents.Count} sensors, {controllerUnassignEvents.Count} controllers).");

                var (deleteSuccess, deleteFail) = await publisher.SendAllAsync(allDeleteEvents);
                Console.WriteLine($"Delete completed: {deleteSuccess} succeeded, {deleteFail} failed out of {allDeleteEvents.Count} total events.\n");
                Logger.LogInfo($"[{inventoryFileName}] Delete completed: {deleteSuccess} succeeded, {deleteFail} failed out of {allDeleteEvents.Count} total events.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting instruments: {ex.Message}\n");
                Logger.LogError($"[{inventoryFileName}] Error deleting instruments.", ex);
            }
            break;

        case "6":
            var deleteCount = await TenantFacade.DeleteTenantsAsync(tenants);
            Console.WriteLine($"Tenant deletion completed: {deleteCount} out of {tenants.Count} tenants deleted successfully.\n");
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