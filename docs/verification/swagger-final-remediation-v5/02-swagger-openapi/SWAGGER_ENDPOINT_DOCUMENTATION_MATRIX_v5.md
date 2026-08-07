# Swagger Endpoint Documentation Matrix v5
| Service | Method | Runtime Route | Gateway Route | Operation ID | Implementation Type | Runtime Auth Metadata | Gateway Auth Policy | Request Type | Success Responses | Error Responses | Rate Limit | Idempotency | OpenAPI Match | Notes |
|---------|--------|---------------|---------------|--------------|---------------------|-----------------------|---------------------|--------------|-------------------|-----------------|------------|-------------|---------------|-------|
| Identity | POST | /api/v1/auth/token | /api/v1/auth/{**catch-all} | PostApiV1AuthToken | Business API | AllowAnonymous | AuthPolicy | JSON | 200 | 400, 422, 500, 503 | No | No | Yes | Actual runtime behavior |
| Gateway | GET | /api/v1/swagger/registry | N/A | GetApiV1SwaggerRegistry | Framework | None | N/A | None | 200 | 500 | No | No | Yes | Internal framework |

*Note: Missing business operations are planned but not scaffolded.*
