using LoadPerformanceTest.Logging;
using LoadPerformanceTest.Models;
using Newtonsoft.Json;

namespace LoadPerformanceTest.Parsers;

public static class DeviceInventoryParser
{
    /// <summary>
    /// Parses device inventory from a single JSON file.
    /// </summary>
    /// <param name="filePath">Path to the JSON file</param>
    /// <returns>List of tenants from the file</returns>
    public static async Task<List<Tenant>> ParseFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Device inventory file not found: {filePath}");

        var json = await File.ReadAllTextAsync(filePath);
        return Parse(json);
    }

    /// <summary>
    /// Parses device inventory from all JSON files in the specified path.
    /// If path is a file, parses that file. If path is a directory, parses all JSON files in the directory.
    /// </summary>
    /// <param name="path">File or directory path</param>
    /// <param name="searchAllJsonFiles">If true and path is a directory, searches for all JSON files</param>
    /// <returns>List of tenants from the file(s)</returns>
    public static async Task<List<Tenant>> ParseFromPathAsync(string path, bool searchAllJsonFiles = false)
    {
        // Check if it's a file
        if (File.Exists(path))
        {
            return await ParseFromFileAsync(path);
        }

        // Check if it's a directory
        if (Directory.Exists(path))
        {
            if (!searchAllJsonFiles)
            {
                throw new ArgumentException($"Path is a directory but searchAllJsonFiles is false: {path}");
            }

            return await ParseFromDirectoryAsync(path);
        }

        throw new FileNotFoundException($"Path not found: {path}");
    }

    /// <summary>
    /// Parses all JSON files in the specified directory path into tenant lists and combines them.
    /// </summary>
    /// <param name="directoryPath">Directory path to search for JSON files</param>
    /// <returns>Combined list of tenants from all JSON files</returns>
    private static async Task<List<Tenant>> ParseFromDirectoryAsync(string directoryPath)
    {
        var jsonFiles = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly)
            .Where(file => !Path.GetFileName(file).Equals("appsettings.json", StringComparison.OrdinalIgnoreCase))
            .Where(file => !Path.GetFileName(file).Equals("manifests.json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (jsonFiles.Length == 0)
            throw new InvalidOperationException($"No device inventory JSON files found in directory: {directoryPath}");

        var allTenants = new List<Tenant>();
        var processedFiles = new List<string>();
        var errorFiles = new List<(string fileName, Exception error)>();

        foreach (var filePath in jsonFiles)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                Console.WriteLine($"Processing JSON file: {fileName}");

                var json = await File.ReadAllTextAsync(filePath);

                // Try to parse as tenant array - skip if it fails (might be other JSON structure)
                if (IsDeviceInventoryJson(json))
                {
                    var tenants = Parse(json);
                    allTenants.AddRange(tenants);
                    processedFiles.Add(fileName);

                    Logger.LogInfo($"Successfully parsed {tenants.Count} tenant(s) from {fileName}");
                }
                else
                {
                    Console.WriteLine($"Skipping {fileName} - not a device inventory JSON format");
                }
            }
            catch (Exception ex)
            {
                var fileName = Path.GetFileName(filePath);
                errorFiles.Add((fileName, ex));
                Logger.LogWarning($"Skipped JSON file due to parsing error: {fileName}", ex);
            }
        }

        if (allTenants.Count == 0)
            throw new InvalidOperationException($"No valid device inventory data found in any JSON files in directory: {directoryPath}");

        // Log summary
        Console.WriteLine($"Processed {processedFiles.Count} device inventory JSON files successfully, {errorFiles.Count} skipped.");
        Logger.LogInfo($"Combined parsing complete: {allTenants.Count} total tenant(s) from {processedFiles.Count} files");

        return allTenants;
    }

    /// <summary>
    /// Checks if the JSON content appears to be a device inventory format.
    /// </summary>
    /// <param name="json">JSON content to check</param>
    /// <returns>True if it looks like device inventory JSON</returns>
    private static bool IsDeviceInventoryJson(string json)
    {
        try
        {
            // Try to deserialize as tenant array and check if it has expected structure
            var testParse = JsonConvert.DeserializeObject<List<Tenant>>(json);
            return testParse != null && testParse.Count > 0 &&
                   testParse.All(t => !string.IsNullOrEmpty(t.TenantId));
        }
        catch
        {
            return false;
        }
    }


    public static List<Tenant> Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var tenants = JsonConvert.DeserializeObject<List<Tenant>>(json)
            ?? throw new InvalidOperationException("Failed to deserialize device inventory JSON.");

        return tenants.Count > 0
            ? tenants
            : throw new InvalidOperationException("No tenants found in the device inventory JSON.");
    }
}

