using Newtonsoft.Json;

namespace EventGridSender.Models
{
    public class Tenant
    {
        [JsonProperty(nameof(TenantId))]
        public string TenantId { get; set; } = string.Empty;

        [JsonProperty(nameof(TenantName))]
        public string TenantName { get; set; } = string.Empty;

        [JsonProperty(nameof(Controllers))]
        public List<Controller> Controllers { get; set; } = new();
    }

    public class Controller
    {
        [JsonProperty(nameof(DeviceName))]
        public string DeviceName { get; set; } = string.Empty;

        [JsonProperty(nameof(FusionId))]
        public string FusionId { get; set; } = string.Empty;

        [JsonProperty(nameof(DeviceId))]
        public string DeviceId { get; set; } = string.Empty;

        [JsonProperty(nameof(DeviceTypeId))]
        public string DeviceTypeId { get; set; } = string.Empty;

        [JsonProperty(nameof(DeviceGroupId))]
        public string DeviceGroupId { get; set; } = string.Empty;

        [JsonProperty(nameof(Sensors))]
        public List<Sensor> Sensors { get; set; } = new();
    }

    public class Sensor
    {
        [JsonProperty(nameof(DeviceName))]
        public string DeviceName { get; set; } = string.Empty;

        [JsonProperty(nameof(SensorType))]
        public string SensorType { get; set; } = string.Empty;

        [JsonProperty(nameof(DeviceGroupId))]
        public string DeviceGroupId { get; set; } = string.Empty;

        [JsonProperty(nameof(FusionId))]
        public string FusionId { get; set; } = string.Empty;

        [JsonProperty(nameof(DeviceId))]
        public string DeviceId { get; set; } = string.Empty;

        [JsonProperty(nameof(DeviceTypeId))]
        public string DeviceTypeId { get; set; } = string.Empty;
    }
}
