# New MacBook Setup Guide for OASIS

This guide will help you move OASIS_CLEAN from your external drive to your MacBook and get everything working with GitHub and a stable API build.

## Quick Setup (Automated)

### Step 1: Install Prerequisites

1. **Install Xcode Command Line Tools** (Required for Git):
   ```bash
   xcode-select --install
   ```
   Or download from: https://developer.apple.com/download/all/

2. **Install .NET 9.0 SDK**:
   - Download from: https://dotnet.microsoft.com/download
   - Or using Homebrew: `brew install --cask dotnet-sdk`
   - Verify: `dotnet --version` (should show 9.x.x)

### Step 2: Run Setup Script

1. Open Terminal
2. Navigate to the external drive:
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN
   ```
3. Run the setup script:
   ```bash
   ./setup_new_mac.sh
   ```

The script will:
- ✅ Check prerequisites
- ✅ Copy codebase to `~/OASIS_CLEAN`
- ✅ Preserve Git history
- ✅ Configure GitHub remote
- ✅ Restore .NET dependencies
- ✅ Build the OASIS API

## Manual Setup (If Script Doesn't Work)

### Step 1: Copy Codebase

```bash
# Create destination directory
mkdir -p ~/OASIS_CLEAN

# Copy everything (excluding build artifacts)
rsync -av --progress \
  --exclude='.git' \
  --exclude='bin' \
  --exclude='obj' \
  --exclude='node_modules' \
  /Volumes/Storage/OASIS_CLEAN/ ~/OASIS_CLEAN/

# Copy .git directory separately
cp -R /Volumes/Storage/OASIS_CLEAN/.git ~/OASIS_CLEAN/.git
```

### Step 2: Configure Git

```bash
cd ~/OASIS_CLEAN

# Verify remote
git remote -v

# If needed, set GitHub remote
git remote set-url origin https://github.com/NextGenSoftwareUK/OASIS.git

# Fetch latest
git fetch origin
```

### Step 3: Build OASIS API

```bash
cd ~/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI

# Restore dependencies
dotnet restore

# Build in Release mode
dotnet build --configuration Release
```

### Step 4: Test Run

```bash
# Run the API
dotnet run --configuration Release
```

The API should start on:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

## Open in Cursor

1. Open Cursor
2. **File → Open Folder**
3. Navigate to: `~/OASIS_CLEAN`
   - Or just the API: `~/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI`
4. Install C# extension if prompted

## Verify Everything Works

```bash
# Check .NET
dotnet --version

# Check Git
git --version
git remote -v

# Check API builds
cd ~/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet build --configuration Release
```

## GitHub Repository

- **Main Repo**: https://github.com/NextGenSoftwareUK/OASIS.git
- **Current Remote**: Already configured as `origin`

## Troubleshooting

### Issue: "xcode-select: error: no developer tools were found"
**Solution**: Install Xcode Command Line Tools:
```bash
xcode-select --install
```

### Issue: ".NET not found"
**Solution**: Install .NET 9.0 SDK from https://dotnet.microsoft.com/download

### Issue: "Git remote not configured"
**Solution**:
```bash
cd ~/OASIS_CLEAN
git remote add origin https://github.com/NextGenSoftwareUK/OASIS.git
```

### Issue: Build errors
**Solution**:
```bash
cd ~/OASIS_CLEAN
dotnet restore
dotnet build ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj --configuration Release
```

## Next Steps

1. ✅ Codebase moved to MacBook
2. ✅ GitHub linked
3. ✅ API builds successfully
4. ✅ Ready to develop!

## Quick Reference

```bash
# Build API
cd ~/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet build --configuration Release

# Run API
dotnet run --configuration Release

# Check Git status
cd ~/OASIS_CLEAN
git status

# Pull latest from GitHub
git pull origin master
```

