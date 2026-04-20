using LoadPerformanceTest.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LoadPerformanceTest;

/// <summary>
/// Updates instrument data JSON with fusionId and tenantId for each sensor/controller in tenants.
/// </summary>
public static class InstrumentDataUpdater
{
    /// <summary>
    /// Generates updated instrument data objects for publishing, based on tenants and data type.
    /// </summary>
    /// <param name="dataJson">The JSON string of the instrument data array (template).</param>
    /// <param name="tenants">The tenants model (parsed from inventory file).</param>
    /// <param name="dataType">The type of data ("measurement", "diagnostic", "event", "status", "settings").</param>
    /// <returns>List of updated JSON strings, one per instrument to publish.</returns>
    public static List<string> UpdateWithInventory(string dataJson, IEnumerable<Tenant> tenants, string dataType)
    {
        var templateArray = JArray.Parse(dataJson);
        var result = new List<string>();

        foreach (var tenant in tenants)
        {
            // CONTROLLERS: settings, event
            if (dataType is "settings" or "event")
            {
                foreach (var controller in tenant.Controllers)
                {
                    foreach (var template in templateArray)
                    {
                        var obj = (JObject)template.DeepClone();
                        obj["fusionId"] = controller.FusionId;
                        obj["tenantId"] = tenant.TenantId;
                        obj["instrumentId"] = controller.DeviceId; // Use DeviceId as InstrumentId
                        result.Add(obj.ToString(Formatting.None));
                    }
                }
            }

            // SENSORS: measurement, diagnostic, event, status
            if (dataType is "measurement" or "diagnostic" or "event" or "status")
            {
                foreach (var controller in tenant.Controllers)
                {
                    foreach (var sensor in controller.Sensors)
                    {
                        foreach (var template in templateArray)
                        {
                            var obj = (JObject)template.DeepClone();
                            obj["fusionId"] = sensor.FusionId;
                            obj["tenantId"] = tenant.TenantId;
                            obj["instrumentId"] = sensor.DeviceId; // Use DeviceId as InstrumentId
                            result.Add(obj.ToString(Formatting.None));
                        }
                    }
                }
            }
        }

        return result;
    }
}
