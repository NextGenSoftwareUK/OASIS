# OASIS .NET SDK Setup Instructions for New MacBook

## Current Status
- ✅ OASIS codebase copied to `~/OASIS_CLEAN` (MacBook internal storage)
- ❌ .NET SDK not installed
- ❌ Homebrew not installed

## Installation Options

### Option 1: Automated Installation (Recommended)

Run the setup script in your terminal:

```bash
# Make sure you're in your Documents folder
cd ~/Documents

# Run the setup script (will prompt for your password)
./setup_dotnet.sh
```

This script will:
1. Install Homebrew (requires your admin password)
2. Install .NET 9.0 SDK via Homebrew
3. Verify the installation
4. Restore OASIS API dependencies

### Option 2: Manual Installation via Homebrew

If you prefer to do it step by step:

```bash
# 1. Install Homebrew
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# 2. Add Homebrew to PATH (for Apple Silicon Macs - M1/M2/M3)
echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> ~/.zshrc
eval "$(/opt/homebrew/bin/brew shellenv)"

# For Intel Macs, use:
# echo 'eval "$(/usr/local/bin/brew shellenv)"' >> ~/.zshrc
# eval "$(/usr/local/bin/brew shellenv)"

# 3. Install .NET 9.0 SDK
brew install --cask dotnet-sdk

# 4. Verify installation
dotnet --version
dotnet --list-sdks
```

### Option 3: Direct Download from Microsoft

If Homebrew installation fails:

1. Visit: https://dotnet.microsoft.com/download/dotnet/9.0
2. Download the **macOS** installer (.pkg file)
3. Run the downloaded .pkg file
4. Follow the installation wizard
5. After installation, verify:
   ```bash
   dotnet --version
   dotnet --list-sdks
   ```

## After Installation: Restore Dependencies

Once .NET SDK is installed, restore the OASIS API project dependencies:

```bash
cd ~/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet restore
```

## Build the Project

```bash
cd ~/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet build --configuration Release
```

## Troubleshooting

### SSL Certificate Issues
If you encounter SSL certificate errors, you may need to:
1. Update your system certificates
2. Or use the manual download option (Option 3)

### PATH Issues
If `dotnet` command is not found after installation:

```bash
# Add to ~/.zshrc
export PATH="$PATH:/usr/local/share/dotnet"
export DOTNET_ROOT=/usr/local/share/dotnet

# Then reload
source ~/.zshrc
```

### Homebrew Installation Issues
If Homebrew installation fails:
- Make sure you have administrator privileges
- Check your internet connection
- Try the manual download option instead

## Next Steps After Installation

1. **Open Cursor**
2. **File → Open Folder**
3. Navigate to: `~/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI`
4. **Install C# Extension** in Cursor:
   - Press `Cmd+Shift+X` to open Extensions
   - Search for "C#" by Microsoft
   - Install "C# Dev Kit" (recommended)
5. **Build the project** to verify everything works

## Quick Verification Commands

```bash
# Check .NET version
dotnet --version

# List installed SDKs
dotnet --list-sdks

# Restore dependencies
cd ~/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet restore

# Build the project
dotnet build --configuration Release
```

