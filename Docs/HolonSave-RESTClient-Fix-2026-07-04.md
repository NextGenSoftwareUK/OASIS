# Holon Duplicate-Insert Fix — 2026-07-04

## Problem

Every save of a trust holon from the SovereignTrust Vercel frontend was creating a NEW
MongoDB document instead of updating the existing one. The dashboard accumulated duplicate
entries on each save.

### Root cause: three-layer bug

1. **`IsNewHolon` was persisted to MongoDB** — `[BsonIgnore]` was missing in the MongoDB
   entity. Loaded holons always deserialised `IsNewHolon = true` from the stored value.

2. **`PrepareHolonForSaving` unconditionally wiped `IsNewHolon`** — after correctly setting
   it to `true` for `Id == Guid.Empty` (new holon) or leaving it for other cases, the
   `else` branch blindly set `holon.IsNewHolon = false`. This destroyed the `IsNewHolon = true`
   that callers like WEB6 and STARNET legitimately set when they pre-assigned a known GUID.

3. **`MongoDBOASIS.SaveHolonAsync` used `|| holon.CreatedDate == DateTime.MinValue`** —
   Stateless REST/JS clients never set `CreatedDate`, so the fallback always triggered an
   `AddAsync` (insert) regardless of whether the holon already existed.

These bugs did not affect C# server-side callers that load a holon and then save it back,
because those holons already have `CreatedDate` populated and `IsNewHolon` loaded from DB.

---

## Changes

### Fix 1 — `[BsonIgnore]` on `IsNewHolon` (MongoDB entity)

**File:** `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Entities/HolonBase.cs`

```csharp
[BsonIgnore] // Runtime flag only — must never be persisted; loaded holons must always start with false (C# default).
public bool IsNewHolon { get; set; }
```

**Why:** Without this, every loaded holon came back from MongoDB with `IsNewHolon = true`,
causing every subsequent save to insert a new document. The flag is a runtime-only signal;
it has no meaning in stored data.

---

### Fix 2 — `PrepareHolonForSaving` honours caller-set `IsNewHolon`

**File:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/HolonManager/HolonManager-Private.cs`

**Before:**
```csharp
if (holon.Id == Guid.Empty)
{
    holon.Id = Guid.NewGuid();
    holon.IsNewHolon = true;
}
else
    holon.IsNewHolon = false;  // ← wiped caller-set IsNewHolon = true
```

**After:**
```csharp
// Case 1: No Id — generate one and insert.
// Case 2: Caller pre-assigned Id AND set IsNewHolon = true — honour it; insert.
// Case 3: Id present and IsNewHolon = false (default after loading) — update; no-op.
if (holon.Id == Guid.Empty)
{
    holon.Id = Guid.NewGuid();
    holon.IsNewHolon = true;
}
else if (!holon.IsNewHolon)
{
    // Case 3: treat as update. IsNewHolon stays false — no-op.
}
```

**Why:** Some callers (WEB6, STARNET, COSMICManager) pre-assign a well-known GUID for a
holon that is being created for the first time. They must set `IsNewHolon = true` alongside
the pre-assigned Id. The old `else holon.IsNewHolon = false` silently destroyed that signal.

---

### Fix 3 — `MongoDBOASIS.SaveHolonAsync` and `SaveHolon` route purely on `IsNewHolon`

**File:** `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/MongoDBOASIS.cs`

**Before:**
```csharp
OASISResult<IHolon> result = holon.IsNewHolon || holon.CreatedDate == DateTime.MinValue
    ? AddAsync(...)
    : UpdateAsync(...);
```

**After:**
```csharp
OASISResult<IHolon> result = holon.IsNewHolon
    ? AddAsync(...)
    : UpdateAsync(...);
