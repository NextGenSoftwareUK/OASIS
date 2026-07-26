# Cross-game powerup & weapon map (ODOOM ↔ OQuake)

This document defines how **powerups** (first) and later **weapons** should align across **ODOOM** and **OQuake** for STAR inventory, beam-in, and “use in other game” behaviour.

## Why this exists

- Today each game sends **`star_api_queue_add_item(name, …, itemType, …)`** with **game-specific display names** (see ODOOM `ToStarItemName()` in `uzdoom_star_integration.cpp` and OQuake names in `oquake_star_integration.c`, e.g. `"Megahealth"`, `"Soul Sphere"`).
- Cross-game features (shared inventory row, unlock in the other title, consistent UI tabs) need a **single canonical identity** per logical powerup, plus rules for **how** each engine applies that powerup when consumed or spawned.

**Recommended approach**

1. Introduce a small set of **canonical STAR names** (stable strings) used in the API for mapped items.
2. Each game keeps a **local alias table**: native pickup / class → canonical name on **add**, and canonical name → native effect on **apply** (beam-in, use-from-overlay, or scripted grant).
3. Store **extra semantics** (decay, duration, cap) in **game code** when applying; the API row can stay a simple name + `Powerup` type unless we later add structured metadata.

---

## Phase 1 — Powerups: “easy” equivalences

These pairs are **close in role**; remaining differences are **timing and caps**, not fundamental redesign.

| Canonical STAR name (proposed) | Quake (source) | Doom (source) | Notes |
|--------------------------------|----------------|---------------|--------|
| **`OASIS.MegaHealth`** | Megahealth: health to 250, then **decay 1/sec** toward 100 | **Soul Sphere**: large health boost toward **200 cap** (vanilla: +100% up to max 200; no decay) | Same *bucket*: “big health overcap / top-up”. When applying in Doom from Quake: grant **up to 200** (or match `SoulSphere` rules). When applying in Quake from Doom: grant **megahealth behaviour** (250 + decay) or a documented subset (e.g. cap at 200 only) — **product decision**. |
| **`OASIS.MegaHealthArmor`** | *(no direct single pickup)* | **Megasphere**: **200 health + 200 armor** (vanilla-style) | Quake has no one pickup; **beam-in to Quake** could grant megahealth **and** yellow armor (or red), or split into two API rows — **spec later**. |
| **`OASIS.QuadDamage`** | Quad Damage (`IT_QUAD`) | **New or existing ZScript powerup**: **×4 damage** for a duration | Quake SFX on Doom side: **ship Quake `items/quaddamage` sounds** (respect GPL/source attribution) or record-alike; wire in ZScript/`A_PlaySound`. |
| **`OASIS.Invulnerability`** | Pentagram of Protection (`IT_INVULNERABILITY`) | Invulnerability sphere (`InvulnSphere` → STAR name `"Invulnerability"`) | Direct map; duration rules differ by game — normalize duration when applying cross-game if needed. |
| **`OASIS.EnvironmentSuit`** | Biosuit (`IT_SUIT`) | Radiation shielding suit (radsuit) | Same role: **environmental / slime / rad** protection; implement as native suit in each port. |

**Current code names (for migration)**

| Game | Today (inventory `name` examples) | Action |
|------|-----------------------------------|--------|
| OQuake | `"Megahealth"`, `"Quad Damage"`, `"Pentagram of Protection"`, `"Biosuit"` | Emit **canonical** `OASIS.*` on add (or alias in client) |
| ODOOM | `"Soul Sphere"`, `"Mega Sphere"`, `"Invulnerability"`, radsuit if wired | Map class → same **`OASIS.*`** |

---

## Phase 2 — Powerups: harder cases

| Topic | Doom | Quake | Problem |
|-------|------|-------|---------|
| **Partial invisibility** | Blur sphere / partial vis | — | No Quake analogue; options: **Doom-only** row, or **weak** Ring of Shadows in Quake (shorter / partial blend). |
| **Light amplification** | Light amp visor | — | Quake has no standard “see in dark” pickup; options: **fullbright-style** post-process / r_lightmap cvar hack / short-lived powerup — **engine-specific**. |
| **Full invisibility** | — | Ring of Shadows | No Doom vanilla analogue; options: **translucent player** in UZDoom or **Doom-only skip** when beaming from Quake. |
| **Berserk** | Berserk pack | — | Quake: could map to **melee damage boost** or **separate** `OASIS.Berserk` only for Doom. |

For these, keep **distinct canonical IDs** (`OASIS.PartialVis`, `OASIS.LightAmp`, `OASIS.RingShadows`, …) and define **per-target-game** “best effort” or **no-op with message** until gameplay is agreed.

