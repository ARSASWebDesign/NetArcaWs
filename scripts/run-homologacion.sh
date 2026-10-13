#!/usr/bin/env bash
set -euo pipefail
unset ARCA_QUERY_CUIT
unset ARCA_HOMOLOGY_PREFLIGHT_ONLY

# This script receives credentials only in the protected workflow step.
# Never enable shell tracing or print fiscal payloads or secret values.
if [[ "${GITHUB_REPOSITORY:-}" != 'ARSASWebDesign/NetArcaWs' || "${GITHUB_REF:-}" != 'refs/heads/main' || "${GITHUB_EVENT_NAME:-}" != 'workflow_dispatch' ]]; then
  echo 'Homologación requiere una ejecución manual desde main del repositorio oficial.' >&2
  exit 1
fi
for required in HOMO_CERTIFICATE_PEM HOMO_PRIVATE_KEY_PEM HOMOLOGATION_STATE_TOKEN ARCA_CUIT RUNNER_TEMP GITHUB_RUN_ID; do
  if [[ -z "${!required:-}" ]]; then
    echo "Falta configuración obligatoria: $required." >&2
    exit 1
  fi
done
if [[ ! "$ARCA_CUIT" =~ ^[0-9]{11}$ ]]; then
  echo 'ARCA_CUIT debe contener 11 dígitos.' >&2
  exit 1
fi

ARCA_HOMOLOGY_MODE="${ARCA_HOMOLOGY_MODE:-consultas}"
if [[ "$ARCA_HOMOLOGY_MODE" != 'consultas' && "$ARCA_HOMOLOGY_MODE" != 'emision' && "$ARCA_HOMOLOGY_MODE" != 'completa' ]]; then
  echo 'ARCA_HOMOLOGY_MODE debe ser consultas, emision o completa.' >&2
  exit 1
fi

if [[ "$ARCA_HOMOLOGY_MODE" == 'emision' ]]; then
  ARCA_HOMOLOGY_SERVICES=''
else
  ARCA_HOMOLOGY_SERVICES='wsfe,wsfex,wsmtxca,wscdc,wsfecred,padron-a4,padron-a5,padron-a10,padron-a13'
fi
export ARCA_HOMOLOGY_MODE ARCA_HOMOLOGY_SERVICES GITHUB_RUN_ID
export ARCA_REQUIRE_HOMOLOGY=1

dotnet_args=(test --project tests/NetArcaWs.IntegrationTests/NetArcaWs.IntegrationTests.csproj \
  --configuration Release --no-build --no-restore \
  --filter-method '*Explicitly_enabled_authenticated_scenarios_run_against_homologation*')

# A matrix preflight uses the exact C# validators and the same test entry point.
# It receives no certificate, issuer CUIT, or GitHub token and runs before PEM creation.
if [[ "$ARCA_HOMOLOGY_MODE" == 'emision' || "$ARCA_HOMOLOGY_MODE" == 'completa' ]]; then
  env -u HOMO_CERTIFICATE_PEM -u HOMO_PRIVATE_KEY_PEM -u WSAA_KEY_PASSWORD \
    -u ARCA_CUIT -u HOMOLOGATION_STATE -u HOMOLOGATION_STATE_TOKEN \
    -u WSAA_CERT_PATH -u WSAA_KEY_PATH -u GITHUB_TOKEN -u GH_TOKEN \
    ARCA_HOMOLOGY_PREFLIGHT_ONLY=1 dotnet "${dotnet_args[@]}"
fi

umask 077
credentials_dir=$(mktemp -d "$RUNNER_TEMP/netarcaws-homologacion.XXXXXX")
trap 'rm -rf -- "$credentials_dir"' EXIT
printf '%s' "$HOMO_CERTIFICATE_PEM" > "$credentials_dir/certificado.pem"
printf '%s' "$HOMO_PRIVATE_KEY_PEM" > "$credentials_dir/privada.key"
unset HOMO_CERTIFICATE_PEM HOMO_PRIVATE_KEY_PEM
export WSAA_CERT_PATH="$credentials_dir/certificado.pem"
export WSAA_KEY_PATH="$credentials_dir/privada.key"
dotnet "${dotnet_args[@]}"
