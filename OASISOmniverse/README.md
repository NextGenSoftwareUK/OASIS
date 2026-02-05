# OASIS STAR API - Doom & Quake Integration

## Overview

This integration enables cross-game item sharing and quest systems between classic open-source games (Doom, Quake) using the OASIS STAR API. 

**Phase 1 Features:**
- Cross-game keycard/item sharing (collect a keycard in Doom, use it in Quake and vice versa)
- Persistent inventory across games
- Item tracking via STAR API

**Future Phase 2 Features:**
- Multi-game quests spanning Doom, Quake, and other games
- NFT-based boss collection and deployment
- Cross-game asset trading

## Architecture

```
┌─────────────┐         ┌─────────────┐
│    Doom     │         │   Quake     │
│  (C Engine) │         │ (C Engine)  │
└──────┬──────┘         └──────┬──────┘
       │                        │
       └────────┬───────────────┘
                │
       ┌────────▼────────┐
       │  C/C++ Wrapper  │
       │  (star_api.h)   │
       └────────┬────────┘
                │
       ┌────────▼────────┐
       │  STAR API       │
       │  Client (C#)    │
       └────────┬────────┘
                │
       ┌────────▼────────┐
       │  STAR API       │
       │  (REST/HTTP)    │
       └─────────────────┘
```

## Directory Structure

```
Game Integration/
├── README.md                          # This file
├── STARAPIClient/                     # C# client library
│   ├── GameIntegrationClient.cs      # Main client for game integrations
│   └── Models/                        # Data models
│       ├── GameItem.cs
│       └── CrossGameItem.cs
├── NativeWrapper/                     # C/C++ wrapper for Doom/Quake
│   ├── star_api.h                     # C header file
│   ├── star_api.cpp                   # C++ implementation
│   └── CMakeLists.txt                 # Build configuration
├── Doom/                              # Doom integration examples
│   ├── doom_star_integration.c        # Integration hooks
│   ├── doom_star_integration.h        # Header file
│   └── README.md                      # Doom-specific docs
├── Quake/                             # Quake integration examples
│   ├── quake_star_integration.c       # Integration hooks
│   ├── quake_star_integration.h       # Header file
│   └── README.md                      # Quake-specific docs
├── Config/                            # Configuration files
│   ├── star_api_config.json           # API configuration template
│   └── game_items.json                # Item definitions
└── Examples/                          # Example implementations
    ├── keycard_example.c              # Keycard integration example
    └── quest_example.c                 # Quest integration example
```

## Quick Start

### 1. Configure STAR API

Edit `Config/star_api_config.json`:
```json
{
  "starApiBaseUrl": "https://star-api.oasisplatform.world/api",
  "apiKey": "YOUR_STAR_API_KEY",
  "avatarId": "YOUR_AVATAR_ID"
}
```

### 2. Build the Native Wrapper

```bash
cd NativeWrapper
mkdir build && cd build
cmake ..
make
```

### 3. Integrate into Doom

See `Doom/README.md` for detailed integration instructions.

### 4. Integrate into Quake

See `Quake/README.md` for detailed integration instructions.

## Key Concepts

### Cross-Game Items

Items collected in one game are stored in the STAR API and can be accessed from any other integrated game. For example:
- Collect "Red Keycard" in Doom → Available in Quake
- Collect "Silver Key" in Quake → Available in Doom

### Item Types

- **Keycards**: Door-opening items that work across games
- **Weapons**: Can be shared (if game mechanics allow)
- **Power-ups**: Temporary items that persist across sessions
- **Quest Items**: Items required for cross-game quests

## API Usage

### C/C++ API (for game engines)

```c
#include "star_api.h"

// Initialize the STAR API client
star_api_init("https://star-api.oasisplatform.world/api", "YOUR_API_KEY");

// Check if player has a specific item
bool has_keycard = star_api_has_item("red_keycard");

// Add an item when collected
star_api_add_item("red_keycard", "Red Keycard", "A red keycard from Doom");

// Use an item (e.g., open a door)
bool door_opened = star_api_use_item("red_keycard", "door_123");

// Get all items for current player
ItemList* items = star_api_get_inventory();
```

## Development

### Building from Source

1. Ensure you have .NET SDK installed (for C# client)
2. Ensure you have CMake and a C++ compiler (for native wrapper)
3. Build the C# client library
4. Build the native wrapper
5. Link the wrapper into your game engine

### Testing

Run the test suite:
```bash
cd Game Integration
dotnet test
```

## Contributing

When adding support for new games:
1. Create a new directory under `Game Integration/`
2. Add integration hooks following the Doom/Quake examples
3. Update this README with game-specific instructions
4. Add test cases

## License

This integration follows the same license as the OASIS project.



