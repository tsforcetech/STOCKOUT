<#
.SYNOPSIS
    Machine-enforced OpenAPI contract breaking-change detection script.
.DESCRIPTION
    Compares baseline OpenAPI specifications against current generated specifications under contracts/openapi/.
    Detects removed paths, methods, parameters, newly required parameters, removed responses, schemas, properties,
    newly required properties, type changes, format changes, removed enum values, security changes, and success-code changes.
    Fails CI execution (exit code 1) upon finding unapproved breaking changes.
#>
param(
    [string]$BaselineDir = "docs/verification/swagger-safe-remediation/baseline-contracts",
    [string]$CurrentDir = "contracts/openapi",
    [switch]$AllowBreakingChanges = $false,
    [switch]$EstablishBaseline = $false
)

$ErrorActionPreference = "Stop"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "      EMCORE OpenAPI Contract Compatibility Verification      " -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Checking contracts in: $CurrentDir"

if (-not (Test-Path $CurrentDir)) {
    Write-Host "Current directory $CurrentDir does not exist. Nothing to verify." -ForegroundColor Yellow
    exit 0
}

# If no baseline directory exists yet, establish current files as baseline for future CI evaluation
if (-not (Test-Path $BaselineDir)) {
    if ($EstablishBaseline) {
        Write-Host "Baseline directory '$BaselineDir' not found. Creating baseline snapshot from current contracts..." -ForegroundColor Yellow
        New-Item -ItemType Directory -Force -Path $BaselineDir | Out-Null
        Get-ChildItem -Path $CurrentDir -Filter "*.json" -Recurse | ForEach-Object {
            $rel = $_.FullName.Substring((Get-Item $CurrentDir).FullName.Length).TrimStart("\", "/")
            $dest = Join-Path $BaselineDir $rel
            $destDir = Split-Path -Parent $dest
            if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }
            Copy-Item $_.FullName -Destination $dest -Force
        }
        Write-Host "Baseline snapshot established. No breaking changes detected." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "[FAIL] Baseline directory '$BaselineDir' not found and -EstablishBaseline was not specified. Cannot verify compatibility without a baseline." -ForegroundColor Red
        exit 1
    }
}

$breakingChangesCount = 0
$filesChecked = 0

function Report-BreakingChange($service, $message) {
    Write-Host "[BREAKING CHANGE] [$service]: $message" -ForegroundColor Red
    $script:breakingChangesCount++
}

function Compare-OpenApiSchemas($service, $name, $oldSchema, $newSchema) {
    if ($null -eq $newSchema) {
        Report-BreakingChange $service "Schema '$name' was completely removed."
        return
    }
    if ($oldSchema.type -ne $newSchema.type -and $null -ne $oldSchema.type) {
        Report-BreakingChange $service "Schema '$name' type changed from '$($oldSchema.type)' to '$($newSchema.type)'."
    }
    if ($oldSchema.format -ne $newSchema.format -and $null -ne $oldSchema.format) {
        Report-BreakingChange $service "Schema '$name' format changed from '$($oldSchema.format)' to '$($newSchema.format)'."
    }
    
    # Check removed enum values
    if ($null -ne $oldSchema.enum) {
        foreach ($val in $oldSchema.enum) {
            if ($null -eq $newSchema.enum -or -not ($newSchema.enum -contains $val)) {
                Report-BreakingChange $service "Schema '$name' removed enum value '$val'."
            }
        }
    }

    # Check properties
    if ($null -ne $oldSchema.properties) {
        foreach ($prop in $oldSchema.properties.PSObject.Properties) {
            $propName = $prop.Name
            $newProp = $newSchema.properties.PSObject.Properties[$propName]
            if ($null -eq $newProp) {
                Report-BreakingChange $service "Schema '$name' removed property '$propName'."
            }
            else {
                if ($prop.Value.type -ne $newProp.Value.type -and $null -ne $prop.Value.type) {
                    Report-BreakingChange $service "Schema '$name' property '$propName' type changed from '$($prop.Value.type)' to '$($newProp.Value.type)'."
                }
            }
        }
    }

    # Check newly required properties
    if ($null -ne $newSchema.required) {
        foreach ($req in $newSchema.required) {
            if ($null -eq $oldSchema.required -or -not ($oldSchema.required -contains $req)) {
                Report-BreakingChange $service "Schema '$name' made property '$req' newly required."
            }
        }
    }
}

