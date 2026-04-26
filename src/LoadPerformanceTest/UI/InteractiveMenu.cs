using LoadPerformanceTest.Core;

namespace LoadPerformanceTest.UI;

/// <summary>
/// Handles the interactive menu display and user input processing.
/// </summary>
public class InteractiveMenu
{
    private readonly OperationOrchestrator _orchestrator;

    public InteractiveMenu(OperationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Runs the interactive menu loop.
    /// </summary>
    public async Task RunAsync()
    {
        bool exit = false;

        while (!exit)
        {
            DisplayMenu();
            var input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1":
                    await _orchestrator.CreateTenantsAsync();
                    break;
                case "2":
                    await _orchestrator.CreateControllersAsync();
                    break;
                case "3":
                    await _orchestrator.CreateSensorsAsync();
                    break;
                case "4":
                    await _orchestrator.UpdateInstrumentsAsync();
                    break;
                case "5":
                    await _orchestrator.DeleteInstrumentsAsync();
                    break;
                case "6":
                    await _orchestrator.DeleteTenantsAsync();
                    break;
                case "7":
                    await _orchestrator.PublishInstrumentDataAsync();
                    break;
                case "Q":
                case "q":
                    exit = true;
                    break;
                default:
                    Console.WriteLine("Invalid selection. Please try again.\n");
                    break;
            }
        }
    }

    /// <summary>
    /// Displays the main menu options.
    /// </summary>
    private static void DisplayMenu()
    {
        Console.WriteLine("Select an option:");
        Console.WriteLine("  1 - Create Tenants");
        Console.WriteLine("  2 - Create Controllers");
        Console.WriteLine("  3 - Create Sensors");
        Console.WriteLine("  4 - Update Instruments");
        Console.WriteLine("  5 - Delete Instruments");
        Console.WriteLine("  6 - Delete Tenants");
        Console.WriteLine("  7 - Publish Instrument Data");
        Console.WriteLine("  Q - Quit");
        Console.Write("\nYour choice: ");
    }
}
