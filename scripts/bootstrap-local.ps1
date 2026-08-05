$ErrorActionPreference = "Stop"
Write-Host "Bootstrapping Local Environment"
dotnet restore
Write-Host "Checking Docker..."
docker --version
Write-Host "To start infrastructure, use: docker compose -f infrastructure/docker/docker-compose.local.yml --profile rabbitmq --profile redis up -d"
