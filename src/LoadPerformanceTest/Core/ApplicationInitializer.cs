using Azure.Identity;
using Azure.Messaging.EventGrid.Namespaces;
using LoadPerformanceTest.Configurations;
using LoadPerformanceTest.Logging;
using LoadPerformanceTest.Parsers;
using LoadPerformanceTest.Publishers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ONE.Models.CSharp.Instrument;

namespace LoadPerformanceTest.Core;

/// <summary>
/// Handles application initialization including configuration, inventory, manifests, and services.
/// </summary>
public class ApplicationInitializer
{
    /// <summary>
    /// Initializes the application and returns the application context.
    /// </summary>
    public async Task<ApplicationContext> InitializeAsync()
    {
        // Initialize configuration
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c =>
            {
                c.AddJsonFile("appsettings.json", optional: false);
                c.AddEnvironmentVariables();
            })
            .Build();

        var config = host.Services.GetRequiredService<IConfiguration>();
        var options = config.GetSection("EventGridSender").Get<EventGridSenderOptions>()!;

        // Initialize file paths
        var inventoryFilePath = config["DeviceInventoryFilePath"];
        var manifestsFilePath = config["ManifestsFilePath"];

        // Parse device inventory
        var tenants = await DeviceInventoryParser.ParseFromPathAsync(inventoryFilePath, true);
        var inventoryFileName = Path.GetFileName(inventoryFilePath);
        Console.WriteLine($"Loaded {tenants.Count} tenant(s) from device inventory.");
        Logger.LogInfo($"Parsed inventory file: {inventoryFileName} — loaded {tenants.Count} tenant(s).");

        var (subTypes, manifests) = await LoadManifestsAsync(manifestsFilePath);

        // Set up EventGrid client
        var credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
        var client = new EventGridSenderClient(new Uri(options.TopicEndpoint), options.TopicName, credential);
        var publisher = new EventGridPublisher(client);

        return new ApplicationContext
        {
            Configuration = config,
            Tenants = tenants,
            InventoryFileName = inventoryFileName,
            Publisher = publisher,
            InstrumentManifests = manifests
        };
    }

    /// <summary>
    /// Loads manifests from the specified file path.
    /// </summary>
    private static async Task<(List<InstrumentTwinSubType> subTypes, List<InstrumentManifest> manifests)> LoadManifestsAsync(string manifestsFilePath)
    {
        try
        {
            var (subTypes, manifests) = await ManifestParser.ParseManifestsCompleteAsync(manifestsFilePath);

            var manifestsFileName = Path.GetFileName(manifestsFilePath);
            Console.WriteLine($"Loaded {subTypes.Count} instrument sub-type(s) from manifests file.");
            Console.WriteLine($"Extracted {manifests.Count} instrument manifest(s) from property bags.\n");

            Logger.LogInfo($"Parsed manifests file: {manifestsFileName} — loaded {subTypes.Count} sub-type(s), extracted {manifests.Count} manifest(s).");

            return (subTypes, manifests);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading manifests: {ex.Message}");
            Logger.LogError("Failed to load manifests file.", ex);

            // Initialize empty collections to prevent null reference exceptions
            return ([], []);
        }
    }
}
