using LoadPerformanceTest.Models;
using Newtonsoft.Json.Linq;
using ONE.Models.CSharp.External;
using ONE.Instrument.Twin.Core.Helpers;
using ONE.Models.CSharp.Instrument;
using ONE.Models.CSharp;

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
    /// <param name="dataType">The type of data to process.</param>
    /// <returns>List of updated JSON strings, one per instrument to publish.</returns>
    public static List<string> UpdateWithInventory(
         string dataJson,
         IEnumerable<Models.Tenant> tenants,
         InstrumentDataType? dataType)
    {
        var result = new List<string>();

        foreach (var tenant in tenants)
        {
            // CONTROLLERS: settings, event
            if (dataType is InstrumentDataType.Settings or InstrumentDataType.Event)
            {
                foreach (var controller in tenant.Controllers)
                {
                    var instrumentData = SerializationHelper.Deserialize<InstrumentData>(dataJson);
                    instrumentData.TenantId = tenant.TenantId;
                    instrumentData.FusionId = controller.FusionId;
                    UpdateDataTypeSpecificProperties(instrumentData, dataType);
                    var jsonObj = JObject.Parse(instrumentData.ToString() ?? throw new InvalidOperationException());
                    result.Add(jsonObj.ToString());
                }
            }

            // SENSORS: measurement, diagnostic, event, status
            if (dataType is InstrumentDataType.Measurement or InstrumentDataType.Diagnostic or
                InstrumentDataType.Event or InstrumentDataType.Status)
            {
                foreach (var controller in tenant.Controllers)
                {
                    foreach (var sensor in controller.Sensors)
                    {
                        var instrumentData = SerializationHelper.Deserialize<InstrumentData>(dataJson);
                        instrumentData.TenantId = tenant.TenantId;
                        instrumentData.FusionId = sensor.FusionId;
                        instrumentData.InstrumentMeasurementDatas = new InstrumentMeasurementDatas();


                        // --- Begin Manifest Parameter/Unit Injection ---
                        if (dataType == InstrumentDataType.Measurement)
                        {
                            // Find the manifest for this sensor
                            var manifest = Program._instrumentManifests
                                .FirstOrDefault(m => m.InstrumentType?.Identifier?.Id == sensor.DeviceTypeId);

                            if (manifest != null && manifest.InstrumentMeasurementCapability?.Definitions?.Items != null)
                            {

                                foreach (var def in manifest.InstrumentMeasurementCapability.Definitions.Items)
                                {
                                    var measurement = new InstrumentMeasurement
                                    {
                                        ParameterId = def.ParameterId,
                                        Value = 2, // Set a dummy value or use a default/test value
                                        DecimalPrecision = def.Attributes?.DisplayDecimalPoints ?? 2,
                                        UnitId = "30d9f576-a6d2-4439-9907-7e147af64508",
                                        TimestampUtc = DateTime.UtcNow.ToClarosDateTime(),
                                    };
                                    instrumentData.InstrumentMeasurementDatas.Items.Add(
                                        new InstrumentMeasurementData
                                        {
                                            Measurement = measurement,
                                            ChannelNumber = 0,
                                        }
                                    );
                                }
                            }
                        }
                        // --- End Manifest Parameter/Unit Injection ---

                        UpdateDataTypeSpecificProperties(instrumentData, dataType);
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
    private static void UpdateDataTypeSpecificProperties(ONE.Models.CSharp.Instrument.InstrumentData instrumentData, InstrumentDataType? dataType)
    {
        var timestamp = DateTime.UtcNow.ToClarosDateTime();

        switch (dataType)
        {
            case InstrumentDataType.Measurement:
                // Update measurement timestamps - follows ProcessMeasurement logic
                foreach (var instrumentMeasurementData in instrumentData.InstrumentMeasurementDatas.Items)
                {
                    instrumentMeasurementData.Measurement.TimestampUtc = timestamp;
                }
                break;

            case InstrumentDataType.Status:
                // Update status timestamps - follows ProcessStatus logic
                foreach (var instrumentStatusData in instrumentData.InstrumentStatuses.Items)
                {
                    instrumentStatusData.StatusDateTimeUtc = timestamp;
                }
                break;

            case InstrumentDataType.Event:
                // Update event timestamps - follows ProcessEvents logic
                foreach (var instrumentEventData in instrumentData.InstrumentEventDatas.Items)
                {
                    instrumentEventData.EventDateTimeUtc = timestamp;
                }
                break;

            case InstrumentDataType.Diagnostic:
                // Update diagnostic timestamps - follows ProcessDiagnostics logic
                foreach (var instrumentDiagnosticData in instrumentData.InstrumentDiagnostics.Items)
                {
                    foreach (var instrumentDiagnosticDatum in instrumentDiagnosticData.Values)
                    {
                        instrumentDiagnosticDatum.TimestampUtc = timestamp;
                    }
                }
                break;

            case InstrumentDataType.Settings:
                // Update settings timestamp and properties - follows ProcessSettings logic
                instrumentData.InstrumentSettings.SettingsDateTimeUtc = timestamp;
                instrumentData.InstrumentSettings.Settings["name"] = $"Instrument-{DateTime.UtcNow.Ticks}";
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
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
