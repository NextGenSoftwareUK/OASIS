# OASIS API Contracts

## Overview
The OASIS API Contracts project contains shared interfaces, enums, and contracts that can be referenced by both OASIS.API.Core and Unity projects. This ensures a single source of truth for all OASIS contracts.

## Features
- **Unity Compatible**: Built with .NET Standard 2.1 for Unity compatibility
- **Single Source of Truth**: All interfaces defined once and shared
- **Clean Architecture**: Separates contracts from implementation
- **Cross-Platform**: Can be referenced by .NET 8/9 and Unity projects

## Contents
- **IOASISMapProvider**: Map provider interface for OASIS ecosystem
- **Future Interfaces**: Will contain all OASIS interfaces and enums
- **Shared Contracts**: Common data structures and enums

## Usage
```csharp
// Reference this project from OASIS.API.Core
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

// Reference this project from Unity
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

// Reference this project from Providers
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
```

## Architecture Benefits
- ✅ **Single Source of Truth** for all interfaces
- ✅ **Unity Compatibility** without .NET version conflicts
- ✅ **Clean Separation** of contracts from implementation
- ✅ **Easy Maintenance** - update interface once, affects all projects
- ✅ **SOLID Principles** maintained

## Future Plans
This project will eventually contain:
- All OASIS interfaces (IHolon, IAvatar, etc.)
- All OASIS enums (MapProviderType, etc.)
- All OASIS contracts and data structures
- Shared utility classes

## License
MIT License - See LICENSE file for details
