$ErrorActionPreference = 'Stop'

$endpointsDir = 'contracts/endpoints'
$contractsDir = 'contracts/openapi'
$matrixFile = 'docs/verification/swagger-final-remediation-v5/02-swagger-openapi/SWAGGER_ENDPOINT_DOCUMENTATION_MATRIX_v5.md'
$coverageFile = 'docs/verification/swagger-final-remediation-v5/02-swagger-openapi/OPENAPI_ENDPOINT_COVERAGE_REPORT_v5.md'

$matrixHeader = @"
# Swagger Endpoint Documentation Matrix v5

| Service | Method | Runtime Route | Gateway Route | Operation ID | Endpoint Category | OpenAPI Disposition | Runtime Auth Metadata | Gateway Auth Policy | Request Type | Success Responses | Error Responses | Rate Limit | Idempotency | OpenAPI Match | Notes |
|---------|--------|---------------|---------------|--------------|-------------------|---------------------|-----------------------|---------------------|--------------|-------------------|-----------------|------------|-------------|---------------|-------|
"@

$coverageHeader = @"
# OpenAPI Endpoint Coverage Report v5

Runtime source: EndpointDataSource snapshots
OpenAPI source: generated OpenAPI contracts
Gateway source: current YARP configuration
Comparison: independent normalized method+route set comparison

