# EMCORE Identity & Access — Verification Evidence & Artifact Index

**Verification Directory**: `docs/verification/identity-access-final/`  
**Generated Timestamp**: `2026-08-05T21:25:00+05:30`  
**Purpose**: Central cryptographic manifest indexing every documentary evidence report, test execution log, database validation log, and automated TRX test run associated with the EMCORE Identity & Access acceptance audit.

---

## 1. Verified Primary Audit Reports & Logs

| Evidence File Name & Relative Path | Description | Source Command / Generation Mechanism | Creation Time (UTC) | SHA-256 Checksum |
|---|---|---|---|---|
| `IDENTITY_ACCESS_FINAL_VERIFICATION_REPORT.md` | Primary executive acceptance score sheet & verification matrix | Direct inspection of source architecture & test runs | 2026-08-05 15:51:33Z | `33C078DD20700D1F629A9800D6F56F608A1C2034D47619A09B622BD71BA1E845` |
| `IDENTITY_ACCESS_TEST_EXECUTION_REPORT.md` | Complete automated test suite breakdown & execution statistics | Synthesis of automated test TRX logs & regression pass | 2026-08-05 15:52:07Z | `1AE9EDB9797589F36931BC30FE4D46A232F8D5CF7DA145C0560B7C169B316307` |
| `IDENTITY_ACCESS_SECURITY_VERIFICATION_RESULTS.md` | Comprehensive security analysis (PBKDF2, OTP HMAC, MFA, JWT, M2M) | Static security repository scan & code review | 2026-08-05 15:52:41Z | `67DC575D0EC93FCE03FB63AE3DA503191A0ABE20481E7C0CE482B5D70EF5BBF5` |
| `IDENTITY_ACCESS_DATABASE_VERIFICATION_RESULTS.md` | Inventory of database tables, stored procedures, & dirty read safety | T-SQL inspection & migrator dry-run validation | 2026-08-05 15:53:11Z | `AB070A93514E371AA6D34391E1114032FCD9C37B0C8A26F8D5276BA6B9F1C034` |
| `IDENTITY_ACCESS_API_GATEWAY_VERIFICATION_RESULTS.md`| Route matrix, RFC 7807 compliance, and header propagation proof | API minimal routing code & gateway test verification | 2026-08-05 15:53:37Z | `734797ED0004F18C2B8586703804B0258DC3D93409A3CA320D02ADD15AB152A0` |
| `IDENTITY_ACCESS_DEPLOYMENT_VERIFICATION_RESULTS.md` | IIS web.config readiness, Windows Service script checks, health endpoints | Infrastructure deployment analysis & script inspection | 2026-08-05 15:53:59Z | `DF5AD0E66B7731F2D1FF55DF36A03F5775960310ABBA950F481FF6965FB45733` |
| `IDENTITY_ACCESS_CONFIGURATION_VERIFICATION_RESULTS.md`| Configuration parameter matrix, secret treatment, & negative start tests | Application DI review & startup exception proofs | 2026-08-05 15:54:26Z | `6E0AB3882C77A3D16609122190F64713814EECBA543535A7EFE936913DCE3FAE` |
| `sql_migrator_validation.log` | Raw output log from running SQL migrator validate & dry-run operations | `dotnet run ... --validate; dotnet run ... --dry-run` | 2026-08-05 15:50:43Z | `E19C9DC8F0B15681DCB20BA9333947469067D3E60FF46482002093BC7AED9B9B` |

---

## 2. Automated Test Suite Execution TRX Evidence Logs

| TRX Test Log Name & Relative Path | Target Test Project | Tests Executed & Status | Source Execution Command | Creation Time (UTC) | SHA-256 Checksum |
|---|---|---|---|---|---|
| `test-results/identity-unit-tests.trx` | `Emcore.IdentityAccess.UnitTests.csproj` | 18 Total, 18 Passed, 0 Failed | `dotnet test ... --logger "trx;LogFileName=identity-unit-tests.trx"` | 2026-08-05 15:42:24Z | `F759054E8343A81C225DC7F35D1C12DDDD6C8668F040CB127D754086FA752FB6` |
| `test-results/identity-integration-tests.trx` | `Emcore.IdentityAccess.IntegrationTests.csproj`| 6 Total, 6 Passed, 0 Failed | `dotnet test ... --logger "trx;LogFileName=identity-integration-tests.trx"`| 2026-08-05 15:42:34Z | `5B73E4E847BD15AB12629CFA3698BAB265DCCF3D8648B057FDB19440D809CBE5` |
| `test-results/identity-architecture-tests.trx` | `Emcore.IdentityAccess.ArchitectureTests.csproj`| 5 Total, 5 Passed, 0 Failed | `dotnet test ... --logger "trx;LogFileName=identity-architecture-tests.trx"`|2026-08-05 15:42:39Z | `039B11723A34C9EABE49EB23EA51DBE8FB9DA121BF1670EC1A4B0EF55E4B1CBD` |
| `test-results/identity-gateway-tests.trx` | `Emcore.ApiGateway.Tests.csproj` | 16 Total, 16 Passed, 0 Failed | `dotnet test ... --logger "trx;LogFileName=identity-gateway-tests.trx"` | 2026-08-05 15:42:53Z | `732CBEDE287F8A937A763215AF2A32BD4F1B6443B401D397E166D07EB95B34C8` |
| `test-results/regression/*.trx` | Entire solution (`Emcore.Platform.slnx` 28 test suites) | 122 Total, 122 Passed, 0 Failed | `dotnet test Emcore.Platform.slnx --logger "trx"` | 2026-08-05 15:43:52Z | 28 individual project TRX reports preserved in `regression/`. |
