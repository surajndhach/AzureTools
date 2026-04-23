using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;

namespace LoadPerformanceTest.Publishers;

public class EventGridPublisher(EventGridSenderClient client, int delayBetweenEventsMs = 100)
{
    public async Task<(int SuccessCount, int FailCount)> SendAllAsync(List<CloudEvent> cloudEvents)
    {
        var successCount = 0;
        var failCount = 0;

        foreach (var cloudEvent in cloudEvents)
        {
            var succeeded = await SendAsync(cloudEvent);

            if (succeeded)
                successCount++;
            else
                failCount++;

            await Task.Delay(delayBetweenEventsMs);
        }

        return (successCount, failCount);
    }

    private async Task<bool> SendAsync(CloudEvent cloudEvent)
    {
        try
        {
            var response = await client.SendAsync(cloudEvent);
            var isSuccess = response.Status is >= 200 and <= 299;
            var status = isSuccess ? "OK" : "FAILED";

            Console.WriteLine($"[{status}] Sent {cloudEvent.Type} | Subject: {cloudEvent.Subject} | Status: {response.Status}");

            if (isSuccess)
                Logger.LogInfo($"Published {cloudEvent.Type} | Subject: {cloudEvent.Subject} | Status: {response.Status}");
            else
                Logger.LogError($"Failed to publish {cloudEvent.Type} | Subject: {cloudEvent.Subject} | Status: {response.Status}");

            return isSuccess;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to send {cloudEvent.Type} | Subject: {cloudEvent.Subject} | Error: {ex.Message}");
            Logger.LogError($"Exception sending {cloudEvent.Type} | Subject: {cloudEvent.Subject}", ex);
            return false;
        }
    }
}