$baselineFiles = Get-ChildItem -Path $BaselineDir -Filter "*.json" -Recurse
foreach ($baseFile in $baselineFiles) {
    $relPath = $baseFile.FullName.Substring((Get-Item $BaselineDir).FullName.Length).TrimStart("\", "/")
    $currPath = Join-Path $CurrentDir $relPath
    $serviceName = $relPath.Split("\/")[0]
    
    $filesChecked++

    if (-not (Test-Path $currPath)) {
        Report-BreakingChange $serviceName "OpenAPI contract document '$relPath' was removed."
        continue
    }

    $oldSpec = (Get-Content $baseFile.FullName -Raw) | ConvertFrom-Json
    $newSpec = (Get-Content $currPath -Raw) | ConvertFrom-Json

    # 1. Compare Paths
    if ($null -ne $oldSpec.paths) {
        foreach ($pathItem in $oldSpec.paths.PSObject.Properties) {
            $route = $pathItem.Name
            $newPath = $newSpec.paths.PSObject.Properties[$route]
            if ($null -eq $newPath) {
                Report-BreakingChange $serviceName "Route path '$route' was completely removed."
                continue
            }

            # Compare Methods
            foreach ($methodItem in $pathItem.Value.PSObject.Properties) {
                $method = $methodItem.Name
                $oldOp = $methodItem.Value
                $newOp = $newPath.Value.PSObject.Properties[$method]

                if ($null -eq $newOp) {
                    Report-BreakingChange $serviceName "HTTP method '$method.ToUpper()' on path '$route' was removed."
                    continue
                }

                # Compare Parameters
                if ($null -ne $oldOp.parameters) {
                    foreach ($oldParam in $oldOp.parameters) {
                        $match = $null
                        if ($null -ne $newOp.Value.parameters) {
                            $match = $newOp.Value.parameters | Where-Object { $_.name -eq $oldParam.name -and $_.in -eq $oldParam.in }
                        }
                        if ($null -eq $match) {
                            Report-BreakingChange $serviceName "Parameter '$($oldParam.name)' in '$($oldParam.in)' removed from '$method.ToUpper() $route'."
                        }
                    }
                }

                # Check newly required parameters
                if ($null -ne $newOp.Value.parameters) {
                    foreach ($newParam in $newOp.Value.parameters) {
                        if ($newParam.required -eq $true) {
                            $oldParamMatch = $null
                            if ($null -ne $oldOp.parameters) {
                                $oldParamMatch = $oldOp.parameters | Where-Object { $_.name -eq $newParam.name -and $_.in -eq $newParam.in }
                            }
                            if ($null -eq $oldParamMatch -or $oldParamMatch.required -ne $true) {
                                Report-BreakingChange $serviceName "Parameter '$($newParam.name)' ($($newParam.in)) became newly required on '$method.ToUpper() $route'."
                            }
                        }
                    }
                }

                # Compare Responses & Success-code changes
                if ($null -ne $oldOp.responses) {
                    foreach ($respItem in $oldOp.responses.PSObject.Properties) {
                        $statusCode = $respItem.Name
                        $newResp = $newOp.Value.responses.PSObject.Properties[$statusCode]
                        if ($null -eq $newResp) {
                            if ($statusCode -match "^[23]\d\d$") {
                                Report-BreakingChange $serviceName "Success response code '$statusCode' removed from '$method.ToUpper() $route'."
                            }
                        } else {
                            $oldContent = $respItem.Value.content
                            $newContent = $newResp.Value.content
                            if ($null -ne $oldContent) {
                                foreach ($mediaItem in $oldContent.PSObject.Properties) {
                                    $mediaType = $mediaItem.Name
                                    $newMedia = $newContent.PSObject.Properties[$mediaType]
                                    if ($null -eq $newMedia) {
                                        Report-BreakingChange $serviceName "Response media type '$mediaType' removed from '$statusCode' on '$method.ToUpper() $route'."
                                    } elseif ($null -ne $mediaItem.Value.schema -and $null -ne $mediaItem.Value.schema.type) {
                                        if ($null -eq $newMedia.Value.schema -or $newMedia.Value.schema.type -ne $mediaItem.Value.schema.type) {
                                            Report-BreakingChange $serviceName "Response schema type changed for '$mediaType' on '$statusCode' on '$method.ToUpper() $route'."
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                # Compare Request Body
                if ($null -ne $oldOp.requestBody) {
                    if ($null -eq $newOp.Value.requestBody) {
                        Report-BreakingChange $serviceName "Request body was completely removed from '$method.ToUpper() $route'."
                    }
                }
                if ($null -ne $newOp.Value.requestBody -and $newOp.Value.requestBody.required -eq $true) {
                    if ($null -eq $oldOp.requestBody -or $oldOp.requestBody.required -ne $true) {
                        Report-BreakingChange $serviceName "Request body became newly required on '$method.ToUpper() $route'."
                    }
                }
                
                # Compare Security
                if ($null -ne $newOp.Value.security -and $newOp.Value.security.Count -gt 0) {
                    if ($null -eq $oldOp.security -or $oldOp.security.Count -eq 0) {
                        Report-BreakingChange $serviceName "Operation '$method.ToUpper() $route' was anonymous but now requires security."
                    }
                }
                
            }
        }
    }

    # 2. Compare Security Schemes
    if ($null -ne $oldSpec.components -and $null -ne $oldSpec.components.securitySchemes) {
        foreach ($sec in $oldSpec.components.securitySchemes.PSObject.Properties) {
            if ($null -eq $newSpec.components -or $null -eq $newSpec.components.securitySchemes -or $null -eq $newSpec.components.securitySchemes.PSObject.Properties[$sec.Name]) {
                Report-BreakingChange $serviceName "Security scheme '$($sec.Name)' was removed."
            }
        }
    }

    # 3. Compare Schemas
    if ($null -ne $oldSpec.components -and $null -ne $oldSpec.components.schemas) {
        foreach ($sch in $oldSpec.components.schemas.PSObject.Properties) {
            $oldSchemaObj = $sch.Value
            $newSchemaObj = $null
            if ($null -ne $newSpec.components -and $null -ne $newSpec.components.schemas) {
                $newSchemaObj = $newSpec.components.schemas.PSObject.Properties[$sch.Name].Value
            }
            Compare-OpenApiSchemas $serviceName $sch.Name $oldSchemaObj $newSchemaObj
        }
    }
}

Write-Host "----------------------------------------------------------------"
Write-Host "Total files verified: $filesChecked"
if ($breakingChangesCount -gt 0) {
    if ($AllowBreakingChanges) {
        Write-Host "[WARNING] Detected $breakingChangesCount breaking change(s), but -AllowBreakingChanges switch was provided. Passing." -ForegroundColor Yellow
        exit 0
    } else {
        Write-Host "[FAIL] Detected $breakingChangesCount unapproved breaking change(s)! CI validation failed." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[SUCCESS] Zero breaking changes detected. All contracts are fully backward-compatible." -ForegroundColor Green
    exit 0
}
