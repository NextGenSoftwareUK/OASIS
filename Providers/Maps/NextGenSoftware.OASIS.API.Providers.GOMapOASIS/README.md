# GOMap OASIS Provider

## Overview
The GOMap OASIS Provider integrates the GO Map Unity package with the OASIS ecosystem, providing a clean abstraction layer for mapping functionality in Unity projects.

## Features
- **Unity Compatible**: Built with .NET Standard 2.1 for Unity compatibility
- **Self-Contained**: No external OASIS dependencies to avoid compatibility issues
- **GO Map Integration**: Leverages the existing GO Map Unity package
- **OASIS Abstraction**: Provides OASIS-compatible mapping interface

## Architecture
- **IOASISMapProvider**: Self-contained interface definition
- **GOMapOASIS**: Main provider implementation
- **Unity Integration**: Can be imported as DLL into Unity projects

## Usage
```csharp
// Initialize the provider
var goMapProvider = new GOMapOASIS();
goMapProvider.Initialize(goMapInstance);

// Use mapping functions
goMapProvider.PlaceGeoNFTOnMap(geoNFT, latitude, longitude);
goMapProvider.DrawRouteOnMap(startX, startY, endX, endY);
```

## Integration with Unity
1. Build this project to create the DLL
2. Import the DLL into your Unity project
3. Use the provider through the IUnityMapProvider interface
4. Implement GO Map-specific functionality as needed

## Future Enhancements
- Complete GO Map API integration
- Route calculation and drawing
- 3D object placement
- Coordinate conversion utilities
- Performance optimizations

## License
MIT License - See LICENSE file for details
