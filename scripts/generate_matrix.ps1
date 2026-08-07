$ErrorActionPreference = 'Stop'

$endpointsDir = 'contracts/endpoints'
$contractsDir = 'contracts/openapi'
$matrixFile = 'docs/verification/swagger-final-remediation-v5/02-swagger-openapi/SWAGGER_ENDPOINT_DOCUMENTATION_MATRIX_v5.md'
$coverageFile = 'docs/verification/swagger-final-remediation-v5/02-swagger-openapi/OPENAPI_ENDPOINT_COVERAGE_REPORT_v5.md'

$matrixHeader = @"
# Swagger Endpoint Documentation Matrix v5

| Service | Method | Runtime Route | Gateway Route | Operation ID | Implementation Type | Runtime Auth Metadata | Gateway Auth Policy | Request Type | Success Responses | Error Responses | Rate Limit | Idempotency | OpenAPI Match | Notes |
|---------|--------|---------------|---------------|--------------|---------------------|-----------------------|---------------------|--------------|-------------------|-----------------|------------|-------------|---------------|-------|
"@

$coverageHeader = @"
# OpenAPI Endpoint Coverage Report v5

Runtime source: EndpointDataSource snapshots
OpenAPI source: generated OpenAPI contracts
Gateway source: current YARP configuration
Comparison: independent normalized method+route set comparison

| Host | Business Ops | Framework Ops | Intentionally Excluded Infra | Runtime Documentable Total | OpenAPI Total | Missing | Unexpected | Route Mismatch | Method Mismatch | Schema Count | Classification |
|------|--------------|---------------|------------------------------|----------------------------|---------------|---------|------------|----------------|-----------------|--------------|----------------|
"@

Set-Content -Path $matrixFile -Value $matrixHeader
Set-Content -Path $coverageFile -Value $coverageHeader

$gatewayConfig = Get-Content "gateways/Emcore.ApiGateway/appsettings.json" -Raw | ConvertFrom-Json
$yarpRoutes = @{}
if ($null -ne $gatewayConfig.ReverseProxy -and $null -ne $gatewayConfig.ReverseProxy.Routes) {
    foreach ($gwRouteItem in $gatewayConfig.ReverseProxy.Routes.PSObject.Properties) {
        $rName = $gwRouteItem.Name
        $rVal = $gwRouteItem.Value
        if ($null -ne $rVal.ClusterId) {
            $cluster = $rVal.ClusterId
            $svcName = "emcore-" + $cluster.Replace("-cluster", "")
            if ($cluster.EndsWith("-cluster") -and -not $cluster.EndsWith("bff-cluster") -and -not $cluster.EndsWith("gateway-cluster")) {
                $svcName += "-api"
            }
            if (-not $yarpRoutes.ContainsKey($svcName)) {
                $yarpRoutes[$svcName] = @()
            }
            $yarpRoutes[$svcName] += @{
                GatewayRoute = $rVal.Match.Path
                AuthPolicy = if ($null -ne $rVal.AuthorizationPolicy) { $rVal.AuthorizationPolicy } else { "N/A" }
                RateLimiterPolicy = if ($null -ne $rVal.RateLimiterPolicy) { $rVal.RateLimiterPolicy } else { "N/A" }
            }
        }
    }
}

$scaffoldCount = 0
$scaffoldHosts = @()

$totalMissing = 0
$totalUnexpected = 0
$totalRouteMismatch = 0
$totalMethodMismatch = 0
$totalRuntime = 0
$totalOpenApi = 0

function Normalize-Route($route) {
    # Replace any path parameter e.g. {id} or {id:guid} with {}
    return ($route -replace '\{[^\}]+\}', '{}').TrimStart('/').ToLowerInvariant()
}

