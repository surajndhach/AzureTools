using LoadPerformanceTest.Models;
using Newtonsoft.Json;

namespace LoadPerformanceTest;

public static class DeviceInventoryParser
{
    public static async Task<List<Tenant>> ParseFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Device inventory file not found: {filePath}");

        var json = await File.ReadAllTextAsync(filePath);
        return Parse(json);
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

