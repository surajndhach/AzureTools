using LoadPerformanceTest.Logging;
using LoadPerformanceTest.Core.Publishing;
using LoadPerformanceTest.Utilities;
using LoadPerformanceTest.Services;

namespace LoadPerformanceTest.Core;

/// <summary>
/// Orchestrates all business operations including tenant, controller, sensor, and instrument operations.
/// </summary>
public class OperationOrchestrator
{
    private readonly ApplicationContext _context;
    private readonly InstrumentDataPublisher _publisher;

    public OperationOrchestrator(ApplicationContext context)
    {
        _context = context;
        _publisher = new InstrumentDataPublisher(context);
    }

    /// <summary>
    /// Handles tenant creation operation.
    /// </summary>
    public async Task CreateTenantsAsync()
    {
        var count = await TenantService.CreateTenantsAsync(_context.Tenants);
        Console.WriteLine($"Tenant creation completed: {count} out of {_context.Tenants.Count} tenants created successfully.\n");
    }

    /// <summary>
    /// Handles controller creation operation.
    /// </summary>
    public async Task CreateControllersAsync()
    {
        try
        {
            var controllerEvents = CloudEventBuilder.BuildControllerAssignedEvents(_context.Tenants);
            Console.WriteLine($"Built {controllerEvents.Count} Controller Instrument.Assigned cloud event(s).");
            Logger.LogInfo($"[{_context.InventoryFileName}] Built {controllerEvents.Count} Controller Instrument.Assigned cloud event(s).");

            var (controllerSuccess, controllerFail) = await _context.Publisher.SendAllAsync(controllerEvents);
            Console.WriteLine($"Controllers completed: {controllerSuccess} succeeded, {controllerFail} failed out of {controllerEvents.Count} total events.\n");
            Logger.LogInfo($"[{_context.InventoryFileName}] Controllers completed: {controllerSuccess} succeeded, {controllerFail} failed out of {controllerEvents.Count} total events.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating controllers: {ex.Message}\n");
            Logger.LogError($"[{_context.InventoryFileName}] Error creating controllers.", ex);
        }
    }

    /// <summary>
    /// Handles sensor creation operation.
    /// </summary>
    public async Task CreateSensorsAsync()
    {
        try
        {
            var sensorEvents = CloudEventBuilder.BuildSensorAssignedEvents(_context.Tenants);
            Console.WriteLine($"Built {sensorEvents.Count} Sensor Instrument.Assigned cloud event(s).");
            Logger.LogInfo($"[{_context.InventoryFileName}] Built {sensorEvents.Count} Sensor Instrument.Assigned cloud event(s).");

            var (sensorSuccess, sensorFail) = await _context.Publisher.SendAllAsync(sensorEvents);
            Console.WriteLine($"Sensors completed: {sensorSuccess} succeeded, {sensorFail} failed out of {sensorEvents.Count} total events.\n");
            Logger.LogInfo($"[{_context.InventoryFileName}] Sensors completed: {sensorSuccess} succeeded, {sensorFail} failed out of {sensorEvents.Count} total events.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating sensors: {ex.Message}\n");
            Logger.LogError($"[{_context.InventoryFileName}] Error creating sensors.", ex);
        }
    }

    /// <summary>
    /// Handles instrument update operation.
    /// </summary>
    public async Task UpdateInstrumentsAsync()
    {
        try
        {
            var controllerUpdateEvents = CloudEventBuilder.BuildControllerUpdatedEvents(_context.Tenants);
            var sensorUpdateEvents = CloudEventBuilder.BuildSensorUpdatedEvents(_context.Tenants);
            var allUpdateEvents = new List<Azure.Messaging.CloudEvent>();
            allUpdateEvents.AddRange(controllerUpdateEvents);
            allUpdateEvents.AddRange(sensorUpdateEvents);

            Console.WriteLine($"Built {allUpdateEvents.Count} Instrument.Updated cloud event(s) ({controllerUpdateEvents.Count} controllers, {sensorUpdateEvents.Count} sensors).");
            Logger.LogInfo($"[{_context.InventoryFileName}] Built {allUpdateEvents.Count} Instrument.Updated cloud event(s) ({controllerUpdateEvents.Count} controllers, {sensorUpdateEvents.Count} sensors).");

            var (updateSuccess, updateFail) = await _context.Publisher.SendAllAsync(allUpdateEvents);
            Console.WriteLine($"Update completed: {updateSuccess} succeeded, {updateFail} failed out of {allUpdateEvents.Count} total events.\n");
            Logger.LogInfo($"[{_context.InventoryFileName}] Update completed: {updateSuccess} succeeded, {updateFail} failed out of {allUpdateEvents.Count} total events.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating instruments: {ex.Message}\n");
            Logger.LogError($"[{_context.InventoryFileName}] Error updating instruments.", ex);
        }
    }

