# Cursor Setup Guide for OASIS API on New MacBook

## Prerequisites

### 1. Install .NET SDK
```bash
# Check if .NET is installed
dotnet --version

# If not installed, download from: https://dotnet.microsoft.com/download
# Install .NET 9.0 SDK (matches your project target framework)
```

### 2. Install Cursor
- Download from: https://cursor.sh
- Install the application

### 3. Verify Hard Drive Access
```bash
# Ensure your hard drive is mounted and accessible
ls /Volumes/Storage/OASIS_CLEAN
```

## Step-by-Step Setup

### Step 1: Open OASIS API Project in Cursor

1. **Open Cursor**
2. **File → Open Folder**
3. Navigate to: `/Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI`
4. Click "Open"

**OR** open the entire workspace:
- Navigate to: `/Volumes/Storage/OASIS_CLEAN`
- This gives you access to everything, but you can focus on the API

### Step 2: Configure Cursor for .NET Development

1. **Install C# Extension** (if not auto-installed):
   - Press `Cmd+Shift+X` to open Extensions
   - Search for "C#" by Microsoft
   - Install "C# Dev Kit" (recommended) or "C#" extension

2. **Configure OmniSharp** (C# language server):
   - Cursor should auto-detect the .NET project
   - Check bottom-right status bar for "OmniSharp" status

### Step 3: Create Workspace Configuration (Optional but Recommended)

Create `.vscode/settings.json` in the API project folder:

```json
{
  "omnisharp.useModernNet": true,
  "omnisharp.enableRoslynAnalyzers": true,
  "dotnet.defaultSolution": "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj",
  "files.exclude": {
    "**/bin": true,
    "**/obj": true,
    "**/node_modules": true
  }
}
```

### Step 4: Restore Dependencies

Open terminal in Cursor (`Ctrl+`` or `View → Terminal`):

```bash
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet restore
```

### Step 5: Build OASIS API

```bash
# Build the API project
dotnet build --configuration Release

# Or build from the workspace root
cd /Volumes/Storage/OASIS_CLEAN
dotnet build ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj --configuration Release
```

### Step 6: Verify Build Success

You should see:
```
Build succeeded.
    0 Error(s)
```

### Step 7: Run OASIS API (Optional Test)

```bash
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run --configuration Release
```

The API should start on `https://localhost:5001` or `http://localhost:5000`

## Working with STAR Separately

### Option 1: Separate Cursor Window (Recommended)

1. **Open a new Cursor window**: `Cmd+Shift+N`
2. **Open STAR project**: `/Volumes/Storage/OASIS_CLEAN/STAR ODK`
3. Work on STAR in this separate window
4. Keep OASIS API window open for reference/testing

### Option 2: Multi-root Workspace

Create a workspace file `oasis-workspace.code-workspace`:

```json
{
  "folders": [
    {
      "path": "/Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI",
      "name": "OASIS API"
    },
    {
      "path": "/Volumes/Storage/OASIS_CLEAN/STAR ODK",
      "name": "STAR CLI"
    }
  ],
  "settings": {
    "files.exclude": {
      "**/bin": true,
      "**/obj": true
    }
  }
}
```

Open this workspace file in Cursor.

## Troubleshooting

### Issue: "OmniSharp not found"
**Solution**: 
- Install C# extension
- Restart Cursor
- Run: `dotnet restore` in the project folder

### Issue: Build errors about missing references
**Solution**:
```bash
cd /Volumes/Storage/OASIS_CLEAN
dotnet restore
dotnet build ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj
```

### Issue: Hard drive not accessible
**Solution**:
- Check System Settings → Privacy & Security → Full Disk Access
- Ensure Cursor has permission to access the hard drive

### Issue: .NET SDK not found
**Solution**:
```bash
# Add to ~/.zshrc or ~/.bash_profile
export PATH="$PATH:/usr/local/share/dotnet"
export DOTNET_ROOT=/usr/local/share/dotnet

# Then reload
source ~/.zshrc
```

## Quick Reference Commands

```bash
# Build OASIS API
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet build --configuration Release

# Run OASIS API
dotnet run --configuration Release

# Build STAR (when ready)
cd /Volumes/Storage/OASIS_CLEAN/STAR\ ODK/NextGenSoftware.OASIS.STAR.CLI
dotnet build --configuration Release

# Check .NET version
dotnet --version

# List installed SDKs
dotnet --list-sdks
```

## Recommended Cursor Settings for .NET

Add to Cursor settings (`Cmd+,`):

```json
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.fixAll": true,
    "source.organizeImports": true
  },
  "omnisharp.enableEditorConfigSupport": true,
  "omnisharp.enableRoslynAnalyzers": true
}
```

## Next Steps

1. ✅ Verify OASIS API builds successfully
2. ✅ Test API runs locally
3. ✅ Set up separate workspace/window for STAR
4. ✅ Work on STAR fixes without affecting API

## Notes

- **OASIS API is independent**: It should build without STAR
- **STAR depends on OASIS**: STAR needs OASIS API to build
- **Isolation**: Keep API and STAR work separate to avoid breaking the working API

