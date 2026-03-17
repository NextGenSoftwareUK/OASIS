# Quest Objective System – Design (Option B)

This document designs the **Objective** as its own class belonging to **Quest**, with game-keyed dictionaries for requirements and progress, and a generated description string. It is intended for review before implementation.

---

## 1. Goals

- **Objectives as first-class type**: `Objective` is its own class (not a child Quest holon), owned by a Quest via `Quest.Objectives`.
- **Per-game tracking**: Requirements and progress are keyed by game id (e.g. `ODOOM`, `OQUAKE`) so we can express “in Doom do X, in Quake do Y”.
- **Rich, extensible structure**: Dictionaries with key `string` (game id) and value `IList<string>` for both “need to do” and “done so far”.
- **Human-readable objective text**: A dynamic `Objective` (string) property built from the requirement dictionaries, e.g. *“Kill 5 Zombie, 3 Imp in ODOOM and collect Red keycard in ODOOM within 15 mins.”*
- **Quest-level totals**: The parent Quest can hold the same tracking shape so we can show overall quest progress (e.g. total monsters killed across all objectives, or a quest-wide “Earn 500 XP” goal).

---

## 2. Recommendation: Per-Objective Progress + Quest-Level Totals

**Per objective**

- Each **Objective** has:
  - **Requirement** dictionaries (what must be done): e.g. `NeedToKillMonsters`, `NeedToCollectKeys`, …
  - **Progress** dictionaries (what has been done): e.g. `MonstersKilled`, `KeysCollected`, …
- Progress is stored only on the objective. Completion = “for each requirement dictionary, progress for that game meets or exceeds the requirement”.

**Quest level**

- **Quest** has the **same** set of dictionary properties used for:
  1. **Quest-wide targets** (optional): e.g. “Earn 500 XP total” → `Quest.NeedToEarnXP` with a target; `Quest.XPEarnt` holds the running total.
  2. **Roll-up display**: e.g. “Quest total: 10 monsters killed” = sum of objective progress, or a single total maintained on the Quest.

**Recommendation:** Use **both**:

- **Objective**: requirement + progress dictionaries (per game); no shared pool between objectives.
- **Quest**: same dictionary types used as **quest-level totals**. When the game reports progress (e.g. “killed 1 monster in ODOOM for objective A”), update:
  1. That **Objective**’s progress (e.g. `MonstersKilled["ODOOM"]`).
  2. The **Quest**’s totals (e.g. `Quest.MonstersKilled["ODOOM"]`), so the quest has a single place for “total so far” and for optional quest-wide goals (e.g. “kill 10 in any game”).

So we are **not** overcomplicating: objectives stay independent; the Quest is the place for “overall” and “quest-wide” targets.

---

## 3. Dictionary Properties

Key = **game id** (e.g. `ODOOM`, `OQUAKE`).  
Value = **IList&lt;string&gt;** (e.g. type names, counts as strings, or identifiers as needed).

### 3.1 Requirements (“need to do”)

| Property | Meaning (value semantics) |
|----------|----------------------------|
| `NeedToCollectArmor` | Armor types or counts to collect per game |
| `NeedToCollectAmmo` | Ammo types or counts |
| `NeedToCollectHealth` | Health types or counts |
| `NeedToCollectWeapons` | Weapon types to collect |
| `NeedToCollectPowerups` | Powerup types |
| `NeedToCollectItems` | Generic item types |
| `NeedToCollectKeys` | Key/keycard types (e.g. Red, Blue) |
| `NeedToKillMonsters` | Monster types and/or counts (e.g. `["Zombie","Zombie","Imp"]` or `["5","Zombie"]`) |
| `NeedToCompleteInMins` | Time limit in minutes (e.g. `["15"]`) |
| `NeedToEarnKarma` | Karma target (e.g. `["100"]`) |
| `NeedToEarnXP` | XP target (e.g. `["500"]`) |
| `NeedToGoToGeoHotSpots` | Geo hotspot ids or names to visit |
| `NeedToCompleteLevel` | Level/map ids or names to complete |

**Optional extras (same shape):**

- `NeedToUseWeapons` – use specific weapons
- `NeedToUsePowerups` – use specific powerups
- `NeedToVisitLocations` – named locations
- `NeedToSurviveMins` – survive for N minutes

### 3.2 Progress (“done so far”)

| Property | Meaning |
|----------|--------|
| `ArmorCollected` | Armor collected (types/ids per game) |
| `AmmoCollected` | Ammo collected |
| `HealthCollected` | Health collected |
| `WeaponsCollected` | Weapons collected |
| `PowerupsCollected` | Powerups collected |
| `ItemsCollected` | Items collected |
| `KeysCollected` | Keys collected |
| `MonstersKilled` | Monsters killed (types/ids; length or separate count as needed) |
| `TimeStarted` | Start time(s) (e.g. one string per game or single quest start) |
| `TimeEnded` | End time(s) |
| `TimeTaken` | Time taken (e.g. `["12"]` = 12 mins) |
| `KarmaEarnt` | Karma earned (e.g. `["50"]`) |
| `XPEarnt` | XP earned |
| `GeoHotSpotsArrived` | Geo hotspots visited |
| `LevelsCompleted` | Levels completed (if we add `NeedToCompleteLevel` / progress) |

