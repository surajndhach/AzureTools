namespace LoadPerformanceTest.Utilities
{
    /// <summary>
    /// Represents the different types of instrument data that can be published.
    /// </summary>
    public enum InstrumentDataType
    {
        Measurement,
        Diagnostic,
        Status,
        Event,
        Settings
    }

    /// <summary>
    /// Extension methods for InstrumentDataType enum.
    /// </summary>
    public static class InstrumentDataTypeExtensions
    {
        /// <summary>
        /// Gets the corresponding JSON file name for the data type.
        /// </summary>
        public static string GetFileName(this InstrumentDataType dataType) => dataType switch
        {
            InstrumentDataType.Measurement => "instrumentmeasurementdata.json",
            InstrumentDataType.Diagnostic => "instrumentdiagnosticdata.json",
            InstrumentDataType.Status => "instrumentstatusdata.json",
            InstrumentDataType.Event => "instrumenteventdata.json",
            InstrumentDataType.Settings => "instrumentsettingdata.json",
            _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
        };

        /// <summary>
        /// Gets the display name for the data type.
        /// </summary>
        public static string GetDisplayName(this InstrumentDataType dataType) => dataType switch
        {
            InstrumentDataType.Measurement => "Measurement Data",
            InstrumentDataType.Diagnostic => "Diagnostic Data",
            InstrumentDataType.Status => "Status Data",
            InstrumentDataType.Event => "Event Data",
            InstrumentDataType.Settings => "Settings Data",
            _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
        };

        /// <summary>
        /// Gets the string value used for processing (lowercase).
        /// </summary>
        public static string GetProcessingValue(this InstrumentDataType dataType) => dataType switch
        {
            InstrumentDataType.Measurement => "measurement",
            InstrumentDataType.Diagnostic => "diagnostic",
            InstrumentDataType.Status => "status",
            InstrumentDataType.Event => "event",
            InstrumentDataType.Settings => "settings",
            _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null)
        };
    }
}