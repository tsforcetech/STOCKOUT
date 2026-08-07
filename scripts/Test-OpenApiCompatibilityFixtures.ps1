$ErrorActionPreference = "Stop"

$fixtureDir = "tests/fixtures/openapi-compatibility"
$baselineDir = "$fixtureDir/baseline"
$currentDir = "$fixtureDir/current"

if (Test-Path $fixtureDir) { Remove-Item -Recurse -Force $fixtureDir }
New-Item -ItemType Directory -Force -Path $baselineDir | Out-Null
New-Item -ItemType Directory -Force -Path $currentDir | Out-Null
New-Item -ItemType Directory -Force -Path "$baselineDir/dummy-service" | Out-Null
New-Item -ItemType Directory -Force -Path "$currentDir/dummy-service" | Out-Null

$baselineSpec = @{
    openapi = "3.0.1"
    paths = @{
        "/api/test" = @{
            get = @{
                summary = "Old summary"
                parameters = @(
                    @{ name = "oldParam"; in = "query"; required = $false; schema = @{ type = "string" } }
                )
                responses = @{
                    "200" = @{ description = "Success" }
                }
            }
        }
    }
} | ConvertTo-Json -Depth 10

$passingSpec = @{
    openapi = "3.0.1"
    paths = @{
        "/api/test" = @{
            get = @{
                summary = "New summary"
                parameters = @(
                    @{ name = "oldParam"; in = "query"; required = $false; schema = @{ type = "string" } }
                )
                responses = @{
                    "200" = @{ description = "Success changed" }
                }
            }
        }
        "/api/new" = @{
            post = @{
                summary = "New endpoint"
            }
        }
    }
} | ConvertTo-Json -Depth 10

$failingSpec = @{
    openapi = "3.0.1"
    paths = @{
        "/api/test" = @{
            get = @{
                summary = "New summary"
                parameters = @(
                    @{ name = "oldParam"; in = "query"; required = $true; schema = @{ type = "string" } }
                )
                responses = @{}
            }
        }
    }
} | ConvertTo-Json -Depth 10

Set-Content -Path "$baselineDir/dummy-service/openapi.json" -Value $baselineSpec

Write-Host "Running passing fixture..."
Set-Content -Path "$currentDir/dummy-service/openapi.json" -Value $passingSpec
$p = Start-Process -FilePath "powershell" -ArgumentList "-Command", "./scripts/Check-OpenApiCompatibility.ps1 -BaselineDir '$baselineDir' -CurrentDir '$currentDir'" -NoNewWindow -Wait -PassThru
if ($p.ExitCode -ne 0) {
    Write-Host "[FAIL] Passing fixture failed." -ForegroundColor Red
    exit 1
}

Write-Host "Running failing fixture..."
Set-Content -Path "$currentDir/dummy-service/openapi.json" -Value $failingSpec
$p = Start-Process -FilePath "powershell" -ArgumentList "-Command", "./scripts/Check-OpenApiCompatibility.ps1 -BaselineDir '$baselineDir' -CurrentDir '$currentDir'" -NoNewWindow -Wait -PassThru
if ($p.ExitCode -ne 1) {
    Write-Host "[FAIL] Failing fixture passed." -ForegroundColor Red
    exit 1
}

Write-Host "[SUCCESS] Compatibility fixtures verified script behavior correctly." -ForegroundColor Green
exit 0
