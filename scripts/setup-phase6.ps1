$ErrorActionPreference = "Stop"
$dir = "c:\DEV\API PROJECT\STOCKOUT"

New-Item -Path "$dir\infrastructure\docker" -ItemType Directory -Force | Out-Null
New-Item -Path "$dir\.github\workflows" -ItemType Directory -Force | Out-Null
New-Item -Path "$dir\docs\architecture" -ItemType Directory -Force | Out-Null
New-Item -Path "$dir\docs\development" -ItemType Directory -Force | Out-Null
New-Item -Path "$dir\docs\deployment" -ItemType Directory -Force | Out-Null
New-Item -Path "$dir\docs\database" -ItemType Directory -Force | Out-Null
New-Item -Path "$dir\docs\setup" -ItemType Directory -Force | Out-Null

# Docker Compose
$dockerCompose = @"
services:
  rabbitmq:
    image: rabbitmq:3.13-management-alpine
    container_name: emcore-rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 5s
      retries: 5
    profiles:
      - rabbitmq

  redis:
    image: redis:7.2-alpine
    container_name: emcore-redis
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    profiles:
      - redis

  opensearch:
    image: opensearchproject/opensearch:2.13.0
    container_name: emcore-opensearch
    environment:
      - discovery.type=single-node
      - OPENSEARCH_INITIAL_ADMIN_PASSWORD=`${OPENSEARCH_PASSWORD:-Admin123!}`
    ports:
      - "9200:9200"
    volumes:
      - opensearch_data:/usr/share/opensearch/data
    healthcheck:
      test: ["CMD", "curl", "-k", "-f", "https://localhost:9200"]
      interval: 15s
      timeout: 5s
      retries: 5
    profiles:
      - opensearch

  minio:
    image: minio/minio
    container_name: emcore-minio
    command: server /data --console-address ":9001"
    environment:
      - MINIO_ROOT_USER=`${MINIO_USER:-admin}`
      - MINIO_ROOT_PASSWORD=`${MINIO_PASSWORD:-admin123}`
    ports:
      - "9000:9000"
      - "9001:9001"
    volumes:
      - minio_data:/data
    healthcheck:
      test: ["CMD", "mc", "ready", "local"]
      interval: 10s
      timeout: 5s
      retries: 5
    profiles:
      - minio

  otel:
    image: otel/opentelemetry-collector:0.100.0
    container_name: emcore-otel
    ports:
      - "4317:4317"
      - "4318:4318"
    profiles:
      - otel

volumes:
  rabbitmq_data:
  redis_data:
  opensearch_data:
  minio_data:
"@
Set-Content -Path "$dir\infrastructure\docker\docker-compose.local.yml" -Value $dockerCompose -Encoding utf8

$envExample = @"
# Local Docker Compose Environment
OPENSEARCH_PASSWORD=Admin123!
MINIO_USER=admin
MINIO_PASSWORD=admin123
"@
Set-Content -Path "$dir\infrastructure\docker\.env.example" -Value $envExample -Encoding utf8

# GitHub Actions
$prValidation = @"
name: PR Validation
on:
  pull_request:
    branches: [ "main" ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        global-json-file: global.json
    - name: Restore dependencies
      run: dotnet restore
    - name: Verify formatting
      run: dotnet format --verify-no-changes
    - name: Build
      run: dotnet build -c Release --no-restore
    - name: Test
      run: dotnet test -c Release --no-build
"@
Set-Content -Path "$dir\.github\workflows\pr-validation.yml" -Value $prValidation -Encoding utf8

$mainValidation = @"
name: Main Validation
on:
  push:
    branches: [ "main" ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        global-json-file: global.json
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build -c Release --no-restore
    - name: Test
      run: dotnet test -c Release --no-build
"@
Set-Content -Path "$dir\.github\workflows\main-validation.yml" -Value $mainValidation -Encoding utf8

$manualContainer = @"
name: Manual Container Build
on:
  workflow_dispatch:
    inputs:
      service:
        description: 'Service to build'
        required: true
        type: string
      tag:
        description: 'Image tag'
        required: true
        type: string
      push:
        description: 'Push image'
        required: false
        type: boolean
        default: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Build Docker Image
      run: docker build -t `${{ inputs.service }}:`${{ inputs.tag }} -f services/`${{ inputs.service }}/Dockerfile.Api .
"@
Set-Content -Path "$dir\.github\workflows\manual-container-build.yml" -Value $manualContainer -Encoding utf8

$dependabot = @"
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
"@
Set-Content -Path "$dir\.github\dependabot.yml" -Value $dependabot -Encoding utf8

# Local Scripts
$bootstrapLocalPs1 = @"
`$ErrorActionPreference = "Stop"
Write-Host "Bootstrapping Local Environment"
dotnet restore
Write-Host "Checking Docker..."
docker --version
Write-Host "To start infrastructure, use: docker compose -f infrastructure/docker/docker-compose.local.yml --profile rabbitmq --profile redis up -d"
"@
Set-Content -Path "$dir\scripts\bootstrap-local.ps1" -Value $bootstrapLocalPs1 -Encoding utf8

$buildPs1 = @"
`$ErrorActionPreference = "Stop"
dotnet restore
dotnet format --verify-no-changes
dotnet build -c Release --no-restore
"@
Set-Content -Path "$dir\scripts\build.ps1" -Value $buildPs1 -Encoding utf8

$testPs1 = @"
`$ErrorActionPreference = "Stop"
dotnet test -c Release --no-build
"@
Set-Content -Path "$dir\scripts\test.ps1" -Value $testPs1 -Encoding utf8

$shScript = @"
#!/bin/bash
set -e
echo "Run powershell equivalent."
"@
Set-Content -Path "$dir\scripts\bootstrap-local.sh" -Value $shScript -Encoding utf8
Set-Content -Path "$dir\scripts\build.sh" -Value $shScript -Encoding utf8
Set-Content -Path "$dir\scripts\test.sh" -Value $shScript -Encoding utf8

# Add basic documentation files
Set-Content -Path "$dir\docs\architecture\repository-structure.md" -Value "# Repository Structure" -Encoding utf8
Set-Content -Path "$dir\docs\architecture\dependency-rules.md" -Value "# Dependency Rules" -Encoding utf8
Set-Content -Path "$dir\docs\development\local-setup.md" -Value "# Local Setup" -Encoding utf8
Set-Content -Path "$dir\docs\development\configuration.md" -Value "# Configuration" -Encoding utf8
Set-Content -Path "$dir\docs\development\adding-a-service.md" -Value "# Adding a Service" -Encoding utf8
Set-Content -Path "$dir\docs\deployment\ecs-fargate-readiness.md" -Value "# ECS Fargate Readiness" -Encoding utf8
Set-Content -Path "$dir\docs\database\deferred-database-work.md" -Value "# Deferred Database Work" -Encoding utf8
Set-Content -Path "$dir\README.md" -Value "# EMCORE Platform" -Encoding utf8

Write-Host "Phase 6 configuration complete."
