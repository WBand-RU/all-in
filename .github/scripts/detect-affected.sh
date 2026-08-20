#!/usr/bin/env bash
set -euo pipefail

base_sha="${1:-}"
head_sha="${2:-HEAD}"

webapi=false
worker=false
web=false
backend_tests=false

if [[ -z "$base_sha" || "$base_sha" =~ ^0+$ ]] || ! git cat-file -e "${base_sha}^{commit}" 2>/dev/null; then
    changed_files="$(git ls-tree -r --name-only "$head_sha")"
else
    changed_files="$(git diff --name-only "$base_sha...$head_sha")"
fi

while IFS= read -r file; do
    [[ -z "$file" ]] && continue

    case "$file" in
        .github/workflows/*|.github/scripts/*|.dockerignore|.editorconfig|WBand.slnx|global.json|NuGet.Config|Directory.Build.*|Directory.Packages.props)
            webapi=true
            worker=true
            web=true
            backend_tests=true
            ;;
        App/web/*)
            web=true
            ;;
        Tests/WBand.Architecture.Tests/*)
            backend_tests=true
            ;;
        App/WBand.WebAPI/*|Common/Auth/*|Common/Shared/*|Modules/WBand.Modules.*/*)
            webapi=true
            backend_tests=true
            ;;
    esac

    case "$file" in
        App/WBand.MixerWorker/*|Common/Shared/*|Modules/WBand.Modules.BandModule/*|Modules/WBand.Modules.FileModule/*|Modules/WBand.Modules.MixerModule/*|Modules/WBand.Modules.PlaylistModule/*|Modules/WBand.Modules.SongModule/*|Modules/WBand.Modules.StemModule/*)
            worker=true
            backend_tests=true
            ;;
    esac
done <<< "$changed_files"

{
    echo "webapi=$webapi"
    echo "worker=$worker"
    echo "web=$web"
    echo "backend=$backend_tests"
} >> "$GITHUB_OUTPUT"

{
    echo "### Affected projects"
    echo "| Project | Changed |"
    echo "| --- | --- |"
    echo "| WBand.WebAPI | $webapi |"
    echo "| WBand.MixerWorker | $worker |"
    echo "| web | $web |"
    echo "| Backend tests | $backend_tests |"
} >> "$GITHUB_STEP_SUMMARY"
