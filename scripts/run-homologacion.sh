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
if [[ ! "$ARCA_CUIT" =~ ^[0-9]{11}$ ]]; then
  echo 'ARCA_CUIT debe contener 11 dígitos.' >&2
  exit 1
fi

ARCA_HOMOLOGY_MODE="${ARCA_HOMOLOGY_MODE:-consultas}"
ARCA_HOMOLOGY_SERVICES="${ARCA_HOMOLOGY_SERVICES-wsfe}"
ARCA_HOMOLOGY_POINT_OF_SALE="${ARCA_HOMOLOGY_POINT_OF_SALE:-}"
ARCA_HOMOLOGY_VOUCHER_TYPE="${ARCA_HOMOLOGY_VOUCHER_TYPE:-11}"
ARCA_HOMOLOGY_VOUCHER_NUMBER="${ARCA_HOMOLOGY_VOUCHER_NUMBER:-}"

if [[ "$ARCA_HOMOLOGY_MODE" != 'consultas' && "$ARCA_HOMOLOGY_MODE" != 'emision' ]]; then
  echo 'ARCA_HOMOLOGY_MODE debe ser consultas o emision.' >&2
  exit 1
fi

if [[ "$ARCA_HOMOLOGY_MODE" == 'emision' ]]; then
  unset ARCA_QUERY_CUIT
  if [[ "$ARCA_HOMOLOGY_SERVICES" != 'wsfe' ]]; then
    echo 'La emisión solo admite ARCA_HOMOLOGY_SERVICES=wsfe.' >&2
    exit 1
  fi
  if [[ ! "$ARCA_HOMOLOGY_POINT_OF_SALE" =~ ^[0-9]+$ || ${#ARCA_HOMOLOGY_POINT_OF_SALE} -gt 5 ]] ||
     (( 10#$ARCA_HOMOLOGY_POINT_OF_SALE < 1 || 10#$ARCA_HOMOLOGY_POINT_OF_SALE > 99999 )); then
    echo 'ARCA_HOMOLOGY_POINT_OF_SALE debe ser un entero entre 1 y 99999 para emisión.' >&2
    exit 1
  fi
  if [[ "$ARCA_HOMOLOGY_VOUCHER_TYPE" != '6' && "$ARCA_HOMOLOGY_VOUCHER_TYPE" != '11' ]]; then
    echo 'ARCA_HOMOLOGY_VOUCHER_TYPE debe ser 6 u 11 para emisión.' >&2
    exit 1
  fi
  if [[ ! "$ARCA_HOMOLOGY_VOUCHER_NUMBER" =~ ^[0-9]+$ || ${#ARCA_HOMOLOGY_VOUCHER_NUMBER} -gt 8 ]] ||
     (( 10#$ARCA_HOMOLOGY_VOUCHER_NUMBER < 1 || 10#$ARCA_HOMOLOGY_VOUCHER_NUMBER > 99999999 )); then
    echo 'ARCA_HOMOLOGY_VOUCHER_NUMBER debe ser un entero explícito entre 1 y 99999999 para emisión.' >&2
    exit 1
  fi
else
  if [[ -z "$ARCA_HOMOLOGY_SERVICES" ]]; then
    echo 'ARCA_HOMOLOGY_SERVICES debe incluir al menos un servicio admitido.' >&2
    exit 1
  fi
  IFS=',' read -r -a selected_services <<< "$ARCA_HOMOLOGY_SERVICES"
  has_padron_service=0
  for service in "${selected_services[@]}"; do
    case "$service" in
      wsfe|wsfex|wsmtxca|wscdc|wsfecred|padron-a4|padron-a5|padron-a10|padron-a13) ;;
      *)
        echo 'ARCA_HOMOLOGY_SERVICES contiene un servicio no admitido.' >&2
        exit 1
        ;;
    esac
    case "$service" in
      padron-*) has_padron_service=1 ;;
    esac
  done
  if [[ "$has_padron_service" == '1' && ! "${ARCA_QUERY_CUIT:-}" =~ ^[0-9]{11}$ ]]; then
    echo 'ARCA_QUERY_CUIT es obligatorio para servicios de padrón y debe contener 11 dígitos.' >&2
    exit 1
  fi
  if [[ "$has_padron_service" != '1' ]]; then
    unset ARCA_QUERY_CUIT
  fi
fi

export ARCA_HOMOLOGY_MODE ARCA_HOMOLOGY_SERVICES ARCA_HOMOLOGY_POINT_OF_SALE
export ARCA_HOMOLOGY_VOUCHER_TYPE ARCA_HOMOLOGY_VOUCHER_NUMBER
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
  --filter-method '*Explicitly_enabled_authenticated_scenarios_run_against_homologation*'