---

## Weapons & ammo (beam-in, player-facing names)

STAR rows use **short display names** plus a **game suffix** in storage (e.g. `Nails (OQUAKE)`, `Bullets (ODOOM)`). The in-game overlay should show **Shotgun**, **Nailgun**, **Chaingun**, etc.—not `OASIS.Weapon.*` strings.

### Config: `oasisstar.json` (both games)

Optional string keys (comma-separated `From=To` pairs, spaces optional):

| Key | Used when playing |
|-----|-------------------|
| **`cross_game_doom_ammo_to_quake`** | **OQuake**: map Doom ammo display name → Quake ammo (Shells / Nails / Rockets / Cells). |
| **`cross_game_quake_ammo_to_doom`** | **ODOOM**: map Quake ammo display name → Doom ammo (Bullets / Shells / Rockets / Cells). |
| **`cross_game_doom_weapon_to_quake`** | **OQuake**: Doom **Weapon** rows → vkQuake **`give 2`–`give 8`** (id1 weapons via `Host_Give_f`), or client `cl.items` OR in **deathmatch** (vanilla `give` is a no-op there). |
| **`cross_game_quake_weapon_to_doom`** | **ODOOM**: Quake weapon display name → Doom actor class for **`give`** (e.g. `PlasmaRifle`, `BFG9000`). |

Defaults match the current product intent: **Nails ↔ Bullets** (chaingun ↔ nailgun), **Lightning Gun → BFG9000**, **Super Nailgun** and **Grenade Launcher** → **PlasmaRifle**, shotguns and rocket launchers aligned by name.

### Behaviour

- **Once per beam-in session**, after STAR inventory is available, the **receiving** game adds **ammo quantities** from the **other** game’s `Ammo` rows (1:1 count, subject to local max ammo).
- **OQuake ammo (vkQuake):** when **not** in deathmatch, uses **`give s|n|r|c <new_total>`** so the **server** owns ammo (same as `Host_Give_f`). In **deathmatch**, `give` is a no-op in vanilla vkQuake — ammo is applied by updating **`cl.stats`** only (best effort; may desync in MP). STAR ammo deltas from that grant are **suppressed** for a few frames so rows are not duplicated.
- **ODOOM ammo:** adds to existing **Clip/Shell/RocketAmmo/Cell** inventory or runs **`give`** to create it. In **deathmatch**, `give` may be blocked without cheats — if nothing is granted, enable **`sv_cheats`** or pick up one box of that ammo type first (debug log when `star_debug` is on).
- **ODOOM** runs **`give <class>`** once per mapped **Weapon** row from Quake if the player does not already have that weapon.
- **OQuake** runs **`give N`** (vkQuake digit form: 2=shotgun … 8=lightning) per mapped **Weapon** row from Doom, and **suppresses duplicate STAR weapon rows** for those bits for a few frames.
- To pull updated counts from STAR after play in the other title, **beam out and beam in again** (or start a new session)—the transfer is intentionally not re-run every map load.

---

## Implementation checklist (engineering)

1. **Canonical strings** — Add `OASIS.*` constants in shared header or single JSON config consumed by both integrations (avoid duplicated spellings).
2. **ODOOM** — Extend `ToStarItemName` / pickup path so Soul Sphere / Megasphere / Invuln / radsuit / future Quad emit canonical IDs; ZScript or ACS for **Quad** behaviour + sounds.
3. **OQuake** — Replace or alias `"Megahealth"` → `OASIS.MegaHealth` (etc.) in `OQ_AddInventoryEvent` / stats paths.
4. **Apply path** — When inventory is **used** or **beam-in** grants an item, resolve canonical ID → **native effect** in the **receiving** game (new functions parallel to existing key/door unlock logic).
5. **UI** — Inventory overlay can show **friendly label** (Doom vs Quake) while sorting/filtering on canonical id if we add a display map.
6. **Tests** — STARAPIClient harness: add_item with `OASIS.QuadDamage`, fetch inventory, second game consumes → **one** row stable across both.

---

## References

- OQuake pickup names: `OQuake/Code/oquake_star_integration.c` (`IT_SUPERHEALTH`, `IT_QUAD`, …).
- ODOOM name mapping: `ODOOM/uzdoom_star_integration.cpp` (`ToStarItemName`, `GetHealthOrArmorAmount`).
- Inventory architecture: [ARCHITECTURE.md](ARCHITECTURE.md), [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md), OQuake [INVENTORY_FLOW.md](../OQuake/Docs/INVENTORY_FLOW.md).

---

*Phase 1 table is the source of truth for “easy” mappings; adjust numeric behaviour (megahealth vs soul sphere) after playtest.*
