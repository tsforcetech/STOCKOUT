<#
.SYNOPSIS
    Automated Windows Server Deployment Script for EMCORE Identity & Access Platform.
.DESCRIPTION
    Configures IIS application pools, websites, reverse proxy forwarding, and installs
    the Identity Access Relay Worker as a persistent Windows Service with automated recovery.
.PARAMETER DeploymentPath
    Root directory where binaries and artifacts are published (e.g. C:\Inetpub\Emcore\Identity).
.PARAMETER ServiceAccount
    Windows identity or Virtual Account under which workloads operate (e.g. "IIS APPPOOL\IdentityAppPool").
#>

Param(
    [string]$DeploymentPath = "C:\Inetpub\Emcore\Identity",
    [string]$ApiPort = "5101",
    [string]$ServiceName = "EmcoreIdentityRelayWorker"
)

$ErrorActionPreference = "Stop"
Write-Host ">>> Starting EMCORE Identity Access Windows Deployment..." -ForegroundColor Cyan

# 1. Create directory structure if absent
if (-not (Test-Path -Path $DeploymentPath)) {
    New-Item -ItemType Directory -Path $DeploymentPath -Force | Out-Null
    Write-Host "Created target publication directory: $DeploymentPath" -ForegroundColor Green
}

# 2. Configure IIS Application Pool
$AppPoolName = "EmcoreIdentityAppPool"
Write-Host "Configuring IIS Application Pool: $AppPoolName..."
Import-Module WebAdministration
if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    New-WebAppPool -Name $AppPoolName
}
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "startMode" -Value "AlwaysRunning"
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"

# 3. Configure IIS Website
$SiteName = "EmcoreIdentityApi"
Write-Host "Configuring IIS Website: $SiteName on Port $ApiPort..."
if (-not (Test-Path "IIS:\Sites\$SiteName")) {
    New-Website -Name $SiteName -Port $ApiPort -PhysicalPath "$DeploymentPath\api" -ApplicationPool $AppPoolName
} else {
    Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name "PhysicalPath" -Value "$DeploymentPath\api"
    Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name "applicationPool" -Value $AppPoolName
}
Start-Website -Name $SiteName

# 4. Install & Configure Windows Service for Outbox & Cleanup Worker
$WorkerExePath = "$DeploymentPath\worker\Emcore.IdentityAccess.Worker.exe"
Write-Host "Managing Windows Service: $ServiceName..."

$ExistingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -ne $ExistingService) {
    Write-Host "Stopping existing Windows Service..."
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

Write-Host "Registering Windows Service..."
sc.exe create $ServiceName binPath= "\"$WorkerExePath\"" start= auto obj= "NT AUTHORITY\NetworkService" | Out-Null
sc.exe description $ServiceName "EMCORE Identity Outbox Relay and Security Cleanup Service" | Out-Null

# Configure service failure recovery (Restart automatically after 5s, 10s, 30s)
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null
Start-Service -Name $ServiceName

Write-Host ">>> EMCORE Identity Access deployment successful!" -ForegroundColor Green
Write-Host "Verify live health checks at: http://localhost:$ApiPort/health/ready"