    /// <summary>
    /// Handles instrument deletion operation.
    /// </summary>
    public async Task DeleteInstrumentsAsync()
    {
        try
        {
            var sensorUnassignEvents = CloudEventBuilder.BuildSensorUnassignedEvents(_context.Tenants);
            var controllerUnassignEvents = CloudEventBuilder.BuildControllerUnassignedEvents(_context.Tenants);
            var allDeleteEvents = new List<Azure.Messaging.CloudEvent>();
            allDeleteEvents.AddRange(sensorUnassignEvents);
            allDeleteEvents.AddRange(controllerUnassignEvents);

            Console.WriteLine($"Built {allDeleteEvents.Count} Instrument.Unassigned cloud event(s) ({sensorUnassignEvents.Count} sensors, {controllerUnassignEvents.Count} controllers).");
            Logger.LogInfo($"[{_context.InventoryFileName}] Built {allDeleteEvents.Count} Instrument.Unassigned cloud event(s) ({sensorUnassignEvents.Count} sensors, {controllerUnassignEvents.Count} controllers).");

            var (deleteSuccess, deleteFail) = await _context.Publisher.SendAllAsync(allDeleteEvents);
            Console.WriteLine($"Delete completed: {deleteSuccess} succeeded, {deleteFail} failed out of {allDeleteEvents.Count} total events.\n");
            Logger.LogInfo($"[{_context.InventoryFileName}] Delete completed: {deleteSuccess} succeeded, {deleteFail} failed out of {allDeleteEvents.Count} total events.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting instruments: {ex.Message}\n");
            Logger.LogError($"[{_context.InventoryFileName}] Error deleting instruments.", ex);
        }
    }

    /// <summary>
    /// Handles tenant deletion operation.
    /// </summary>
    public async Task DeleteTenantsAsync()
    {
        var deleteCount = await TenantService.DeleteTenantsAsync(_context.Tenants);
        Console.WriteLine($"Tenant deletion completed: {deleteCount} out of {_context.Tenants.Count} tenants deleted successfully.\n");
    }

    /// <summary>
    /// Handles instrument data publishing to Event Hub.
    /// </summary>
    public async Task PublishInstrumentDataAsync()
    {
        try
        {
            var (selections, isContinuous) = _publisher.GetDataTypeSelection();
            if (selections == null)
            {
                Console.WriteLine("Invalid data type selection.\n");
                Logger.LogWarning("User made an invalid data type selection for publishing instrument data.");
                return;
            }

            // Validate that all files exist
            var missingFiles = selections.Where(s => !File.Exists(s.fileName)).ToList();
            if (missingFiles.Any())
            {
                Console.WriteLine($"The following files were not found:");
                foreach (var (fileName, _) in missingFiles)
                {
                    Console.WriteLine($"  - {fileName}");
                }
                Logger.LogError($"Missing data files: {string.Join(", ", missingFiles.Select(f => f.fileName))}");
                return;
            }

            if (isContinuous)
            {
                Console.WriteLine("Starting continuous publishing. Press 'Q' to stop...\n");
                Logger.LogInfo("Starting continuous publishing of all data types.");
                await _publisher.PublishContinuouslyAsync(selections);
            }
            else
            {
                // Single publish for the selected type
                var (fileName, dataType) = selections.First();
                var json = await File.ReadAllTextAsync(fileName);
                var updatedDataList = InstrumentDataUpdater.UpdateWithInventory(json, _context.Tenants, dataType, _context.InstrumentManifests);

                if (updatedDataList.Count == 0)
                {
                    Console.WriteLine("No data to publish for the selected type and inventory.\n");
                    return;
                }

                await _publisher.PublishToEventHubAsync(updatedDataList, fileName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error publishing to Event Hub: {ex.Message}\n");
            Logger.LogError("Error in PublishInstrumentDataAsync", ex);
        }
    }
}
