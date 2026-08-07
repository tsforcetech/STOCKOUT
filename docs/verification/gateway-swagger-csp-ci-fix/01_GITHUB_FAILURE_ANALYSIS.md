# GitHub Failure Analysis

- **Workflow Run Number**: Latest (Reproduced locally via format verification step)
- **Workflow Run URL**: (See repository Actions tab for Main Validation)
- **Failing Job**: Format Check / Format Verification
- **Failing Step**: `dotnet format Emcore.Platform.slnx --verify-no-changes`
- **Exact Command**: `dotnet format Emcore.Platform.slnx --verify-no-changes`
- **Exact Exit Code**: `1`
- **Exact Error Output**:
```
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.ApiGateway.Tests\GatewayTests.cs(453,1): error WHITESPACE: Fix whitespace formatting. Replace 18 characters with '\r\n\s\s\s\s\s\s\s\s'.
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.ApiGateway.Tests\GatewayTests.cs(456,1): error WHITESPACE: Fix whitespace formatting. Replace 18 characters with '\r\n\s\s\s\s\s\s\s\s'.
```
- **Skipped Downstream Steps**: Build, Test, Integration Tests, Contract Governance
