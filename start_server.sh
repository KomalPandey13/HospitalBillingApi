#!/usr/bin/env bash
# Start script for Hospital Billing & Fee Management System API

set -e

PORT=${1:-5218}
export DOTNET_CLI_HOME=/Users/mac
export HOME=/Users/mac
export ASPNETCORE_URLS="http://localhost:${PORT}"
export ASPNETCORE_ENVIRONMENT="Development"

echo "================================================================="
echo " Starting Hospital Billing & Fee Management System API"
echo " URL:         http://localhost:${PORT}"
echo " Swagger UI:  http://localhost:${PORT}/swagger or http://localhost:${PORT}/"
echo " Database:    hospital_billing.db (SQLite)"
echo "================================================================="

dotnet run --project /Users/mac/abhishek/HospitalBillingApi/HospitalBillingApi.csproj
