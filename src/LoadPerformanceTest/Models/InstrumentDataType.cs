namespace LoadPerformanceTest.Models
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
}