#!/bin/bash

# OASIS Setup Script for New MacBook
# This script helps move OASIS_CLEAN from external drive to MacBook and set it up

set -e  # Exit on error

echo "🚀 OASIS MacBook Setup Script"
echo "=============================="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Configuration
SOURCE_DIR="/Volumes/Storage/OASIS_CLEAN"
DEST_DIR="$HOME/OASIS_CLEAN"
GITHUB_REPO="https://github.com/NextGenSoftwareUK/OASIS.git"

# Step 1: Check prerequisites
echo -e "${YELLOW}Step 1: Checking prerequisites...${NC}"

# Check for git (optional - only needed for GitHub sync)
GIT_AVAILABLE=false
if command -v git &> /dev/null; then
    if git --version &> /dev/null; then
        echo -e "${GREEN}✅ Git installed: $(git --version 2>&1 | head -1)${NC}"
        GIT_AVAILABLE=true
    else
        echo -e "${YELLOW}⚠️  Git found but requires Xcode Command Line Tools${NC}"
        echo "   Git is optional - you can skip it and set up later"
        echo "   To install: xcode-select --install"
        read -p "   Continue without Git? (Y/n): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Nn]$ ]]; then
            GIT_AVAILABLE=false
        else
            echo "Please install Xcode Command Line Tools first: xcode-select --install"
            exit 1
        fi
    fi
else
    echo -e "${YELLOW}⚠️  Git not found (optional)${NC}"
    echo "   Git is only needed for GitHub sync. You can set it up later."
fi

# Check for .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo -e "${YELLOW}⚠️  .NET SDK not found${NC}"
    echo "Please install .NET 9.0 SDK from: https://dotnet.microsoft.com/download"
    echo "Or run: brew install --cask dotnet-sdk"
    read -p "Press Enter after installing .NET SDK, or Ctrl+C to exit..."
else
    DOTNET_VERSION=$(dotnet --version)
    echo -e "${GREEN}✅ .NET SDK installed: $DOTNET_VERSION${NC}"
    
    # Check if it's .NET 9.0 or compatible
    if [[ ! "$DOTNET_VERSION" =~ ^9\. ]]; then
        echo -e "${YELLOW}⚠️  Warning: .NET 9.0 recommended, but found $DOTNET_VERSION${NC}"
    fi
fi

# Step 2: Check source directory
echo ""
echo -e "${YELLOW}Step 2: Checking source directory...${NC}"
if [ ! -d "$SOURCE_DIR" ]; then
    echo -e "${RED}❌ Source directory not found: $SOURCE_DIR${NC}"
    echo "Please ensure your external drive is mounted"
    exit 1
else
    echo -e "${GREEN}✅ Source directory found${NC}"
fi

# Step 3: Copy codebase to MacBook
echo ""
echo -e "${YELLOW}Step 3: Copying codebase to MacBook...${NC}"
if [ -d "$DEST_DIR" ]; then
    echo -e "${YELLOW}⚠️  Destination directory already exists: $DEST_DIR${NC}"
    read -p "Do you want to remove it and copy fresh? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "Removing existing directory..."
        rm -rf "$DEST_DIR"
    else
        echo "Skipping copy. Using existing directory."
        SKIP_COPY=true
    fi
fi

if [ "$SKIP_COPY" != true ]; then
    echo "Copying from $SOURCE_DIR to $DEST_DIR..."
    echo "This may take a while depending on the size..."
    
    # Use rsync for efficient copying (preserves permissions, can resume)
    rsync -av --progress \
        --exclude='.git' \
        --exclude='bin' \
        --exclude='obj' \
        --exclude='node_modules' \
        --exclude='*.log' \
        "$SOURCE_DIR/" "$DEST_DIR/"
    
    echo -e "${GREEN}✅ Copy completed${NC}"
    
    # Copy .git directory separately to preserve git history
    echo "Copying .git directory..."
    cp -R "$SOURCE_DIR/.git" "$DEST_DIR/.git"
    echo -e "${GREEN}✅ Git history preserved${NC}"
