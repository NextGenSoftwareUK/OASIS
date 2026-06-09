#!/bin/sh
# Write OASIS_DNA.json from env var if provided (set in Railway Variables)
if [ -n "$OASIS_DNA_JSON" ]; then
    echo "$OASIS_DNA_JSON" > /app/OASIS_DNA.json
fi
exec dotnet NextGenSoftware.OASIS.API.ONODE.WebAPI.dll
