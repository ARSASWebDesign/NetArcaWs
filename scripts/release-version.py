#!/usr/bin/env python3
"""Validate the shared NuGet version and, on a release, its exact v-prefixed tag."""
import os
import re
import xml.etree.ElementTree as ET
from pathlib import Path

root = Path(__file__).resolve().parents[1]
projects = [root / 'src/NetArcaWs/NetArcaWs.csproj', root / 'src/NetArcaWs.Tool/NetArcaWs.Tool.csproj']
versions = [ET.parse(path).findtext('./PropertyGroup/Version') for path in projects]
version = versions[0]
pattern = r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9a-z-]+(?:\.[0-9a-z-]+)*))?'
match = re.fullmatch(pattern, version or '')
if not match or versions[1] != version:
    raise SystemExit('Both projects must declare the same SemVer version (lowercase prerelease, no build metadata).')
if match[4] and any(part.isdigit() and len(part) > 1 and part.startswith('0') for part in match[4].split('.')):
    raise SystemExit('Numeric prerelease identifiers cannot have leading zeroes.')
if os.environ.get('GITHUB_EVENT_NAME') == 'release':
    if os.environ.get('RELEASE_TAG') != f'v{version}':
        raise SystemExit('Release tag must equal v followed by the version in both projects.')
    expected_prerelease = 'true' if match[4] else 'false'
    if os.environ.get('RELEASE_PRERELEASE') != expected_prerelease:
        raise SystemExit('GitHub prerelease flag must match the NuGet version suffix.')
print(version)
if output := os.environ.get('GITHUB_OUTPUT'):
    with open(output, 'a', encoding='utf-8') as stream:
        stream.write(f'version={version}\n')
