# Swagger Test Evidence v5

- Branch: fix/swagger-final-remediation
- Git SHA: 17efe47

## Formatting
- Command: dotnet format Emcore.Platform.slnx --verify-no-changes
- Start Timestamp: 2026-08-07T13:25:01+05:30
- End Timestamp: 2026-08-07T13:26:54+05:30
- Exit Code: 0
- Passed: N/A
- Failed: N/A
- Skipped: N/A
- Duration: 1m 53s
- TRX Path: N/A

## Restore
- Command: dotnet restore Emcore.Platform.slnx
- Start Timestamp: 2026-08-07T13:02:05+05:30
- End Timestamp: 2026-08-07T13:03:17+05:30
- Exit Code: 0
- Passed: N/A
- Failed: N/A
- Skipped: N/A
- Duration: 1m 12s
- TRX Path: N/A

## Build
- Command: dotnet build Emcore.Platform.slnx --configuration Release
- Start Timestamp: 2026-08-07T13:26:59+05:30
- End Timestamp: 2026-08-07T13:28:50+05:30
- Exit Code: 0
- Passed: N/A
- Failed: N/A
- Skipped: N/A
- Duration: 1m 51s
- TRX Path: N/A

## OpenAPI Tests
- Command: dotnet test tests/architecture/Emcore.OpenApi.Tests/Emcore.OpenApi.Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=openapi-final-closure.trx"
- Start Timestamp: 2026-08-07T13:28:56+05:30
- End Timestamp: 2026-08-07T13:29:05+05:30
- Exit Code: 0
- Passed: 23
- Failed: 0
- Skipped: 0
- Duration: 9s
- TRX Path: C:\DEV\API PROJECT\STOCKOUT\tests\architecture\Emcore.OpenApi.Tests\TestResults\openapi-final-closure.trx

## Gateway Tests
- Command: dotnet test gateways/Emcore.ApiGateway.Tests/Emcore.ApiGateway.Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=gateway-final-closure.trx"
- Start Timestamp: 2026-08-07T13:29:13+05:30
- End Timestamp: 2026-08-07T13:29:28+05:30
- Exit Code: 0
- Passed: 43
- Failed: 0
- Skipped: 0
- Duration: 15s
- TRX Path: C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.ApiGateway.Tests\TestResults\gateway-final-closure.trx

## Full Regression
- Command: dotnet test Emcore.Platform.slnx --configuration Release --no-build --logger "trx;LogFileName=full-regression-final-closure.trx"
- Start Timestamp: 2026-08-07T13:29:34+05:30
- End Timestamp: 2026-08-07T13:31:02+05:30
- Exit Code: 0
- Passed: 23 (OpenAPI) + 5 (Workflow) + 43 (Gateway) = 71 (total shown in logs for matching assemblies)
- Failed: 0
- Skipped: 0
- Duration: 1m 28s
- TRX Path: Multiple (e.g., tests/architecture/Emcore.OpenApi.Tests/TestResults/full-regression-final-closure.trx)

## OpenAPI Generation
- Command: ./scripts/Generate-OpenApi.sh
- Start Timestamp: 2026-08-07T13:31:08+05:30
- End Timestamp: 2026-08-07T13:31:09+05:30
- Exit Code: 0
- Passed: N/A
- Failed: N/A
- Skipped: N/A
- Duration: 1s
- TRX Path: N/A

## Contract Compatibility Validation
- Command: ./scripts/Check-OpenApiCompatibility.ps1 -BaselineDir "docs/verification/swagger-final-remediation-v5/03-contract-governance/baseline-contracts" -CurrentDir "contracts/openapi"
- Start Timestamp: 2026-08-07T13:31:15+05:30
- End Timestamp: 2026-08-07T13:31:16+05:30
- Exit Code: 0
- Passed: 17 files verified
- Failed: 0 breaking changes
- Skipped: 0
- Duration: 1s
- TRX Path: N/A

## Compatibility Fixtures
- Command: ./scripts/Test-OpenApiCompatibilityFixtures.ps1
- Start Timestamp: 2026-08-07T13:31:24+05:30
- End Timestamp: 2026-08-07T13:31:27+05:30
- Exit Code: 0
- Passed: 2 fixtures verified
- Failed: 0
- Skipped: 0
- Duration: 3s
- TRX Path: N/A

## Live Development Verification
- Command: N/A
- Start Timestamp: N/A
- End Timestamp: N/A
- Exit Code: N/A
- Passed: N/A
- Failed: N/A
- Skipped: N/A
- Duration: N/A
- TRX Path: N/A
- Result: BLOCKED (SQL SERVER CONNECTION STRING REQUIRED - ConnectionStrings:IdentityDatabase)
