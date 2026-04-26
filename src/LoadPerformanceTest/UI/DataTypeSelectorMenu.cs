using LoadPerformanceTest.Utilities;
using Microsoft.Extensions.Configuration;

namespace LoadPerformanceTest.UI;

/// <summary>
/// Handles user selection of data types for publishing.
/// </summary>
public class DataTypeSelectorMenu
{
    private readonly IConfiguration _configuration;

    public DataTypeSelectorMenu(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the user's data type selection and returns the corresponding file name and data type.
    /// </summary>
    public (List<(string fileName, InstrumentDataType dataType)>? selections, bool isContinuous) GetDataTypeSelection()
    {
        Console.WriteLine("Select data type to publish:");
        Console.WriteLine("  1 - Measurement Data");
        Console.WriteLine("  2 - Diagnostic Data");
        Console.WriteLine("  3 - Status Data");
        Console.WriteLine("  4 - Event Data");
        Console.WriteLine("  5 - Settings Data");
        Console.WriteLine("  6 - All Data Types (Continuous)");
        Console.Write("Your choice: ");

        var dataTypeInput = Console.ReadLine()?.Trim();

        // Read file paths from configuration
        var measurementFile = _configuration["DataFilePaths:Measurement"];
        var diagnosticFile = _configuration["DataFilePaths:Diagnostic"];
        var statusFile = _configuration["DataFilePaths:Status"];
        var eventFile = _configuration["DataFilePaths:Event"];
        var settingsFile = _configuration["DataFilePaths:Settings"];

        return dataTypeInput switch
        {
            "1" => ([(measurementFile, InstrumentDataType.Measurement)], true),
            "2" => ([(diagnosticFile, InstrumentDataType.Diagnostic)], true),
            "3" => ([(statusFile, InstrumentDataType.Status)], true),
            "4" => ([(eventFile, InstrumentDataType.Event)], true),
            "5" => ([(settingsFile, InstrumentDataType.Settings)], true),
            "6" => ([
                (measurementFile, InstrumentDataType.Measurement),
                (diagnosticFile, InstrumentDataType.Diagnostic),
                (statusFile, InstrumentDataType.Status),
                (eventFile, InstrumentDataType.Event),
                (settingsFile, InstrumentDataType.Settings)
            ], true),
            _ => (null, false)
        };
    }
}