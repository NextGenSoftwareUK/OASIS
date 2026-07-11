#!/bin/sh
if [ -n "$OASIS_DNA_JSON" ]; then
    printf '%s' "$OASIS_DNA_JSON" > /app/OASIS_DNA.json
    echo "OASIS_DNA.json written ($(wc -c < /app/OASIS_DNA.json) bytes)"
else
    echo "WARNING: OASIS_DNA_JSON not set - OASIS_DNA.json will not be written"
fi
exec dotnet NextGenSoftware.OASIS.Web7.WebAPI.dll
