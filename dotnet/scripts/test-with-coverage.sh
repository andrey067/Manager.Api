#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
rm -rf TestResults/coverage
mkdir -p TestResults/coverage

dotnet test Manager.Vsa.sln /p:CollectCoverage=true --verbosity minimal
echo Done
