#!/bin/sh
if [ -n "$OASIS_DNA_JSON" ]; then
    printf '%s' "$OASIS_DNA_JSON" > /app/OASIS_DNA.json
    echo "OASIS_DNA.json written ($(wc -c < /app/OASIS_DNA.json) bytes)"
else
    echo "WARNING: OASIS_DNA_JSON not set - OASIS_DNA.json will not be written"
fi
case "${OASIS_SANDBOX_IPFS:-false}" in
    true) exec node /app/host-sandbox-ipfs.cjs ;;
    false) exec dotnet NextGenSoftware.OASIS.API.ONODE.WebAPI.dll ;;
    *) echo "Invalid OASIS_SANDBOX_IPFS value" >&2; exit 1 ;;
esac
