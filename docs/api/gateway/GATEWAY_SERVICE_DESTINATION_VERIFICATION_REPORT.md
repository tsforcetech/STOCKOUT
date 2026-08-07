# EMCORE Platform — Gateway Service Destination & Route Reconciliation Report

**Executive Summary:**
A critical platform route disparity was identified wherein the Identity & Access API target destination configured within `Emcore.ApiGateway` diverged from the actual runtime HTTP port bound when executing `Emcore.IdentityAccess.Api` under Visual Studio / Debug launch profiles. This document records the complete systematic audit, remediation, and automated CI lock implemented across all 16 microservice clusters to eliminate routing failures and align local development configuration.

---

## 1. Root Cause Analysis & The Critical Routing Defect

During multi-service execution, invocations routed through `Emcore.ApiGateway` targeting Identity & Access endpoints resulted in HTTP 502 (Bad Gateway) or 404 (Unmatched Route) failures. 

* **Defect Identification:** `Emcore.ApiGateway/appsettings.Development.json` specified arbitrary or outdated localhost ports (e.g., placeholder or non-matching ports), whereas `Emcore.IdentityAccess.Api/Properties/launchSettings.json` explicitly defined the Debug binding URL as `http://localhost:5194`.
* **Systemic Scope:** An immediate architecture audit revealed that while production deployments leverage internal Kubernetes / Aspire container DNS discovery, local developer debugging under **Option B (Configuration-Driven Static Development URLs)** lacked strict synchronization between Gateway cluster destinations and domain API debug launch profiles.

---

## 2. Route & Port Reconciliation Audit

A rigorous verification audit was conducted across every HTTP API host in the EMCORE Platform. Each `launchSettings.json` profile was parsed to extract the exact HTTP profile port, which was subsequently updated into `Emcore.ApiGateway/appsettings.Development.json` under `ReverseProxy:Clusters:<ClusterId>:Destinations:<DestinationId>:Address`.

| Cluster Identifier | Target Service Name | Confirmed Debug Port (`launchSettings.json`) | Reconciled Gateway Cluster Destination Address | Reconciliation Status |
| :--- | :--- | :---: | :--- | :---: |
| `identity-access-cluster` | Identity & Access API | **5194** | `http://localhost:5194` | **RECONCILED & LOCKED** |
| `user-organization-cluster` | User & Organization API | **5291** | `http://localhost:5291` | **RECONCILED & LOCKED** |
| `catalog-listing-cluster` | Catalog & Listing API | **5072** | `http://localhost:5072` | **RECONCILED & LOCKED** |
| `inventory-media-cluster` | Inventory & Media API | **5079** | `http://localhost:5079` | **RECONCILED & LOCKED** |
| `search-discovery-cluster` | Search & Discovery API | **5255** | `http://localhost:5255` | **RECONCILED & LOCKED** |
| `bidding-deal-cluster` | Bidding & Deal API | **5186** | `http://localhost:5186` | **RECONCILED & LOCKED** |
| `inspection-trust-cluster` | Inspection & Trust API | **5283** | `http://localhost:5283` | **RECONCILED & LOCKED** |
| `subscription-payment-cluster`| Subscription & Payment API | **5091** | `http://localhost:5091` | **RECONCILED & LOCKED** |
| `conversation-realtime-cluster`| Conversation & Realtime API | **5208** | `http://localhost:5208` | **RECONCILED & LOCKED** |
| `notification-integration-cluster`| Notification & Integration API| **5201** | `http://localhost:5201` | **RECONCILED & LOCKED** |
| `workflow-scheduler-cluster` | Workflow & Scheduler API | **5266** | `http://localhost:5266` | **RECONCILED & LOCKED** |
| `audit-reporting-cluster` | Audit & Reporting API | **5003** | `http://localhost:5003` | **RECONCILED & LOCKED** |
| `public-bff-cluster` | Public BFF Gateway | **5005** | `http://localhost:5005` | **RECONCILED & LOCKED** |
| `portal-bff-cluster` | Portal BFF Gateway | **5127** | `http://localhost:5127` | **RECONCILED & LOCKED** |
| `mcp-gateway-cluster` | MCP Gateway | **5055** | `http://localhost:5055` | **RECONCILED & LOCKED** |
| `realtime-gateway-cluster` | Realtime Gateway | **5225** | `http://localhost:5225` | **RECONCILED & LOCKED** |

---

## 3. Automated CI Enforcement (`GatewayUrlVerificationTests`)

To guarantee that future development never re-introduces destination drift, an automated test suite was developed within `Emcore.ApiGateway.Tests.GatewayUrlVerificationTests.cs`:

1. **`Gateway_IdentityCluster_Destination_Matches_IdentityLaunchSettings`**: Explicitly tests the core defect scenario by parsing `Emcore.IdentityAccess.Api` launch settings and asserting absolute alignment with `identity-access-cluster` in Gateway configuration.
2. **`Gateway_AllDownstreamClusters_Match_DiscoveredLaunchSettings`**: Automatically parses all 16 microservice and BFF project directories, extracts debug ports, and validates that every configured Gateway destination address corresponds precisely to its target launch setting.
3. **`Gateway_SwaggerProxyRoutes_Match_KnownClusters`**: Confirms that every OpenAPI reverse-proxy route (`swagger-<service-key>`) maps to an existing, valid destination cluster in YARP.

**Test Verification Output:**
```
Test run for Emcore.ApiGateway.Tests.dll (.NETCoreApp,Version=v10.0)
Passed!  - Failed:     0, Passed:    36, Skipped:     0, Total:    36, Duration: 11 s
```

---

## 4. Architectural Sign-off

With route reconciliation completed and guarded by strict CI automated verification tests, local development routing through `http://localhost:5000` is guaranteed to match individual microservice Debug behavior across all IDE launch mechanisms.
