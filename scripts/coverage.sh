#!/usr/bin/env bash
set -euo pipefail
results_dir="${1:-artifacts/qualification}"
mkdir -p "$results_dir"
run_dir="$(mktemp -d "$results_dir/collector.XXXXXX")"
dotnet test ManagedCode.TimeSeries.Tests/ManagedCode.TimeSeries.Tests.csproj \
  --configuration Release --no-build --no-restore \
  --logger 'trx;LogFileName=timeseries.trx' --results-directory "$run_dir" \
  --collect 'XPlat Code Coverage' --settings scripts/coverage.runsettings
python3 scripts/verify-coverage.py "$run_dir" "$results_dir"
