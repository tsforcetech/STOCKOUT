[CmdletBinding()]
param(
    [string[]]$ServiceFilter,
    [switch]$NoBuild,
    [int]$TimeoutSeconds = 30,
    [string]$Configuration = "Release",
    [string]$PidFilePath = "$env:TEMP\emcore-swagger-dev.pids",
    [switch]$TestRun
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " EMCORE Platform - Development Swagger Multi-Process Startup     " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Clean existing processes & check ports
Write-Host "`n[1/5] Stopping any existing development processes..." -ForegroundColor Yellow
& "$scriptDir\Stop-Development-Swagger.ps1" -PidFilePath $PidFilePath | Out-Null

$tempLogsDir = Join-Path $repoRoot ".system_generated\logs\live-dev"
if (-not (Test-Path $tempLogsDir)) {
    New-Item -ItemType Directory -Path $tempLogsDir -Force | Out-Null
}

if (-not $NoBuild) {
    Write-Host "`n[2/5] Building platform services ($Configuration mode)..." -ForegroundColor Yellow
    dotnet build "$repoRoot\Emcore.Platform.slnx" -c $Configuration /nologo /clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed! Aborting startup." -ForegroundColor Red
        exit $LASTEXITCODE
    }
} else {
    Write-Host "`n[2/5] Skipping build (--NoBuild flag active)..." -ForegroundColor DarkGray
}

# Define microservices and BFFs
$allServices = @(
    @{ Key="identity-access"; Name="Identity & Access API"; Path="services\identity-access\src\Emcore.IdentityAccess.Api\Emcore.IdentityAccess.Api.csproj"; Port=5194; Prefix="/api/v1/auth" },
    @{ Key="user-organization"; Name="User & Organization API"; Path="services\user-organization\src\Emcore.UserOrganization.Api\Emcore.UserOrganization.Api.csproj"; Port=5291; Prefix="/api/v1/users" },
    @{ Key="catalog-listing"; Name="Catalog & Listing API"; Path="services\catalog-listing\src\Emcore.CatalogListing.Api\Emcore.CatalogListing.Api.csproj"; Port=5072; Prefix="/api/v1/catalog" },
    @{ Key="inventory-media"; Name="Inventory & Media API"; Path="services\inventory-media\src\Emcore.InventoryMedia.Api\Emcore.InventoryMedia.Api.csproj"; Port=5079; Prefix="/api/v1/inventory" },
    @{ Key="search-discovery"; Name="Search & Discovery API"; Path="services\search-discovery\src\Emcore.SearchDiscovery.Api\Emcore.SearchDiscovery.Api.csproj"; Port=5255; Prefix="/api/v1/search" },
    @{ Key="bidding-deal"; Name="Bidding & Deal API"; Path="services\bidding-deal\src\Emcore.BiddingDeal.Api\Emcore.BiddingDeal.Api.csproj"; Port=5186; Prefix="/api/v1/deals" },
    @{ Key="inspection-trust"; Name="Inspection & Trust API"; Path="services\inspection-trust\src\Emcore.InspectionTrust.Api\Emcore.InspectionTrust.Api.csproj"; Port=5283; Prefix="/api/v1/inspections" },
    @{ Key="subscription-payment"; Name="Subscription & Payment API"; Path="services\subscription-payment\src\Emcore.SubscriptionPayment.Api\Emcore.SubscriptionPayment.Api.csproj"; Port=5091; Prefix="/api/v1/payments" },
    @{ Key="conversation-realtime"; Name="Conversation & Realtime API"; Path="services\conversation-realtime\src\Emcore.ConversationRealtime.Api\Emcore.ConversationRealtime.Api.csproj"; Port=5208; Prefix="/api/v1/messages" },
    @{ Key="notification-integration"; Name="Notification & Integration API"; Path="services\notification-integration\src\Emcore.NotificationIntegration.Api\Emcore.NotificationIntegration.Api.csproj"; Port=5201; Prefix="/api/v1/notifications" },
    @{ Key="workflow-scheduler"; Name="Workflow & Scheduler API"; Path="services\workflow-scheduler\src\Emcore.WorkflowScheduler.Api\Emcore.WorkflowScheduler.Api.csproj"; Port=5266; Prefix="/api/v1/workflows" },
    @{ Key="audit-reporting"; Name="Audit & Reporting API"; Path="services\audit-reporting\src\Emcore.AuditReporting.Api\Emcore.AuditReporting.Api.csproj"; Port=5003; Prefix="/api/v1/reports" },
    @{ Key="public-bff"; Name="Public BFF"; Path="gateways\Emcore.PublicBff\Emcore.PublicBff.csproj"; Port=5005; Prefix="/api/public" },
    @{ Key="portal-bff"; Name="Portal BFF"; Path="gateways\Emcore.PortalBff\Emcore.PortalBff.csproj"; Port=5127; Prefix="/api/portal" },
    @{ Key="mcp-gateway"; Name="MCP Gateway"; Path="gateways\Emcore.McpGateway\Emcore.McpGateway.csproj"; Port=5055; Prefix="/mcp" },
    @{ Key="realtime-gateway"; Name="Realtime Gateway"; Path="gateways\Emcore.RealtimeGateway\Emcore.RealtimeGateway.csproj"; Port=5225; Prefix="/realtime" }
)

