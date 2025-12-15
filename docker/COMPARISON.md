# Dockerfile Comparison: Previous Working vs Current

## Previous Working Image (`mainnet-update`)

**Image ID**: `96a7bd158ecb`  
**Digest**: `sha256:96a7bd158ecb93b459fca9e65155fed14e233328d921a42d6b2a7f69bb0cf3d6`  
**Created**: September 24, 2025

### Key Characteristics

1. **.NET Version**: 9.0.9
2. **Ports**: 80, 443
3. **Configuration**: Includes `OASIS_DNA.json` copied to `/app`
4. **Build Method**: Multi-stage build with publish directory
5. **Size**: 522MB (149MB compressed in ECR)

### Build History Analysis

From `docker history`, the previous image:
- Used .NET 9.0.9 runtime and SDK
- Copied published files from `/app/publish` to `/app`
- Included `OASIS_DNA.json` configuration file
- Set environment variables: `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_URLS=http://+:80`
- Exposed ports 80 and 443

## Current Dockerfile (`docker/Dockerfile`)

### Improvements

1. **Solution File Restoration**: Uses `The OASIS.sln` to restore all dependencies
2. **Optimized Build Context**: Better `.dockerignore` to reduce build size
3. **Provider Inclusion**: Ensures all 30+ providers are included via solution file
4. **Configuration Handling**: Attempts to copy `OASIS_DNA.json` from multiple locations

### Differences

| Aspect | Previous Working | Current |
|--------|----------------|---------|
| Build Context | Unknown (likely large) | Optimized with `.dockerignore` |
| Solution File | Not explicitly used | Explicitly restored |
| Configuration | Copied from specific location | Tries multiple locations |
| .NET Version | 9.0.9 | 9.0 (latest) |
| Port Configuration | 80, 443 | 80, 443 |

## Recommendations

1. **Use Solution File**: The current approach of restoring from solution file is correct
2. **Include OASIS_DNA.json**: Ensure configuration file is copied (previous image had this)
3. **Build Context**: Current `.dockerignore` is good, but ensure required directories are included
4. **Dependencies**: Make sure `NextGenSoftware-Libraries` and `holochain-client-csharp` are included

## Migration Notes

The previous working image was likely built with:
- All source code copied
- Solution file used for restoration
- Published output copied to final image
- Configuration file explicitly included

The current Dockerfile follows the same pattern but with:
- Better organization
- More explicit dependency management
- Optimized build context





