using LoadPerformanceTest.Logging;
using LoadPerformanceTest.Services.EventHub;
using LoadPerformanceTest.Utilities;
using Microsoft.Extensions.Configuration;

namespace LoadPerformanceTest.Core;

/// <summary>
/// Handles continuous publishing of instrument data to Event Hub.
/// </summary>
public class InstrumentDataPublisher
{
    private readonly ApplicationContext _context;

    public InstrumentDataPublisher(ApplicationContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Publishes the updated data list to Event Hub.
    /// </summary>
    public async Task PublishToEventHubAsync(List<(string json, string tenantId)> updatedDataList, string fileName)
    {
        var eventType = Path.GetFileNameWithoutExtension(fileName);
        var eventHubConfig = _context.Configuration.GetSection("EventHub");
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

    /// <summary>
    /// Publishes all data types continuously at their configured intervals.
    /// </summary>
    public async Task PublishContinuouslyAsync(List<(string fileName, InstrumentDataType dataType)> selections)
    {
        using var cts = new CancellationTokenSource();

        // Start monitoring for 'Q' key press to stop
        var keyMonitorTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("\nStopping continuous publishing...");
                        Logger.LogInfo("User requested stop for continuous publishing.");
                        cts.Cancel();
                        break;
                    }
                }
                Thread.Sleep(100);
            }
        });

        // Get publishing intervals from configuration (in seconds, convert to milliseconds)
        var publishingConfig = _context.Configuration.GetSection("PublishingIntervals");
        var intervals = new Dictionary<InstrumentDataType, int>
        {
            [InstrumentDataType.Measurement] = publishingConfig.GetValue("MeasurementIntervalSeconds", 30) * 1000,
            [InstrumentDataType.Diagnostic] = publishingConfig.GetValue("DiagnosticIntervalSeconds", 600) * 1000,
            [InstrumentDataType.Status] = publishingConfig.GetValue("StatusIntervalSeconds", 900) * 1000,
            [InstrumentDataType.Event] = publishingConfig.GetValue("EventIntervalSeconds", 480) * 1000,
            [InstrumentDataType.Settings] = publishingConfig.GetValue("SettingsIntervalSeconds", 1200) * 1000
        };

        // Load all JSON files once
        var dataTypeFiles = new Dictionary<InstrumentDataType, string>();
        foreach (var (fileName, dataType) in selections)
        {
            dataTypeFiles[dataType] = await File.ReadAllTextAsync(fileName);
        }

        // Create publishing tasks for each data type
        var publishingTasks = selections.Select(selection =>
            PublishDataTypeContinuouslyAsync(
                selection.dataType,
                dataTypeFiles[selection.dataType],
                selection.fileName,
                intervals[selection.dataType],
                cts.Token))
            .ToArray();

        try
        {
            // Wait for either all tasks to complete or cancellation
            await Task.WhenAny(Task.WhenAll(publishingTasks), keyMonitorTask);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        finally
        {
            cts.Cancel(); // Ensure all tasks are cancelled
            try
            {
                await Task.WhenAll(publishingTasks.Where(t => !t.IsCompleted));
            }
            catch (OperationCanceledException)
            {
                // Expected during cleanup
            }
        }

        Console.WriteLine("Continuous publishing stopped.\n");
        Logger.LogInfo("Continuous publishing session ended.");
    }

    /// <summary>
    /// Publishes a specific data type continuously at the configured interval.
    /// </summary>
    private async Task PublishDataTypeContinuouslyAsync(
        InstrumentDataType dataType,
        string jsonTemplate,
        string fileName,
        int intervalMs,
        CancellationToken cancellationToken)
    {
        var eventHubConfig = _context.Configuration.GetSection("EventHub");
        var eventHubConnectionString = eventHubConfig["ConnectionString"];
        var eventHubName = eventHubConfig["Name"];

        await using var eventHubPublisher = new EventHubPublisher(eventHubConnectionString, eventHubName);

        var publishCount = 0;
        var startTime = DateTime.UtcNow;
        var intervalSeconds = intervalMs / 1000.0;

        Logger.LogInfo($"Started continuous publishing for {dataType} with {intervalSeconds} second(s) interval.");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var updatedDataList = InstrumentDataBuilder.GenerateInstrumentDataFromInventory(jsonTemplate, _context.Tenants, dataType, _context.InstrumentManifests);

                if (updatedDataList.Count > 0)
                {
                    int successCount = 0, failCount = 0;

                    foreach (var (updatedJson, tenantId) in updatedDataList)
                    {
                        try
                        {
                            await eventHubPublisher.PublishAsync(updatedJson, Path.GetFileNameWithoutExtension(fileName), tenantId, cancellationToken);
                            successCount++;
                        }
                        catch (Exception ex) when (!(ex is OperationCanceledException))
                        {
                            failCount++;
                            Logger.LogError($"Failed to publish {dataType} for instrument: {ex.Message}", ex);
                        }
                    }

                    publishCount++;
                    var elapsed = DateTime.UtcNow - startTime;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {dataType}: Published batch #{publishCount} - {successCount} succeeded, {failCount} failed (Running: {elapsed:hh\\:mm\\:ss})");
                }

                await Task.Delay(intervalMs, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }

        Logger.LogInfo($"Stopped continuous publishing for {dataType}. Total batches published: {publishCount}");
    }
}
