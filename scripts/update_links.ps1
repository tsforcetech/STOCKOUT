$mdFiles = Get-ChildItem -Path . -Filter "*.md" -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|\.git|\.vs|packages)\\.*' }
foreach ($file in $mdFiles) {
    $content = Get-Content $file.FullName -Raw
    $original = $content
    $content = $content -replace 'docs/verification/swagger-safe-remediation', 'docs/verification/archive/swagger/swagger-safe-remediation'
    $content = $content -replace 'docs/verification/swagger-closure-v4', 'docs/verification/archive/swagger/swagger-closure-v4'
    $content = $content -replace 'docs/verification/identity-access-final', 'docs/verification/archive/identity/identity-access-final'
    $content = $content -replace 'SWAGGER_ENDPOINT_DOCUMENTATION_MATRIX\.md', 'docs/api/openapi/SWAGGER_ENDPOINT_DOCUMENTATION_MATRIX.md'
    
    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content
        Write-Host "Updated links in $($file.Name)"
    }
}
