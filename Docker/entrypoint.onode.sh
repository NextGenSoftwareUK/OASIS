#!/bin/sh
if [ -n "$OASIS_DNA_JSON" ]; then
    printf '%s' "$OASIS_DNA_JSON" > /app/OASIS_DNA.json
    echo "OASIS_DNA.json written ($(wc -c < /app/OASIS_DNA.json) bytes)"
else
    echo "WARNING: OASIS_DNA_JSON not set - OASIS_DNA.json will not be written"
fi
for value in "${OASIS_SANDBOX_IPFS:-false}" "${OASIS_SANDBOX_SOLID:-false}" "${OASIS_SANDBOX_POUCHDB:-false}" \
    "${OASIS_SANDBOX_ORBITDB:-false}" "${OASIS_SANDBOX_GUN:-false}" "${OASIS_SANDBOX_WAKU:-false}" "${OASIS_SANDBOX_DUCKDB:-false}" "${OASIS_SANDBOX_NATS:-false}" "${OASIS_SANDBOX_DAPR:-false}"; do
    case "$value" in
        true|false) ;;
        *) echo "Invalid native sandbox runtime flag" >&2; exit 1 ;;
    esac
done
if [ "${OASIS_SANDBOX_IPFS:-false}" = true ] || [ "${OASIS_SANDBOX_SOLID:-false}" = true ] || \
    [ "${OASIS_SANDBOX_POUCHDB:-false}" = true ] || [ "${OASIS_SANDBOX_ORBITDB:-false}" = true ] || \
    [ "${OASIS_SANDBOX_GUN:-false}" = true ] || [ "${OASIS_SANDBOX_WAKU:-false}" = true ] || \
    [ "${OASIS_SANDBOX_DUCKDB:-false}" = true ] || [ "${OASIS_SANDBOX_NATS:-false}" = true ] || \
    [ "${OASIS_SANDBOX_DAPR:-false}" = true ]; then
    exec node /app/host-sandbox-ipfs.cjs
fi
exec dotnet NextGenSoftware.OASIS.API.ONODE.WebAPI.dll
