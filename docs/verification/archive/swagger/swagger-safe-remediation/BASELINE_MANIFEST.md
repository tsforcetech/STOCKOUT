# EMCORE Swagger/OpenAPI — Safe Remediation Baseline Manifest

**Document Location:** `docs/verification/archive/swagger/swagger-safe-remediation/BASELINE_MANIFEST.md`  
**Purpose:** Cryptographic verification baseline and non-regression impact evaluation for implementation files targeted during safe remediation.  
**Created Timestamp:** 2026-08-06T18:38:00+05:30  

---

## Targeted Files Baseline Table

| Repository-Relative Path | Original SHA-256 Hash | File Purpose | Planned Change | Change Category | Runtime Behavior Impact | Risk Level |
| :--- | :--- | :--- | :--- | :--- | :---: | :---: |
| `gateways/Emcore.ApiGateway/Properties/launchSettings.json` | `2F3E55B17E62BEC24585D53B5802EA6125ECC6488D8C4CA328DD2E11C95ADCEE` | Direct Visual Studio Debug and development server launch profile configuration for Central API Gateway. | Change `applicationUrl` for HTTP profile from `5041` to `5000` to synchronize with operational startup scripts; add `launchUrl: swagger`. | Gateway launch profile correction | **No** | Low |
| `gateways/Emcore.ApiGateway/Program.cs` | `A9F7F561414267E02F6A292513DE32A4FF589D4F359724C5AFF58C171508A4C1` | Gateway application startup, reverse proxy pipeline setup, central Swagger UI mounting, and metadata registry definition. | Update anonymous registry metadata array to support `gatewayPrefixes: string[]` while retaining `gatewayPrefix`; enforce Production environment guards on `/api/v1/swagger/registry`. | Swagger registry metadata & Production exposure controls | **No** | Low |
| `building-blocks/Emcore.BuildingBlocks.Api/OpenApiExtensions.cs` | `E1A8DC075CE121954274F251D53160AC9EC7714BE20A0B9297C74A6576C320E7` | Core platform OpenAPI schema generation transformers, environment isolation guards, security descriptions, and UI customization. | Remove `Swagger:Enabled` from Production fallback; require explicit `OpenApi:EnableInProduction` flags; disable UI Try-It-Out in Production by default; remove enforced idempotency claims and universal Problem Details auto-injections; clarify multitenant header definitions. | Swagger/OpenAPI metadata & Production exposure controls | **No** | Low-Medium |
| `gateways/Emcore.ApiGateway.Tests/GatewayUrlVerificationTests.cs` | `C5B4C73BF974551C8FFE9CF2334D702DF3B00D41FA7A281778D350766AE6A5FF` | Unit and architecture verification test suite asserting YARP cluster configuration and launch profile integrity. | Add tests verifying default HTTP port is 5000 (`Gateway_LaunchSettings_DefaultHttpPort_Is_5000`), unique registry URLs/ServiceIDs, and presence of multiple gateway prefixes for Identity and User & Organization. | Gateway configuration tests | **No** | Zero |
| `tests/architecture/Emcore.OpenApi.Tests/ServiceOpenApiIntegrationTests.cs` | `1E6EAB97E4A99D4A4A71A77A4A2BC6AA8FBAA676DA452457CCD30E4BF88F1F4D` | Integration test harness executing real-time AST controller and endpoint coverage checks against generated OpenAPI documents. | Add tests asserting exact Identity routes, no synthetic downstream domain routes, unsupported headers absence, NoOp store idempotency metadata, rate-limit header precision, Production guards by default, and Try-It-Out server ingress resolution. | OpenAPI test additions | **No** | Zero |
| `tests/architecture/Emcore.OpenApi.Tests/TransformerUnitTests.cs` | `3ED6287EAC628AF7ACF81F76AD821E3214598B475D0253F6036DA8D0E88DEE80` | Unit test suite for building block OpenAPI document and schema transformers. | Extend unit tests to verify updated transformer behavior (no automatic 409/422 injections without actual response paths, explicit configuration guard evaluations). | OpenAPI test additions | **No** | Zero |

---

## Baseline Integrity Affirmation

1. **Exact File Copies Preserved:** True identical copies of all above files have been saved to `docs/verification/archive/swagger/swagger-safe-remediation/baseline/` with relative directory structures preserved.
2. **Zero Secrets Copied:** No private keys, connection strings, user-secrets, certificates, or production secrets exist in these source configuration or C# source files.
3. **Zero Business Behavior Changes Permitted:** Every planned change operates strictly within presentation documentation metadata, test suites, or safe development/production interface controls. No business domain logic, database queries, or runtime API route execution will be altered.

