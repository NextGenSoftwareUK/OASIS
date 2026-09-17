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

## Audit results (16 September 2026)

This table records the completed source, build, client-contract, and deployment-contract
checks for this change. It is the release evidence for the identity matrix above; it does
not claim that a particular environment's database has been exercised unless that check is
listed as an environment test below.

| Check | Scope | Result |
|---|---|---|
| Provider identity decision | `MongoDBOASIS.SaveHolon` and `SaveHolonAsync` | Passed source review: the public `HolonId` lookup is the sole generic insert-versus-replace decision. |
| Duplicate identity protection | Mongo repository activation | Passed source review: `ux_holon_public_identity` is a unique index on the public `HolonId`; an existing duplicate is a visible activation failure. |
| Preallocated server GUID | STAR/graph creation path | Passed source review: a new non-empty public GUID inserts once and remains available to child/metadata references. |
| GUID-only update | REST, C#, Unity, native, and JavaScript | Passed source review: the persisted entity is resolved by public GUID and its private Mongo `_id` is restored before replacement. |
| Creation audit preservation | Existing full-replacement update | Passed source review: persisted creation audit values are restored before replacement. |
| Missing explicit update target | Repository `Update` / `UpdateAsync` | Passed source review: an unmatched update returns an `OASISResult` error. |
| WEB5 create/update routing | All inspected direct resource endpoints | Passed: `Scripts/validate_webapi_holon_identity_contract.ps1` inspected 21 resource `POST` routes and 21 `PUT /{id}` routes; no violations. |
| WEB4 and WEB6-WEB10 generic persistence | Shared `HolonManager` / `Data.SaveHolonAsync` callers | Passed source audit: these callers flow into the provider invariant instead of maintaining divergent lifecycle rules. |
| JavaScript SDK caller-assigned GUID create | WEB5 NPM package | Passed: Node smoke tests confirm the GUID remains in the create body. |
| JavaScript SDK update | WEB5 NPM package | Passed: Node smoke tests confirm `id` becomes `PUT /api/holons/{id}` and is not a required Mongo key in the body. |
| Mongo provider compilation | `NextGenSoftware.OASIS.API.Providers.MongoOASIS.csproj` | Passed with `dotnet build --no-restore -v:q -m:1` (zero errors). |
| STAR WebAPI compilation | `NextGenSoftware.OASIS.STAR.WebAPI.csproj` | Passed with `dotnet build --no-restore -v:q -m:1` (zero errors). |
| Railway dependency integrity | WEB4-WEB10 deployment inputs | Passed: `python3 Scripts/validate_railway_dependency_manifest.py --require-gitlinks` confirmed manifest pins, parent gitlinks, and all seven Dockerfiles agree. |
| Development health endpoints | Hosted WEB4 and WEB5 development services | Passed: both `/api/health` endpoints returned HTTP 200 at verification time. |

### Remaining environment acceptance test

The only check that must be run against each freshly deployed Mongo environment is the
five-step real-database verification in **Verification requirements**. It verifies runtime
credentials, database migration state, and the actual duplicate-free index on that
environment; source/build checks cannot substitute for it. Record the deployment URL, UTC
time, caller type, public GUID, and result beside the release or incident ticket when it is
performed.

## Storage-provider coverage

The public-GUID contract is a provider contract, not a Mongo-only convention. A provider
is conformant only when its `SaveHolon` and `SaveHolonAsync` operations use `IHolon.Id`
as their persistence identity, do not use `CreatedDate`, `IsNewHolon`, or a provider key to
choose insert versus update, and preserve immutable creation audit data on replacement.

| Provider | Static audit status | Current result |
|---|---|---|
| MongoDBOASIS | Complete | Conformant: public `HolonId` lookup plus unique public-identity index. |
| SQLLiteDBOASIS | Complete | Conformant after the public-ID repository fix: the SQLite primary key is the public GUID, existing rows are updated, and their creation date is preserved. |
| Neo4jOASIS | Complete | Conformant for identity: Cypher `MERGE` keys on public `Id`; `CreatedDate` is set only by `ON CREATE`. |
| Neo4jOASIS2 | Not an active implementation | Its holon save implementation is commented out, so it cannot be represented as a tested persistence provider. |
| Other storage, search, cache, vector, and blockchain providers | Pending individual audit | Do not infer conformance from the Mongo fix. Each has its own persistence contract and must receive the same source review and, where active, a provider-backed create/update/read test before it is certified. |

The SQLite and Neo4j source builds passed after their changes. Runtime certification still
requires each provider's configured integration environment; a source-only audit cannot
prove a remote service's schema, credentials, uniqueness constraints, or transaction
behaviour.
