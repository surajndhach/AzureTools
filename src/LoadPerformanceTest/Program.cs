using Azure.Identity;
using Azure.Messaging.EventGrid.Namespaces;
using LoadPerformanceTest.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ONE.Models.CSharp.Instrument;


namespace LoadPerformanceTest
{
    internal static class Program
    {
        private static IConfiguration _config = null!;
        private static List<Tenant> _tenants = null!;
        private static string _inventoryFileName = null!;
        private static EventGridPublisher _publisher = null!;
        private static List<InstrumentTwinSubType> _instrumentSubTypes = null!;
        public static List<InstrumentManifest> _instrumentManifests = null!;

        public static async Task Main(string[] args)
        {
            await InitializeApplicationAsync();
            await RunInteractiveMenuAsync();

            Console.WriteLine("Exiting. Press any key to close.");
            Console.ReadKey();
        }

        /// <summary>
        /// Initializes the application configuration, inventory, manifests, and services.
        /// </summary>
        private static async Task InitializeApplicationAsync()
        {
            // Initialize configuration
            using var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(c =>
                {
                    c.AddJsonFile("appsettings.json", optional: false);
                    c.AddEnvironmentVariables();
                })
                .Build();

            _config = host.Services.GetRequiredService<IConfiguration>();
            var options = _config.GetSection("EventGridSender").Get<EventGridSenderOptions>()!;

            // Initialize file paths
            var inventoryFilePath = _config["DeviceInventoryFilePath"];
            var manifestsFilePath = "manifests.json";

            // Parse device inventory
            _tenants = await DeviceInventoryParser.ParseFromFileAsync(inventoryFilePath);
            _inventoryFileName = Path.GetFileName(inventoryFilePath);
            Console.WriteLine($"Loaded {_tenants.Count} tenant(s) from device inventory.");
            Logger.LogInfo($"Parsed inventory file: {_inventoryFileName} — loaded {_tenants.Count} tenant(s).");

            await LoadManifestsAsync(manifestsFilePath);

            // Set up EventGrid client
            var credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
            var client = new EventGridSenderClient(new Uri(options.TopicEndpoint), options.TopicName, credential);
            _publisher = new EventGridPublisher(client);
        }

        private static async Task LoadManifestsAsync(string manifestsFilePath)
        {
            // Parse manifests and extract instrument manifests
            try
            {
                var (subTypes, manifests) = await ManifestParser.ParseManifestsCompleteAsync(manifestsFilePath);
                _instrumentSubTypes = subTypes;
                _instrumentManifests = manifests;

                var manifestsFileName = Path.GetFileName(manifestsFilePath);
                Console.WriteLine($"Loaded {_instrumentSubTypes.Count} instrument sub-type(s) from manifests file.");
                Console.WriteLine($"Extracted {_instrumentManifests.Count} instrument manifest(s) from property bags.\n");

                Logger.LogInfo($"Parsed manifests file: {manifestsFileName} — loaded {_instrumentSubTypes.Count} sub-type(s), extracted {_instrumentManifests.Count} manifest(s).");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading manifests: {ex.Message}");
                Logger.LogError("Failed to load manifests file.", ex);

                // Initialize empty collections to prevent null reference exceptions
                _instrumentSubTypes = [];
                _instrumentManifests = [];
            }
        }

