using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ONE.Instrument.Twin.Core.Helpers
{
    public static class SerializationHelper
    {
        public static JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new DefaultNamingStrategy()
            }
        };

        public static T Deserialize<T>(string value)
        {
            if (string.IsNullOrEmpty(value)) return default;
            return JsonConvert.DeserializeObject<T>(value, JsonSettings);
        }

        public static string? Serialize<T>(T value)
        {
            if (value == null) return null;
            return JsonConvert.SerializeObject(value, JsonSettings);
        }
    }
}
