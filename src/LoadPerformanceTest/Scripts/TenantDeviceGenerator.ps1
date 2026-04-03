param (
    [Parameter(Mandatory = $false)]
    [int]$TenantCount,

    [Parameter(Mandatory = $false)]
    [int]$ControllersPerTenant,

    [Parameter(Mandatory = $false)]
    [hashtable]$SensorCounts = @{},

    [Parameter(Mandatory = $false)]
    [switch]$ListSensorTypes
)

# Device type configuration: Add new types here by following the same pattern
$deviceTypeConfiguration = @{
    Controller = @{
        FusionPrefix = "HL001_17385"
        DeviceTypeId = "27056753-613f-4bc2-8b2f-72d1cdcada36"
        DeviceGroupId = "12ed4794-fda9-4187-af34-6da2774b4d28"
    }
    Anise = @{
        FusionPrefix = "HL001_00119"
        DeviceTypeId = "28dd1baa-a8b8-4066-b9f1-53dc027857f7"
        DeviceGroupId = "55892762-da61-4f88-99d1-dc6c93b0f7b3"
    }
    Nitratax = @{
        FusionPrefix = "HL001_00103"
        DeviceTypeId = "9756ce17-da66-4876-9392-a92c44985c59"
        DeviceGroupId = "55892762-da61-4f88-99d1-dc6c93b0f7b3"
    }
    Solitax = @{
        FusionPrefix = "HL001_00104"
        DeviceTypeId = "7c2bb0b6-439d-4f15-9c99-61e209f45aae"
        DeviceGroupId = "55892762-da61-4f88-99d1-dc6c93b0f7b3"
    }
}

function Show-AvailableSensorTypes {
    param (
        [hashtable]$DeviceTypeConfiguration
    )

    $sensorTypes = $DeviceTypeConfiguration.Keys | Where-Object { $_ -ne 'Controller' } | Sort-Object

    Write-Host "`nAvailable sensor types:" -ForegroundColor Cyan
    Write-Host "========================" -ForegroundColor Cyan
    foreach ($type in $sensorTypes) {
        $config = $DeviceTypeConfiguration[$type]
        Write-Host "  - $type" -ForegroundColor Yellow -NoNewline
        Write-Host "  (FusionPrefix: $($config.FusionPrefix), DeviceTypeId: $($config.DeviceTypeId))"
    }
    Write-Host "`nUsage example:" -ForegroundColor Cyan
    Write-Host "  .\TenantDeviceGenerator.ps1 -TenantCount 2 -ControllersPerTenant 3 -SensorCounts @{ $($sensorTypes[0]) = 2; $($sensorTypes[1]) = 1 }" -ForegroundColor Green
    Write-Host ""
}

if ($ListSensorTypes) {
    Show-AvailableSensorTypes -DeviceTypeConfiguration $deviceTypeConfiguration
    return
}

if (-not $TenantCount -or -not $ControllersPerTenant) {
    Write-Host "Error: -TenantCount and -ControllersPerTenant are required (unless using -ListSensorTypes)." -ForegroundColor Red
    Show-AvailableSensorTypes -DeviceTypeConfiguration $deviceTypeConfiguration
    return
}

function Build-SensorConfiguration {
    param (
        [hashtable]$SensorCounts,
        [hashtable]$DeviceTypeConfiguration
    )

    $validSensorTypes = $DeviceTypeConfiguration.Keys | Where-Object { $_ -ne 'Controller' }

    foreach ($key in $SensorCounts.Keys) {
        if ($key -notin $validSensorTypes) {
            throw "Unknown sensor type '$key'. Valid types: $($validSensorTypes -join ', ')"
        }
        if ($SensorCounts[$key] -lt 0) {
            throw "Sensor count for '$key' cannot be negative."
        }
    }

    $requestedSensorConfig = @()

    foreach ($sensorType in $validSensorTypes) {
        $count = if ($SensorCounts.ContainsKey($sensorType)) { $SensorCounts[$sensorType] } else { 0 }

        if ($count -gt 0) {
            $requestedSensorConfig += [PSCustomObject]@{
                SensorType = $sensorType
                Count      = $count
            }
        }
    }

    if ($requestedSensorConfig.Count -gt 0) {
        return $requestedSensorConfig
    }

    throw "Provide at least one sensor count via -SensorCounts. Valid types: $($validSensorTypes -join ', '). Example: -SensorCounts @{ Anise = 2; Nitratax = 1 }"
}

