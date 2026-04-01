param (
    [Parameter(Mandatory = $true)]
    [int]$TenantCount,

    [Parameter(Mandatory = $true)]
    [int]$ControllersPerTenant,

    [Parameter(Mandatory = $true)]
    [int]$LDOCount = 0,

    [Parameter(Mandatory = $true)]
    [int]$NitrataxCount = 0
)

# Device type configuration: Add new types here by following the same pattern
$deviceTypeConfiguration = @{
    Controller = @{
        FusionPrefix = "HL001_17385"
        DeviceTypeId = "5d0f7f5a-6d5a-4d06-b0d3-1f2b8c3d1001"
    }
    LDO = @{
        FusionPrefix = "HL001_0019"
        DeviceTypeId = "d2df6b3b-f3b5-4cb8-9a39-6f5bf11d1002"
    }
    Nitratax = @{
        FusionPrefix = "HL001_0018"
        DeviceTypeId = "40a1a62e-8f31-4af0-93f3-c5146e8f1003"
    }
    GenericSensor = @{
        FusionPrefix = "HL001_00119"
        DeviceTypeId = "0f1d69f0-0ef5-49f6-b44f-6ef7c8a91004"
    }
}

function Build-SensorConfiguration {
    param (
        [int]$LDOCount,
        [int]$NitrataxCount
    )

    if ($LDOCount -lt 0 -or $NitrataxCount -lt 0) {
        throw "Sensor count parameters cannot be negative."
    }

    $requestedSensorConfig = @()

    if ($LDOCount -gt 0) {
        $requestedSensorConfig += [PSCustomObject]@{
            SensorType = 'LDO'
            Count = $LDOCount
        }
    }

    if ($NitrataxCount -gt 0) {
        $requestedSensorConfig += [PSCustomObject]@{
            SensorType = 'Nitratax'
            Count = $NitrataxCount
        }
    }

    if ($requestedSensorConfig.Count -gt 0) {
        return $requestedSensorConfig
    }

    throw "Provide at least one sensor type count using -LDOCount or -NitrataxCount."
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
    $deviceName = if ($SensorType -eq 'GenericSensor') { "Sensor-$SensorIndex" } else { "$SensorType-Sensor-$SensorIndex" }

    return @{
        DeviceName   = $deviceName
        SensorType   = $SensorType
        FusionId     = "{0}_{1}" -f $typeConfig.FusionPrefix, $serialNumber
        DeviceId     = [System.Guid]::NewGuid().ToString()
        DeviceTypeId = $typeConfig.DeviceTypeId
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
        DeviceTypeId = $controllerTypeConfig.DeviceTypeId
        Sensors      = $sensors
    }
}

# Build tenant hierarchy with controllers and sensors
$requestedSensorConfig = Build-SensorConfiguration `
    -LDOCount $LDOCount `
    -NitrataxCount $NitrataxCount
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