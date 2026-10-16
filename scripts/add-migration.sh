#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 || ! "$1" =~ ^[A-Za-z][A-Za-z0-9_]*$ ]]; then
    echo "Usage: bash scripts/add-migration.sh <MigrationName>" >&2
    exit 1
fi

cd "$(dirname "${BASH_SOURCE[0]}")/.."
dotnet ef migrations add "$1" --project App/WBand.WebAPI --output-dir Data/Migrations