```

Applied to both `SaveHolonAsync` (async) and `SaveHolon` (sync).

**Why:** Stateless REST/JS clients never set `CreatedDate`, so `CreatedDate == DateTime.MinValue`
was always true — every REST save called `AddAsync` and created a new document.
`PrepareHolonForSaving` (Fix 2) is now the single authoritative place that sets `IsNewHolon`
based on `Id == Guid.Empty`. The `CreatedDate` fallback is redundant and dangerous.

**Avatar save paths** (`SaveAvatarAsync`, `SaveAvatarDetailAsync`, `SaveAvatar`) are
deliberately left unchanged — they still use `|| avatar.CreatedDate == DateTime.MinValue`
because `PrepareAvatarForSaving` has not yet been updated to match. Changing the avatar path
without first fixing `PrepareAvatarForSaving` broke login in a previous attempt.

---

### Fix 4 — `HolonRepository.UpdateAsync` / `Update` — `MatchedCount == 0` fallback

**File:** `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/HolonRepository.cs`

```csharp
var replaceResult = await _dbContext.Holon.ReplaceOneAsync(filter: g => g.HolonId == holon.HolonId, replacement: holon);
if (replaceResult.MatchedCount == 0)
    return await AddAsync(holon); // Safety fallback: document not in DB yet — insert instead.
```

Same for the sync `Update` / `ReplaceOne` path.

**Why:** If `PrepareHolonForSaving` routes to `UpdateAsync` but the document does not yet
exist (e.g. a race condition or a first-ever save with a pre-assigned Id that somehow missed
`IsNewHolon = true`), `ReplaceOneAsync` will silently match nothing. The fallback ensures
the document is still created rather than lost. This is a MongoDB-only safety net; other
providers rely on `IsNewHolon` and have no equivalent.

---

### Fix 5 — Caller contract enforcement (`IsNewHolon = true` where Id is pre-assigned)

Any caller that pre-assigns an `Id` for a NEW holon MUST also set `IsNewHolon = true`.
Failing to do so causes `PrepareHolonForSaving` (Fix 2) to treat it as an update and the
record will be silently not created.

Updated callers:

| File | Location | Change |
|---|---|---|
| `WEB6/HolonicBraidManager.cs` | `new Holon(LibraryHolonId)` | Added `IsNewHolon = true` |
| `WEB6/HolonicMemoryManager.cs` | `new Holon(EarthHolonId)` | Added `IsNewHolon = true` |
| `STARNET/STARNETManagerBase.cs` | Both `new T1() { Id = Guid.NewGuid() }` blocks | Added `IsNewHolon = true` |

COSMICManager, Star, GrandSuperStarCore, GreatGrandSuperStarCore, and KarmaManager already
had `IsNewHolon = true` set explicitly after their `new T()` calls — these were left unchanged.

---

### Fix 6 — `HolonBase.cs` (OASIS core) — XML doc on `IsNewHolon`

**File:** `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Holons/HolonBase.cs`

Replaced the old `//TODO: Want to remove this ASAP!` comment with a full XML doc block
spelling out the three-case IsNewHolon contract for all callers.

---

## IsNewHolon Caller Contract (enforced)

| Case | What to do | Result |
|---|---|---|
| 1 — Creating a holon, no specific Id needed | Set nothing — leave `Id` as `Guid.Empty` | System assigns a new GUID; insert |
| 2 — Creating a holon with a pre-assigned Id | Set `Id = <known GUID>` AND `IsNewHolon = true` | Insert with that specific Id |
| 3 — Updating an existing holon | Load the holon, modify, save — `IsNewHolon` stays false | Update |

**Never set `IsNewHolon = false` explicitly.** It defaults to false after loading from DB
(because `[BsonIgnore]` means it is never persisted). Only set it to `true`.

---

## What was NOT changed (avatar paths)

Avatar save paths in `MongoDBOASIS.cs` (`SaveAvatarAsync`, `SaveAvatarDetailAsync`,
`SaveAvatar`) still use `|| avatar.CreatedDate == DateTime.MinValue`. This is because
`PrepareAvatarForSaving` in `AvatarManager-Private.cs` has the SAME OLD pattern as the
holon path before these fixes, and relies on `CreatedDate == DateTime.MinValue` to detect
new avatars. Fixing the avatar path requires the same three-layer treatment but is a
separate task — it is tracked but not yet applied.

A previous attempt to prematurely remove `CreatedDate = DateTime.Now` from avatar
registration helpers (without first fixing `PrepareAvatarForSaving`) broke login and
avatar verification. All avatar-path changes were reverted. Only the holon path was fixed.

---

## Branch

All changes in this doc are on the `Fixing-Stateless-Holon-Update` feature branch.
They have NOT been merged to `master` or `Development` yet — testing required first.
