using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ONE.Shared.Helpers.Azure.SignalR;
using ONE.Shared.Helpers.Azure.SignalR.Models;

internal class Program
{
    private static async Task Main(string[] args)
    {
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(c =>
            {
                c.AddJsonFile("appsettings.json", optional: false);
                c.AddEnvironmentVariables();
            })
            .ConfigureLogging(l =>
            {
                l.ClearProviders();
                l.AddConsole();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var config = host.Services.GetRequiredService<IConfiguration>();

        var options = config.GetSection("SignalR").Get<SignalRMessagePublisherOptions>()!;

        var publisher = await SignalRMessagePublisher.CreateAsync(options, logger);

        var hub = "instrumentHub";
        var group = "testgroup";

        await publisher.CreateHubContextAsync(hub, CancellationToken.None);

        var payload = $"Testing {DateTime.Now}";

        //await publisher.AddConnectionToGroupAsync(hub, "8Fm7b1t7wsN-9UedbmIraAK12oOgK02", group, CancellationToken.None);

        //await publisher.SendToAllAsync(hub, "test-method", payload, CancellationToken.None);

        //await publisher.SendToConnectionAsync(
        //    hub,
        //    "LFXtjSLaWvbAjI2ekZL3aw-4YAoQK02",
        //    "test-method",
        //    payload,
        //    CancellationToken.None);

        await publisher.SendToGroupAsync(
            hub,
            group,
            "test-method",
            payload,
            CancellationToken.None);

        logger.LogInformation("Message published successfully");

        Console.ReadLine();
    }
}

