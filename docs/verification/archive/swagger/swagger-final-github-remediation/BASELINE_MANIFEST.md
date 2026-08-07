# Baseline Manifest

| File | Original SHA-256 | Purpose | Planned Change | Change Category | Runtime Behavior Risk |
|---|---|---|---|---|---|
| gateways/Emcore.ApiGateway.Tests/Emcore.ApiGateway.Tests.csproj | E7482580E914233122AB1E96CE96A6D7216872FCA051F513BE5E524AA52E0013 | Gateway integration tests project | Add Microsoft.AspNetCore.Mvc.Testing reference | Build dependency | None |
| building-blocks/Emcore.BuildingBlocks.Api/OpenApiExtensions.cs | 64CC4EA7F00C3C9CBE53AD23D80A49BAD97AEE8BBA2AC9E93D1B33C49186486B | Swagger extensions configuration | Remove path heuristics, fix response/idempotency generation | OpenAPI metadata | None |
| services/identity-access/src/Emcore.IdentityAccess.Api/Program.cs | 6D4A062536E9E308DA1CBC8C44319D594AB7C439CC7830D56AE666D12D104413 | Identity service endpoints | Add AllowAnonymous and RequireAuthorization | OpenAPI metadata | Low (aligning with intent) |
| gateways/Emcore.ApiGateway/Program.cs | 6CF547DAD73CEA3321449D7248F8F2E28107797C726C77C51CF84D44300DCEA7 | API Gateway configuration | Fix Prod Swagger Guard and registry prefixes | Production exposure guard | Low |
| tests/architecture/Emcore.OpenApi.Tests/ServiceOpenApiIntegrationTests.cs | DE3FEB711A05DE435BAE6E1CB99E85290FBDC2E53175AAF48E93626234B6CFC4 | OpenAPI unit tests | Update tests to assert new logic | Test | None |
| .github/workflows/pr-validation.yml | F71F3D67B9569A89AC035028C98E4AF093B01C59EEC0539F2F7660237DC13FA4 | PR CI Workflow | Run Contract Compatibility check | CI workflow | None |
| .github/workflows/main-validation.yml | 1341E5A70F24A9B13080BCF6E22FBF9EFFF8CB06216E54BCFB9AC0055D1F4324 | Main CI Workflow | Run Contract Compatibility check | CI workflow | None |
