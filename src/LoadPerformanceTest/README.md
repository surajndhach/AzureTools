# LoadPerformanceTest — IoT Registry Load Performance Testing Utility

A .NET 8 console application for load performance testing (LPT) of the Instrument Health Application. It publishes `Instrument.Assigned`, `Instrument.Updated`, and `Instrument.Unassigned` cloud events to Azure Event Grid, publishes telemetry data (measurements, diagnostics, status, events, settings) to Azure Event Hub, manages device inventory, and handles tenant lifecycle via REST APIs.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
  - [appsettings.json](#appsettingsjson)
  - [Device Inventory File](#device-inventory-file)
- [Generating a Device Inventory](#generating-a-device-inventory)
- [Running the Load Performance Test](#running-the-load-performance-test)
  - [Recommended Order (Full LPT Cycle)](#recommended-order-full-lpt-cycle)
  - [Cleanup Order](#cleanup-order)
- [Menu Options](#menu-options)
- [Logging](#logging)
- [Project Structure](#project-structure)

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Access to the target Azure Event Grid Namespace (topic endpoint, topic name)
- Access to Azure Event Hub (connection string, hub name)
- A registered Azure AD Service Principal (`ClientId`, `ClientSecret`, `TenantId`)
- Valid admin credentials for the Claros API (used for tenant create/delete operations)
- PowerShell (for running the device inventory generator script)

---

## Configuration

### appsettings.json

Update `LoadPerformanceTest/appsettings.json` with the correct values before running:

| Section | Key | Description |
|---|---|---|
| `EventGridSender` | `TopicEndpoint` | Azure Event Grid Namespace endpoint URL |
| `EventGridSender` | `TopicName` | Event Grid topic name |
| `EventGridSender` | `ClientId` | Azure AD App Registration Client ID |
| `EventGridSender` | `ClientSecret` | Azure AD App Registration Client Secret |
| `EventGridSender` | `TenantId` | Azure AD Tenant ID |
| `EventHub` | `ConnectionString` | Azure Event Hub connection string |
| `EventHub` | `Name` | Event Hub name |
| `DeviceInventoryFilePath` | — | Relative path to the device inventory JSON file |
| `ManifestsFilePath` | — | Path to instrument manifests JSON file |
| `DataFilePaths` | `Measurement` | Path to measurement data template file |
| `DataFilePaths` | `Diagnostic` | Path to diagnostic data template file |
| `DataFilePaths` | `Status` | Path to status data template file |
| `DataFilePaths` | `Event` | Path to event data template file |
| `DataFilePaths` | `Settings` | Path to settings data template file |
| `PublishingIntervals` | `MeasurementIntervalSeconds` | Interval for publishing measurements (default: 30s) |
| `PublishingIntervals` | `DiagnosticIntervalSeconds` | Interval for publishing diagnostics (default: 600s) |
| `PublishingIntervals` | `StatusIntervalSeconds` | Interval for publishing status (default: 900s) |
| `PublishingIntervals` | `EventIntervalSeconds` | Interval for publishing events (default: 480s) |
| `PublishingIntervals` | `SettingsIntervalSeconds` | Interval for publishing settings (default: 1200s) |
| `AdminAuth` | `TokenEndpoint` | Claros API token endpoint |
| `AdminAuth` | `Username` | Admin username for tenant operations |
| `AdminAuth` | `Password` | Admin password for tenant operations |
| `AdminAuth` | `ClientId` | OAuth client ID for token request |
| `AdminAuth` | `ClientSecret` | OAuth client secret for token request |
| `AdminAuth` | `Scope` | OAuth scopes |
| `LogSettings` | `LogFolder` | Folder name for log output (default: `Logs`) |

> **⚠️ Important:** Never commit secrets (`ClientSecret`, `Password`, `ConnectionString`) to source control. Use environment variables or a secrets manager for sensitive values.

### Device Inventory File

The utility reads a JSON device inventory file that defines tenants, controllers, and sensors. The file path is configured via `DeviceInventoryFilePath` in `appsettings.json`.

The JSON structure follows this hierarchy:

```
Tenant
 └── Controller
      └── Sensor
```

Each entity includes fields such as `DeviceId`, `FusionId`, `DeviceTypeId`, and `DeviceGroupId`.

---

## Generating a Device Inventory

Use the included PowerShell script to generate a device inventory file:

```powershell
cd LoadPerformanceTest\Scripts

# List available sensor types
.\TenantDeviceGenerator.ps1 -ListSensorTypes

# Generate an inventory with 2 tenants, 3 controllers each, with Anise and Nitratax sensors
.\TenantDeviceGenerator.ps1 -TenantCount 2 -ControllersPerTenant 3 -SensorCounts @{ Anise = 2; Nitratax = 1 }
```

**Available sensor types:** `Anise`, `Nitratax`, `Solitax`, `RTC`

The script outputs a JSON file named with the pattern:
`device-inventory-tenants{count}-sensors{total}-{timestamp}.json`

After generation, update `DeviceInventoryFilePath` in `appsettings.json` to point to the new file.

---

## Running the Load Performance Test

1. Ensure `appsettings.json` is configured correctly.
2. Build and run the `LoadPerformanceTest` project:

   ```
   dotnet run --project LoadPerformanceTest
   ```

3. The application loads the device inventory and presents an interactive menu.

### Recommended Order (Full LPT Cycle)

> **To perform a smooth and complete LPT, follow the options in the order listed below.** Each step depends on the previous one.

| Step | Option | Action | Required? | Description |
|------|--------|--------|-----------|-------------|
| 1 | `1` | **Create Tenants** | ✅ Yes | Provisions tenants via the Claros REST API. This must be done first. |
| 2 | `2` | **Create Controllers** | ✅ Yes | Publishes `Instrument.Assigned` events for all controllers in the inventory. |
| 3 | `3` | **Create Sensors** | ✅ Yes | Publishes `Instrument.Assigned` events for all sensors under each controller. |
| 4 | `4` | **Update Instruments** | ⚡ Optional | Publishes `Instrument.Updated` events for all controllers and sensors (sets status to `NotConnected`). **This step is not mandatory and can be skipped without affecting cleanup.** |

### Cleanup Order

> **To clean up everything after the LPT and ensure no resources are left behind, follow the options in this order.** Sensors are deleted before controllers, and instruments before tenants.
>
> **Note:** You do **not** need to run "Update Instruments" (option 4) before cleanup. Cleanup works regardless of whether the update step was performed.

| Step | Option | Action | Description |
|------|--------|--------|-------------|
| 1 | `5` | **Delete Instruments** | Publishes `Instrument.Unassigned` events — sensors first, then controllers. |
| 2 | `6` | **Delete Tenants** | Removes tenants via the Claros REST API. |

After cleanup, press `Q` to quit.

---

## Menu Options

```
Select an option:
  1 - Create Tenants
  2 - Create Controllers
  3 - Create Sensors
  4 - Update Instruments
  5 - Delete Instruments
  6 - Delete Tenants
  7 - Publish Instrument Data
  Q - Quit
```

### Menu Option Details

| Option | Action | Description |
|--------|--------|-------------|
| **1** | Create Tenants | Provisions tenants via the Claros REST API |
| **2** | Create Controllers | Publishes `Instrument.Assigned` events for all controllers to Event Grid |
| **3** | Create Sensors | Publishes `Instrument.Assigned` events for all sensors to Event Grid |
| **4** | Update Instruments | Publishes `Instrument.Updated` events for all controllers and sensors to Event Grid |
| **5** | Delete Instruments | Publishes `Instrument.Unassigned` events (sensors first, then controllers) to Event Grid |
| **6** | Delete Tenants | Removes tenants via the Claros REST API |
| **7** | Publish Instrument Data | Publishes telemetry data (measurements, diagnostics, status, events, settings) to Event Hub |
| **Q** | Quit | Exits the application |

### Option 7: Publish Instrument Data

When selecting option 7, you'll be prompted to choose a data type:

```
Select data type to publish:
  1 - Measurement Data
  2 - Diagnostic Data
  3 - Status Data
  4 - Event Data
  5 - Settings Data
  6 - All Data Types (Continuous)
Your choice:
```

#### Data Type Options:

- **Options 1-5**: Publishes the selected data type continuously at the configured interval
- **Option 6**: Publishes all data types simultaneously, each at their own configured intervals

#### Continuous Publishing:

- Data is published repeatedly based on intervals configured in `appsettings.json` under `PublishingIntervals`
- Each publish batch updates timestamps and generates fresh data for all instruments in the inventory
- Press **'Q'** at any time to stop continuous publishing
- Real-time statistics are displayed showing batch count, success/failure counts, and elapsed time

#### Example Output:
```
[14:32:15] Measurement: Published batch #1 - 45 succeeded, 0 failed (Running: 00:00:30)
[14:32:25] Status: Published batch #1 - 45 succeeded, 0 failed (Running: 00:00:40)
[14:32:45] Measurement: Published batch #2 - 45 succeeded, 0 failed (Running: 00:01:00)
```

Each option prints progress and summary counts (succeeded / failed) to the console.

---

## Logging

All operations are logged to structured JSON files under the `Logs` folder (configurable via `LogSettings:LogFolder`). Logs are organized by date:

```
LoadPerformanceTest/
└── Logs/
    └── 2026-04-10/
        ├── info.json       ← Successful operations and general information
        ├── warning.json    ← Non-critical issues (e.g., unexpected HTTP status codes)
        └── error.json      ← Failures and exceptions with full stack traces
```

### What gets logged

- **Info:** Inventory parsing, event publish successes, tenant create/delete successes, Event Hub publish operations, continuous publishing sessions, summary counts.
- **Warning:** Unexpected HTTP responses during tenant operations, token acquisition issues, skipped RTC measurements without ParameterId.
- **Error:** Exceptions during event publishing, Event Hub publishing failures, network errors, authentication failures, RTC measurement creation errors (includes exception type, message, and stack trace).

### Investigating issues

If an operation reports failures in the console output, check the corresponding date folder under `Logs/`:

1. Open `Logs/<date>/error.json` for detailed exception information.
2. Open `Logs/<date>/warning.json` for non-fatal issues such as HTTP 409 (conflict) or 404 (not found) responses.
3. Open `Logs/<date>/info.json` for a full audit trail of successful operations.

Each log entry includes a `timestamp`, `level`, `message`, and optionally an `exception` object with `type`, `message`, `stackTrace`, and `innerException`.

---

## Project Structure

```
LoadPerformanceTest/
├── Program.cs                          # Main entry point
├── Core/                               # Application orchestration
│   ├── ApplicationInitializer.cs      # Initializes configuration, inventory, manifests, services
│   ├── ApplicationContext.cs          # Holds application-wide context and dependencies
│   ├── OperationOrchestrator.cs       # Orchestrates all business operations
│   ├── InstrumentDataPublisher.cs     # Handles Event Hub publishing (single/continuous)
│   └── EventGridSenderOptions.cs      # Event Grid configuration model
├── Services/                           # Business logic services
│   ├── EventGrid/
│   │   ├── EventGridPublisher.cs      # Sends cloud events to Azure Event Grid
│   │   └── CloudEventBuilder.cs       # Builds CloudEvent payloads for instruments
│   ├── EventHub/
│   │   └── EventHubPublisher.cs       # Publishes telemetry data to Azure Event Hub
│   ├── TenantService.cs                # Handles tenant create/delete via REST API
│   └── Parsers/
│       └── DeviceInventoryParser.cs    # Parses device inventory JSON file
├── Utilities/                          # Helper utilities
│   ├── InstrumentDataBuilder.cs        # Generates InstrumentData from templates
│   ├── AuthTokenProvider.cs            # Acquires OAuth admin tokens for API calls
│   └── Logger.cs                       # Structured JSON logger (info/warning/error)
├── UI/                                 # User interface components
│   ├── MainMenu.cs                     # Interactive menu display and input handling
│   └── DataTypeSelectorMenu.cs         # Data type selection for Event Hub publishing
├── Models/                             # Data models
│   ├── DeviceInventory.cs              # Tenant, Controller, Sensor models
│   └── InstrumentDataType.cs           # Enum for data types (Measurement, Diagnostic, etc.)
├── Configuration/                      # Configuration models
│   ├── AdminAuthOptions.cs             # Admin authentication configuration
│   └── LogSettings.cs                  # Logging configuration
├── Data/                               # Template data files
│   ├── instrumentmeasurementdata.json  # Measurement data template
│   ├── instrumentdiagnosticdata.json   # Diagnostic data template
│   ├── instrumentstatusdata.json       # Status data template
│   ├── instrumenteventdata.json        # Event data template
│   ├── instrumentsettingdata.json      # Settings data template
│   └── manifests.json                  # Instrument manifests (capabilities, parameters)
├── Scripts/                            # PowerShell scripts
│   └── TenantDeviceGenerator.ps1       # Generates device inventory files
├── Logs/                               # Auto-generated log output (by date)
└── appsettings.json                    # Application configuration
```
