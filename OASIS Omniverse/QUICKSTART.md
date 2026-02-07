# Quick Start Guide - Doom & Quake STAR API Integration

## 5-Minute Setup

### 1. Get STAR API Credentials

1. Sign up at [OASIS Platform](https://oasisplatform.world)
2. Get your API key from the dashboard
3. Get your Avatar ID

### 2. Set Environment Variables

```bash
# Linux/Mac
export STAR_API_KEY="your_api_key_here"
export STAR_AVATAR_ID="your_avatar_id_here"

# Windows (Command Prompt)
set STAR_API_KEY=your_api_key_here
set STAR_AVATAR_ID=your_avatar_id_here

# Windows (PowerShell)
$env:STAR_API_KEY="your_api_key_here"
$env:STAR_AVATAR_ID="your_avatar_id_here"
```

### 3. Build the Native Wrapper

```bash
cd "Game Integration/NativeWrapper"
mkdir build
cd build

# Linux/Mac
cmake ..
make

# Windows
cmake ..
cmake --build . --config Release
```

### 4. Integrate into Doom

1. Copy `Doom/doom_star_integration.c` and `Doom/doom_star_integration.h` to your Doom source
2. Add to your Makefile:
   ```makefile
   LIBS += -L../Game\ Integration/NativeWrapper/build -lstar_api
   CFLAGS += -I../Game\ Integration/NativeWrapper -I../Game\ Integration/Doom
   ```
3. In `d_main.c`, add:
   ```c
   #include "doom_star_integration.h"
   
   void D_DoomMain(void) {
       Doom_STAR_Init();
       // ... rest of code ...
   }
   ```
4. In `p_inter.c`, add keycard tracking (see `Doom/README.md`)

### 5. Integrate into Quake

1. Copy `Quake/quake_star_integration.c` and `Quake/quake_star_integration.h` to your Quake source
2. Add to your Makefile:
   ```makefile
   LIBS += -L../Game\ Integration/NativeWrapper/build -lstar_api
   CFLAGS += -I../Game\ Integration/NativeWrapper -I../Game\ Integration/Quake
   ```
3. In `host.c`, add:
   ```c
   #include "quake_star_integration.h"
   
   void Host_Init(void) {
       Quake_STAR_Init();
       // ... rest of code ...
   }
   ```
4. In `items.c`, add key tracking (see `Quake/README.md`)

### 6. Test It!

1. **Start Doom:**
   - Pick up a red keycard
   - Check console: Should see "STAR API: Added red_keycard to cross-game inventory"

2. **Start Quake:**
   - Approach a door requiring silver key
   - If mapped correctly, the door should open using Doom's red keycard!

## What You Get

✅ **Cross-Game Item Sharing:** Collect keycards in Doom, use them in Quake  
✅ **Persistent Inventory:** Items stored in STAR API, accessible from any game  
✅ **Future-Ready:** Foundation for quests and NFT systems  

## Next Steps

- Read [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) for detailed documentation
- Check [Doom/README.md](Doom/README.md) for Doom-specific details
- Check [Quake/README.md](Quake/README.md) for Quake-specific details
- See [Examples/keycard_example.c](Examples/keycard_example.c) for code examples

## Troubleshooting

**Problem:** STAR API not initializing  
**Solution:** Check that `STAR_API_KEY` and `STAR_AVATAR_ID` are set

**Problem:** Items not appearing  
**Solution:** Check console output for errors, verify network connectivity

**Problem:** Doors not opening  
**Solution:** Verify item names match, check door access logic is integrated

For more help, see [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md#troubleshooting)



