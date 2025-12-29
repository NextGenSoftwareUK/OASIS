#!/bin/bash

# OASIS SDK Installation Script for macOS
# This script installs all required SDKs and dependencies for OASIS development

set -e

echo "=========================================="
echo "OASIS SDK Installation Script"
echo "=========================================="
echo ""

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Step 1: Check/Install Xcode Command Line Tools
echo -e "${YELLOW}Step 1: Checking Xcode Command Line Tools...${NC}"
if xcode-select -p &>/dev/null; then
    echo -e "${GREEN}✓ Xcode Command Line Tools already installed${NC}"
else
    echo "Xcode Command Line Tools not found. Installing..."
    xcode-select --install
    echo "Please complete the Xcode Command Line Tools installation, then run this script again."
    exit 0
fi

# Step 2: Install Homebrew
echo ""
echo -e "${YELLOW}Step 2: Checking Homebrew...${NC}"
if command -v brew &> /dev/null; then
    echo -e "${GREEN}✓ Homebrew already installed: $(brew --version | head -1)${NC}"
else
    echo "Installing Homebrew..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    
    # Add Homebrew to PATH
    if [[ -f /opt/homebrew/bin/brew ]]; then
        echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> ~/.zshrc
        eval "$(/opt/homebrew/bin/brew shellenv)"
        echo -e "${GREEN}✓ Homebrew installed (Apple Silicon)${NC}"
    elif [[ -f /usr/local/bin/brew ]]; then
        echo 'eval "$(/usr/local/bin/brew shellenv)"' >> ~/.zshrc
        eval "$(/usr/local/bin/brew shellenv)"
        echo -e "${GREEN}✓ Homebrew installed (Intel)${NC}"
    fi
fi

# Step 3: Install .NET 9.0 SDK
echo ""
echo -e "${YELLOW}Step 3: Installing .NET 9.0 SDK...${NC}"
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo -e "${GREEN}✓ .NET SDK already installed: $DOTNET_VERSION${NC}"
    
    # Check if it's .NET 9.0
    if [[ "$DOTNET_VERSION" =~ ^9\. ]]; then
        echo -e "${GREEN}✓ .NET 9.0 is installed${NC}"
    else
        echo -e "${YELLOW}⚠ Found .NET $DOTNET_VERSION, but 9.0 is recommended${NC}"
        read -p "Install .NET 9.0 anyway? (y/N): " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            brew install --cask dotnet-sdk
        fi
    fi
else
    echo "Installing .NET 9.0 SDK via Homebrew..."
    brew install --cask dotnet-sdk
    echo -e "${GREEN}✓ .NET SDK installed${NC}"
fi

# Step 4: Verify installations
echo ""
echo -e "${YELLOW}Step 4: Verifying installations...${NC}"
echo ""
echo "Homebrew:"
brew --version | head -1
echo ""
echo ".NET SDK:"
dotnet --version
dotnet --list-sdks
echo ""

# Step 5: Restore OASIS dependencies
echo ""
echo -e "${YELLOW}Step 5: Restoring OASIS API dependencies...${NC}"
OASIS_DIR="$HOME/OASIS_CLEAN"
API_PROJECT="$OASIS_DIR/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"

if [ -d "$API_PROJECT" ]; then
    cd "$API_PROJECT"
    echo "Running 'dotnet restore'..."
    dotnet restore
    echo -e "${GREEN}✓ Dependencies restored${NC}"
else
    echo -e "${RED}⚠ API project not found at: $API_PROJECT${NC}"
fi

# Step 6: Build OASIS API
echo ""
echo -e "${YELLOW}Step 6: Building OASIS API...${NC}"
if [ -d "$API_PROJECT" ]; then
    cd "$API_PROJECT"
    echo "Building in Release configuration..."
    if dotnet build --configuration Release; then
        echo -e "${GREEN}✓ Build successful!${NC}"
    else
        echo -e "${RED}⚠ Build completed with warnings/errors${NC}"
    fi
else
    echo -e "${RED}⚠ Cannot build - API project not found${NC}"
fi

# Summary
echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Installation Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Installed:"
echo "  ✓ Xcode Command Line Tools"
echo "  ✓ Homebrew"
echo "  ✓ .NET 9.0 SDK"
echo ""
echo "Next steps:"
echo "1. Open Cursor"
echo "2. File → Open Folder → $OASIS_DIR"
echo "3. Install C# extension in Cursor (Cmd+Shift+X)"
echo "4. Start developing!"
echo ""


