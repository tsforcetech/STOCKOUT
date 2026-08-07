# Swagger Closure v4 Baseline Manifest

| File | SHA-256 | Purpose | Planned Change | Category | Runtime Behavior Impact |
|------|---------|---------|----------------|----------|-------------------------|
| gateways/Emcore.ApiGateway.Tests/Emcore.ApiGateway.Tests.csproj | 9D2EB186863D882F3CE8BE9219BDBE1AE1AD5993D0011FE44370D064184489EE | Gateway Integration Tests Project | Adjust package version if required | Test | NONE |
| Directory.Packages.props | F1331E58EE29B0C68B01A9F3471215FC1244F38A7800ED21AF5F98EC3C775A1E | Central Package Management | Adjust Microsoft.AspNetCore.Mvc.Testing version if required | Build | NONE |
| .github/workflows/main-validation.yml | 6C5238AC87B070C596DA1BCC3D1B1D8721E8D0111CD32100536DF30645AA7730 | CI Main Workflow | Add explicit test stages, upload artifacts | CI/CD | NONE |
| .github/workflows/pr-validation.yml | 3D05A49AF05ABBFF967AD1E90C2C7F5AB9EFA9EF8FBF7CE17F14D900B6D1877C | CI PR Workflow | Add explicit test stages, upload artifacts | CI/CD | NONE |
| building-blocks/Emcore.BuildingBlocks.Api/OpenApiExtensions.cs | 453DFB19829C60147D54267D870BB47BA20C7A6054B58C6D9355C0CE99699EB5 | OpenAPI Generator Configuration | Correct security claims, headers, and responses | Infrastructure | NONE |
| gateways/Emcore.ApiGateway/Program.cs | D95CAE44BD2DED31464BB6EB0BE5BD7A81D519D7E15CCC3FA2007895844BBED6 | API Gateway Pipeline | Secure Production documentation endpoints | Infrastructure | NONE |
| scripts/Check-OpenApiCompatibility.ps1 | 011D2248033765351A65884DACE27AF5E8567C6BD4C41067F5E0BB0A051361CE | Contract Compatibility Check | Fix exit codes, regex, and fail-closed logic | Tooling | NONE |
| tests/architecture/Emcore.OpenApi.Tests/ServiceOpenApiIntegrationTests.cs | 51CE0BFFD231EC5F3EA480A10148C9AC24357CBAFC568D12F8F7C26875DA025D | OpenAPI Architecture Tests | Fix tests causing CI to fail | Test | NONE |
| services/identity-access/src/Emcore.IdentityAccess.Api/Program.cs | 816E13BB9ABF74DF0032C846B2E830F40F5A4971C07B6F5167AB8C7E2B0724D4 | Identity API Configuration | Review for security audit | Audit | NONE |