        /// <summary>
        /// Runs the interactive menu loop.
        /// </summary>
        private static async Task RunInteractiveMenuAsync()
        {
            bool exit = false;

            while (!exit)
            {
                DisplayMenu();
                var input = Console.ReadLine()?.Trim();

                switch (input)
                {
                    case "1":
                        await HandleCreateTenantsAsync();
                        break;
                    case "2":
                        await HandleCreateControllersAsync();
                        break;
                    case "3":
                        await HandleCreateSensorsAsync();
                        break;
                    case "4":
                        await HandleUpdateInstrumentsAsync();
                        break;
                    case "5":
                        await HandleDeleteInstrumentsAsync();
                        break;
                    case "6":
                        await HandleDeleteTenantsAsync();
                        break;
                    case "7":
                        await HandlePublishInstrumentDataAsync();
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
        }

        /// <summary>
        /// Displays the main menu options.
        /// </summary>
        private static void DisplayMenu()
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
        }

        /// <summary>
        /// Handles tenant creation operation.
        /// </summary>
        private static async Task HandleCreateTenantsAsync()
        {
            var count = await TenantFacade.CreateTenantsAsync(_tenants);
            Console.WriteLine($"Tenant creation completed: {count} out of {_tenants.Count} tenants created successfully.\n");
        }

        /// <summary>
        /// Handles controller creation operation.
        /// </summary>
        private static async Task HandleCreateControllersAsync()
        {
            try
            {
                var controllerEvents = CloudEventBuilder.BuildControllerAssignedEvents(_tenants);
                Console.WriteLine($"Built {controllerEvents.Count} Controller Instrument.Assigned cloud event(s).");
                Logger.LogInfo($"[{_inventoryFileName}] Built {controllerEvents.Count} Controller Instrument.Assigned cloud event(s).");

                var (controllerSuccess, controllerFail) = await _publisher.SendAllAsync(controllerEvents);
                Console.WriteLine($"Controllers completed: {controllerSuccess} succeeded, {controllerFail} failed out of {controllerEvents.Count} total events.\n");
                Logger.LogInfo($"[{_inventoryFileName}] Controllers completed: {controllerSuccess} succeeded, {controllerFail} failed out of {controllerEvents.Count} total events.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating controllers: {ex.Message}\n");
                Logger.LogError($"[{_inventoryFileName}] Error creating controllers.", ex);
            }
        }

        /// <summary>
        /// Handles sensor creation operation.
        /// </summary>
        private static async Task HandleCreateSensorsAsync()
        {
            try
            {
                var sensorEvents = CloudEventBuilder.BuildSensorAssignedEvents(_tenants);
                Console.WriteLine($"Built {sensorEvents.Count} Sensor Instrument.Assigned cloud event(s).");
                Logger.LogInfo($"[{_inventoryFileName}] Built {sensorEvents.Count} Sensor Instrument.Assigned cloud event(s).");

                var (sensorSuccess, sensorFail) = await _publisher.SendAllAsync(sensorEvents);
                Console.WriteLine($"Sensors completed: {sensorSuccess} succeeded, {sensorFail} failed out of {sensorEvents.Count} total events.\n");
                Logger.LogInfo($"[{_inventoryFileName}] Sensors completed: {sensorSuccess} succeeded, {sensorFail} failed out of {sensorEvents.Count} total events.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating sensors: {ex.Message}\n");
                Logger.LogError($"[{_inventoryFileName}] Error creating sensors.", ex);
            }
        }

        /// <summary>
        /// Handles instrument update operation.
        /// </summary>
        private static async Task HandleUpdateInstrumentsAsync()
        {
            try
            {
                var controllerUpdateEvents = CloudEventBuilder.BuildControllerUpdatedEvents(_tenants);
                var sensorUpdateEvents = CloudEventBuilder.BuildSensorUpdatedEvents(_tenants);
                var allUpdateEvents = new List<Azure.Messaging.CloudEvent>();
                allUpdateEvents.AddRange(controllerUpdateEvents);
                allUpdateEvents.AddRange(sensorUpdateEvents);

                Console.WriteLine($"Built {allUpdateEvents.Count} Instrument.Updated cloud event(s) ({controllerUpdateEvents.Count} controllers, {sensorUpdateEvents.Count} sensors).");
                Logger.LogInfo($"[{_inventoryFileName}] Built {allUpdateEvents.Count} Instrument.Updated cloud event(s) ({controllerUpdateEvents.Count} controllers, {sensorUpdateEvents.Count} sensors).");

                var (updateSuccess, updateFail) = await _publisher.SendAllAsync(allUpdateEvents);
                Console.WriteLine($"Update completed: {updateSuccess} succeeded, {updateFail} failed out of {allUpdateEvents.Count} total events.\n");
                Logger.LogInfo($"[{_inventoryFileName}] Update completed: {updateSuccess} succeeded, {updateFail} failed out of {allUpdateEvents.Count} total events.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating instruments: {ex.Message}\n");
                Logger.LogError($"[{_inventoryFileName}] Error updating instruments.", ex);
            }
        }

        /// <summary>
        /// Handles instrument deletion operation.
        /// </summary>
        private static async Task HandleDeleteInstrumentsAsync()
        {
            try
            {
                var sensorUnassignEvents = CloudEventBuilder.BuildSensorUnassignedEvents(_tenants);
                var controllerUnassignEvents = CloudEventBuilder.BuildControllerUnassignedEvents(_tenants);
                var allDeleteEvents = new List<Azure.Messaging.CloudEvent>();
                allDeleteEvents.AddRange(sensorUnassignEvents);
                allDeleteEvents.AddRange(controllerUnassignEvents);

                Console.WriteLine($"Built {allDeleteEvents.Count} Instrument.Unassigned cloud event(s) ({sensorUnassignEvents.Count} sensors, {controllerUnassignEvents.Count} controllers).");
                Logger.LogInfo($"[{_inventoryFileName}] Built {allDeleteEvents.Count} Instrument.Unassigned cloud event(s) ({sensorUnassignEvents.Count} sensors, {controllerUnassignEvents.Count} controllers).");

                var (deleteSuccess, deleteFail) = await _publisher.SendAllAsync(allDeleteEvents);
                Console.WriteLine($"Delete completed: {deleteSuccess} succeeded, {deleteFail} failed out of {allDeleteEvents.Count} total events.\n");
                Logger.LogInfo($"[{_inventoryFileName}] Delete completed: {deleteSuccess} succeeded, {deleteFail} failed out of {allDeleteEvents.Count} total events.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting instruments: {ex.Message}\n");
                Logger.LogError($"[{_inventoryFileName}] Error deleting instruments.", ex);
            }
        }

        /// <summary>
        /// Handles tenant deletion operation.
        /// </summary>
        private static async Task HandleDeleteTenantsAsync()
        {
            var deleteCount = await TenantFacade.DeleteTenantsAsync(_tenants);
            Console.WriteLine($"Tenant deletion completed: {deleteCount} out of {_tenants.Count} tenants deleted successfully.\n");
        }

        /// <summary>
        /// Handles instrument data publishing to Event Hub.
        /// </summary>
        private static async Task HandlePublishInstrumentDataAsync()
        {
            try
            {
                var (fileName, dataType) = GetDataTypeSelection();
                if (fileName == null || dataType == null)
                {
                    Console.WriteLine("Invalid data type selection.\n");
                    return;
                }

                if (!File.Exists(fileName))
                {
                    Console.WriteLine($"File '{fileName}' not found.\n");
                    return;
                }

                var json = await File.ReadAllTextAsync(fileName);
                var updatedDataList = InstrumentDataUpdater.UpdateWithInventory(json, _tenants, dataType);

                if (updatedDataList.Count == 0)
                {
                    Console.WriteLine("No data to publish for the selected type and inventory.\n");
                    return;
                }

                await PublishToEventHubAsync(updatedDataList, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing to Event Hub: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Gets the user's data type selection and returns the corresponding file name and data type.
        /// </summary>
        private static (string? fileName, InstrumentDataType? dataType) GetDataTypeSelection()
        {
            Console.WriteLine("Select data type to publish:");
            Console.WriteLine("  1 - Measurement Data");
            Console.WriteLine("  2 - Diagnostic Data");
            Console.WriteLine("  3 - Status Data");
            Console.WriteLine("  4 - Event Data");
            Console.WriteLine("  5 - Settings Data");
            Console.Write("Your choice: ");

            var dataTypeInput = Console.ReadLine()?.Trim();

            string? fileName = dataTypeInput switch
            {
                "1" => "instrumentmeasurementdata.json",
                "2" => "instrumentdiagnosticdata.json",
                "3" => "instrumentstatusdata.json",
                "4" => "instrumenteventdata.json",
                "5" => "instrumentsettingdata.json",
                _ => null
            };

            InstrumentDataType? dataType = dataTypeInput switch
            {
                "1" => InstrumentDataType.Measurement,
                "2" => InstrumentDataType.Diagnostic,
                "3" => InstrumentDataType.Status,
                "4" => InstrumentDataType.Event,
                "5" => InstrumentDataType.Settings,
                _ => null
            };

            return (fileName, dataType);
        }

        /// <summary>
        /// Publishes the updated data list to Event Hub.
        /// </summary>
        private static async Task PublishToEventHubAsync(List<(string json, string tenantId)> updatedDataList, string fileName)
        {
            var eventType = Path.GetFileNameWithoutExtension(fileName);
            var eventHubConfig = _config.GetSection("EventHub");
            var eventHubConnectionString = eventHubConfig["ConnectionString"];
            var eventHubName = eventHubConfig["Name"];

            await using var eventHubPublisher = new EventHubPublisher(eventHubConnectionString, eventHubName);

            int successCount = 0, failCount = 0;
            foreach (var (updatedJson, tenantId) in updatedDataList)
            {
                try
                {
                    await eventHubPublisher.PublishAsync(updatedJson, eventType, tenantId);
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
    }
}