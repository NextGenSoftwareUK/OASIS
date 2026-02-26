# DOOM Items and Map Data Reference

Quick reference for **power-ups**, **keys**, and **weapons** in classic DOOM (doom1.wad / js-dos), and whether the OASIS integration can "see" where they are.

---

## 1. Types that exist in DOOM (at any given moment)

From the engine source in `doom-wasm-src/src/doom/` (info.c, p_inter.c).

### Keys (WAD thing type = **doomednum**)

| doomednum | In-game       | Opens              |
|----------|---------------|--------------------|
| 5        | Blue keycard  | Blue doors/lifts   |
| 6        | Yellow keycard| Yellow doors/lifts |
| 13       | Red keycard   | Red doors/lifts    |
| 38       | Red skull key | Red skull doors    |
| 39       | Yellow skull  | Yellow skull doors|
| 40       | Blue skull    | Blue skull doors   |

### Weapons (pickups on the map)

| doomednum | Weapon         |
|----------|----------------|
| 2001     | Shotgun        |
| 2002     | Chaingun       |
| 2003     | Rocket launcher|
| 2004     | Plasma rifle   |
| 2005     | BFG 9000       |
| 2006     | Chainsaw       |
| 82       | Super shotgun  (Doom II) |

### Power-ups (pickups)

| doomednum | Power-up        | Effect              |
|----------|------------------|---------------------|
| 2018     | Armor (green)    | +100% armor         |
| 2019     | Armor (blue)     | +200% armor         |
| 2011     | Stimpack         | +10% health         |
| 2012     | Medikit          | +25% health         |
| 2013     | Soul sphere      | +100% health        |
| 2022     | Invulnerability  | Temporary invuln    |
| 2023     | Berserk          | Fist damage + green |
| 2024     | Invisibility     | Partial invis       |
| 2025     | Radiation suit   | Nukage damage       |
| 2045     | Computer map     | Automap             |
| 2046     | Light amp        | Infrared vis        |
| 83       | Megasphere       | Health + armor max  |

(Other misc doomednums exist for ammo, barrels, decorations, etc.)

---

## 2. Where are keys/weapons located?

- **In the WAD:** Each map has a **THINGS** lump. Each "thing" has: **type** (doomednum), **x**, **y**, **angle**, **options** (e.g. skill flags, multiplayer). So *positions are defined per map* in the WAD.
- **In the running game:** The js-dos bundle (and the doom-wasm build in this repo) loads the WAD, calls `P_LoadThings()` which reads that lump and spawns map objects via `P_SpawnMapThing()` → `P_SpawnMobj()`. All of that is **inside the engine**; there is **no API exposed to JavaScript** that reports "key at (x,y)" or "weapon at (x,y)".

So **we do not currently "see" where a key or weapon is** from the portal/doom-browser JS. The game runs as a black box.

---

## 3. How *could* we see locations?

1. **WAD parser in JS/TS**  
   - Read the WAD file (or a copy of the THINGS lump), parse thing records (type, x, y, angle, options).  
   - Map **doomednum** → name using the table above.  
   - Then you can build a per-map list of "key at (x,y)", "shotgun at (x,y)", etc. for overlays, quest hints, or NFT-on-location. No engine changes.

2. **Engine hooks**  
   - Modify the C/doom-wasm or the js-dos runtime to expose thing/mobj data (e.g. callbacks or a small API when things are spawned or picked up). More invasive, not done in this repo.

---

## 4. Summary

- **Insight into types:** Yes — the **types** of power-ups, keys, and weapons are defined in the engine (see tables above).
- **Insight into locations:** Not from the running game. Locations live in the WAD **THINGS** lump; to use them you need a **WAD parser** (or engine hooks) outside the current doom-browser/portal stack.

For quests or NFT-on-pickup you can:
- Use the **doomednum** list above to identify *what* was picked up if you add pickup hooks later, or
- Use a WAD parser to precompute "key/weapon at (x,y)" per map for UI or design.
