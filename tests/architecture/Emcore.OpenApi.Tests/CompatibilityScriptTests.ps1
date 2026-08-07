$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "..\..\..\scripts\Check-OpenApiCompatibility.ps1"
$testBaseDir = Join-Path $PSScriptRoot "TestData\Base"
$testCurrDir = Join-Path $PSScriptRoot "TestData\Curr"

# Ensure test directories exist and are clean
if (Test-Path $testBaseDir) { Remove-Item -Recurse -Force $testBaseDir }
if (Test-Path $testCurrDir) { Remove-Item -Recurse -Force $testCurrDir }
New-Item -ItemType Directory -Force $testBaseDir | Out-Null
New-Item -ItemType Directory -Force $testCurrDir | Out-Null

$baseJson = @{
    openapi = "3.0.1"
    info = @{ title = "API"; version = "v1" }
    paths = @{
        "/test" = @{
            get = @{
                operationId = "GetTest"
                responses = @{
                    "200" = @{ description = "OK" }
                }
            }
        }
    }
} | ConvertTo-Json -Depth 10

$currJson = @{
    openapi = "3.0.1"
    info = @{ title = "API"; version = "v1" }
    paths = @{
        "/test" = @{
            get = @{
                operationId = "GetTest"
                responses = @{
                    "400" = @{ description = "Bad Request" } # 200 removed
                }
            }
        }
    }
} | ConvertTo-Json -Depth 10

$serviceBaseDir = Join-Path $testBaseDir "test-service\v1"
$serviceCurrDir = Join-Path $testCurrDir "test-service\v1"
New-Item -ItemType Directory -Force $serviceBaseDir | Out-Null
New-Item -ItemType Directory -Force $serviceCurrDir | Out-Null

Set-Content -Path (Join-Path $serviceBaseDir "openapi.json") -Value $baseJson
Set-Content -Path (Join-Path $serviceCurrDir "openapi.json") -Value $currJson

Write-Host "Running Test 1: Should fail due to missing baseline and no -EstablishBaseline flag"
try {
    & pwsh -NoProfile -Command "& '$scriptPath' -BaselineDir 'MissingDir' -CurrentDir '$testCurrDir'; exit `$LASTEXITCODE"
    if ($LASTEXITCODE -ne 1) { throw "Expected exit code 1, but got $LASTEXITCODE" }
    Write-Host "Test 1 Passed." -ForegroundColor Green
} catch {
    throw "Test 1 Failed: $_"
}

Write-Host "Running Test 2: Should pass and establish baseline when -EstablishBaseline is specified"
$newBaseDir = Join-Path $PSScriptRoot "TestData\NewBase"
if (Test-Path $newBaseDir) { Remove-Item -Recurse -Force $newBaseDir }
try {
    & pwsh -NoProfile -Command "& '$scriptPath' -BaselineDir '$newBaseDir' -CurrentDir '$testCurrDir' -EstablishBaseline; exit `$LASTEXITCODE"
    if ($LASTEXITCODE -ne 0) { throw "Expected exit code 0, but got $LASTEXITCODE" }
    if (-not (Test-Path $newBaseDir)) { throw "Baseline directory was not created." }
    Write-Host "Test 2 Passed." -ForegroundColor Green
} catch {
    throw "Test 2 Failed: $_"
}

Write-Host "Running Test 3: Should fail due to breaking change (200 OK removed)"
try {
    & pwsh -NoProfile -Command "& '$scriptPath' -BaselineDir '$testBaseDir' -CurrentDir '$testCurrDir'; exit `$LASTEXITCODE"
    if ($LASTEXITCODE -ne 1) { throw "Expected exit code 1, but got $LASTEXITCODE" }
    Write-Host "Test 3 Passed." -ForegroundColor Green
} catch {
    throw "Test 3 Failed: $_"
}

Write-Host "Running Test 4: Should pass when breaking change is ignored via -AllowBreakingChanges"
try {
    & pwsh -NoProfile -Command "& '$scriptPath' -BaselineDir '$testBaseDir' -CurrentDir '$testCurrDir' -AllowBreakingChanges; exit `$LASTEXITCODE"
    if ($LASTEXITCODE -ne 0) { throw "Expected exit code 0, but got $LASTEXITCODE" }
    Write-Host "Test 4 Passed." -ForegroundColor Green
} catch {
    throw "Test 4 Failed: $_"
}

Write-Host "All tests passed successfully!" -ForegroundColor Green