function New-Sensor {
    param (
        [int]$TenantIndex,
        [int]$ControllerIndex,
        [int]$SensorIndex,
        [string]$SensorType,
        [hashtable]$DeviceTypeConfiguration
    )

    $typeConfig = $DeviceTypeConfiguration[$SensorType]
    $serialNumber = Get-Random -Minimum 100000000 -Maximum 999999999
    $deviceName = "$SensorType-Sensor-$SensorIndex"

    return @{
        DeviceName   = $deviceName
        SensorType   = $SensorType
        FusionId     = "{0}_{1}" -f $typeConfig.FusionPrefix, $serialNumber
        DeviceId     = [System.Guid]::NewGuid().ToString()
        DeviceTypeId = $typeConfig.DeviceTypeId
        DeviceGroupId = $typeConfig.DeviceGroupId
    }
}

function New-Controller {
    param (
        [int]$TenantIndex,
        [int]$ControllerIndex,
        [object[]]$RequestedSensorConfig,
        [hashtable]$DeviceTypeConfiguration
    )

    $sensors = @()
    $sensorIndex = 1

    foreach ($sensorConfig in $RequestedSensorConfig) {
        for ($s = 1; $s -le $sensorConfig.Count; $s++) {
            $sensors += New-Sensor `
                -TenantIndex $TenantIndex `
                -ControllerIndex $ControllerIndex `
                -SensorIndex $sensorIndex `
                -SensorType $sensorConfig.SensorType `
                -DeviceTypeConfiguration $DeviceTypeConfiguration

            $sensorIndex++
        }
    }

    $controllerTypeConfig = $DeviceTypeConfiguration.Controller
    $serialNumber = Get-Random -Minimum 100000000 -Maximum 999999999

    return @{
        DeviceName   = "Controller-$ControllerIndex"
        FusionId     = "{0}_{1}" -f $controllerTypeConfig.FusionPrefix, $serialNumber
        DeviceId     = [System.Guid]::NewGuid().ToString()
        DeviceGroupId = $controllerTypeConfig.DeviceGroupId
        DeviceTypeId = $controllerTypeConfig.DeviceTypeId
        Sensors      = $sensors
    }
}

# Build tenant hierarchy with controllers and sensors
$requestedSensorConfig = Build-SensorConfiguration `
    -SensorCounts $SensorCounts `
    -DeviceTypeConfiguration $deviceTypeConfiguration
$sensorsPerController = ($requestedSensorConfig | Measure-Object -Property Count -Sum).Sum

$tenantHierarchy = @()

for ($t = 1; $t -le $TenantCount; $t++) {

    $controllers = @()

    for ($c = 1; $c -le $ControllersPerTenant; $c++) {
        $controllers += New-Controller `
            -TenantIndex $t `
            -ControllerIndex $c `
            -RequestedSensorConfig $requestedSensorConfig `
            -DeviceTypeConfiguration $deviceTypeConfiguration
    }

    $tenantHierarchy += @{
        TenantId   = [System.Guid]::NewGuid().ToString()
        TenantName = "Tenant-$t"
        Controllers = $controllers
    }
}

# Generate output file
$totalDevices = $TenantCount * $ControllersPerTenant * $sensorsPerController
$outputFileName = "device-inventory-tenants{0}-sensors{1}.json" -f $TenantCount, $totalDevices
$outputPath = Join-Path -Path "." -ChildPath $outputFileName

$tenantHierarchy | ConvertTo-Json -Depth 10 | Out-File $outputPath -Encoding utf8

Write-Host "Device inventory JSON generated successfully at: $outputPath"  -ForegroundColor Green