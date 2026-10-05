# Quick Start: Install SDKs and Dependencies

## Current Status
- ✅ OASIS codebase is at: `~/OASIS_CLEAN`
- ❌ Xcode Command Line Tools: **Not installed** (required first)
- ❌ Homebrew: **Not installed**
- ❌ .NET 9.0 SDK: **Not installed**

## Installation Steps

### Option 1: Automated Installation (Recommended)

Run the installation script:

```bash
cd ~/OASIS_CLEAN
./INSTALL_SDKS.sh
```

This script will:
1. Install Xcode Command Line Tools (if needed)
2. Install Homebrew
3. Install .NET 9.0 SDK
4. Restore OASIS API dependencies
5. Build the OASIS API project

**Note:** The script will prompt you for your password when installing Xcode Command Line Tools and Homebrew.

### Option 2: Manual Installation

#### Step 1: Install Xcode Command Line Tools (Required First)

```bash
xcode-select --install
```

A popup will appear. Click "Install" and wait for it to complete (this may take 10-15 minutes).

#### Step 2: Install Homebrew

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

Follow the on-screen instructions. After installation, you may need to add Homebrew to your PATH:

**For Apple Silicon Macs (M1/M2/M3):**
```bash
echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> ~/.zshrc
eval "$(/opt/homebrew/bin/brew shellenv)"
```

**For Intel Macs:**
```bash
echo 'eval "$(/usr/local/bin/brew shellenv)"' >> ~/.zshrc
eval "$(/usr/local/bin/brew shellenv)"
```

#### Step 3: Install .NET 9.0 SDK

```bash
brew install --cask dotnet-sdk
```

#### Step 4: Verify Installation

```bash
# Check Homebrew
brew --version

# Check .NET
dotnet --version
dotnet --list-sdks
```

#### Step 5: Restore OASIS Dependencies

```bash
cd ~/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet restore
```

#### Step 6: Build OASIS API

```bash
cd ~/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet build --configuration Release
```

### Option 3: Direct .NET Download (If Homebrew Fails)

1. Visit: https://dotnet.microsoft.com/download/dotnet/9.0
2. Download the **macOS** installer (.pkg file)
3. Run the installer
4. Verify: `dotnet --version`

## After Installation

1. **Open Cursor**
2. **File → Open Folder**
3. Navigate to: `~/OASIS_CLEAN`
4. **Install C# Extension**:
   - Press `Cmd+Shift+X`
   - Search for "C#" by Microsoft
   - Install "C# Dev Kit" (recommended)

## Troubleshooting

### "xcode-select: error: Unable to get active developer directory"
**Solution:** Run `xcode-select --install` and complete the installation.

### "brew: command not found" after installation
**Solution:** Add Homebrew to your PATH (see Step 2 above), then restart your terminal.

### ".NET not found" after installation
**Solution:** 
- Restart your terminal
- Or add to `~/.zshrc`:
  ```bash
  export PATH="$PATH:/usr/local/share/dotnet"
  export DOTNET_ROOT=/usr/local/share/dotnet
  ```
- Then run: `source ~/.zshrc`

### SSL Certificate Errors
**Solution:** Update your system or use the direct download option (Option 3).

## Quick Verification

```bash
# Check all installations
xcode-select -p
brew --version
dotnet --version

# Restore and build
cd ~/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet restore
dotnet build --configuration Release
```

## Next Steps

Once everything is installed:
1. ✅ SDKs installed
2. ✅ Dependencies restored
3. ✅ Project builds successfully
4. 🚀 Ready to develop!


