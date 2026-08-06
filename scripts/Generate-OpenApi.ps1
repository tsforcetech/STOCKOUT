$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$exportPath = Join-Path $repoRoot "contracts\openapi"
$env:EMCORE_OPENAPI_EXPORT_PATH = $exportPath

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " EMCORE Platform - Automated OpenAPI Specification Generator     " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Target Export Path: $exportPath" -ForegroundColor Green

Write-Host "`n[1/2] Executing WebApplicationFactory OpenAPI generation tests..." -ForegroundColor Yellow
dotnet test "$repoRoot\tests\architecture\Emcore.OpenApi.Tests\Emcore.OpenApi.Tests.csproj" -c Release --filter "FullyQualifiedName~GenerateAndValidateOpenApiContract" --logger "console;verbosity=minimal"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: OpenAPI specification generation or validation failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "`n[2/2] Verifying generated contract documents..." -ForegroundColor Yellow
$generatedSpecs = Get-ChildItem -Path $exportPath -Filter "openapi.json" -Recurse
Write-Host "Successfully exported $($generatedSpecs.Count) specification files:" -ForegroundColor Green
foreach ($spec in $generatedSpecs) {
    $relPath = $spec.FullName.Substring($repoRoot.Length + 1)
    $sizeKb = [math]::Round($spec.Length / 1KB, 2)
    Write-Host "  -> $relPath ($sizeKb KB)" -ForegroundColor White
}

Write-Host "`nOpenAPI generation completed successfully!" -ForegroundColor Cyan
