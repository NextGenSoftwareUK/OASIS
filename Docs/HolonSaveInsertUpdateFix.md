# Holon Save: Insert vs Update Fix

**Status:** Phases 1 & 2 complete. Phases 3 & 4 pending.  
**Last updated:** 2026-07-26  
**Affected repos:** `OASIS2` (master + Development), `trust` (SovereignTrust site)

---

## The Bug

Saving a holon from a stateless REST client (e.g. SovereignTrust's Vercel serverless functions) created a **new MongoDB document on every save** instead of updating the existing one. The dashboard would fill up with duplicate entries.

The symptom was observed in SovereignTrust (trust holon type `141`) but the root cause was systemic — affecting any caller that saved a holon without knowing its `CreatedDate` or MongoDB `_id`.

---

## Root Cause Analysis

The bug has three interlocking layers:

### Layer 1 — `IsNewHolon` was persisted in MongoDB

`IsNewHolon` in the MongoDB entity (`HolonBase.cs`) had no `[BsonIgnore]` attribute. When a holon was first inserted, `IsNewHolon = true` was written to the document. On every subsequent load, the holon came back with `IsNewHolon = true`.

### Layer 2 — `PrepareHolonForSaving` unconditionally reset `IsNewHolon`

To work around Layer 1, `HolonManager-Private.cs`'s `PrepareHolonForSaving` method unconditionally set `holon.IsNewHolon = false` for any holon with a non-empty `Id`:

```csharp
if (holon.Id == Guid.Empty)
{
    holon.Id = Guid.NewGuid();
    holon.IsNewHolon = true;
}
else
    holon.IsNewHolon = false;  // wiped ANY caller-set IsNewHolon = true
```

This meant callers that pre-assigned an `Id` (e.g. STAR celestial bodies, COSMIC manager, avatar registration) and set `IsNewHolon = true` were silently ignored — their holons went through `UpdateAsync` instead of `AddAsync`.

### Layer 3 — `MongoDBOASIS.SaveHolonAsync` had a `CreatedDate` fallback that kept being re-introduced

The `|| holon.CreatedDate == DateTime.MinValue` check was added as a workaround so internal C# callers (which pre-assign IDs but don't set `CreatedDate`) would still hit `AddAsync`:

```csharp
// BAD — stateless REST clients also have CreatedDate == MinValue, so they always inserted
if (holon.IsNewHolon || holon.CreatedDate == DateTime.MinValue)
    // → AddAsync (insert)
else
    // → UpdateAsync (update)
```

Removing this check broke internal C# code. Re-adding it broke REST clients. This tension was unresolvable without fixing the underlying `IsNewHolon` persistence problem.

---

## Fixes Applied

### Fix 1 — Remove `|| holon.CreatedDate == DateTime.MinValue` from `MongoDBOASIS.cs`

**File:** `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/MongoDBOASIS.cs`  
**Branches:** master (`b25285e52`), Development (`891b9e355`)

Both `SaveHolonAsync` and `SaveHolon` now branch purely on `holon.IsNewHolon`:

```csharp
// SaveHolonAsync
OASISResult<IHolon> result = holon.IsNewHolon
    ? DataHelper.ConvertMongoEntityToOASISHolon(await _holonRepository.AddAsync(...))
    : DataHelper.ConvertMongoEntityToOASISHolon(await _holonRepository.UpdateAsync(...));
```

### Fix 2 — Add `MatchedCount == 0` upsert fallback to `HolonRepository`

**File:** `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/HolonRepository.cs`  
**Branches:** master (`1b4897e6f`), Development (`ec94a6ac8`)

`UpdateAsync` now uses `ReplaceOneAsync` (filter by `HolonId`) and falls back to `AddAsync` if no document was matched:

```csharp
var replaceResult = await _dbContext.Holon.ReplaceOneAsync(
    filter: g => g.HolonId == holon.HolonId, replacement: holon);

if (replaceResult.MatchedCount == 0)
    return await AddAsync(holon);   // pre-assigned GUID, document not yet in DB
```

The same fallback was added to the synchronous `Update` method using `ReplaceOne`.

This handles the case where internal C# code pre-assigns a GUID (to link holons without a round-trip reload) and calls save for the first time — the document doesn't exist yet so `UpdateAsync` must insert it. The fallback eliminates any need for the `CreatedDate` workaround.

### Fix 3 (Phase 1) — Add `[BsonIgnore]` to `IsNewHolon` in MongoDB `HolonBase`

**File:** `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Entities/HolonBase.cs` line 99

```csharp
[BsonIgnore] // Runtime flag only — must never be persisted; loaded holons must always start with false (C# default).
public bool IsNewHolon { get; set; }
```

`IsNewHolon` is a runtime coordination flag, not persistent state. Persisting it caused every loaded holon to arrive with `IsNewHolon = true`, forcing `PrepareHolonForSaving` to unconditionally reset it (see Layer 2 above). With `[BsonIgnore]`, loaded holons always deserialize with the C# default `false`.

### Fix 4 (Phase 2) — Stop unconditionally overriding caller-set `IsNewHolon`

**File:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/HolonManager/HolonManager-Private.cs` line 85

```csharp
// Old: else holon.IsNewHolon = false;  — wiped any caller-set IsNewHolon = true
// New:
else if (!holon.IsNewHolon)
{
    // Normal loaded holon: IsNewHolon is already false (C# default after [BsonIgnore]).
    // Do NOT reset to false unconditionally — callers that pre-assign an Id and set
    // IsNewHolon = true explicitly must be respected.
}
```

This is safe **only because** `[BsonIgnore]` is now in place (Fix 3). Without it, loaded holons would have `IsNewHolon = true` from MongoDB and would be incorrectly re-inserted.

---

## Full Codebase Audit — Pre-Assigned ID Patterns

The following files contain holons constructed with a pre-assigned GUID. Before Fix 2 (`MatchedCount` fallback) and Fix 3 (`[BsonIgnore]`) many of these had ineffective `IsNewHolon = true` settings that were silently wiped by `PrepareHolonForSaving`.

### WEB6

| File | Line | Pattern |
|------|------|---------|
| `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/HolonicBraidManager.cs` | 50 | `new Holon(LibraryHolonId)` — well-known GUID |
| `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/HolonicMemoryManager.cs` | 65 | `new Holon(EarthHolonId)` — well-known GUID |

### STAR ODK — CelestialSpace

| File | Lines | Pattern |
|------|-------|---------|
| `STAR ODK/NextGenSoftware.OASIS.STAR/CelestialSpace/Omiverse.cs` | 77, 81 | Pre-assigned GUID |
| `STAR ODK/NextGenSoftware.OASIS.STAR/CelestialSpace/Universe.cs` | 151 | Pre-assigned GUID |
| `STAR ODK/NextGenSoftware.OASIS.STAR/CelestialSpace/SuperVerse.cs` | 39 | Pre-assigned GUID |
| `STAR ODK/NextGenSoftware.OASIS.STAR/CelestialSpace/Multiverse.cs` | 58, 62 | Pre-assigned GUID |
| All 14 Dimension class files | various | Pre-assigned GUID |

### STAR ODK — CelestialBodies

| File | Lines | Pattern |
|------|-------|---------|
| `STAR ODK/NextGenSoftware.OASIS.STAR/Star.cs` | 1173, 1230, 1357, 2981, 3224 | Pre-assigned GUID + **ineffective** `IsNewHolon = true` (was wiped by PrepareHolonForSaving) |
| `STAR ODK/NextGenSoftware.OASIS.STAR/CelestialBodies/GreatGrandSuperStarCore.cs` | 100–101 | Pre-assigned GUID + **ineffective** `IsNewHolon = true` |
| `STAR ODK/NextGenSoftware.OASIS.STAR/CelestialBodies/GrandSuperStarCore.cs` | 305–306, 314–315 | Pre-assigned GUID + **ineffective** `IsNewHolon = true` |

### ONODE

| File | Lines | Pattern |
|------|-------|---------|
| `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/STARNET/STARNETManagerBase.cs` | 168–173, 341–345 | Pre-assigned GUID |
| `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/COSMICManager.cs` | 12+ locations | Pre-assigned GUID + **ineffective** `IsNewHolon = true` |

### Core Managers

| File | Lines | Pattern |
|------|-------|---------|
| `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/KarmaManager.cs` | 798–810 | Pre-assigned GUID + **ineffective** `IsNewHolon = true` |
| `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/AvatarManager/AvatarManager-Private.cs` | 383–390, 442 | Pre-assigned GUID **AND** `CreatedDate = DateTime.Now` set in constructor — avatar registration bug |

---

## 4-Phase Fix Plan

| Phase | Description | Status |
|-------|-------------|--------|
| **1** | Add `[BsonIgnore]` to `IsNewHolon` in MongoDB `HolonBase.cs` | ✅ Complete |
| **2** | Fix `PrepareHolonForSaving` to not unconditionally override caller-set `IsNewHolon = true` | ✅ Complete |
| **3** | Fix avatar registration: add `MatchedCount` fallback to `AvatarRepository`; fix `AvatarManager-Private.cs` to not set `CreatedDate = DateTime.Now` before save | ⏳ Pending |
| **4** | Cleanup: remove now-effective but redundant `IsNewHolon = true` settings from `COSMICManager.cs`, `Star.cs`, `GrandSuperStarCore.cs`, `GreatGrandSuperStarCore.cs`, `KarmaManager.cs` | ⏳ Pending |

### Phase 3 Detail

`AvatarManager-Private.cs:383` currently runs:

```csharp
avatar.Id = Guid.NewGuid();
avatar.CreatedDate = DateTime.Now;  // sets CreatedDate even before save — breaks IsNewHolon logic
```

This pre-sets `CreatedDate`, which was the original motivation for the `|| holon.CreatedDate == DateTime.MinValue` hack in `MongoDBOASIS.cs`. The fix:
1. Remove `CreatedDate = DateTime.Now` from the constructor call in `AvatarManager-Private.cs:383`
2. Add `MatchedCount == 0` → `AddAsync` fallback to `AvatarRepository.UpdateAsync` and `AvatarRepository.Update` (same pattern as `HolonRepository`)

### Phase 4 Detail

All `IsNewHolon = true` settings in `Star.cs`, `COSMICManager.cs`, `GrandSuperStarCore.cs`, `GreatGrandSuperStarCore.cs`, and `KarmaManager.cs` were previously wiped by `PrepareHolonForSaving`. After Phases 1–2 they are now **effective**: the holons will be treated as new inserts. These callers use pre-assigned well-known GUIDs, so the `MatchedCount == 0` fallback in `HolonRepository` ensures they are inserted only if they don't already exist. Phase 4 is cosmetic cleanup — the `IsNewHolon = true` lines are no longer harmful, just redundant since the `else if (!holon.IsNewHolon)` branch in `PrepareHolonForSaving` already handles pre-assigned IDs via the `MatchedCount` fallback.

---

## Trust Site (`C:\Source\trust`)

**File:** `api/trust-save.js`

The Vercel serverless handler was the original symptom surface. The load-before-update workaround (reading the existing holon before saving to merge fields) was added, removed, re-added, and commented out several times as the backend was debugged.

**Current state:** the load-before-update block is commented out, relying on the backend fixes (Fixes 1–4 above) to correctly upsert based on the `Id` field.

**Next step:** once the OASIS API is redeployed with these fixes, verify saves work correctly without the workaround, then delete the commented block.

---

## Key Invariants (for future reference)

- `IsNewHolon` is a **runtime flag** set by `PrepareHolonForSaving`. It must never be read from or written to MongoDB. `[BsonIgnore]` enforces this.
- `PrepareHolonForSaving` is the **single source of truth** for `IsNewHolon`. Callers may set `IsNewHolon = true` on holons with pre-assigned IDs, and `PrepareHolonForSaving` will now respect that.
- `HolonRepository.UpdateAsync` always uses `ReplaceOneAsync` (filter by `HolonId`). If no document is matched, it falls back to `AddAsync`. This makes `UpdateAsync` safe to call for holons with pre-assigned IDs that may not yet exist in MongoDB.
- The `MongoDBOASIS` provider branches on `IsNewHolon` only. It no longer looks at `CreatedDate`, `ProviderUniqueStorageKey`, or any other field to decide insert vs update.
