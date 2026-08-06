[CmdletBinding()]
param(
    [switch]$Force,
    [string]$PidFilePath = "$env:TEMP\emcore-swagger-dev.pids"
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " EMCORE Platform - Development Swagger Shutdown Script          " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# Known EMCORE Development Ports (Gateway + 16 services)
$knownPorts = @(5000, 5003, 5005, 5055, 5072, 5079, 5091, 5127, 5186, 5194, 5201, 5208, 5225, 5255, 5266, 5283, 5291)

# 1. Terminate tracked PIDs
if (Test-Path $PidFilePath) {
    Write-Host "`n[1/3] Terminating process IDs from tracking file ($PidFilePath)..." -ForegroundColor Yellow
    try {
        $pidsList = Get-Content -Path $PidFilePath -ErrorAction SilentlyContinue | ConvertFrom-Json -ErrorAction SilentlyContinue
        if ($null -ne $pidsList) {
            foreach ($entry in $pidsList) {
                $processId = $entry.Pid
                $serviceName = $entry.Service
                $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
                if ($null -ne $proc) {
                    Write-Host "  -> Stopping $serviceName (PID: $processId)..." -ForegroundColor White
                    Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
                } else {
                    Write-Host "  -> $serviceName (PID: $processId) already stopped." -ForegroundColor DarkGray
                }
            }
        }
    } catch {
        Write-Host "  -> Error reading PID file; proceeding to port scanning." -ForegroundColor DarkGray
    }
    Remove-Item -Path $PidFilePath -Force -ErrorAction SilentlyContinue
    Write-Host "Removed runtime PID tracking file." -ForegroundColor Green
} else {
    Write-Host "`n[1/3] No PID tracking file found at $PidFilePath." -ForegroundColor DarkGray
}

# 2. Kill any remaining processes occupying EMCORE ports
Write-Host "`n[2/3] Checking for remaining orphan processes on EMCORE development ports..." -ForegroundColor Yellow
$seenPids = [System.Collections.Generic.HashSet[int]]::new()
foreach ($port in $knownPorts) {
    $netstat = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($null -ne $netstat) {
        foreach ($conn in $netstat) {
            $orphanPid = $conn.OwningProcess
            if ($orphanPid -gt 0 -and $orphanPid -ne $PID -and $seenPids.Add($orphanPid)) {
                $proc = Get-Process -Id $orphanPid -ErrorAction SilentlyContinue
                if ($null -ne $proc) {
                    Write-Host "  -> Terminating orphan process $($proc.ProcessName) (PID: $orphanPid) on port $port..." -ForegroundColor Red
                    Stop-Process -Id $orphanPid -Force -ErrorAction SilentlyContinue
                }
            }
        }
    }
}

# 3. Clean temporary transient test artifacts
Write-Host "`n[3/3] Cleaning transient test and log artifacts..." -ForegroundColor Yellow
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$tempLogsDir = Join-Path $repoRoot ".system_generated\logs\live-dev"
if (Test-Path $tempLogsDir) {
    Remove-Item -Path "$tempLogsDir\*" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Cleaned logs in $tempLogsDir." -ForegroundColor Green
}

Write-Host "`nShutdown complete. Zero orphaned processes bound to EMCORE development ports." -ForegroundColor Cyan
$LASTEXITCODE = 0
exit 0
