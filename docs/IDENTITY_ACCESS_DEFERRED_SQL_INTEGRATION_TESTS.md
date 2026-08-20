# Identity Access Deferred SQL Integration Tests

## STATUS
DEFERRED

## REASON
SQL-backed security/concurrency acceptance tests require an accessible integration SQL Server. Current coding-agent environment does not have a reliable integration database.

## PRODUCTION IMPLEMENTATION STATUS
Completed and merged through PR #10.

## DEFERRED TESTS

- verification five-attempt SQL enforcement
- verification replay using real SQL
- verification concurrent consume
- verification resend invalidation using real SQL
- password-reset token-only SQL flow
- expired reset token using real SQL
- reset-token replay using real SQL
- concurrent reset-token consumption
- session revocation SQL verification
- refresh-token revocation SQL verification
- stored-procedure result-code integration verification

## EXECUTION STAGE
Run these tests during IdentityAccess integration/deployment acceptance when EMCORE_IDENTITY_DB development/integration SQL Server is available.

## IMPORTANT
These are deferred tests, not skipped security requirements.
