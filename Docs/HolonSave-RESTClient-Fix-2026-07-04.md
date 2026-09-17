# Holon Persistence Identity Contract

## Purpose

All clients — C#, JavaScript/NPM, REST, Unity, and native — address a holon with its
public OASIS GUID (`IHolon.Id`). MongoDB's `_id` / `ProviderUniqueStorageKey` is a
private storage detail. Clients must never need to read, retain, or send it to update a
holon.

This document replaces the earlier lifecycle guidance which incorrectly made
`CreatedDate` or `IsNewHolon` decide whether MongoDB inserted or replaced a document.

## The invariant

MongoDB persists a holon by its public OASIS GUID:

1. Convert the holon to its Mongo entity, whose `HolonId` is the public GUID.
2. Look up the persisted document by `HolonId`.
3. If it exists, replace that document. The repository obtains its private `_id` before
   `ReplaceOne`, so a GUID-only REST/JavaScript payload cannot change MongoDB's immutable
   `_id`.
4. If it does not exist, insert it once. This permits trusted server-side creation flows
   such as STAR, which allocate the public GUID before their first save so that metadata
   can refer to it.

`CreatedDate` remains audit data. On a matched GUID, Mongo preserves the persisted creation audit fields before the full replacement. On a first insert with a preallocated GUID, Mongo establishes the creation audit fields from the server-supplied metadata. `IsNewHolon` remains an in-memory lifecycle hint used by manager audit preparation. Neither is a Mongo persistence key and neither can decide insert versus update for a stateless client.

## Implementation

| Layer | Responsibility |
|---|---|
| `HolonManager.PrepareHolonForSaving` | Assigns a GUID for an object that has none and prepares audit/version fields. It does not use `CreatedDate` as a persistence signal. |
| `MongoDBOASIS.SaveHolon` / `SaveHolonAsync` | Resolves the operation by looking up the Mongo document with the public `HolonId`. This is the only generic Mongo create-or-update decision. |
| `HolonRepository.Update` / `UpdateAsync` | Resolves the private Mongo `_id` from `HolonId` before replacing an existing document, and returns an error when an explicit repository update has no match. |

The previous `ProviderUniqueStorageKey.ContainsKey(MongoDBOASIS)` path is obsolete and
is retained only in source history. It was invalid for stateless callers because that key
is the Mongo ObjectId. The `CreatedDate == DateTime.MinValue` branch is likewise obsolete:
deserialised JavaScript objects routinely omit audit fields.

## Client contract

| Operation | Client input | Result |
|---|---|---|
| Create | Omit `Id`, or use a GUID allocated by a trusted server-side creator | OASIS/Mongo persists the first document for that public GUID. |
| Update | Supply the existing OASIS GUID in `Id` | The matching document is replaced, even when no Mongo ObjectId, `ProviderUniqueStorageKey`, `CreatedDate`, or `IsNewHolon` is present. |

For public API design, create endpoints should call the relevant manager's `CreateAsync`
and update endpoints should call `UpdateAsync`. The storage layer still protects the
cross-client identity invariant for generic `SaveHolon` callers.

## Verification requirements

Before releasing a change to generic holon persistence, verify all of the following
against a real Mongo deployment:

1. Create a holon through the API and read it back by its public GUID.
2. Send a second update payload containing only the public GUID and changed data; read it
   back and confirm the same GUID now contains the changed data.
3. Confirm the collection contains one document for that GUID.
4. Repeat step 2 through the JavaScript/NPM client.
5. Confirm an explicit `UpdateAsync` for a nonexistent GUID returns an `OASISResult`
   error rather than reporting a successful update.

The Our World seed script exercises steps 1–3 for a quest and its objective progress;
the JavaScript package contract must be exercised as part of its own release tests.

## Complete supported identity matrix

The following cases are supported by one persistence rule: a public GUID identifies one
Mongo holon. The value is never inferred from audit timestamps, client process state, or
Mongo's private `_id`.

| Caller and intent | Public `Id` | Mongo outcome | Audit outcome |
|---|---|---|---|
| C#, Unity, native, REST, or JavaScript creates a normal object | Empty | `HolonManager` allocates a GUID; Mongo inserts one document | Manager writes creation audit fields |
| STAR/graph/server creator allocates a GUID before saving so a child, metadata, or event can reference it | New non-empty GUID | Mongo finds no matching `HolonId` and inserts one document | Mongo establishes creation audit fields and clears modification audit fields |
| Any client updates an existing object with only its public GUID plus the replacement entity | Existing non-empty GUID | Mongo resolves the matching document by `HolonId` and replaces it | Persisted creation audit values are retained; manager writes modification audit values |
| JavaScript SDK `updateHolon({ id, ... })` | Existing non-empty GUID in URL | SDK consumes `id` into `PUT /api/holons/{id}`; the controller stamps that route GUID into the body before updating | Same as any REST update; no Mongo key is required in the body |
| Client retries a create with the same preallocated GUID after the first request completed | Existing non-empty GUID | The generic save route resolves the existing document, preventing a second document | Existing creation audit is retained |
| Two requests race to create the same preallocated GUID | Same new non-empty GUID | Mongo's unique `ux_holon_public_identity` index permits exactly one insert; the conflicting request returns a real `OASISResult` error | No duplicate document or silent fallback |
| Explicit repository `UpdateAsync` for a nonexistent GUID | Non-empty GUID with no document | The repository returns an error; it never reports a successful replacement of zero documents | No audit mutation |

A normal `PUT` is a full replacement API: clients must send the mutable entity fields they
intend to retain. Immutable creation audit fields are always restored from storage. A
partial-edit endpoint must load the persisted entity, apply the requested changes, and then
save that entity; it must not rely on absent client fields.

## Deployment and regression checks

`Scripts/validate_webapi_holon_identity_contract.ps1` enforces the API half of the
contract across the deployed WEB4-WEB10 source set. It verifies that bare WEB5 resource
`POST` endpoints do not call `UpdateAsync`, and that direct-resource `PUT /{id}` handlers
make the route GUID authoritative. WEB4 and WEB6-WEB10 persist through the shared
`HolonManager` / `Data.SaveHolonAsync` path and therefore use the provider invariant
rather than duplicating CRUD identity code.

The WEB5 JavaScript package has a Node contract test proving both supported JavaScript
shapes: a caller-assigned GUID remains in the create body, while `updateHolon` consumes it
as the `PUT /api/holons/{id}` route token and does not require a provider key.

Mongo provider activation creates the unique public identity index. If a database already
contains duplicate `HolonId` values from an older build, activation fails visibly while
building that index. Resolve those duplicates deliberately before deployment; do not remove
the index or introduce a second save path.
