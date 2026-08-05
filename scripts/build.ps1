$ErrorActionPreference = "Stop"
dotnet restore
dotnet format --verify-no-changes
dotnet build -c Release --no-restore
