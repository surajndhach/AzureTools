using LoadPerformanceTest.Utilities;
using Newtonsoft.Json;
using ONE.Models.CSharp.Instrument;

namespace LoadPerformanceTest.Parsers;

/// <summary>
/// Handles parsing of manifests JSON file into strongly-typed protobuf objects.
/// Follows DRY and SOLID principles with clear separation of concerns.
/// </summary>
public static class ManifestParser
{
    /// <summary>
    /// Container for the manifests JSON structure.
    /// </summary>
    private class ManifestsContainer
    {
        [JsonProperty("items")]
        public List<InstrumentTwinSubType> Items { get; set; } = [];
    }

    /// <summary>
    /// Parses manifests.json file into InstrumentTwinSubType array.
    /// </summary>
    /// <param name="filePath">Path to the manifests.json file</param>
    /// <returns>List of InstrumentTwinSubType objects</returns>
    public static async Task<List<InstrumentTwinSubType>> ParseFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Manifests file not found: {filePath}");

        var json = await File.ReadAllTextAsync(filePath);
        return ParseManifests(json);
    }

    /// <summary>
    /// Parses the manifests JSON string into strongly-typed objects.
    /// </summary>
    /// <param name="json">JSON string containing the manifests</param>
    /// <returns>List of InstrumentTwinSubType objects</returns>
    public static List<InstrumentTwinSubType> ParseManifests(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        // Parse using Newtonsoft.Json to handle the outer structure
        var container = JsonConvert.DeserializeObject<ManifestsContainer>(json)
            ?? throw new InvalidOperationException("Failed to deserialize manifests JSON.");

        return container.Items.Count > 0
            ? container.Items
            : throw new InvalidOperationException("No instrument sub-types found in the manifests JSON.");
    }

    /// <summary>
    /// Extracts InstrumentManifest objects from InstrumentTwinSubType property bags.
    /// Uses Protocol Buffers JsonParser for strongly-typed parsing.
    /// </summary>
    /// <param name="subTypes">List of InstrumentTwinSubType objects</param>
    /// <returns>List of InstrumentManifest objects parsed from property bags</returns>
    public static List<InstrumentManifest> ExtractInstrumentManifests(IEnumerable<InstrumentTwinSubType> subTypes)
    {
        var manifests = new List<InstrumentManifest>();

        foreach (var subType in subTypes)
        {
            if (string.IsNullOrWhiteSpace(subType.PropertyBag))
            {
                Console.WriteLine($"Warning: PropertyBag is empty for InstrumentTwinSubType ID: {subType.Id}");
                continue;
            }

            try
            {
                // Parse the PropertyBag JSON into InstrumentManifest using Protocol Buffers
                var manifest = SerializationHelper.Deserialize<InstrumentManifest>(subType.PropertyBag);

                manifests.Add(manifest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing PropertyBag for InstrumentTwinSubType ID {subType.Id}: {ex.Message}");
                Logger.LogError($"Failed to parse PropertyBag for InstrumentTwinSubType ID: {subType.Id}", ex);
            }
        }

        return manifests;
    }

    /// <summary>
    /// Complete parsing workflow: parses manifests file and extracts InstrumentManifest objects.
    /// </summary>
    /// <param name="filePath">Path to the manifests.json file</param>
    /// <returns>Tuple containing both InstrumentTwinSubType and InstrumentManifest lists</returns>
    public static async Task<(List<InstrumentTwinSubType> SubTypes, List<InstrumentManifest> Manifests)> ParseManifestsCompleteAsync(string filePath)
    {
        var subTypes = await ParseFromFileAsync(filePath);
        var manifests = ExtractInstrumentManifests(subTypes);
        
        return (subTypes, manifests);
    }
}
