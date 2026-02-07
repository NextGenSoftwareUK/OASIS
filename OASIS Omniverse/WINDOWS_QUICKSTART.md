# Windows Quick Start Guide

## Your Setup

- **DOOM Fork**: `C:\Source\DOOM`
- **Quake Fork**: `C:\Source\quake-rerelease-qc`
- **OASIS Project**: `C:\Source\OASIS-master`

## Quick Setup (PowerShell)

Run the setup script:

```powershell
cd C:\Source\OASIS-master\Game Integration
.\WINDOWS_SETUP.ps1 -BuildWrapper -CopyFiles -SetupEnv
```

This will:
1. ✅ Build the native wrapper library
2. ✅ Copy integration files to your forks
3. ✅ Set up environment variables (prompts for credentials)

## Manual Setup

### 1. Build Native Wrapper

```powershell
cd C:\Source\OASIS-master\Game Integration\NativeWrapper
mkdir build
cd build
cmake .. -G "Visual Studio 16 2019" -A x64
cmake --build . --config Release
```

### 2. Set Environment Variables

```powershell
# SSO (Recommended)
$env:STAR_USERNAME = "your_username"
$env:STAR_PASSWORD = "your_password"

# Or API Key
$env:STAR_API_KEY = "your_api_key"
$env:STAR_AVATAR_ID = "your_avatar_id"
```

### 3. Copy Files

```powershell
# DOOM
Copy-Item "C:\Source\OASIS-master\Game Integration\Doom\doom_star_integration.*" "C:\Source\DOOM\linuxdoom-1.10\"
Copy-Item "C:\Source\OASIS-master\Game Integration\NativeWrapper\star_api.h" "C:\Source\DOOM\linuxdoom-1.10\"

# Quake
Copy-Item "C:\Source\OASIS-master\Game Integration\Quake\quake_star_integration.*" "C:\Source\quake-rerelease-qc\"
Copy-Item "C:\Source\OASIS-master\Game Integration\NativeWrapper\star_api.h" "C:\Source\quake-rerelease-qc\"
```

## Integration Guides

Follow these detailed guides:

- **DOOM**: [Doom/WINDOWS_INTEGRATION.md](Doom/WINDOWS_INTEGRATION.md)
- **Quake**: [Quake/WINDOWS_INTEGRATION.md](Quake/WINDOWS_INTEGRATION.md)

## What You Get

✅ **Cross-Game Item Sharing**: Collect keycards in DOOM, use in Quake  
✅ **Avatar/SSO Login**: Single sign-on across all games  
✅ **Multi-Game Quests**: Complete objectives across multiple games  
✅ **NFT Boss Collection**: Defeat bosses, collect as NFTs (Phase 3 ready)  

## Testing

1. **Start DOOM** → Pick up red keycard
2. **Start Quake** → Use red keycard to open doors (if mapped)
3. **Create Quest** → Complete objectives across both games
4. **Collect Boss NFT** → Defeat boss in DOOM, deploy in Quake!

## Troubleshooting

### Library Not Found
- Check: `C:\Source\OASIS-master\Game Integration\NativeWrapper\build\Release\star_api.lib`
- Verify build completed successfully

### Include Errors
- Check include paths in project settings
- Verify files were copied correctly

### Authentication Failed
- Check environment variables are set
- Verify credentials are correct
- Check network connectivity

## Next Steps

1. ✅ Complete DOOM integration
2. ✅ Complete Quake integration  
3. ✅ Test cross-game item sharing
4. ✅ Create your first cross-game quest!

For detailed instructions, see the Windows integration guides above.



