# EMCORE Platform — Swagger Development & Local Environment Guide

**Target Audience:** EMCORE Platform Developers, QA Automation Engineers, and System Testers
**Prerequisites:** .NET 10.0 SDK, Windows / PowerShell 5.1+, or Core PowerShell (macOS/Linux).

---

## 1. Introduction & The Central Developer Portal

When developing or integrating against the EMCORE Platform locally, developers do not need to memorize individual microservice port numbers or manually manage concurrent terminal instances. The Central API Gateway (`Emcore.ApiGateway`), running on **Port 5000**, hosts a unified interactive Swagger Try-It-Out developer portal that aggregates OpenAPI contracts across all 16 background backend microservices and BFFs.

* **Central Portal Dashboard URL:** `http://localhost:5000/swagger`
* **Central Contract Registry JSON:** `http://localhost:5000/api/v1/swagger/registry`

---

## 2. Automated Startup & Shutdown Scripts

To effortlessly start up the entire EMCORE backend multi-process ecosystem or launch isolated service slices for fast iterative debugging, two dedicated automation scripts are provided in the repository repository `scripts/` directory:

### 2.1 Starting the Development Topology (`Start-Development-Swagger.ps1`)

This script cleans leftover orphan processes, compiles necessary binaries, launches requested microservices inside their correct project root working directories, monitors endpoint health liveness, starts the Central API Gateway, and outputs an operational test summary.

```powershell
# Launch the FULL platform (all 16 microservices + Central Gateway)
powershell -ExecutionPolicy Bypass -File .\scripts\Start-Development-Swagger.ps1

# Launch fast without rebuilding binaries (useful after clean builds)
powershell -ExecutionPolicy Bypass -File .\scripts\Start-Development-Swagger.ps1 -NoBuild

# Launch a targeted slice of backend services (e.g., identity + organizations + bidding only)
powershell -ExecutionPolicy Bypass -File .\scripts\Start-Development-Swagger.ps1 -ServiceFilter "identity-access,user-organization,bidding-deal"

# Execute automated multi-process validation test run and immediately shutdown cleanly
powershell -ExecutionPolicy Bypass -File .\scripts\Start-Development-Swagger.ps1 -NoBuild -TestRun
```

**Script Parameters:**
* `-ServiceFilter <String[]>`: Comma-delimited list of service keys to launch (defaults to all services).
* `-NoBuild <Switch>`: Skips solution build before startup to accelerate local loop iteration.
* `-TestRun <Switch>`: Performs an automated integration verification test of YARP proxy routing and cleanly terminates all spawned services upon completion.
* `-TimeoutSeconds <Int>`: Maximum duration to wait for endpoint liveness probes during startup (default: 30).
* `-Configuration <String>`: Build configuration profile (default: `Release`).

### 2.2 Stopping & Shutting Down (`Stop-Development-Swagger.ps1`)

To cleanly shut down all running background microservices, release port bindings, and wipe transient diagnostic logs:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Stop-Development-Swagger.ps1
```
This script terminates processes tracked via runtime PID files and performs orphan detection across all known EMCORE reserved HTTP ports (5000–5291), ensuring zero lingering memory footprints.

---

## 3. Navigating & Using the Central Swagger UI

1. Open your browser to `http://localhost:5000/swagger`.
2. In the top-right corner of the interface, locate the **Select a definition** dropdown menu.
3. Select any registered backend service (e.g., `EMCORE Identity & Access API`, `EMCORE Catalog & Listing API`, or `EMCORE Portal BFF`). The Gateway will dynamically fetch and render the selected OpenAPI contract via YARP reverse-proxying.

---

## 4. Testing Secure Endpoints with JWT Authentication

Many core operational endpoints require authenticated JWT Bearer tokens to test via "Try-It-Out":

1. **Obtain a Token:**
   * Within the Swagger portal, switch the dropdown definition to **EMCORE Identity & Access API**.
   * Expand the `POST /api/v1/auth/login` endpoint and click **Try it out**.
   * Provide valid test credentials in the request JSON body and click **Execute**.
   * Copy the returned JWT token string from the response payload (`accessToken` property).
2. **Authorize the Portal:**
   * At the top of the Swagger page, click the green **Authorize** icon button.
   * In the JWT Bearer input modal, paste your access token (if required by UI configuration, prefix with `Bearer <token>` or paste raw token string directly). Click **Authorize** and close the modal.
3. **Invoke Protected Operations:**
   * Switch to any target service definition in the top dropdown (e.g., `EMCORE Bidding & Deal API`).
   * When executing protected mutations, the Swagger interface will automatically inject the `Authorization: Bearer <token>` HTTP header into requests proxied cleanly through Port 5000.