fi

# Step 4: Verify Git remote (optional)
if [ "$GIT_AVAILABLE" = true ]; then
    echo ""
    echo -e "${YELLOW}Step 4: Verifying Git configuration...${NC}"
    cd "$DEST_DIR"
    
    # Check current remote
    CURRENT_REMOTE=$(git remote get-url origin 2>/dev/null || echo "")
    if [ -z "$CURRENT_REMOTE" ]; then
        echo "Adding GitHub remote..."
        git remote add origin "$GITHUB_REPO"
        echo -e "${GREEN}✅ GitHub remote added${NC}"
    elif [ "$CURRENT_REMOTE" != "$GITHUB_REPO" ]; then
        echo -e "${YELLOW}⚠️  Current remote: $CURRENT_REMOTE${NC}"
        echo "Expected: $GITHUB_REPO"
        read -p "Update remote to GitHub? (y/N): " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            git remote set-url origin "$GITHUB_REPO"
            echo -e "${GREEN}✅ Remote updated${NC}"
        fi
    else
        echo -e "${GREEN}✅ Git remote correctly configured: $CURRENT_REMOTE${NC}"
    fi
    
    # Fetch latest from GitHub
    echo "Fetching latest from GitHub..."
    git fetch origin || echo -e "${YELLOW}⚠️  Could not fetch (may need authentication)${NC}"
else
    echo ""
    echo -e "${YELLOW}Step 4: Skipping Git configuration (Git not available)${NC}"
    echo "   You can set up Git later by:"
    echo "   1. Install Xcode Command Line Tools: xcode-select --install"
    echo "   2. Then run: cd ~/OASIS_CLEAN && git remote add origin $GITHUB_REPO"
fi

# Step 5: Restore .NET dependencies
echo ""
echo -e "${YELLOW}Step 5: Restoring .NET dependencies...${NC}"
API_PROJECT="$DEST_DIR/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj"

if [ -f "$API_PROJECT" ]; then
    cd "$(dirname "$API_PROJECT")"
    echo "Running dotnet restore..."
    dotnet restore || echo -e "${YELLOW}⚠️  Some restore warnings may be normal${NC}"
    echo -e "${GREEN}✅ Dependencies restored${NC}"
else
    echo -e "${RED}❌ API project not found: $API_PROJECT${NC}"
fi

# Step 6: Build OASIS API
echo ""
echo -e "${YELLOW}Step 6: Building OASIS API...${NC}"
if [ -f "$API_PROJECT" ]; then
    cd "$(dirname "$API_PROJECT")"
    echo "Building in Release configuration..."
    if dotnet build --configuration Release; then
        echo -e "${GREEN}✅ Build successful!${NC}"
    else
        echo -e "${RED}❌ Build failed. Please check errors above.${NC}"
        exit 1
    fi
else
    echo -e "${RED}❌ Cannot build - API project not found${NC}"
fi

# Summary
echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}✅ Setup Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Your OASIS codebase is now at: $DEST_DIR"
echo ""
echo "Next steps:"
echo "1. Open Cursor and open the folder: $DEST_DIR"
echo "2. Or open just the API: $DEST_DIR/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
echo "3. Install C# extension in Cursor if needed"
echo "4. Run the API: cd $DEST_DIR/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI && dotnet run --configuration Release"
echo ""
if [ "$GIT_AVAILABLE" = true ]; then
    echo "GitHub Repository: $GITHUB_REPO"
    echo "✅ Git is configured and ready"
else
    echo "⚠️  Git not set up yet. To link to GitHub later:"
    echo "   1. Install: xcode-select --install"
    echo "   2. Then: cd $DEST_DIR && git remote add origin $GITHUB_REPO"
fi
echo ""