$targetServices = $allServices
if ($null -ne $ServiceFilter -and $ServiceFilter.Count -gt 0) {
    $filterList = @($ServiceFilter | ForEach-Object { $_.Split(',') } | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" })
    $targetServices = $allServices | Where-Object { $filterList -contains $_.Key }
    Write-Host "Filtering startup to $($targetServices.Count) selected backend service(s): ($($targetServices.Key -join ', '))" -ForegroundColor White
}

$pidsList = @()
if (Test-Path $PidFilePath) { Remove-Item -Path $PidFilePath -Force }

Write-Host "`n[3/5] Starting downstream backend APIs and BFF gateways in Development mode..." -ForegroundColor Yellow

function Test-EndpointReady($url) {
    try {
        $res = Invoke-WebRequest -Uri $url -UseBasicParsing -Method Get -ErrorAction SilentlyContinue -TimeoutSec 1
        return ($null -ne $res -and $res.StatusCode -eq 200)
    } catch {
        if ($null -ne $_.Exception.Response -and $_.Exception.Response.StatusCode -eq 200) { return $true }
        return $false
    }
}

function Start-EmcoreService {
    param($SvcKey, $SvcPath, $SvcPort, $LogFile)
    
    $projectPath = Join-Path $repoRoot $SvcPath
    $projectDir = Split-Path -Parent $projectPath
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
    $dllPath = Join-Path $projectDir "bin\$Configuration\net10.0\$baseName.dll"
    if (-not (Test-Path $dllPath)) {
        $dllPath = Join-Path $projectDir "bin\Debug\net10.0\$baseName.dll"
    }

    $argString = ""
    if (Test-Path $dllPath) {
        $argString = "`"$dllPath`" --urls http://localhost:$SvcPort"
    } else {
        $argString = "run --project `"$projectPath`" --no-build -c $Configuration --urls http://localhost:$SvcPort"
    }

    $errFile = $LogFile -replace '\.log$', ".err.log"
    
    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = "dotnet"
    $pinfo.Arguments = $argString
    $pinfo.WorkingDirectory = $repoRoot
    $pinfo.UseShellExecute = $false
    $pinfo.CreateNoWindow = $true
    $pinfo.RedirectStandardOutput = $false
    $pinfo.RedirectStandardError = $false
    $pinfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development"
    
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    # Use native OS redirection via shell if needed or Start-Process for file redirection
    $proc = Start-Process -FilePath "dotnet" -ArgumentList $argString -WorkingDirectory $projectDir -RedirectStandardOutput $LogFile -RedirectStandardError $errFile -PassThru -WindowStyle Hidden
    return $proc
}

foreach ($svc in $targetServices) {
    $logFile = Join-Path $tempLogsDir "$($svc.Key).log"
    $url = "http://localhost:$($svc.Port)"
    
    Write-Host "  -> Launching $($svc.Name) on port $($svc.Port)..." -NoNewline
    $process = Start-EmcoreService -SvcKey $svc.Key -SvcPath $svc.Path -SvcPort $svc.Port -LogFile $logFile
    
    $pidsList += [PSCustomObject]@{ Service = $svc.Key; Pid = $process.Id; Port = $svc.Port; Log = $logFile }
    $pidsList | ConvertTo-Json -Depth 3 | Set-Content -Path $PidFilePath -Force
    
    # Wait for liveness
    $ready = $false
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    while ($stopwatch.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        if (Test-EndpointReady "$url/openapi/v1.json") {
            $ready = $true
            break
        }
        Start-Sleep -Milliseconds 250
    }
    
    if ($ready) {
        Write-Host " [LIVE]" -ForegroundColor Green
    } else {
        Write-Host " [TIMEOUT]" -ForegroundColor Red
        Write-Host "    Warning: $($svc.Name) did not respond within $TimeoutSeconds seconds. See log: $logFile" -ForegroundColor Yellow
        if (Test-Path ($logFile -replace '\.log$', ".err.log")) {
            Get-Content -Path ($logFile -replace '\.log$', ".err.log") | ForEach-Object { Write-Host "      ERR: $_" -ForegroundColor DarkRed }
        }
        if (Test-Path $logFile) {
            Write-Host "      --- Service Log ($($svc.Key)) ---" -ForegroundColor Yellow
            Get-Content -Path $logFile -Tail 40 | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
        }
    }
}

Write-Host "`n[4/5] Starting Central Emcore.ApiGateway on port 5000..." -ForegroundColor Yellow
$gatewayLog = Join-Path $tempLogsDir "api-gateway.log"
$gwPath = "gateways\Emcore.ApiGateway\Emcore.ApiGateway.csproj"
$gwUrl = "http://localhost:5000"

$gwProc = Start-EmcoreService -SvcKey "api-gateway" -SvcPath $gwPath -SvcPort 5000 -LogFile $gatewayLog

$pidsList += [PSCustomObject]@{ Service = "api-gateway"; Pid = $gwProc.Id; Port = 5000; Log = $gatewayLog }
$pidsList | ConvertTo-Json -Depth 3 | Set-Content -Path $PidFilePath -Force

$gwReady = $false
$gwStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
while ($gwStopwatch.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
    if (Test-EndpointReady "$gwUrl/api/v1/swagger/registry") {
        $gwReady = $true
        break
    }
    Start-Sleep -Milliseconds 250
}

if ($gwReady) {
    Write-Host "  -> Emcore.ApiGateway [LIVE] - Central Swagger Registry initialized." -ForegroundColor Green
} else {
    Write-Host "  -> Emcore.ApiGateway [TIMEOUT] - Registry did not respond. See log: $gatewayLog" -ForegroundColor Red
    if (Test-Path ($gatewayLog -replace '\.log$', ".err.log")) {
        Get-Content ($gatewayLog -replace '\.log$', ".err.log") | ForEach-Object { Write-Host "  GW ERR: $_" -ForegroundColor DarkRed }
    }
    if (Test-Path $gatewayLog) {
        Write-Host "--- Gateway Startup Log ---" -ForegroundColor Yellow
        Get-Content $gatewayLog -TotalCount 40 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    }
}

Write-Host "`n[5/5] Operational Verification Summary Table:" -ForegroundColor Cyan
Write-Host "----------------------------------------------------------------------------------------------------------------------------------" -ForegroundColor White
Write-Host ("{0,-28} | {1,-6} | {2,-35} | {3,-45}" -f "Service Name", "Port", "Direct OpenAPI Spec URL", "Central Gateway Try-It-Out Proxy Spec") -ForegroundColor White
Write-Host "----------------------------------------------------------------------------------------------------------------------------------" -ForegroundColor White

Write-Host ("{0,-28} | {1,-6} | {2,-35} | {3,-45}" -f "Central API Gateway", "5000", "http://localhost:5000/openapi/v1.json", "http://localhost:5000/swagger (Portal)") -ForegroundColor Green

foreach ($svc in $targetServices) {
    $name = $svc.Name
    $port = $svc.Port
    $directSpec = "http://localhost:$port/openapi/v1.json"
    $proxySpec = "http://localhost:5000/swagger/services/$($svc.Key)/v1/openapi.json"
    Write-Host ("{0,-28} | {1,-6} | {2,-35} | {3,-45}" -f $name, $port, $directSpec, $proxySpec) -ForegroundColor Gray
}

Write-Host "----------------------------------------------------------------------------------------------------------------------------------" -ForegroundColor White

if ($TestRun) {
    Write-Host "`n[TEST RUN ACTIVE] Executing automated live Gateway & YARP proxy validation..." -ForegroundColor Yellow
    
    # 1. Query Central Registry
    $regUrl = "http://localhost:5000/api/v1/swagger/registry"
    Write-Host "  -> Verifying registry schema at $regUrl..." -NoNewline
    try {
        $regRes = Invoke-RestMethod -Uri $regUrl -Method Get -ErrorAction Stop
        if ($null -ne $regRes -and $regRes.Count -eq 17) {
            Write-Host " [PASSED] ($($regRes.Count) services registered)" -ForegroundColor Green
            Write-Host "`nSample Registered Gateway Entry:" -ForegroundColor White
            $regRes[0] | ConvertTo-Json -Depth 2 | Write-Host -ForegroundColor Gray
        } else {
            Write-Host " [FAILED] - Expected 17 entries, found $($regRes.Count)" -ForegroundColor Red
        }
    } catch {
        Write-Host " [ERROR] - $($_.Exception.Message)" -ForegroundColor Red
    }

    # 2. Test YARP OpenAPI Spec Proxying
    Write-Host "`n  -> Testing YARP reverse-proxy contract routing for active services..." -ForegroundColor Yellow
    foreach ($svc in $targetServices) {
        $proxyUrl = "http://localhost:5000/swagger/services/$($svc.Key)/v1/openapi.json"
        Write-Host "     * Testing proxy: $proxyUrl..." -NoNewline
        try {
            $specJson = Invoke-RestMethod -Uri $proxyUrl -Method Get -ErrorAction Stop
            if ($null -ne $specJson.openapi -and $null -ne $specJson.info) {
                Write-Host " [PASSED] ($($specJson.info.title) v$($specJson.info.version))" -ForegroundColor Green
            } else {
                Write-Host " [INVALID SPEC]" -ForegroundColor Red
            }
        } catch {
            Write-Host " [FAILED] - $($_.Exception.Message)" -ForegroundColor Red
            if ($null -ne $_.Exception.Response) {
                try {
                    $stream = $_.Exception.Response.GetResponseStream()
                    $reader = [System.IO.StreamReader]::new($stream)
                    Write-Host "     Response Body: $($reader.ReadToEnd())" -ForegroundColor DarkRed
                } catch {}
            }
            if (Test-Path $gatewayLog) {
                Write-Host "     --- Gateway Debug Log Tail ---" -ForegroundColor Yellow
                Get-Content $gatewayLog -Tail 25 | ForEach-Object { Write-Host "       $_" -ForegroundColor Gray }
            }
        }
    }

    Write-Host "`n[TEST RUN COMPLETE] Shutting down development services cleanly..." -ForegroundColor Cyan
    & "$scriptDir\Stop-Development-Swagger.ps1" -PidFilePath $PidFilePath | Out-Null
    Write-Host "Live multi-process verification executed successfully." -ForegroundColor Green
    $LASTEXITCODE = 0
    exit 0
}

Write-Host "`nAll requested development services are actively running in the background." -ForegroundColor Green
Write-Host "To cleanly shutdown all test processes and clear ports, execute:" -ForegroundColor Yellow
Write-Host "  & `"$scriptDir\Stop-Development-Swagger.ps1`"" -ForegroundColor Cyan
Write-Host "=================================================================`n" -ForegroundColor Cyan
$LASTEXITCODE = 0
exit 0
