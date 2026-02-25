# Windows Integration Guide: STAR API in ODOOM

This guide covers integrating the OASIS STAR API into **ODOOM** (UZDoom-based, a GZDoom fork). ODOOM uses a native Windows/SDL2 stack with proper sound, music, and mouse handling, so you avoid the issues common with the older Linux Doom port (sound, music, mouse).

## Credits and license

**ODOOM is a fork of UZDoom.** Full credit goes to the [UZDoom](https://github.com/UZDoom/UZDoom) project. UZDoom is licensed under the [GNU General Public License v3.0](https://github.com/UZDoom/UZDoom/blob/trunk/LICENSE). When you build or distribute ODOOM, you must comply with UZDoom’s license and give appropriate credit. See **[CREDITS_AND_LICENSE.md](CREDITS_AND_LICENSE.md)** in this folder for details.

## Prerequisites

- **Visual Studio** 2019 or later with C++ tools
- **CMake** 3.16+
- **Python 3.6+** (required by UZDoom’s build; [python.org](https://www.python.org/downloads/) – add to PATH or use default install so the script can find it)
- **UZDoom** fork cloned (e.g. `C:\Source\UZDoom`)
- **OASIS** repo with STARAPIClient and Doom folder (e.g. `C:\Source\OASIS-master`)

## Automated build (recommended)

**No separate client build** – the script uses the existing `star_api.dll` and `star_api.lib` from `OASIS Omniverse\Doom\` (same build used for the other Doom port). `star_api.h` is taken from **STARAPIClient** (ODOOM uses STARAPIClient, not NativeWrapper).

From the OASIS repo, in `OASIS Omniverse\ODOOM\`, run the single script:

```batch
BUILD ODOOM.bat
```
Build only. When it finishes, **ODOOM.exe** and DLLs are in `OASIS Omniverse\ODOOM\build\`. Put your WAD there and run **ODOOM.exe**.

To **build and launch** ODOOM in one go:
```batch
BUILD ODOOM.bat run
```

The script: copies integration files into the UZDoom source `src`, configures with STAR (header from STARAPIClient, lib/dll from Doom folder), builds, and copies the exe as **ODOOM.exe** plus DLLs to `ODOOM\build\`. If your UZDoom clone is not at `C:\Source\UZDoom`, edit the `UZDOOM_SRC` variable at the top of `BUILD ODOOM.bat`. CMake and Visual Studio must be in PATH (or run from **Developer Command Prompt for VS 2022**).

### ODOOM branding (name and version)

When building with STAR, the script also applies **ODOOM** branding and build/version so it appears in:

- **Loading screen** – header shows “ODOOM” and version (e.g. “ODOOM 1.0 (Build 1) (UZDoom x.x.x)”).
- **Window title** – uses the same name/version where the engine shows the game name.
- **In-game HUD** – bottom-right shows “ODOOM” and version with UZDoom in brackets (e.g. “ODOOM 1.0 (Build 1) (UZDoom 1.2.3)”).
- **Console** – at startup (and when you type `star`) you see the OASIS ODOOM banner and “Welcome to ODOOM!”; the `-version` flag shows ODOOM.

The script copies `odoom_branding.h` and `odoom_version_generated.h` into UZDoom `src` and runs `apply_odoom_branding.ps1` to patch `version.h`, `common/startscreen/startscreen.cpp`, and `g_statusbar/shared_sbar.cpp` if not already patched. **To change ODOOM's version or build number**, edit **`OASIS Omniverse\ODOOM\odoom_version.txt`** (line 1 = version, line 2 = build). The build script regenerates the header from this file.

### OASIS banner (launcher and loading screen)

When `oasis_banner.png` is present in `OASIS Omniverse\ODOOM\`, the build script copies it into UZDoom’s wadsrc so that the **launcher** (splash/game select) and the **in-game loading screen** both use the OASIS banner (`ui/banner-light.png`, `ui/banner-dark.png`, and `bootlogo.png`). After a full build that includes the game data (uzdoom.pk3), the OASIS graphic will appear there.

**To use it:** Put your OASIS banner at `OASIS Omniverse\ODOOM\oasis_banner.png`, then run `BUILD ODOOM.bat`. The script copies the banner into UZDoom’s wadsrc and runs the full build (including packing uzdoom.pk3), so the new banner appears after that single run.

**Banner size:** The UZDoom/GZDoom launcher and menu system use a **320×200** base resolution (classic Doom). If your banner is too large and overflows the launcher:

- **Original (UZDoom):** The stock banners in UZDoom are in your UZDoom clone at `wadsrc\static\ui\banner-light.png` and `banner-dark.png`. Check those image dimensions before overwriting them to match the intended size.
- **Recommended size for ODOOM:** Use a **640×200** (wide strip, 2× base width) or **320×100** for a compact header. Avoid very tall or very high‑resolution images (e.g. 1920×400 or larger) so the launcher layout stays readable.

**PNG compatibility:** The launcher uses a decoder that only supports standard PNGs. Save `oasis_banner.png` as **24‑bit or 32‑bit RGBA, with interlacing turned off**, to avoid “Could not decode PNG file”. (In most editors: export/save as PNG and choose “none” for interlacing.)

### Testing the STAR API

1. **Authentication**  
   Set environment variables before starting the game (or in a launcher script):
   - **SSO:** `STAR_USERNAME` and `STAR_PASSWORD` (recommended).
   - **API key:** `STAR_API_KEY` and `STAR_AVATAR_ID`.  
   If the API is initialized, the console window or in-game console will show: `STAR API: Authenticated via SSO. Cross-game features enabled.` (or the API-key message). If not, you’ll see `STAR API: No authentication configured...` or an error.

2. **In-game console commands**  
   Open the console (usually **~** or **`**) and type **`star`** with no arguments to see all subcommands. Use these to confirm the STAR API is working:

   | Command | Description |
   |--------|--------------|
   | `star version` | Show integration version and whether the API is initialized. |
   | `star status` | Show init state and last error message. |
   | `star inventory` | List all items in your STAR inventory. |
   | `star has <item>` | Check if you have an item (e.g. `star has red_keycard`). |
   | `star add <item> [desc] [type]` | Add an item (e.g. `star add red_keycard` or `star add red_keycard "Red key" KeyItem`). |
   | `star use <item> [context]` | Use an item (e.g. `star use red_keycard door`). |
   | `star quest start <id>` | Start a quest. |
   | `star quest objective <quest> <obj>` | Complete a quest objective. |
   | `star quest complete <id>` | Complete a quest. |
   | `star bossnft <name> [desc]` | Create a boss NFT. |
   | `star deploynft <nft_id> <game> [loc]` | Deploy a boss NFT to a game. |
   | `star pickup keycard <red\|blue\|yellow\|skull>` | Add a keycard to inventory (e.g. `star pickup keycard red`). |
   | `star beamin` | Log in / authenticate (uses `STAR_USERNAME`/`STAR_PASSWORD` or API key from env). |
   | `star beamout` | Log out / disconnect from STAR. |
   | `star config` | Show STAR config (URLs, stack, **mint NFT** options, **nft_provider**). |
   | `star config save` | Write config to **oasisstar.json** (also saved on exit). |
   | `star mint <armor\|weapons\|powerups\|keys> <0\|1>` | Turn **mint NFT** on (1) or off (0) when collecting that category. |
   | `star nftprovider <name>` | Set default NFT mint provider (e.g. `SolanaOASIS`). |

   **Config files:** STAR options are stored in **oasisstar.json** (when found) and in the engine config (**uzdoom.ini** or equivalent) via CVars. Keys: `star_api_url`, `oasis_api_url`, `beam_face`, `stack_armor`, `stack_weapons`, `stack_powerups`, `stack_keys`, **`mint_weapons`**, **`mint_armor`**, **`mint_powerups`**, **`mint_keys`** (0/1), and **`nft_provider`** (default `SolanaOASIS`). When mint is on for a category, picking up that item mints an NFT (WEB4 NFTHolon) and adds the inventory item with that NFT ID in metadata. In the **inventory popup**, NFT items show **[NFT]** at the front of the name and are grouped separately (e.g. “NFT Shotgun” x2 and “Shotgun” x2).

   **Quick checks:**  
   - `star version` — should show “Initialized: yes” if auth is set.  
   - `star beamin` — log in (if you set `STAR_USERNAME`/`STAR_PASSWORD` or API key in the environment).  
   - `star beamout` — log out; then `star beamin` to log in again.  
   - `star add red_keycard` — add a red keycard; then `star inventory` or `star has red_keycard` to verify.  
   - Pick up a keycard in-game and run `star inventory` to see it listed.

3. **In-game behavior**  
   - Picking up keycards (red/blue/yellow/skull) should print a line like `STAR API: Added red_keycard to cross-game inventory.`  
   - If you have a keycard from STAR (e.g. from another game or from `star add`), doors that need that key can open using the cross-game inventory (you’ll see `STAR API: Door opened using cross-game keycard: ...`).

4. **Cross-game with OQuake**  
   ODOOM (UZDoom) door checks also accept **OQuake** keys: a **red** door can open with OQuake’s **silver_key**, and **blue/yellow** doors with OQuake’s **gold_key**. So keys collected in OQuake can open doors in ODOOM and vice versa. Use `BUILD_OQUAKE.bat` in `OASIS Omniverse\OQuake\` to set up OQuake the same way.

---

## Manual steps (optional)

### Step 1: STAR API client (pre-built)

Use the existing build from the other Doom port: `OASIS Omniverse\Doom\` should already contain `star_api.dll` and `star_api.lib`. The header `star_api.h` is in `OASIS Omniverse\STARAPIClient\`. ODOOM uses **STARAPIClient** (not NativeWrapper). No need to build the client again if the Doom folder already has the lib/dll.

### Step 2: Integration files in ODOOM source (UZDoom)

The integration files are:

- `uzdoom_star_integration.cpp`
- `uzdoom_star_integration.h`
- `star_sync.c` and `star_sync.h` (generic async layer from `OASIS Omniverse\STARAPIClient`)

They should live in your UZDoom **src** folder (e.g. `C:\Source\UZDoom\src\`). If you are syncing from the OASIS repo, copy them from:

- `OASIS-master\OASIS Omniverse\ODOOM\uzdoom_star_integration.cpp` → `UZDoom\src\`
- `OASIS-master\OASIS Omniverse\ODOOM\uzdoom_star_integration.h` → `UZDoom\src\`
- `OASIS-master\OASIS Omniverse\STARAPIClient\star_sync.c` → `UZDoom\src\`
- `OASIS-master\OASIS Omniverse\STARAPIClient\star_sync.h` → `UZDoom\src\`

**Build:** Add `star_sync.c` to the UZDoom CMake source list when `OASIS_STAR_API` is ON (so it is compiled and linked with the game).

The UZDoom source tree already has the hooks in `d_main.cpp`, `p_interaction.cpp`, and `gamedata/a_keys.cpp` guarded by `#ifdef OASIS_STAR_API`.

### Step 3: Configure ODOOM with STAR API

From your UZDoom repo root:

```powershell
cd C:\Source\UZDoom
mkdir build -Force
cd build
cmake .. -G "Visual Studio 17 2022" -A x64 `
  -DOASIS_STAR_API=ON `
  -DSTAR_API_DIR="C:/Source/OASIS-master/OASIS Omniverse/STARAPIClient" `
  -DSTAR_API_LIB_DIR="C:/Source/OASIS-master/OASIS Omniverse/Doom"
```

`STAR_API_DIR` = path to **STARAPIClient** (for `star_api.h`). ODOOM uses STARAPIClient, not NativeWrapper. `STAR_API_LIB_DIR` = path to Doom folder (for pre-built `star_api.lib`). Use forward slashes.

### Step 4: Build ODOOM

```powershell
cmake --build . --config Release
```

The executable will be in `build\Release\uzdoom.exe`. The `BUILD ODOOM.bat` script copies it as **ODOOM.exe** to `OASIS Omniverse\ODOOM\build\`.

### Step 5: Deploy star_api.dll

Copy the pre-built DLL from the Doom folder next to the executable:

```powershell
copy "C:\Source\OASIS-master\OASIS Omniverse\Doom\star_api.dll" "C:\Source\UZDoom\build\Release\"
```

### Step 6: Environment variables (optional)

To use the STAR API (SSO or API key), set before running ODOOM:

**SSO (recommended):**

```powershell
$env:STAR_USERNAME = "your_username"
$env:STAR_PASSWORD = "your_password"
```

**Or API key:**

```powershell
$env:STAR_API_KEY = "your_api_key"
$env:STAR_AVATAR_ID = "your_avatar_id"
```

### Step 7: Run and test

1. Put your WAD (e.g. `doom2.wad`) in the same folder as **ODOOM.exe** (e.g. `OASIS Omniverse\ODOOM\build\`) or where the engine expects it.
2. Run **ODOOM.exe** (or use `BUILD & RUN ODOOM.bat` to build and launch).
3. In the console you should see either:
   - `STAR API: Authenticated via SSO. Cross-game features enabled.` or
   - `STAR API: No authentication configured...` if env vars are not set.
4. Pick up a keycard in-game; you should see: `STAR API: Added red_keycard to cross-game inventory.` (or blue/yellow/skull).
5. Doors that require a key will also check the STAR API inventory, so keycards from other STAR-integrated games can open doors when configured.

## Building without STAR API

Omit the STAR options to build a normal UZDoom (no ODOOM/STAR) with no STAR code:

```powershell
cmake .. -G "Visual Studio 17 2022" -A x64
```

No `OASIS_STAR_API` or `STAR_API_DIR`; the STAR hooks are compiled out.

## Troubleshooting

- **star_api.lib not found**  
  Use pre-built client: set `STAR_API_LIB_DIR` to the Doom folder (`OASIS Omniverse\Doom`) that contains `star_api.lib` and `star_api.dll`. Set `STAR_API_DIR` to the **STARAPIClient** folder (for `star_api.h`). ODOOM uses STARAPIClient, not NativeWrapper.

- **star_api.dll missing at runtime**  
  Copy `star_api.dll` from `OASIS Omniverse\Doom\` to the same directory as `ODOOM.exe`.

- **No sound or music / “Sound init failed”**  
  ODOOM uses **OpenAL Soft** for audio. You need **soft_oal.dll** (64-bit) in the same folder as `ODOOM.exe`.  
  - Download from: [OpenAL Soft binaries](https://openal-soft.org/openal-binaries/) — use the **Win64** build, then copy **soft_oal.dll** from the `bin` folder into `UZDoom\build\Release\` (or wherever `ODOOM.exe` is).  
  - The game will now print a clearer reason if sound fails (e.g. “soft_oal.dll not found” or “OpenAL device could not be opened”).

- **STAR API: Failed to initialize**  
  Check env vars, network, and firewall. The client uses WinHTTP to talk to the STAR API.

## Summary

- STAR integration is **optional** and enabled only when `OASIS_STAR_API=ON` and `STAR_API_DIR` is set.
- Keycard pickups in ODOOM are reported to the STAR API; door/lock checks can use cross-game inventory (including **OQuake** silver_key/gold_key for red/blue/yellow doors).
- UZDoom’s Windows port should give you correct sound, music, and mouse behavior compared to the older Linux Doom port.
- **ODOOM (UZDoom) is the supported Doom port** for STAR; use **BUILD ODOOM.bat** to build (output: **ODOOM.exe** in `ODOOM\build\`) or **BUILD & RUN ODOOM.bat** to build and launch.
- **Credits:** ODOOM is a fork of [UZDoom](https://github.com/UZDoom/UZDoom) (GPL-3.0). See **CREDITS_AND_LICENSE.md** in this folder.
