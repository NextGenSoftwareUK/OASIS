#!/bin/bash

# OASIS .NET SDK Setup Script for macOS
# This script helps set up .NET 9.0 SDK on your new MacBook

set -e

echo "=========================================="
echo "OASIS .NET SDK Setup for macOS"
echo "=========================================="
echo ""

# Check if .NET is already installed
if command -v dotnet &> /dev/null; then
    echo "✓ .NET SDK is already installed!"
    dotnet --version
    dotnet --list-sdks
    echo ""
    echo "If you need .NET 9.0 specifically, please install it manually."
    exit 0
fi

echo "Step 1: Installing Homebrew (if not already installed)..."
if ! command -v brew &> /dev/null; then
    echo "Homebrew not found. Installing Homebrew..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    
    # Add Homebrew to PATH (for Apple Silicon Macs)
    if [[ -f /opt/homebrew/bin/brew ]]; then
        echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> ~/.zshrc
        eval "$(/opt/homebrew/bin/brew shellenv)"
    # For Intel Macs
    elif [[ -f /usr/local/bin/brew ]]; then
        echo 'eval "$(/usr/local/bin/brew shellenv)"' >> ~/.zshrc
        eval "$(/usr/local/bin/brew shellenv)"
    fi
else
    echo "✓ Homebrew is already installed"
fi

echo ""
echo "Step 2: Installing .NET 9.0 SDK via Homebrew..."
brew install --cask dotnet-sdk

echo ""
echo "Step 3: Verifying installation..."
if command -v dotnet &> /dev/null; then
    echo "✓ .NET SDK installed successfully!"
    dotnet --version
    dotnet --list-sdks
else
    echo "⚠ .NET SDK installation may have failed. Please check the output above."
    echo ""
    echo "Manual installation option:"
    echo "1. Visit: https://dotnet.microsoft.com/download/dotnet/9.0"
    echo "2. Download the macOS installer (.pkg file)"
    echo "3. Run the installer"
    exit 1
fi

echo ""
echo "Step 4: Restoring OASIS API dependencies..."
OASIS_DIR="$HOME/OASIS_CLEAN"
if [ -d "$OASIS_DIR/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI" ]; then
    cd "$OASIS_DIR/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
    echo "Running 'dotnet restore'..."
    dotnet restore
    echo "✓ Dependencies restored!"
else
    echo "⚠ OASIS API project not found at expected location"
    echo "   Expected: $OASIS_DIR/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
fi

echo ""
echo "=========================================="
echo "Setup Complete!"
echo "=========================================="
echo ""
echo "Next steps:"
echo "1. Open Cursor"
echo "2. File → Open Folder"
echo "3. Navigate to: $OASIS_DIR/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
echo "4. Install C# extension in Cursor (Cmd+Shift+X, search for 'C#')"
echo "5. Build the project: dotnet build --configuration Release"
echo ""

