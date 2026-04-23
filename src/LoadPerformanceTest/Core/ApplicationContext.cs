using LoadPerformanceTest.Models;
using LoadPerformanceTest.Publishers;
using Microsoft.Extensions.Configuration;
using ONE.Models.CSharp.Instrument;

namespace LoadPerformanceTest.Core;

/// <summary>
/// Contains the shared application state and configuration.
/// </summary>
public class ApplicationContext
{
    public IConfiguration Configuration { get; init; } = null!;
    public List<Tenant> Tenants { get; init; } = null!;
    public string InventoryFileName { get; init; } = null!;
    public EventGridPublisher Publisher { get; init; } = null!;
    public List<InstrumentManifest> InstrumentManifests { get; init; } = null!;
}