**Semantics for “Collected/Killed” lists:**  
Either (a) list of instance ids/names (count = list length), or (b) list of counts per type, e.g. `["3","Zombie","2","Imp"]`. Design can fix this in implementation (e.g. “first element = count, rest = types” or a small structured type). For the shape we keep `Dictionary<string, IList<string>>`.

---

## 4. Objective Class (new)

- **Own class**, not a holon (unless we later decide to persist objectives as separate holons; initially they can live inside the Quest holon as a serialized list).

**Core:**

- `Id` (e.g. `Guid`) – for API and completion tracking.
- `Order` (int) – display and sequencing.
- `Objective` (string) – **read-only, computed** from the requirement dictionaries (see §6).

**Requirement dictionaries (all `Dictionary<string, IList<string>>`):**

- NeedToCollectArmor, NeedToCollectAmmo, NeedToCollectHealth, NeedToCollectWeapons, NeedToCollectPowerups, NeedToCollectItems, NeedToCollectKeys  
- NeedToKillMonsters, NeedToCompleteInMins, NeedToEarnKarma, NeedToEarnXP, NeedToGoToGeoHotSpots, NeedToCompleteLevel  
- Plus any “NeedTo*” extras (NeedToUseWeapons, etc.).

**Progress dictionaries (same shape):**

- ArmorCollected, AmmoCollected, HealthCollected, WeaponsCollected, PowerupsCollected, ItemsCollected, KeysCollected  
- MonstersKilled  
- TimeStarted, TimeEnded, TimeTaken  
- KarmaEarnt, XPEarnt  
- GeoHotSpotsArrived  
- LevelsCompleted (if we add level completion).

**Optional:**

- `IsCompleted` (bool) – derived from “progress meets requirements” or set when the system marks it complete.
- `CompletedAt`, `CompletedBy` – for auditing.

---

## 5. Quest Class (changes)

- **`Objectives`**: **`IList<IObjective>`** – Quest has a list of objectives. This is the only objectives model; no migration from child Quests (dev-only, DB can be reset).
- **Same dictionary properties as Objective** (both requirement and progress) on the Quest for:
  - **Quest-level targets** (e.g. “Earn 500 XP total”).
  - **Quest-level progress** updated whenever any objective (or direct quest action) contributes (e.g. `Quest.MonstersKilled["ODOOM"]` = total for that game across the quest).

So Quest has:

- **`IList<IObjective> Objectives`** – the list of objectives belonging to this quest.
- All `NeedTo*` and progress dictionaries listed in §3.

---

## 6. Building the Dynamic `Objective` String

- **Input**: The objective’s **requirement** dictionaries only.
- **Output**: A single string for the `Objective` property, e.g.  
  *“Kill 5 Zombie, 3 Imp in ODOOM and collect Red keycard in ODOOM within 15 mins.”*

**Algorithm (conceptual):**

1. For each requirement dictionary that has at least one game key with non-empty list:
   - Format a phrase (e.g. “Kill X, Y in {game}”, “Collect X in {game}”, “Within N mins”, “Earn N XP in {game}”).
2. Concatenate phrases (e.g. with “ and ”).
3. Optionally: “in X game” / “in Y game” can be grouped (e.g. “in ODOOM and OQUAKE”) depending on UX.

**Examples:**

- `NeedToKillMonsters["ODOOM"] = ["5","Zombie","3","Imp"]`, `NeedToCollectKeys["ODOOM"] = ["Red"]`, `NeedToCompleteInMins["ODOOM"] = ["15"]`  
  → *“Kill 5 Zombie, 3 Imp in ODOOM. Collect Red key in ODOOM. Complete within 15 mins.”*
- `NeedToCollectPowerups["ODOOM"] = ["2","Stimpack"]`, `NeedToEarnXP["OQUAKE"] = ["200"]`, `NeedToEarnKarma["ODOOM"] = ["50"]`  
  → *“Collect 2 Stimpack in ODOOM and earn 200 XP in OQUAKE and 50 Karma in ODOOM.”*

Formatting rules (exact wording, grouping, localization) can be refined in implementation; the important part is that **Objective (string) is always derived from the requirement dictionaries**, not stored separately.

---

## 7. No Migration (Dev-Only)

- We are implementing Option B only. No backward compatibility with the old “objective as child Quest” model.
- **Persistence**: Quest has **`IList<IObjective> Objectives`**. When the Quest holon is saved to the database, this list is stored as part of the Quest (e.g. the storage provider serializes the whole Quest, including the `Objectives` list, into one document/record). We do **not** create separate holon records for each objective.
- DB can be reset when needed during development.

---

## 8. Summary

| Item | Recommendation |
|------|----------------|
| **Objective** | New class with Id, Order, requirement dictionaries, progress dictionaries, and computed `Objective` string. |
| **Quest** | Has `Objectives` (list of Objective) and the **same** dictionary set for quest-level requirements and totals. |
| **Per-game key** | All dictionaries: key = game id (e.g. ODOOM, OQUAKE); value = IList&lt;string&gt;. |
| **Totals** | Update both the relevant Objective progress and the Quest’s progress dictionaries when reporting progress. |
| **Dynamic text** | `Objective.Objective` is built from the objective’s requirement dictionaries only. |

If this design matches what you have in mind, next step is to implement **IObjective** / **Objective** and add the dictionary properties to both **Objective** and **Quest**, then the string builder and API updates.
