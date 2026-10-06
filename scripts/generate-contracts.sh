#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tool_dir="$(mktemp -d "${TMPDIR:-/tmp}/netarca-xscgen.XXXXXX")"
trap 'rm -rf "$tool_dir"' EXIT

dotnet tool install dotnet-xscgen --tool-path "$tool_dir" --version 3.0.1240
python3 "$repo_root/scripts/generate-contracts.py" --xscgen "$tool_dir/xscgen"
