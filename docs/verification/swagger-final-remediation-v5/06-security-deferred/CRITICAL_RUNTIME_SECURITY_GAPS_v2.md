# Critical Runtime Security Gaps v2

| Route | Intended Security | Gateway Policy | Service Auth Metadata | Principal Validation | Current Exposure |
|-------|-------------------|----------------|-----------------------|----------------------|------------------|
| /api/v1/auth/{**catch-all} | PROTECTED AND ENFORCED | PublicPolicy | AllowAnonymous/Authorize | Mixed | PUBLIC BY DESIGN (login) / INTENDED PROTECTED BUT NOT ENFORCED (sessions) |

## Details
- **X-User-Id Behavior**: Trust behavior requires redesign.
- **Gateway /auth policy**: No real JWT validation in Gateway for production.
- **Severity**: CRITICAL
- **Required Approval**: Architecture team must approve new identity extraction logic.
