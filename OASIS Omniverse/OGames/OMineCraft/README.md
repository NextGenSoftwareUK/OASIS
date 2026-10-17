# OMineCraft — Minetest + OASIS STAR API

**OMineCraft** is an **OASIS Minetest mod** (`mods/oasis/`) that integrates the **OASIS STAR API** directly from Lua, bringing voxel sandbox gaming into the OASIS Omniverse. The second **Generation 2 OGame** alongside OMorrowind.

Step through an obsidian-frame portal in your Minetest world and arrive in a Doom level. A Nether Star in your inventory becomes a cross-game OASIS relic. Mob kills award XP visible in every connected OGame. OASIS quests span Minetest, Morrowind, Doom, Quake — all of them.

Unlike the C++-based Gen-1 integrations, OMineCraft calls the STAR API **directly from Lua** via Minetest's `request_http_api()` — the cleanest integration of any OGame, requiring no native code or compiled DLL.

Engine: Minetest (C++ voxel engine — LGPL-2.1, the open-source Minecraft-compatible engine)

---

## Quick start

### Windows, Linux, macOS

1. **Prerequisites:** Minetest 5.6+ with the `default` game (MineClone2 or Minetest Game).
2. **Install:** Copy this folder into your Minetest `mods/` directory:
   ```
   minetest/mods/oasis/
   ```
3. **Enable:** In the Minetest world settings, enable the `oasis` mod.
4. **Configure** `minetest.conf`:
   ```
   secure.http_mods = oasis
   oasis_star_url = https://star-api.oasisplatform.world/api
   ```
5. **Log in** in-game:
   ```
   /oasis login <username> <password>
   ```

No build step required — Minetest loads Lua mods directly.

---

## OASIS features

| Command | Action |
|---------|--------|
| `/oasis login <user> <pass>` | Log in to OASIS |
| `/oasis logout` | Log out |
| `/oasis inv` | Show cross-game inventory in chat |
| `/oasis configkey <game> <map>` | Stamp held Portal Key with a destination |

| HUD element | Position | Content |
|-------------|----------|---------|
| Username | Top-left | `[ username ]` (cyan) |
| XP counter | Top-right | `XP: 12345` (gold) |
| Toast | Top-centre | Kill XP, item pickups, portal events |

---

## Cross-game portals

Place an **obsidian frame** (same shape as a Nether portal) and right-click the air inside to light the `oasis:portal` node. Configure the destination with an `oasis:portal_key`:

```
/oasis configkey ODOOM E1M1
```

Then right-click a Portal Key onto the lit portal node. Step into the portal to request a cross-game teleport to `ODOOM / E1M1`. The STAR API notifies the destination OGame to spawn you at the registered coordinates.

---

## Cross-game items

| Minetest item | Cross-game ID | Effect |
|---------------|--------------|--------|
| `oasis:cross_game_key_blue` | `blue_keycard` | Opens blue doors in ODOOM, ODuke3D, ODOOM3 |
| `oasis:cross_game_key_red` | `red_keycard` | Opens red doors in any OGame |
| `oasis:ender_eye` | `mc_ender_eye` | Cross-game OASIS relic (NFT eligible) |
| `oasis:nether_star` | `mc_nether_star` | Cross-game OASIS relic (NFT eligible) |

---

## Architecture

```
init.lua          (mod entry point — player hooks, chat commands, mob XP)
api.lua           (STAR API bridge via minetest.request_http_api)
portals.lua       (oasis:portal node + oasis:portal_key item)
hud.lua           (username, XP, and toast HUD elements)
oasisstar.json    (mob XP table + key item mapping)
         ↓  (HTTP — no C layer needed)
  OASIS STAR API  (WEB4 / WEB5)
```

Minetest's HTTP API sends POST/GET requests to the STAR API on the main thread via callbacks — no separate thread or native DLL required.

---

## Files

| File | Purpose |
|------|---------|
| `init.lua` | Mod entry point — commands, hooks, mob XP |
| `api.lua` | STAR API HTTP bridge |
| `portals.lua` | Cross-game portal node and portal key |
| `hud.lua` | HUD username, XP, and toast overlays |
| `mod.conf` | Minetest mod manifest |
| `oasisstar.json` | Mob XP / key item config |

---

## Documentation

| Document | Description |
|----------|-------------|
| [../ARCHITECTURE.md](../ARCHITECTURE.md) | Full OASIS Omniverse architecture |
| [../OMorrowind/README.md](../OMorrowind/README.md) | OMorrowind — the other Gen-2 OGame |

---

OMineCraft is a Minetest mod (Lua, LGPL-2.1 compatible).  
Minetest is copyright Perttu Ahola and contributors — [minetest.net](https://www.minetest.net/).  
Minecraft is a trademark of Mojang/Microsoft; OMineCraft is not affiliated with or endorsed by Mojang.
