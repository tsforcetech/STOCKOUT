$ErrorActionPreference = "Stop"

$scriptPath = Resolve-Path "./scripts/Check-OpenApiCompatibility.ps1"
$fixtureRoot = "tests/fixtures/openapi-compatibility"

if (Test-Path $fixtureRoot) { Remove-Item -Recurse -Force $fixtureRoot }

$passCases = @(
    @{ Name = "DescriptionChanged"; Setup = { $_.paths."/api/test".get.description = "New description" } },
    @{ Name = "SummaryChanged"; Setup = { $_.paths."/api/test".get.summary = "New summary" } },
    @{ Name = "ExampleChanged"; Setup = { $_.paths."/api/test".get.parameters[0] | Add-Member -NotePropertyName "example" -NotePropertyValue "new example" } },
    @{ Name = "OptionalPropertyAdded"; Setup = { $_.components.schemas.User.properties | Add-Member -NotePropertyName "newProp" -NotePropertyValue @{ type="string" } } },
    @{ Name = "NewEndpointAdded"; Setup = { $_.paths | Add-Member -NotePropertyName "/api/new" -NotePropertyValue @{ get = @{ responses = @{ "200" = @{ description="ok" } } } } } },
    @{ Name = "AdditionalSuccessResponseAdded"; Setup = { $_.paths."/api/test".get.responses | Add-Member -NotePropertyName "201" -NotePropertyValue @{ description="Created" } } }
)

$failCases = @(
    @{ Name = "PathRemoved"; Setup = { $_.paths.PSObject.Properties.Remove("/api/test") } },
    @{ Name = "MethodRemoved"; Setup = { $_.paths."/api/test".PSObject.Properties.Remove("get") } },
    @{ Name = "RequiredParameterAdded"; Setup = { $_.paths."/api/test".get.parameters += @{ name="newParam"; in="query"; required=$true; schema=@{ type="string" } } } },
    @{ Name = "OptionalParameterBecomesRequired"; Setup = { $_.paths."/api/test".get.parameters[0].required = $true } },
    @{ Name = "ParameterTypeChanged"; Setup = { $_.paths."/api/test".get.parameters[0].schema.type = "integer" } },
    @{ Name = "ParameterFormatChanged"; Setup = { $_.paths."/api/test".get.parameters[0].schema.format = "int64" } },
    @{ Name = "ParameterEnumRemoved"; Setup = { $_.paths."/api/test".get.parameters[0].schema.enum = @("A") } },
    @{ Name = "RequestBodyBecomesRequired"; Setup = { $_.paths."/api/test".post.requestBody.required = $true } },
    @{ Name = "RequestMediaTypeRemoved"; Setup = { $_.paths."/api/test".post.requestBody.content.PSObject.Properties.Remove("application/json") } },
    @{ Name = "RequestPropertyRemoved"; Setup = { $_.components.schemas.User.properties.PSObject.Properties.Remove("id") } },
    @{ Name = "RequiredRequestPropertyAdded"; Setup = { $_.components.schemas.User | Add-Member -NotePropertyName "required" -NotePropertyValue @("newReq"); $_.components.schemas.User.properties | Add-Member -NotePropertyName "newReq" -NotePropertyValue @{ type="string" } } },
    @{ Name = "RequestPropertyTypeChanged"; Setup = { $_.components.schemas.User.properties.id.type = "integer" } },
    @{ Name = "SuccessResponseRemoved"; Setup = { $_.paths."/api/test".get.responses.PSObject.Properties.Remove("200") } },
    @{ Name = "ResponseMediaTypeRemoved"; Setup = { $_.paths."/api/test".get.responses."200".content.PSObject.Properties.Remove("application/json") } },
    @{ Name = "ResponsePropertyRemoved"; Setup = { $_.components.schemas.User.properties.PSObject.Properties.Remove("id") } },
    @{ Name = "ResponsePropertyTypeChanged"; Setup = { $_.components.schemas.User.properties.name.type = "integer" } },
    @{ Name = "ComponentSchemaRemoved"; Setup = { $_.components.schemas.PSObject.Properties.Remove("User") } },
    @{ Name = "RequiredComponentPropertyAdded"; Setup = { $_.components.schemas.User | Add-Member -NotePropertyName "required" -NotePropertyValue @("anotherReq"); $_.components.schemas.User.properties | Add-Member -NotePropertyName "anotherReq" -NotePropertyValue @{ type="string" } } },
    @{ Name = "ComponentPropertyRemoved"; Setup = { $_.components.schemas.User.properties.PSObject.Properties.Remove("name") } },
    @{ Name = "EnumValueRemoved"; Setup = { $_.components.schemas.UserRole.enum = @("Admin") } },
    @{ Name = "SecuritySchemeRemoved"; Setup = { $_.components.securitySchemes.PSObject.Properties.Remove("Bearer") } },
    @{ Name = "SecurityRequirementBecomesIncompatible"; Setup = { $_.paths."/api/test".get.security = @( @{ "OAuth" = @() } ) } }
)

