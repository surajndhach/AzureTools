using LoadPerformanceTest.Core;

namespace LoadPerformanceTest;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var initializer = new ApplicationInitializer();
        var context = await initializer.InitializeAsync();

        var orchestrator = new OperationOrchestrator(context);
        var menu = new InteractiveMenu(orchestrator);

        await menu.RunAsync();

        Console.WriteLine("Exiting. Press any key to close.");
        Console.ReadKey();
    }
}