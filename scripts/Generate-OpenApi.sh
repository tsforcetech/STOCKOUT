#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
EXPORT_PATH="${REPO_ROOT}/contracts/openapi"

export EMCORE_OPENAPI_EXPORT_PATH="${EXPORT_PATH}"

echo "================================================================="
echo " EMCORE Platform - Automated OpenAPI Specification Generator     "
echo "================================================================="
echo "Target Export Path: ${EXPORT_PATH}"

echo ""
echo "[1/2] Executing WebApplicationFactory OpenAPI generation tests..."
dotnet test "${REPO_ROOT}/tests/architecture/Emcore.OpenApi.Tests/Emcore.OpenApi.Tests.csproj" -c Release --filter "FullyQualifiedName~GenerateAndValidateOpenApiContract" --logger "console;verbosity=minimal"

echo ""
echo "[2/2] Verifying generated contract documents..."
find "${EXPORT_PATH}" -name "openapi.json" -type f | while read -r f; do
    echo "  -> ${f#"$REPO_ROOT/"} ($(du -k "$f" | cut -f1) KB)"
done

echo ""
echo "OpenAPI generation completed successfully!"
