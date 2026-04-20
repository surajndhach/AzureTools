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
    Console.WriteLine("  7 - Publish Instrument Data");
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
        case "7":
            try
            {
                Console.WriteLine("Select data type to publish:");
                Console.WriteLine("  1 - Measurement Data");
                Console.WriteLine("  2 - Diagnostic Data");
                Console.WriteLine("  3 - Status Data");
                Console.WriteLine("  4 - Event Data");
                Console.WriteLine("  5 - Settings Data");
                Console.Write("Your choice: ");
                var dataTypeInput = Console.ReadLine()?.Trim();

                string fileName = dataTypeInput switch
                {
                    "1" => "instrumentmeasurementdata.json",
                    "2" => "instrumentdiagnosticdata.json",
                    "3" => "instrumentstatusdata.json",
                    "4" => "instrumenteventdata.json",
                    "5" => "instrumentsettingdata.json",
                    _ => null
                };

                string dataType = dataTypeInput switch
                {
                    "1" => "measurement",
                    "2" => "diagnostic",
                    "3" => "status",
                    "4" => "event",
                    "5" => "settings",
                    _ => null
                };

                if (fileName == null || dataType == null)
                {
                    Console.WriteLine("Invalid data type selection.\n");
                    break;
                }

                if (!File.Exists(fileName))
                {
                    Console.WriteLine($"File '{fileName}' not found.\n");
                    break;
                }

                var json = await File.ReadAllTextAsync(fileName);

                // Generate a list of updated data objects for publishing
                var updatedDataList = InstrumentDataUpdater.UpdateWithInventory(json, tenants, dataType);

                if (updatedDataList.Count == 0)
                {
                    Console.WriteLine("No data to publish for the selected type and inventory.\n");
                    break;
                }

                var eventType = Path.GetFileNameWithoutExtension(fileName);

                var eventHubConfig = config.GetSection("EventHub");
                var eventHubConnectionString = eventHubConfig["ConnectionString"];
                var eventHubName = eventHubConfig["Name"];

                await using var eventHubPublisher = new EventHubPublisher(eventHubConnectionString, eventHubName);

                int successCount = 0, failCount = 0;
                foreach (var updatedJson in updatedDataList)
                {
                    try
                    {
                        await eventHubPublisher.PublishAsync(updatedJson, eventType);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        Logger.LogError($"Failed to publish {eventType} for instrument: {ex.Message}", ex);
                    }
                }

                Console.WriteLine($"{eventType} published to Event Hub for {successCount} instrument(s), {failCount} failed.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing to Event Hub: {ex.Message}\n");
            }
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