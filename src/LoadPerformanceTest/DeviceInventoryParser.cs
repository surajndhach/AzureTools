using EventGridSender.Models;
using Newtonsoft.Json;

namespace EventGridSender
{
    public static class DeviceInventoryParser
    {
        public static async Task<List<Tenant>> ParseFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Device inventory file not found: {filePath}");
            }

            var json = await File.ReadAllTextAsync(filePath);
            return Parse(json);
        }

        public static List<Tenant> Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON content cannot be null or empty.", nameof(json));
            }

            var tenants = JsonConvert.DeserializeObject<List<Tenant>>(json);

            if (tenants == null || tenants.Count == 0)
            {
                throw new InvalidOperationException("No tenants found in the device inventory JSON.");
            }

            return tenants;
        }
    }
}
