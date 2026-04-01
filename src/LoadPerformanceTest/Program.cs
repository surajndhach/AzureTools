// Parse from file
using EventGridSender;

var tenants = await DeviceInventoryParser.ParseFromFileAsync("C:\\Users\\rsingh\\source\\repos\\AzureTools\\src\\Scripts\\device-inventory-tenants2-sensors12.json");

// Parse from string
//var tenants = DeviceInventoryParser.Parse(jsonString);

// Access the hierarchy
foreach (var tenant in tenants)
{
    Console.WriteLine($"Tenant: {tenant.TenantName} ({tenant.TenantId})");
    foreach (var controller in tenant.Controllers)
    {
        Console.WriteLine($"  Controller: {controller.DeviceName} - {controller.FusionId}");
        foreach (var sensor in controller.Sensors)
        {
            Console.WriteLine($"    Sensor: {sensor.DeviceName} ({sensor.SensorType}) - {sensor.FusionId}");
        }
    }
}