Get-ChildItem -Path $contractsDir -Filter 'openapi.json' -Recurse | ForEach-Object {
    $openapiJson = Get-Content $_.FullName -Raw | ConvertFrom-Json
    $service = $_.Directory.Parent.Name
    
    $runtimePath = Join-Path $endpointsDir "$service\endpoints.json"
    $runtimeEndpoints = @()
    if (Test-Path $runtimePath) {
        $runtimeEndpoints = Get-Content $runtimePath -Raw | ConvertFrom-Json
    }
    
    $serviceYarp = $yarpRoutes[$service]
    if ($null -eq $serviceYarp) { $serviceYarp = @() }

    $hostMissing = 0
    $hostUnexpected = 0
    $hostRouteMismatch = 0
    $hostMethodMismatch = 0
    
    $hostBusinessOps = 0
    $hostFrameworkOps = 0
    $hostExcludedInfraOps = 0
    $openapiTotal = 0
    
    $runtimeSet = @{}
    $openApiSet = @{}
    
    # 1. Process OpenAPI Set
    if ($null -ne $openapiJson.paths) {
        foreach ($pathItem in $openapiJson.paths.PSObject.Properties) {
            $rawPath = $pathItem.Name
            $normPath = Normalize-Route $rawPath
            foreach ($methodItem in $pathItem.Value.PSObject.Properties) {
                $openapiTotal++
                $normMethod = $methodItem.Name.ToUpperInvariant()
                $key = "$normMethod|$normPath"
                $openApiSet[$key] = $true
            }
        }
    }
    
    # 2. Process Runtime Set
    foreach ($endpoint in $runtimeEndpoints) {
        $route = $endpoint.Route
        $methodName = $endpoint.Method.ToUpperInvariant()
        $normPath = Normalize-Route $route
        
        $implType = 'BUSINESS'
        if ($service -eq 'emcore-api-gateway') { $implType = 'GATEWAY' }
        elseif ($service -like '*-bff') { $implType = 'BFF' }
        elseif ($endpoint.IsFramework) { $implType = 'FRAMEWORK' }
        
        if ($implType -eq 'FRAMEWORK') { 
            $hostFrameworkOps++
            $hostExcludedInfraOps++ # Count framework as intentionally excluded
        } else { 
            $hostBusinessOps++ 
            $key = "$methodName|$normPath"
            $runtimeSet[$key] = $true
        }
        
        $authMetadata = $endpoint.AuthMetadata
        if ([string]::IsNullOrWhiteSpace($authMetadata)) { $authMetadata = "AllowAnonymous" }
        
        # Determine Gateway Route & Policy
        $gwRoute = "NOT ROUTED THROUGH GATEWAY"
        $gwAuth = "N/A"
        $gwRate = "N/A"
        $exactPath = "/" + $route.TrimStart("/")
        
        foreach ($gw in $serviceYarp) {
            $gwPrefix = $gw.GatewayRoute -replace '\{\*\*catch-all\}', ''
            if ($exactPath.StartsWith($gwPrefix) -or $gw.GatewayRoute -eq "{**catch-all}") {
                $gwRoute = $gw.GatewayRoute
                $gwAuth = $gw.AuthPolicy
                $gwRate = $gw.RateLimiterPolicy
                break
            }
        }
        
        # Determine OpenAPI Match for Matrix Display
        $opMatch = "No"
        $opId = "N/A"
        $reqType = "None"
        $success = @()
        $errors = @()
        
        if ($implType -ne 'FRAMEWORK' -and $openApiSet.ContainsKey("$methodName|$normPath")) {
            $opMatch = "Yes"
            
            # Extract operation details if matched
            if ($null -ne $openapiJson.paths) {
                foreach ($pathItem in $openapiJson.paths.PSObject.Properties) {
                    if ((Normalize-Route $pathItem.Name) -eq $normPath) {
                        $opObj = $pathItem.Value.PSObject.Properties[$methodName.ToLowerInvariant()]
                        if ($null -ne $opObj) {
                            $opId = $opObj.Value.operationId
                            if ($opObj.Value.requestBody) { $reqType = "JSON" }
                            if ($opObj.Value.responses) {
                                foreach ($res in $opObj.Value.responses.PSObject.Properties) {
                                    if ($res.Name -match '^2') { $success += $res.Name }
                                    elseif ($res.Name -match '^[45]') { $errors += $res.Name }
                                }
                            }
                        }
                    }
                }
            }
        } elseif ($implType -eq 'FRAMEWORK') {
            $opMatch = "INTENTIONALLY_EXCLUDED_INFRASTRUCTURE"
        }
        
        $idempotency = "NOT ENFORCED - NoOp runtime"
        $notes = "Actual runtime behavior"
        
        $line = "| $service | $methodName | $route | $gwRoute | $opId | $implType | $authMetadata | $gwAuth | $reqType | " + ($success -join ', ') + " | " + ($errors -join ', ') + " | $gwRate | $idempotency | $opMatch | $notes |"
        Add-Content -Path $matrixFile -Value $line
    }
    
    # 3. Calculate Independent Mismatches
    foreach ($rtKey in $runtimeSet.Keys) {
        if (-not $openApiSet.ContainsKey($rtKey)) {
            $hostMissing++
            $rtParts = $rtKey.Split('|')
            $rtRoute = $rtParts[1]
            $routeExists = $false
            foreach ($oaKey in $openApiSet.Keys) {
                if ($oaKey.EndsWith("|$rtRoute")) {
                    $routeExists = $true
                    break
                }
            }
            if ($routeExists) {
                $hostMethodMismatch++
            } else {
                $hostRouteMismatch++
            }
        }
    }
    
    foreach ($oaKey in $openApiSet.Keys) {
        if (-not $runtimeSet.ContainsKey($oaKey)) {
            $hostUnexpected++
        }
    }
    
    # 4. Schema count
    $schemas = 0
    if ($null -ne $openapiJson.components -and $null -ne $openapiJson.components.schemas) {
        $schemas = @($openapiJson.components.schemas.PSObject.Properties).Count
    }
    
    # 5. Scaffold classification
    $classification = "IMPLEMENTED"
    if ($hostBusinessOps -eq 0) {
        $scaffoldCount++
        $scaffoldHosts += $service
        $classification = "Business API Implementation: NOT IMPLEMENTED"
    }
    
    $covLine = "| $service | $hostBusinessOps | $hostFrameworkOps | $hostExcludedInfraOps | $hostBusinessOps | $openapiTotal | $hostMissing | $hostUnexpected | $hostRouteMismatch | $hostMethodMismatch | $schemas | $classification |"
    Add-Content -Path $coverageFile -Value $covLine
    
    $totalMissing += $hostMissing
    $totalUnexpected += $hostUnexpected
    $totalRouteMismatch += $hostRouteMismatch
    $totalMethodMismatch += $hostMethodMismatch
    $totalRuntime += $hostBusinessOps
    $totalOpenApi += $openapiTotal
}

Add-Content -Path $coverageFile -Value ""
Add-Content -Path $coverageFile -Value "## Grand Totals"
Add-Content -Path $coverageFile -Value "- Runtime Business Operations: $totalRuntime"
Add-Content -Path $coverageFile -Value "- OpenAPI Documented Operations: $totalOpenApi"
Add-Content -Path $coverageFile -Value "- Total Missing: $totalMissing"
Add-Content -Path $coverageFile -Value "- Total Unexpected: $totalUnexpected"
Add-Content -Path $coverageFile -Value "- Total Route Mismatch: $totalRouteMismatch"
Add-Content -Path $coverageFile -Value "- Total Method Mismatch: $totalMethodMismatch"
Add-Content -Path $coverageFile -Value "- Scaffold host count: $scaffoldCount"

if ($scaffoldHosts.Count -gt 0) {
    Add-Content -Path $coverageFile -Value ""
    Add-Content -Path $coverageFile -Value "### Scaffold Hosts"
    foreach ($h in $scaffoldHosts) {
        Add-Content -Path $coverageFile -Value "- $h"
    }
}
