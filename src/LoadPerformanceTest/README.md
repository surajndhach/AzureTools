# LoadPerformanceTest — IoT Registry Load Performance Testing Utility

A .NET 8 console application for load performance testing (LPT) of the Instrument Health Application. It publishes `Instrument.Assigned`, `Instrument.Updated`, and `Instrument.Unassigned` cloud events to Azure Event Grid using a device inventory file, and manages tenant lifecycle via REST APIs.

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
| `DeviceInventoryFilePath` | — | Relative path to the device inventory JSON file |
| `AdminAuth` | `TokenEndpoint` | Claros API token endpoint |
| `AdminAuth` | `Username` | Admin username for tenant operations |
| `AdminAuth` | `Password` | Admin password for tenant operations |
| `AdminAuth` | `ClientId` | OAuth client ID for token request |
| `AdminAuth` | `ClientSecret` | OAuth client secret for token request |
| `AdminAuth` | `Scope` | OAuth scopes |
| `LogSettings` | `LogFolder` | Folder name for log output (default: `Logs`) |

> **⚠️ Important:** Never commit secrets (`ClientSecret`, `Password`) to source control. Use environment variables or a secrets manager for sensitive values.

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
  Q - Quit
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

- **Info:** Inventory parsing, event publish successes, tenant create/delete successes, summary counts.
- **Warning:** Unexpected HTTP responses during tenant operations, token acquisition issues.
- **Error:** Exceptions during event publishing, network errors, authentication failures (includes exception type, message, and stack trace).

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
├── Program.cs                  # Main entry point — interactive menu
├── CloudEventBuilder.cs        # Builds Azure CloudEvent payloads for all event types
├── EventGridPublisher.cs       # Sends cloud events to Azure Event Grid
├── DeviceInventoryParser.cs    # Parses the device inventory JSON file
├── TenantFacade.cs             # Handles tenant create/delete via REST API
├── AuthToken.cs                # Acquires OAuth admin tokens for API calls
├── EventGridSenderOptions.cs   # Configuration model for Event Grid settings
├── Logger.cs                   # Structured JSON logger (info/warning/error)
├── Models/
│   └── DeviceInventory.cs      # Data models: Tenant, Controller, Sensor
├── Scripts/
│   └── TenantDeviceGenerator.ps1  # PowerShell script to generate inventory files
├── Logs/                       # Auto-generated log output (by date)
└── appsettings.json            # Application configuration
```
