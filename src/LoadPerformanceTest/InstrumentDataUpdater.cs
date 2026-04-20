using LoadPerformanceTest.Models;
using Newtonsoft.Json.Linq;
using Google.Protobuf;
using ONE.Models.CSharp.External;

namespace LoadPerformanceTest;

/// <summary>
/// Updates instrument data JSON with fusionId and tenantId for each sensor/controller in tenants.
/// Follows the same parsing logic as EventHubPublish project using Protocol Buffers.
/// </summary>
public static class InstrumentDataUpdater
{
    /// <summary>
    /// Generates updated instrument data objects for publishing, based on tenants and data type.
    /// Uses Protocol Buffers parsing similar to EventHubPublish.ProcessMeasurement approach.
    /// </summary>
    /// <param name="dataJson">The JSON string of the instrument data template.</param>
    /// <param name="tenants">The tenants model (parsed from inventory file).</param>
    /// <param name="dataType">The type of data ("measurement", "diagnostic", "event", "status", "settings").</param>
    /// <returns>List of updated JSON strings, one per instrument to publish.</returns>
    public static List<string> UpdateWithInventory(string dataJson, IEnumerable<Tenant> tenants, string dataType)
    {
        var result = new List<string>();
        var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

        foreach (var tenant in tenants)
        {
            // CONTROLLERS: settings, event
            if (dataType is "settings" or "event")
            {
                foreach (var controller in tenant.Controllers)
                {
                    // Parse JSON into strongly-typed protobuf object
                    var instrumentData = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentData>(dataJson);

                    // Update tenant and fusion IDs
                    instrumentData.TenantId = tenant.TenantId;
                    instrumentData.FusionId = controller.FusionId;

                    // Update timestamps and properties based on data type
                    UpdateDataTypeSpecificProperties(instrumentData, dataType);

                    // Convert back to JSON
                    var jsonObj = JObject.Parse(instrumentData.ToString() ?? throw new InvalidOperationException());
                    result.Add(jsonObj.ToString());
                }
            }

            // SENSORS: measurement, diagnostic, event, status
            if (dataType is "measurement" or "diagnostic" or "event" or "status")
            {
                foreach (var controller in tenant.Controllers)
                {
                    foreach (var sensor in controller.Sensors)
                    {
                        // Parse JSON into strongly-typed protobuf object
                        var instrumentData = parser.Parse<ONE.Models.CSharp.Instrument.InstrumentData>(dataJson);

                        // Update tenant and fusion IDs
                        instrumentData.TenantId = tenant.TenantId;
                        instrumentData.FusionId = sensor.FusionId;

                        // Update timestamps and properties based on data type
                        UpdateDataTypeSpecificProperties(instrumentData, dataType);

                        // Convert back to JSON
                        var jsonObj = JObject.Parse(instrumentData.ToString() ?? throw new InvalidOperationException());
                        result.Add(jsonObj.ToString());
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Updates data type specific properties following EventHubPublish patterns.
    /// </summary>
    private static void UpdateDataTypeSpecificProperties(ONE.Models.CSharp.Instrument.InstrumentData instrumentData, string dataType)
    {
        var timestamp = DateTime.UtcNow.ToClarosDateTime();

        switch (dataType)
        {
            case "measurement":
                // Update measurement timestamps - follows ProcessMeasurement logic
                foreach (var instrumentMeasurementData in instrumentData.InstrumentMeasurementDatas.Items)
                {
                    instrumentMeasurementData.Measurement.TimestampUtc = timestamp;
                }
                break;

            case "status":
                // Update status timestamps - follows ProcessStatus logic
                foreach (var instrumentStatusData in instrumentData.InstrumentStatuses.Items)
                {
                    instrumentStatusData.StatusDateTimeUtc = timestamp;
                }
                break;

            case "event":
                // Update event timestamps - follows ProcessEvents logic
                foreach (var instrumentEventData in instrumentData.InstrumentEventDatas.Items)
                {
                    instrumentEventData.EventDateTimeUtc = timestamp;
                }
                break;

            case "diagnostic":
                // Update diagnostic timestamps - follows ProcessDiagnostics logic
                foreach (var instrumentDiagnosticData in instrumentData.InstrumentDiagnostics.Items)
                {
                    foreach (var instrumentDiagnosticDatum in instrumentDiagnosticData.Values)
                    {
                        instrumentDiagnosticDatum.TimestampUtc = timestamp;
                    }
                }
                break;

            case "settings":
                // Update settings timestamp and properties - follows ProcessSettings logic
                instrumentData.InstrumentSettings.SettingsDateTimeUtc = timestamp;
                instrumentData.InstrumentSettings.Settings["name"] = $"Instrument-{DateTime.UtcNow.Ticks}";
                break;
        }
    }
}

/// <summary>
/// Helper extensions following EventHubPublish pattern.
/// </summary>
internal static class HelperExtensions
{
    public static ClarosJsonTicksDateTime ToClarosDateTime(this DateTime dateTime)
    {
        return new ClarosJsonTicksDateTime
        {
            JsonDateTime = dateTime.ToUtc().ToString("O")
        };
    }

    public static DateTime ToUtc(this DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        return dateTime.ToUniversalTime();
    }
}
