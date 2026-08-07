# OpenAPI Endpoint Coverage Report v5

Runtime source: EndpointDataSource snapshots
OpenAPI source: generated OpenAPI contracts
Gateway source: current YARP configuration
Comparison: independent normalized method+route set comparison

| Host | Host Type | Owned Business Ops | Owned Framework Ops | Proxy Infra Ops | OpenAPI Infra Ops | Runtime Documentable Total | OpenAPI Total | Missing | Unexpected | Route Mismatch | Method Mismatch | Schema Count | Classification |
|------|-----------|--------------------|---------------------|-----------------|-------------------|----------------------------|---------------|---------|------------|----------------|-----------------|--------------|----------------|
| emcore-api-gateway | GATEWAY | 0 | 4 | 18 | 22 | 5 | 5 | 0 | 0 | 0 | 0 | 1 | IMPLEMENTED |
| emcore-audit-reporting-api | BUSINESS_SERVICE | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | Business API implementation: NOT IMPLEMENTED |
| emcore-bidding-deal-api | BUSINESS_SERVICE | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | Business API implementation: NOT IMPLEMENTED |
| emcore-catalog-listing-api | BUSINESS_SERVICE | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | Business API implementation: NOT IMPLEMENTED |
| emcore-conversation-realtime-api | BUSINESS_SERVICE | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | Business API implementation: NOT IMPLEMENTED |
| emcore-identity-access-api | BUSINESS_SERVICE | 37 | 3 | 0 | 2 | 37 | 37 | 0 | 0 | 0 | 0 | 42 | IMPLEMENTED |
| emcore-inspection-trust-api | BUSINESS_SERVICE | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | Business API implementation: NOT IMPLEMENTED |
| emcore-inventory-media-api | BUSINESS_SERVICE | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | Business API implementation: NOT IMPLEMENTED |
| emcore-mcp-gateway | SPECIALIZED_GATEWAY | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | IMPLEMENTED |
| emcore-notification-integration-api | BUSINESS_SERVICE | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | Business API implementation: NOT IMPLEMENTED |
| emcore-portal-bff | BFF | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | IMPLEMENTED |
| emcore-public-bff | BFF | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | IMPLEMENTED |
| emcore-realtime-gateway | SPECIALIZED_GATEWAY | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | IMPLEMENTED |
| emcore-search-discovery-api | BUSINESS_SERVICE | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | Business API implementation: NOT IMPLEMENTED |
| emcore-subscription-payment-api | BUSINESS_SERVICE | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | Business API implementation: NOT IMPLEMENTED |
| emcore-user-organization-api | BUSINESS_SERVICE | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | Business API implementation: NOT IMPLEMENTED |
| emcore-workflow-scheduler-api | BUSINESS_SERVICE | 0 | 3 | 0 | 2 | 3 | 3 | 0 | 0 | 0 | 0 | 1 | Business API implementation: NOT IMPLEMENTED |

## Grand Totals
- Runtime Documentable Operations: 87
- OpenAPI Documented Operations: 87
- Total Missing: 0
- Total Unexpected: 0
- Total Route Mismatch: 0
- Total Method Mismatch: 0
- Scaffold host count: 11

### Scaffold Hosts
- emcore-audit-reporting-api
- emcore-bidding-deal-api
- emcore-catalog-listing-api
- emcore-conversation-realtime-api
- emcore-inspection-trust-api
- emcore-inventory-media-api
- emcore-notification-integration-api
- emcore-search-discovery-api
- emcore-subscription-payment-api
- emcore-user-organization-api
- emcore-workflow-scheduler-api
