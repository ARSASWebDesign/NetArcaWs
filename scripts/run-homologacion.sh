#!/usr/bin/env bash
set -euo pipefail

# This script receives credentials only in the protected workflow step.
# Do not enable shell tracing, print payloads, or publish its temporary directory.
if [[ "${GITHUB_REPOSITORY:-}" != 'ARSASWebDesign/NetArcaWs' || "${GITHUB_REF:-}" != 'refs/heads/main' || "${GITHUB_EVENT_NAME:-}" != 'workflow_dispatch' ]]; then
  echo 'Homologación requiere una ejecución manual desde main del repositorio oficial.' >&2
  exit 1
fi
for required in HOMO_CERTIFICATE_PEM HOMO_PRIVATE_KEY_PEM ARCA_CUIT RUNNER_TEMP; do
  if [[ -z "${!required:-}" ]]; then
    echo "Falta configuración obligatoria: $required." >&2
    exit 1
  fi
done
if [[ "${WSAA_SERVICE:-}" != 'wsfe' || "${ARCA_HOMOLOGY_SERVICES:-}" != 'wsfe' ]]; then
  echo 'Este flujo solo admite WSAA_SERVICE=wsfe y ARCA_HOMOLOGY_SERVICES=wsfe.' >&2
  exit 1
fi
if [[ ! "$ARCA_CUIT" =~ ^[0-9]{11}$ ]]; then
  echo 'ARCA_CUIT debe contener 11 dígitos.' >&2
  exit 1
fi
umask 077
credentials_dir=$(mktemp -d "$RUNNER_TEMP/netarcaws-homologacion.XXXXXX")
trap 'rm -rf -- "$credentials_dir"' EXIT
printf '%s' "$HOMO_CERTIFICATE_PEM" > "$credentials_dir/certificado.pem"
printf '%s' "$HOMO_PRIVATE_KEY_PEM" > "$credentials_dir/privada.key"
unset HOMO_CERTIFICATE_PEM HOMO_PRIVATE_KEY_PEM
export WSAA_CERT_PATH="$credentials_dir/certificado.pem"
export WSAA_KEY_PATH="$credentials_dir/privada.key"
export ARCA_REQUIRE_HOMOLOGY=1
# The authenticated lookup obtains a ticket and reuses it within this process.
# Do not run the independent WSAA test too: a second login can be rejected by WSAA.
dotnet test --project tests/NetArcaWs.IntegrationTests/NetArcaWs.IntegrationTests.csproj \
  --configuration Release --no-build --no-restore \
  --filter-method '*Explicitly_enabled_authenticated_read_only_lookups_run_against_homologation*'
