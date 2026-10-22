#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${NUGET_CONFIG:-}" ]]; then
    echo "NUGET_CONFIG secret is required to restore the private BFF package." >&2
    exit 1
fi
: "${RUNNER_TEMP:?RUNNER_TEMP is required}"
: "${GITHUB_ENV:?GITHUB_ENV is required}"
umask 077
config_path="$RUNNER_TEMP/wband-nuget.config"
printf '%s' "$NUGET_CONFIG" > "$config_path"
printf 'NUGET_CONFIG_PATH=%s\n' "$config_path" >> "$GITHUB_ENV"
