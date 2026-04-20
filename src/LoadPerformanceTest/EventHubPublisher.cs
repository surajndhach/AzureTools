using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Newtonsoft.Json;

namespace LoadPerformanceTest
{
    /// <summary>
    /// Publishes strongly-typed data to Azure Event Hub.
    /// </summary>
    public class EventHubPublisher : IAsyncDisposable
    {
        private readonly EventHubProducerClient _client;

        public EventHubPublisher(string connectionString, string eventHubName)
        {
            _client = new EventHubProducerClient(connectionString, eventHubName);
        }

        /// <summary>
        /// Publishes a single data object as a JSON event to Event Hub.
        /// </summary>
        public async Task PublishAsync<T>(T data, string eventType, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(data);
            var eventData = new EventData(json)
            {
                ContentType = "application/json"
            };
            eventData.Properties["EventType"] = eventType;

            using EventDataBatch batch = await _client.CreateBatchAsync(cancellationToken);
            if (!batch.TryAdd(eventData))
                throw new InvalidOperationException("Event is too large for the batch.");

            await _client.SendAsync(batch, cancellationToken);
        }

        public async ValueTask DisposeAsync() => await _client.DisposeAsync();
    }
}
