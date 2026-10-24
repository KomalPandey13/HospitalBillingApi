#!/usr/bin/env bash
set -e

PORT=5218
export DOTNET_CLI_HOME=/Users/mac
export HOME=/Users/mac
export ASPNETCORE_URLS="http://127.0.0.1:${PORT}"
export ASPNETCORE_ENVIRONMENT="Development"

echo "Starting HospitalBillingApi on ${ASPNETCORE_URLS}..."
dotnet run --no-build --project /Users/mac/abhishek/HospitalBillingApi/HospitalBillingApi.csproj &
SERVER_PID=$!

cleanup() {
    echo "Stopping server (PID: ${SERVER_PID})..."
    kill ${SERVER_PID} 2>/dev/null || true
    wait ${SERVER_PID} 2>/dev/null || true
}
trap cleanup EXIT

echo "Running endpoint tests..."
python3 /Users/mac/abhishek/HospitalBillingApi/test_endpoints.py

echo "Tests completed successfully!"
