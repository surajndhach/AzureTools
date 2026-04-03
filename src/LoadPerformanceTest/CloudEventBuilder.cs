using Claros.Common.Core;
using Claros.Instrument.Core;
using Claros.IoT.Registry;
using LoadPerformanceTest.Models;
using Google.Protobuf;
using CloudEvent = Azure.Messaging.CloudEvent;

namespace LoadPerformanceTest;

public static class CloudEventBuilder
{
    private const string EventSource = "Claros.IoT.Registry";
    private const string AssignedEventType = "Instrument.Assigned";
    private const string ControllerGroupGuid = "12ed4794-fda9-4187-af34-6da2774b4d28";
    private const string DefaultManifestVersion = "3.90.0.0";

    public static List<CloudEvent> BuildInstrumentAssignedEvents(List<Tenant> tenants)
    {
        var events = new List<CloudEvent>();

        foreach (var tenant in tenants)
        {
            foreach (var controller in tenant.Controllers)
            {
                events.Add(BuildControllerAssignedEvent(tenant, controller));

                foreach (var sensor in controller.Sensors)
                {
                    events.Add(BuildSensorAssignedEvent(tenant, controller, sensor));
                }
            }
        }

        return events;
    }

    public static List<CloudEvent> BuildControllerAssignedEvents(List<Tenant> tenants)
    {
        var events = new List<CloudEvent>();

        foreach (var tenant in tenants)
        {
            foreach (var controller in tenant.Controllers)
            {
                events.Add(BuildControllerAssignedEvent(tenant, controller));
            }
        }

        return events;
    }

    public static List<CloudEvent> BuildSensorAssignedEvents(List<Tenant> tenants)
    {
        var events = new List<CloudEvent>();

        foreach (var tenant in tenants)
        {
            foreach (var controller in tenant.Controllers)
            {
                foreach (var sensor in controller.Sensors)
                {
                    events.Add(BuildSensorAssignedEvent(tenant, controller, sensor));
                }
            }
        }

        return events;
    }

    private static CloudEvent BuildControllerAssignedEvent(Tenant tenant, Controller controller)
    {
        var instrument = new Instrument
        {
            InstrumentReference = BuildInstrumentReference(
                controller.FusionId, controller.DeviceId,
                controller.DeviceTypeId, ResolveGroupId(controller.DeviceGroupId)),
            TenantId = tenant.TenantId,
            ConnectionStatus = EnumConnectionStatus.ConnectionStatusConnected,
            RegistryStatus = EnumInstrumentRegistryStatus.InstrumentRegistryStatusAssigned,
            ConnectionStatusReason = EnumConnectionStatusReason.ConnectionStatusReasonHeartbeat,
            ConnectionStatusChangedOn = CreateTimestamp(),
            RecordAuditInfo = CreateAuditInfo()
        };

        return CreateCloudEvent(instrument, tenant.TenantId, controller.DeviceId);
    }

    private static CloudEvent BuildSensorAssignedEvent(Tenant tenant, Controller controller, Sensor sensor)
    {
        var instrument = new Instrument
        {
            InstrumentReference = BuildInstrumentReference(
                sensor.FusionId, sensor.DeviceId,
                sensor.DeviceTypeId, sensor.DeviceGroupId),
            TenantId = tenant.TenantId,
            EdgeInstrumentReference = BuildInstrumentReference(
                controller.FusionId, controller.DeviceId,
                controller.DeviceTypeId, ResolveGroupId(controller.DeviceGroupId)),
            ConnectionStatus = EnumConnectionStatus.ConnectionStatusConnected,
            RegistryStatus = EnumInstrumentRegistryStatus.InstrumentRegistryStatusAssigned,
            ConnectionStatusReason = EnumConnectionStatusReason.ConnectionStatusReasonHeartbeat,
            ConnectionStatusChangedOn = CreateTimestamp(),
            RecordAuditInfo = CreateAuditInfo()
        };

        return CreateCloudEvent(instrument, tenant.TenantId, sensor.DeviceId);
    }

    private static InstrumentReference BuildInstrumentReference(
        string fusionId, string deviceId, string typeId, string groupId) => new()
        {
            InstrumentIdentifier = new InstrumentIdentifier { FusionId = fusionId, Guid = deviceId },
            SerialNumber = ExtractSerialNumber(fusionId),
            InstrumentTypeGuid = typeId,
            InstrumentGroupGuid = groupId,
            InstrumentManifestVersionString = DefaultManifestVersion
        };

    private static CloudEvent CreateCloudEvent(Instrument instrument, string tenantId, string instrumentId)
    {
        var json = JsonFormatter.Default.Format(instrument);

        return new CloudEvent(
            source: EventSource,
            type: AssignedEventType,
            data: BinaryData.FromString(json),
            dataContentType: "application/json")
        {
            Id = Guid.NewGuid().ToString(),
            Subject = $"Instrument/Assigned/tenant/{tenantId}/instrument/{instrumentId}",
            Time = DateTimeOffset.UtcNow
        };
    }

    private static RecordAuditInfo CreateAuditInfo()
    {
        var now = CreateTimestamp();
        var systemId = Guid.NewGuid().ToString();

        return new RecordAuditInfo
        {
            CreatedById = systemId,
            CreatedOn = now,
            ModifiedById = systemId,
            ModifiedOn = now
        };
    }

    private static ClarosDateTime CreateTimestamp() => new() { Ticks = (ulong?)DateTime.UtcNow.Ticks };

    private static string ResolveGroupId(string groupId) =>
        string.IsNullOrEmpty(groupId) ? ControllerGroupGuid : groupId;

    private static string ExtractSerialNumber(string fusionId)
    {
        var lastUnderscore = fusionId.LastIndexOf('_');
        return lastUnderscore >= 0 ? fusionId[(lastUnderscore + 1)..] : fusionId;
    }
}