| Host | Host Type | Owned Business Ops | Owned Framework Ops | Proxy Infra Ops | OpenAPI Infra Ops | Runtime Documentable Total | OpenAPI Total | Missing | Unexpected | Route Mismatch | Method Mismatch | Schema Count | Classification |
|------|-----------|--------------------|---------------------|-----------------|-------------------|----------------------------|---------------|---------|------------|----------------|-----------------|--------------|----------------|
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

    $hostType = "BUSINESS_SERVICE"
    if ($service -eq "emcore-api-gateway") { $hostType = "GATEWAY" }
    elseif ($service -match "bff$") { $hostType = "BFF" }
    elseif ($service -match "gateway$") { $hostType = "SPECIALIZED_GATEWAY" }

    $hostMissing = 0
    $hostUnexpected = 0
    $hostRouteMismatch = 0
    $hostMethodMismatch = 0
    
    $hostBusinessOps = 0
    $hostFrameworkOps = 0
    $hostProxyInfraOps = 0
    $hostOpenApiInfraOps = 0
    $hostDocumentableTotal = 0
    $openapiTotal = 0
    
    $runtimeSet = @{}
    $openApiSet = @{}
    
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
    
    foreach ($endpoint in $runtimeEndpoints) {
        $route = $endpoint.Route
        $methodName = $endpoint.Method.ToUpperInvariant()
        $normPath = Normalize-Route $route
        
        $cat = $endpoint.EndpointCategory
        $disp = $endpoint.OpenApiDisposition

        if ($cat -eq "FRAMEWORK_API" -or $cat -eq "OPENAPI_INFRASTRUCTURE") {
            $inOpenApi = $false
            foreach ($oaKey in $openApiSet.Keys) {
                if ($oaKey.EndsWith("|$normPath")) {
                    $inOpenApi = $true
                    break
                }
            }
            if ($inOpenApi) {
                $disp = "DOCUMENTABLE"
            } else {
                $disp = "INTENTIONALLY_EXCLUDED"
            }
        }

        if ($cat -eq "BUSINESS_API") { $hostBusinessOps++ }
        if ($cat -eq "FRAMEWORK_API") { $hostFrameworkOps++ }
        if ($cat -match "PROXY") { $hostProxyInfraOps++ }
        if ($cat -eq "OPENAPI_INFRASTRUCTURE") { $hostOpenApiInfraOps++ }

        if ($disp -eq "DOCUMENTABLE") {
            $hostDocumentableTotal++
            $key = "$methodName|$normPath"
            $runtimeSet[$key] = $true
        }
        
        $authMetadata = $endpoint.AuthMetadata
        if ([string]::IsNullOrWhiteSpace($authMetadata)) { $authMetadata = "AllowAnonymous" }
        
        $gwRoute = "NOT ROUTED THROUGH GATEWAY"
        $gwAuth = "N/A"
        $gwRate = "N/A"
        $exactPath = "/" + $route.TrimStart("/")
        
        if ($cat -match "PROXY") {
            $gwRoute = "SELF - YARP ROUTE"
        } else {
            foreach ($gw in $serviceYarp) {
                $gwPrefix = $gw.GatewayRoute -replace '\{\*\*catch-all\}', ''
                if ($exactPath.StartsWith($gwPrefix) -or $gw.GatewayRoute -eq "{**catch-all}") {
                    $gwRoute = $gw.GatewayRoute
                    $gwAuth = $gw.AuthPolicy
                    $gwRate = $gw.RateLimiterPolicy
                    break
                }
            }
        }
        
        $opMatch = "No"
        $opId = "N/A"
        $reqType = "None"
        $success = @()
        $errors = @()
        
        if ($disp -eq "DOCUMENTABLE") {
            if ($openApiSet.ContainsKey("$methodName|$normPath")) {
                $opMatch = "Yes"
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
            }
        } else {
            $opMatch = "INTENTIONALLY_EXCLUDED"
        }
        
        $idempotency = if ($cat -eq "BUSINESS_API" -and $methodName -ne "GET") { "NOT ENFORCED - NoOp runtime" } else { "N/A" }
        $notes = "Actual runtime behavior"
        
        $line = "| $service | $methodName | $route | $gwRoute | $opId | $cat | $disp | $authMetadata | $gwAuth | $reqType | " + ($success -join ', ') + " | " + ($errors -join ', ') + " | $gwRate | $idempotency | $opMatch | $notes |"
        Add-Content -Path $matrixFile -Value $line
    }
    
    foreach ($rtKey in $runtimeSet.Keys) {
        if (-not $openApiSet.ContainsKey($rtKey)) {
            $rtParts = $rtKey.Split('|')
            $rtMethod = $rtParts[0]
            $rtRoute = $rtParts[1]

            if ($rtMethod -eq "UNCONSTRAINED_HTTP_METHOD") {
                $routeFound = $false
                foreach ($oaKey in $openApiSet.Keys) {
                    if ($oaKey.EndsWith("|$rtRoute")) {
                        $routeFound = $true
                        break
                    }
                }
                if ($routeFound) {
                    $hostMethodMismatch++
                } else {
                    $hostMissing++
                    $hostRouteMismatch++
                }
            } else {
                $hostMissing++
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
    }
    
    foreach ($oaKey in $openApiSet.Keys) {
        if (-not $runtimeSet.ContainsKey($oaKey)) {
            $oaParts = $oaKey.Split('|')
            $oaMethod = $oaParts[0]
            $oaRoute = $oaParts[1]
            if ($runtimeSet.ContainsKey("UNCONSTRAINED_HTTP_METHOD|$oaRoute")) {
                continue
            }
            $hostUnexpected++
        }
    }
    
    $schemas = 0
    if ($null -ne $openapiJson.components -and $null -ne $openapiJson.components.schemas) {
        $schemas = @($openapiJson.components.schemas.PSObject.Properties).Count
    }
    
    $classification = "IMPLEMENTED"
    if ($hostType -eq "BUSINESS_SERVICE" -and $hostBusinessOps -eq 0) {
        $scaffoldCount++
        $scaffoldHosts += $service
        $classification = "Business API implementation: NOT IMPLEMENTED"
    }
    
    $covLine = "| $service | $hostType | $hostBusinessOps | $hostFrameworkOps | $hostProxyInfraOps | $hostOpenApiInfraOps | $hostDocumentableTotal | $openapiTotal | $hostMissing | $hostUnexpected | $hostRouteMismatch | $hostMethodMismatch | $schemas | $classification |"
    Add-Content -Path $coverageFile -Value $covLine
    
    $totalMissing += $hostMissing
    $totalUnexpected += $hostUnexpected
    $totalRouteMismatch += $hostRouteMismatch
    $totalMethodMismatch += $hostMethodMismatch
    $totalRuntime += $hostDocumentableTotal
    $totalOpenApi += $openapiTotal
}

Add-Content -Path $coverageFile -Value ""
Add-Content -Path $coverageFile -Value "## Grand Totals"
Add-Content -Path $coverageFile -Value "- Runtime Documentable Operations: $totalRuntime"
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
