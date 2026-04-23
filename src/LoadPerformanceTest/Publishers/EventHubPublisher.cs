using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Newtonsoft.Json;
using System.Text;

namespace LoadPerformanceTest.Publishers
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
        public async Task PublishAsync(string jsonData, string eventType, string tenantId, CancellationToken cancellationToken = default)
        {
            var eventBytes = Encoding.UTF8.GetBytes(jsonData);

            var eventData = new EventData(eventBytes);
            eventData.Properties.Add("tenantId", tenantId);

            using EventDataBatch batch = await _client.CreateBatchAsync(cancellationToken);
            if (!batch.TryAdd(eventData))
                throw new InvalidOperationException("Event is too large for the batch.");

            await _client.SendAsync(batch, cancellationToken);
        }


        public async ValueTask DisposeAsync() => await _client.DisposeAsync();
    }
}
