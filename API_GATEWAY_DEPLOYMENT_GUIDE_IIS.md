# EMCORE API Gateway IIS Deployment & Publishing Guide

This guide defines the standardized, reproducible procedures for publishing, copying, and hosting `Emcore.ApiGateway` on Internet Information Services (IIS) running on Windows Server under the single-server initial deployment architecture.

## 1. Standardized Naming & Directory Conventions

To prevent operational ambiguity and configuration mismatch across staging and production environments, deployment automation must strictly enforce the following parameter naming conventions:

| Parameter Name | Standardized Convention Value | Description & Mandatory Rules |
| :--- | :--- | :--- |
| **IIS Site Name** | `EMCORE-ApiGateway` | Primary web site identity registered within IIS Manager. |
| **Application Pool Name** | `EMCORE-ApiGateway-Pool` | Dedicated application pool ensuring process and compute resource isolation. |
| **Physical Folder Path** | `C:\Emcore\Gateway\ApiGateway` | Target destination directory where executable binaries and configs reside. |
| **Application Pool CLR** | `No Managed Code` | Mandatory for ASP.NET Core hosting via ASP.NET Core Module v2 (ANCM). |
| **Pipeline Mode** | `Integrated` | Enables integrated request handling between IIS and Kestrel. |
| **Enable 32-Bit Applications** | `False` | Mandates native 64-bit (`win-x64`) architectural execution. |
| **Start Mode** | `AlwaysRunning` | Prevents cold-start request latency by keeping worker processes warmed. |
| **Idle Timeout** | `0` | Disables idle recycling, ensuring uninterrupted reverse proxy readiness. |

---

## 2. Two-Stage Build & Publish Workflow

To adhere to safe release management practices, generating compiled binaries must occur independently from the actual physical file copy to the production runtime folder.

### Stage 1: Compile & Publish to Repository Artifacts
Execute the dotnet CLI to publish optimized, native win-x64 binaries into a secure repository-relative staging folder without modifying production IIS directories:

```powershell
# Run from repository root (c:/DEV/API PROJECT/STOCKOUT)
dotnet publish gateways/Emcore.ApiGateway/Emcore.ApiGateway.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output artifacts/publish/Emcore.ApiGateway
```

### Stage 2: Independent Deployment File Transfer
After verifying artifact integrity and generating audit inventories, execute an atomic file copy to the physical IIS host directory:

```powershell
$sourcePath = "C:\DEV\API PROJECT\STOCKOUT\artifacts\publish\Emcore.ApiGateway\*"
$targetPath = "C:\Emcore\Gateway\ApiGateway"

# Ensure destination exists
if (!(Test-Path -Path $targetPath)) {
    New-Item -ItemType Directory -Force -Path $targetPath | Out-Null
}

# Stop Application Pool before replacing locked DLLs
Import-Module WebAdministration
Stop-WebAppPool -Name "EMCORE-ApiGateway-Pool" -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Execute deployment copy
Copy-Item -Path $sourcePath -Destination $targetPath -Recurse -Force

# Restart Application Pool and Web Site
Start-WebAppPool -Name "EMCORE-ApiGateway-Pool"
Start-Website -Name "EMCORE-ApiGateway" -ErrorAction SilentlyContinue
```

---

## 3. Automated IIS Configuration Script

The following PowerShell script utilizes the IIS Administration module to provision the standardized Application Pool and Web Site:

```powershell
Import-Module WebAdministration

$siteName = "EMCORE-ApiGateway"
$poolName = "EMCORE-ApiGateway-Pool"
$physicalPath = "C:\Emcore\Gateway\ApiGateway"

# 1. Create Application Pool with strict conventions
if (!(Test-Path "IIS:\AppPools\$poolName")) {
    New-WebAppPool -Name $poolName
}
Set-ItemProperty "IIS:\AppPools\$poolName" -Name "managedRuntimeVersion" -Value "" # No Managed Code
Set-ItemProperty "IIS:\AppPools\$poolName" -Name "managedPipelineMode" -Value 0   # Integrated (0)
Set-ItemProperty "IIS:\AppPools\$poolName" -Name "enable32BitAppOnWin64" -Value $false
Set-ItemProperty "IIS:\AppPools\$poolName" -Name "autoStart" -Value $true
Set-ItemProperty "IIS:\AppPools\$poolName" -Name "startMode" -Value 1            # AlwaysRunning (1)
Set-ItemProperty "IIS:\AppPools\$poolName" -Name "processModel.idleTimeout" -Value ([TimeSpan]::FromMinutes(0))

# 2. Create IIS Web Site bound to Application Pool
if (!(Test-Path "IIS:\Sites\$siteName")) {
    New-Website -Name $siteName -Port 443 -PhysicalPath $physicalPath -ApplicationPool $poolName -Ssl
} else {
    Set-ItemProperty "IIS:\Sites\$siteName" -Name "physicalPath" -Value $physicalPath
    Set-ItemProperty "IIS:\Sites\$siteName" -Name "applicationPool" -Value $poolName
}

# 3. Grant NTFS READ/EXECUTE permissions to AppPool Identity
$acl = Get-Acl $physicalPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS AppPool\$poolName", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.AddAccessRule($rule)
Set-Acl -Path $physicalPath -AclObject $acl

Write-Host "IIS Deployment provisioning complete for $siteName." -ForegroundColor Green
```
