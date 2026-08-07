# Baseline Information
- Repository root: C:\DEV\API PROJECT\STOCKOUT
- Current branch: fix/swagger-final-remediation
- Current Git SHA: df271ffa218574846e4f550ea887760654c0d350
- Latest commit message: Fix Swagger Proxy production guard tests
- OS: Microsoft Windows 11 Pro
- .NET SDK version: 10.0.302
- PowerShell version: 5.1.26100.8875
- Current GitHub PR URL: (Not specified / local environment)
- Current GitHub Actions run URL: (Not specified / local environment)
- Current failing step: dotnet format --verify-no-changes
- Current workflow status: failing

## Files Expected to Change

| File | Original SHA-256 | Purpose | Planned Change | Category | Business Behavior Impact |
|------|-------------------|---------|----------------|----------|---------------------------|
| building-blocks/Emcore.BuildingBlocks.Api/OpenApiExtensions.cs | DF7E430BD9E8FC74BF53FB77B5734B1342A178B1DABCF321412BDF6FD36C8D8B | Formatting fix | Format | Code Formatting | NONE |
| building-blocks/Emcore.BuildingBlocks.Core/Types.cs | 83E25C21A00498EF6AE18E6D51B17A9067AC5E3E7CF1175A21B721B93623A35C | Formatting fix | Format | Code Formatting | NONE |
| gateways/Emcore.ApiGateway.Tests/Fixtures/GatewayTestFixture.cs | 951D85883DEEDB3EDE9A41DA3BFD6A1163FDA9BAD9A0D2AF504C71E59E8C8EEE | Formatting fix | Format | Code Formatting | NONE |
| gateways/Emcore.ApiGateway.Tests/GatewayTests.cs | FBEAEAF2C1F17CC03F8EC058C3A474BEAC6E4B233C3B611E8633924A015EA7F4 | Formatting fix | Format | Code Formatting | NONE |
| gateways/Emcore.ApiGateway.Tests/GatewayUrlVerificationTests.cs | B88F3E955BA027F3CDF43904B0464230E31CD2E035ADF0A3D970B0734F22B84C | Formatting fix | Format | Code Formatting | NONE |
| gateways/Emcore.ApiGateway/Extensions/GatewayExtensions.cs | E75BBB2FF2B8C9490583B0BF8DFE0B27F681542F7B58708CEF498D08C387AB3B | Formatting fix | Format | Code Formatting | NONE |
| gateways/Emcore.ApiGateway/Middleware/HeaderManagementMiddleware.cs | D6F3A29378161E31D4DF19ECD8D16D714A919DD101E144DAA4C7E115A05EDE52 | Formatting fix | Format | Code Formatting | NONE |
| gateways/Emcore.ApiGateway/Middleware/StructuredLoggingMiddleware.cs | 79B3F0143727E0A00E3B7914FF7080C48250258B2F38AD02E863520003571F9F | Formatting fix | Format | Code Formatting | NONE |
| gateways/Emcore.ApiGateway/Program.cs | ED43652786731EE01D45594C8C1A9708CDB8AB38A1A4332E61BBD4F607AC7CBE | Formatting fix | Format | Code Formatting | NONE |
| services/identity-access/Emcore.IdentityAccess.Migrator/Program.cs | 10DAA86A52E799573F085A98116C6F6D8204A21F5F52B1E5595BB6B9261F77A0 | Formatting fix | Format | Code Formatting | NONE |
| services/identity-access/src/Emcore.IdentityAccess.Api/Program.cs | 8974B40D20859FFEBC6BB81E03DE83F0F89F78DBCF875B8498FEAF611416B0E9 | Formatting fix | Format | Code Formatting | NONE |
| services/identity-access/src/Emcore.IdentityAccess.Application/Abstractions/Interfaces.cs | DA4ACA6A81647CAD0BE246E2D7A965CD4629E316CA8BF26B11002D86592AD72F | Formatting fix | Format | Code Formatting | NONE |
| services/identity-access/src/Emcore.IdentityAccess.Application/Commands/Handlers.cs | 20E2BA4A0E0D34D2BFBF34D4083ADE80A87E2026A7DA8AB684927C4A365D6966 | Formatting fix | Format | Code Formatting | NONE |
| services/identity-access/src/Emcore.IdentityAccess.Domain/Entities/Entities.cs | C11ED33A89D46FB0921BB34F31CEBDBEE9EB8063C0F0B9F091761B405F1B0DB9 | Formatting fix | Format | Code Formatting | NONE |
| services/identity-access/src/Emcore.IdentityAccess.Infrastructure/Security/SecurityServices.cs | 8FC81B1064849228C8223A70908AFFCD35A5D17693D9FB2D4CCE22305045C3A6 | Formatting fix | Format | Code Formatting | NONE |
| services/identity-access/src/Emcore.IdentityAccess.Worker/Program.cs | D4652DC0DEAAD566220FEB7F0DC3BDB5308B9220C912B9D70702C766A5E2B036 | Formatting fix | Format | Code Formatting | NONE |
| tests/architecture/Emcore.OpenApi.Tests/ServiceOpenApiIntegrationTests.cs | 212512F3EA15E0E21D5FE060B77BF48E7F0E46FD61D52346BF76145BE51DF5A7 | Formatting fix | Format | Code Formatting | NONE |
| tests/architecture/Emcore.OpenApi.Tests/TransformerUnitTests.cs | 3C0EAE0F5868957A02CF8CEC02DBCD0A3AB5E8EBF129905CC548521E30F44C56 | Formatting fix | Format | Code Formatting | NONE |
| scripts/Check-OpenApiCompatibility.ps1 | 2C7851F06C36CFE73DF97DD1598EE8A1CEE057E9D5CB23F7B1E3D0EC78120B06 | Fix pipeline and package versions | Modify | CI/Build | NONE |
| .github/workflows/pr-validation.yml | 9A41FF69DE3A65B1A1337F8EA848BC000488E78B231320ACA6A8C70525C76B75 | Fix pipeline and package versions | Modify | CI/Build | NONE |
| .github/workflows/main-validation.yml | 5E941538E056F60F0FE6696A558C4CC80AA2B6A5A29DC08367E1277DA869DA9E | Fix pipeline and package versions | Modify | CI/Build | NONE |
| Directory.Packages.props | F1331E58EE29B0C68B01A9F3471215FC1244F38A7800ED21AF5F98EC3C775A1E | Fix pipeline and package versions | Modify | CI/Build | NONE |
