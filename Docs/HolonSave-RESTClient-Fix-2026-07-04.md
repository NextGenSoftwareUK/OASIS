# Holon Save Fix for REST/JS Clients — 2026-07-04

## Problem

Stateless REST/JS clients (e.g. Vercel serverless functions) construct holon objects from
scratch using only the OASIS GUID (`Id`). They have no access to internal provider state
such as the MongoDB ObjectId (`_id`) or the `CreatedDate` of a previously saved holon.

This caused three separate failures when a JS client tried to **update** an existing holon:

1. Every save was treated as an **insert** — a new MongoDB document was created each time
   instead of updating the existing one.
2. When the async save path was forced to call `UpdateAsync`, MongoDB rejected it with
   **error code 66** ("immutable field `_id` was altered to null") because the replacement
   document had `_id: null`.

These bugs did not affect C# callers that keep a holon in memory — loaded holons already
carry `CreatedDate`, `ProviderUniqueStorageKey`, and the MongoDB `_id`, so they passed all
three checks silently.

---

## Changes Made

### 1. `HolonManager-Private.cs` — `PrepareHolonForSaving`

**File:**
`OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/HolonManager/HolonManager-Private.cs`

**What changed:**
Removed `CreatedDate == DateTime.MinValue` from the "is this a new holon?" check.
`IsNewHolon` is now set based solely on `Id == Guid.Empty`.

**Before:**
```csharp
if (holon.Id == Guid.Empty || holon.CreatedDate == DateTime.MinValue)
{
    if (holon.Id == Guid.Empty)
        holon.Id = Guid.NewGuid();
    holon.IsNewHolon = true;
}
else if (holon.CreatedDate != DateTime.MinValue)
    holon.IsNewHolon = false;
```

**After:**
```csharp
if (holon.Id == Guid.Empty)
{
    holon.Id = Guid.NewGuid();
    holon.IsNewHolon = true;
}
else
    holon.IsNewHolon = false;
```

**Why:** REST/JS clients never set `CreatedDate`, so it is always `DateTime.MinValue`.
The old check treated every REST save as a new insert regardless of whether a real `Id`
was supplied. C# callers are unaffected — their in-memory holons already have
`CreatedDate` populated, but it is no longer used for this decision.

**Risk:** Low. `Id == Guid.Empty` has always been the primary signal. The only scenario
that would change behaviour is a C# caller that manually constructs a holon with a real
`Id` but deliberately leaves `CreatedDate` at `MinValue` expecting an insert — that
pattern was already incorrect.

---

### 2. `MongoDBOASIS.cs` — `SaveHolonAsync`

**File:**
`Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/MongoDBOASIS.cs`

**What changed:**
The async `SaveHolonAsync` now uses `IsNewHolon` to decide insert vs update, matching
the sync `SaveHolon` which already used `IsNewHolon`.

**Before:**
```csharp
OASISResult<IHolon> result = !holon.ProviderUniqueStorageKey.ContainsKey(ProviderType.MongoDBOASIS)
    ? AddAsync(...)
    : UpdateAsync(...);
```

**After:**
```csharp
OASISResult<IHolon> result = holon.IsNewHolon
    ? AddAsync(...)
    : UpdateAsync(...);
```

**Why:** `ProviderUniqueStorageKey` is the internal MongoDB ObjectId — an implementation
detail that external callers cannot know. Any caller without this key always hit
`AddAsync`, creating a new document on every save. C# callers that load then save carry
the key in the in-memory holon, so they were unaffected. Now both the sync and async
paths use the same `IsNewHolon` logic.

**Risk:** Low. The only caller that could be affected is one that has `ProviderUniqueStorageKey`
set but `Id == Guid.Empty` — an unusual combination that would indicate a bug in the
caller anyway.

---

### 3. `HolonRepository.cs` — `UpdateAsync`

**File:**
`Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/HolonRepository.cs`

**What changed:**
Before calling `ReplaceOneAsync`, if the holon's MongoDB `_id` (`Id` field on the entity)
is null or empty, the existing document is fetched by `HolonId` and its `_id` is copied
across.

**Added logic:**
```csharp
if (string.IsNullOrEmpty(holon.Id))
{
    Holon originalHolon = await GetHolonAsync(holon.HolonId);
    if (originalHolon != null)
        holon.Id = originalHolon.Id;
}
```

**Why:** `ReplaceOneAsync` requires the replacement document's `_id` to equal the existing
document's `_id`. REST/JS clients only know the OASIS GUID (`HolonId`); they have no way
to supply the MongoDB ObjectId. Without this fix, `ReplaceOneAsync` received `_id: null`
and MongoDB rejected it with error code 66. C# callers that hold a loaded holon always
have `_id` populated, so the lookup is skipped for them.

**Risk:** Very low. The extra `GetHolonAsync` call only runs when `_id` is missing — C#
in-memory holons always have it. The worst case is one additional MongoDB read per update
for REST clients.

---

## Caller Contract (after fix)

Callers do **not** need to set `IsNewHolon` — it is derived automatically by
`PrepareHolonForSaving` and should never be set by a caller.

| Scenario | What to set | Result |
|---|---|---|
| Creating a new holon | `Id = Guid.Empty` (or omit `Id`) | OASIS assigns a new GUID, MongoDB inserts |
| Updating an existing holon | `Id = <the existing GUID>` | MongoDB updates the existing document |

The MongoDB ObjectId (`_id`) and `ProviderUniqueStorageKey` are internal details —
callers never need to supply them.
