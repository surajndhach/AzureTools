namespace EventGridReceiver
{
    public class EventGridRecieverOptions
    {
        public string TopicEndpoint { get; set; } = string.Empty;


        public string UserAssignedClientId { get; set; } = string.Empty;


        public string ClientId { get; set; } = string.Empty;


        public string ClientSecret { get; set; } = string.Empty;


        public string TenantId { get; set; } = string.Empty;


        public string TopicName { get; set; } = string.Empty;

        public string SubscriptionName { get; set; } = string.Empty;
    }
}
