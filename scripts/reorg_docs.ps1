$ErrorActionPreference = 'Stop'
$rootDir = (Get-Item .).FullName

$inventoryPath = "docs/DOCUMENTATION_INVENTORY.md"
$manifestPath = "docs/verification/swagger-final-remediation-v5/07-change-control/DOCUMENTATION_MOVE_MANIFEST.md"

# Ensure directories exist
$dirs = @(
    "docs/architecture/current",
    "docs/architecture/decisions",
    "docs/development/setup",
    "docs/development/standards",
    "docs/development/service-guides",
    "docs/database/design",
    "docs/database/migration-guides",
    "docs/deployment/development",
    "docs/deployment/iis",
    "docs/deployment/production",
    "docs/api/gateway",
    "docs/api/identity",
    "docs/api/openapi",
    "docs/verification/current",
    "docs/verification/archive/swagger",
    "docs/verification/archive/identity",
    "docs/verification/archive/architecture",
    "docs/verification/archive/historical",
    "docs/security/current",
    "docs/security/deferred",
    "docs/planning"
)

foreach ($d in $dirs) {
    if (-not (Test-Path $d)) {
        New-Item -ItemType Directory -Force -Path $d | Out-Null
    }
}

$inventoryHeader = "| Current Path | Document Purpose | Current/Archive | Target Path | Referenced By | Move Required |`n|---|---|---|---|---|---|"
Set-Content -Path $inventoryPath -Value $inventoryHeader

$manifestHeader = "| Original Path | Final Path | Category | Historical/Current | Content Modified | References Updated | Link Check |`n|---|---|---|---|---|---|---|"
Set-Content -Path $manifestPath -Value $manifestHeader

# Helper to determine target
function Get-Target($path) {
    $name = Split-Path $path -Leaf
    if ($name -match '^API_GATEWAY_.*IIS') { return "docs/deployment/iis/$name" }
    if ($name -match '^API_GATEWAY_.*') { return "docs/api/gateway/$name" }
    if ($name -match 'GATEWAY_SERVICE_DESTINATION') { return "docs/api/gateway/$name" }
    if ($name -match 'OPENAPI_|SWAGGER_') { return "docs/api/openapi/$name" }
    if ($name -match '^\d\d_') { return "docs/planning/$name" }
    if ($name -eq "EMCORE_ANTIGRAVITY_SETUP_MASTER.md") { return "docs/planning/$name" }
    
    # Archive old verification folders
    if ($path -match 'docs\\verification\\swagger-safe-remediation') {
        $rel = $path.Substring($path.IndexOf('docs\verification\swagger-safe-remediation'))
        return $rel -replace 'docs\\verification\\swagger-safe-remediation', 'docs/verification/archive/swagger/swagger-safe-remediation'
    }

    return $null
}

# Find all MD files
$mdFiles = Get-ChildItem -Path . -Filter "*.md" -Recurse | Where-Object { 
    $_.FullName -notmatch '\\(bin|obj|node_modules|\.git|\.vs|packages)\\.*' 
}

$moves = @()
$inventoryLines = @()

foreach ($file in $mdFiles) {
    $relPath = $file.FullName.Substring($rootDir.Length).TrimStart("\", "/")
    
    # Skip standard root files
    if ($relPath -eq "README.md" -or $relPath -eq "LICENSE.md" -or $relPath -eq "CONTRIBUTING.md" -or $relPath -eq "SECURITY.md" -or $relPath -eq "CODE_OF_CONDUCT.md" -or $relPath -eq "CHANGELOG.md") {
        $inventoryLines += "| $relPath | Root Documentation | Current | $relPath | Root | No |"
        continue
    }

    # Skip files already in their final intended spots or in component folders
    if ($relPath.StartsWith("services\") -or $relPath.StartsWith("building-blocks\") -or $relPath.StartsWith("gateways\") -or $relPath.StartsWith("scripts\") -or $relPath.StartsWith("tests\") -or $relPath.StartsWith("contracts\")) {
        $inventoryLines += "| $relPath | Component Documentation | Current | $relPath | Component | No |"
        continue
    }
    
    # Check if we should move it
    $targetPath = Get-Target $relPath
    
    if ($null -ne $targetPath) {
        $targetPath = $targetPath.Replace("\", "/")
        $relPathFixed = $relPath.Replace("\", "/")
        $isArchived = if ($targetPath -match 'archive') { "Historical" } else { "Current" }
        
        $inventoryLines += "| $relPathFixed | Technical Report/Plan | $isArchived | $targetPath | Unknown | Yes |"
        $moves += @{ Old = $relPathFixed; New = $targetPath; Category = "Documentation Reorganization"; Hist = $isArchived }
    } else {
        $relPathFixed = $relPath.Replace("\", "/")
        $inventoryLines += "| $relPathFixed | Project Doc | Current | $relPathFixed | Unknown | No |"
    }
}

foreach ($line in $inventoryLines) {
    Add-Content -Path $inventoryPath -Value $line
}

# Now perform moves
foreach ($m in $moves) {
    $old = $m.Old
    $new = $m.New
    $newDir = Split-Path $new -Parent
    if (-not (Test-Path $newDir)) {
        New-Item -ItemType Directory -Force -Path $newDir | Out-Null
    }
    
    Move-Item -Path $old -Destination $new -Force
    
    $manifestLine = "| $old | $new | $($m.Category) | $($m.Hist) | No | Yes | Pending |"
    Add-Content -Path $manifestPath -Value $manifestLine
}

Write-Host "Moved $($moves.Count) files."
