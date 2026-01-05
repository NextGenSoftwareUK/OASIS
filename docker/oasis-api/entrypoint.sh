#!/bin/bash
set -e

# Path to the config file
DNA_FILE="OASIS_DNA.json"

# Check if the environment variable is set
if [ ! -z "$OASIS_DNA__OASIS__Security__SecretKey" ]; then
    echo "Injecting SecretKey from environment variable..."
    # Escape special characters in the key if necessary (basic escaping)
    # Use sed to replace the SecretKey value. 
    # This assumes "SecretKey": "VALUE" format with double quotes.
    sed -i 's/"SecretKey": ".*"/"SecretKey": "'"$OASIS_DNA__OASIS__Security__SecretKey"'"/' "$DNA_FILE"
    
    # Verify (optional, be careful not to log the actual secret in production logs)
    if grep -q "$OASIS_DNA__OASIS__Security__SecretKey" "$DNA_FILE"; then
        echo "SecretKey injected successfully."
    else
        echo "WARNING: Failed to inject SecretKey."
    fi
else
    echo "No OASIS_DNA__OASIS__Security__SecretKey environment variable found. Using default/existing config."
fi

# Execute the main command
exec "$@"
