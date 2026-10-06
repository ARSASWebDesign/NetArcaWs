#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tool_dir="$(mktemp -d "${TMPDIR:-/tmp}/netarca-xscgen.XXXXXX")"
trap 'rm -rf "$tool_dir"' EXIT

dotnet tool install dotnet-xscgen --tool-path "$tool_dir" --version 3.0.1240
cd "$repo_root"
dotnet run --project tools/NetArcaWs.Build -- contracts --xscgen "$tool_dir/xscgen"
