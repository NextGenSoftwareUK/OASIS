#!/bin/bash

# Script to update OASIS_DNA.json in the running ECS container
# This script will be executed on the ECS container

echo "Updating OASIS_DNA.json to use mainnet Solana RPC..."

# Create a backup of the original file
cp /app/OASIS_DNA.json /app/OASIS_DNA.json.backup

# Update the Solana connection string to mainnet
sed -i 's|"ConnectionString": "https://api.devnet.solana.com"|"ConnectionString": "https://api.mainnet-beta.solana.com"|g' /app/OASIS_DNA.json

echo "OASIS_DNA.json updated successfully!"
echo "Original file backed up as OASIS_DNA.json.backup"

# Verify the change
echo "Verifying the change:"
grep -A 5 "SolanaOASIS" /app/OASIS_DNA.json

echo "Restarting the application to pick up the new configuration..."
# Note: The application will need to be restarted to pick up the new configuration