$baseTemplate = @"
{
  "openapi": "3.0.1",
  "paths": {
    "/api/test": {
      "get": {
        "summary": "Old summary",
        "description": "Old description",
        "parameters": [
          { "name": "oldParam", "in": "query", "required": false, "schema": { "type": "string", "format": "uuid", "enum": ["A", "B"] } }
        ],
        "responses": {
          "200": { "description": "Success", "content": { "application/json": { "schema": { "`$ref": "#/components/schemas/User" } } } }
        },
        "security": []
      },
      "post": {
        "requestBody": {
          "required": false,
          "content": { "application/json": { "schema": { "`$ref": "#/components/schemas/User" } } }
        },
        "responses": { "200": { "description": "ok" } }
      }
    }
  },
  "components": {
    "schemas": {
      "User": {
        "type": "object",
        "properties": {
          "id": { "type": "string" },
          "name": { "type": "string" },
          "role": { "`$ref": "#/components/schemas/UserRole" }
        }
      },
      "UserRole": {
        "type": "string",
        "enum": ["Admin", "User"]
      }
    },
    "securitySchemes": {
      "Bearer": { "type": "http", "scheme": "bearer" }
    }
  }
}
"@

$passCount = 0
$failCount = 0

function Run-Test {
    param($Name, $SetupBlock, $ExpectedExitCode)
    
    $testDir = Join-Path $fixtureRoot $Name
    $baseDir = Join-Path $testDir "baseline"
    $currDir = Join-Path $testDir "current"
    
    New-Item -ItemType Directory -Force -Path "$baseDir/dummy-service" | Out-Null
    New-Item -ItemType Directory -Force -Path "$currDir/dummy-service" | Out-Null
    
    Set-Content -Path "$baseDir/dummy-service/openapi.json" -Value $baseTemplate
    
    $currentJsonObj = ConvertFrom-Json $baseTemplate
    $currentJsonObj = ConvertFrom-Json (ConvertTo-Json $currentJsonObj -Depth 10)
    
    $_ = $currentJsonObj
    & $SetupBlock
    
    $finalJson = ConvertTo-Json $currentJsonObj -Depth 10
    Set-Content -Path "$currDir/dummy-service/openapi.json" -Value $finalJson
    
    $shellExec = if (Get-Command pwsh -ErrorAction SilentlyContinue) { "pwsh" } else { "powershell" }
    $proc = Start-Process -FilePath $shellExec -ArgumentList "-NoProfile", "-Command", "& '$scriptPath' -BaselineDir '$baseDir' -CurrentDir '$currDir'" -Wait -NoNewWindow -PassThru
    
    if ($proc.ExitCode -ne $ExpectedExitCode) {
        Write-Host "[FAIL] Fixture '$Name' expected exit code $ExpectedExitCode but got $($proc.ExitCode)." -ForegroundColor Red
        exit 1
    } else {
        Write-Host "[PASS] Fixture '$Name' passed." -ForegroundColor Green
    }
}

Write-Host "Running PASS fixtures..."
foreach ($tc in $passCases) {
    Run-Test -Name $tc.Name -SetupBlock $tc.Setup -ExpectedExitCode 0
    $passCount++
}

Write-Host "Running FAIL fixtures..."
foreach ($tc in $failCases) {
    Run-Test -Name $tc.Name -SetupBlock $tc.Setup -ExpectedExitCode 1
    $failCount++
}

Write-Host "[SUCCESS] All $passCount PASS fixtures and $failCount FAIL fixtures verified correctly." -ForegroundColor Green
exit 0
