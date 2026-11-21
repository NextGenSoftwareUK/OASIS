# Holon version purge (opt-in)

Status: written 18 Nov 2026. Part 1 is the findings that drove the design (read from the code on `Development`, nothing run against a live database). Part 2 is the design and the API contract.

## Part 1. What deletion removes today

### Where copies of a holon live

| Store | What it holds | Removed by hard delete today? |
|---|---|---|
| MongoDBOASIS, collection `Holon` | ONE document per holon: the current version plus the embedded `OasisHolonRevisionArchive` (every earlier saved version, byte for byte, with its checksum). Seals (`VersionSeal`...) are inside the same document. | Yes, all versions at once, but only for a LIVE holon (see below). |
| MongoDBOASIS HyperDrive bookkeeping: `CosmicHolonOutbox`, `CosmicWritePreparation`, `CosmicWriteReceipt`, `CosmicReplicaHead`, `CosmicHolonLineageFence`, `CosmicDestinationBinding` | Replication outbox rows, write receipts and replica heads. Outbox rows and replica heads can carry a snapshot of the holon. Outbox rows are pruned by sequence after delivery (`MongoDBOASIS.HyperDriveSync.cs`). | No. Not touched by any delete. |
| Other storage providers that were auto-replicated to or are HyperDrive replicas (SQLLiteDBOASIS, Neo4jOASIS, IPFSOASIS and the rest) | A copy of the holon as of each replicated save. Whether older versions are kept depends on the provider. | Only partly, see below. IPFS content is addressed by hash and cannot be deleted by OASIS at all. |
| MongoDB Atlas backups / snapshots | Whole-cluster copies. | No, and OASIS cannot reach them. They expire on Atlas' own schedule. |
| Application logs and the access log | Ids and request lines, no holon content. | Out of scope; no content. |

### What a soft delete does (the API default)

`DeleteHolonAsync(..., softDelete: true)` (and V2: `HolonManager-Delete.V2.cs`) loads the holon, sets `DeletedDate` and `DeletedByAvatarId`, and saves it. That is an ordinary save: the Mongo writer archives the previous version into `OasisHolonRevisionArchive` and writes the tombstone as the new current version. So after a soft delete every earlier version is still stored and still readable by anyone who can read the document (the audit-trail endpoint returns them).

### What a hard delete does (`softDelete: false`)

- Mongo: `HolonRepository.DeleteObservedHolonAsync` does `FindOneAndDelete` on the exact observed document with majority + journal write concern. The whole document is removed, so the current version AND the embedded archive go together. One version is never removed alone.
- BUT the filter is `HolonId == id && DeletedDate == DateTime.MinValue` (`HolonRepository.Part2.cs`, `Part3.cs`). A holon that has already been soft deleted cannot be hard deleted: the call answers "Holon not found". That is exactly the PIPER case (PIPER erases by soft delete), so the existing hard delete cannot purge the history of a record that was already erased. The purge needs its own provider operation that does not require the holon to be live.
- Other providers (V1 path, `HolonManager-Delete.cs`): after the primary delete, the loop over auto-replicating providers calls `DeleteHolon(id)` and `break`s on the first success, so at most ONE replica provider is cleaned, and the loop reuses the `result` variable, so the reported outcome is the last replica's, not the primary's. Replicas beyond the first keep their copy and nothing says so.
- V2 hard delete routes `DeleteHolon` through HyperDrive with `AutoReplicate` unset; deletion of replica copies is not confirmed back to the caller.
- Authority: API endpoint `delete-holon` allows the creator or a Wizard on the V1 route; the owned conditional delete (`HolonManager-OwnedDelete.cs`) allows the creator or an avatar with a Delete grant and needs the expected provider revision.
- The Mongo HyperDrive bookkeeping rows above are never deleted by a delete.

### Consequences for the purge feature

1. It needs a new provider operation (`IHolonPurgeProvider`) that removes the Mongo document whether or not the holon is deleted, and says how many versions it contained.
2. Other providers that do not implement the operation must be reported as "not purged" in the result, never silently skipped.
3. Mongo bookkeeping rows are listed as "not purged (transient)" in the result; backups are reported with the configured retention window.
4. A purged holon is not found any more, so the seal verify endpoint needs a small purge record (id, owner, time, version count, no content) to answer "purged" instead of "not found".


## Part 2. The feature

Opt-in. With the default DNA nothing changes: no hook work, no worker queries, no new endpoint answers anything but "not enabled".

### Settings (OASIS_DNA.json, `OASIS.VersionPurge`; typed defaults in `OASISDNA.VersionPurge.cs`)

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | false | Master switch. False forbids every purge (scheduled and explicit) whatever an avatar or app chose. |
| `ApplyToAll` | false | When true, deleted holons are purged unless their owner or app chose "off". When false only owners/apps that chose "on" are. |
| `PurgeAfterDays` | 30 | Days after the deletion. 0 = at the next worker run. Fixed per holon when it is deleted, so changing it later does not shorten a promise already made. |
| `BatchSize` | 25 | Most holons one worker cycle purges. |
| `CycleSeconds` | 300 | Seconds between worker cycles and switch refreshes. |
| `BackupRetentionDays` | 0 | Days the database host keeps backups, quoted in purge results. 0 = unknown, and the result says backups are outside OASIS control. |

### How the layers combine (most specific wins; master can forbid)

1. A legal hold on the holon always blocks a purge (explicit or scheduled).
2. The node master switch: `Enabled` false means no purge at all.
3. The holon OWNER's choice (the holon's `CreatedByAvatarId`; OPORTAL Settings > Security > "Purge earlier versions of my deleted data": follow default / purge / never). Beats the app.
4. The calling app's choice (`AppUsageOverrides.VersionPurgeEnabled`, set by the app's owner/creator/developer through `POST api/subscription/app-owner/{appId}/settings`; null = follow default).
5. `ApplyToAll`.

**Days** resolve separately from the switch, in one place (`HolonPurgePolicy.EffectiveDays`, used when a deletion is scheduled): the holon owner's own days, else the app's days (`AppUsageOverrides.VersionPurgeDays`), else `OASIS.VersionPurge.PurgeAfterDays` (typed default 30, 30 in every DNA copy). Valid range 0 to 3650 (0 = at deletion); anything else is rejected with "Days must be a whole number from 0 to 3650" and nothing is saved. The days are fixed when the holon is deleted. The PIPER server's `PIPER_PURGE_VERSION_DAYS` (default 30) has the same meaning.

The policy only ever applies to holons that have been deleted. The scheduled worker re-checks the stored holon before it purges: a holon that is live again is never touched.

Caveat: `POST app-owner/{appId}/settings` replaces all of an app's overrides at once (existing behaviour), so a client that omits `versionPurgeEnabled` resets it to "follow default". OPORTAL sends every field.

### Mechanism

- Hook: when a holon is saved as deleted (V1 soft-delete paths and the V2 delete path in `HolonManager`), `HolonPurging.NotifyDeleted` tells the registered hook. The hook only puts an item in an in-memory queue (nothing touches a database in the delete path) and does nothing when the policy is off for that holon. App id comes from `X-OASIS-App-Id` (same flow as sealed history).
- Worker (`HolonPurgeHostedService`): every `CycleSeconds`: refresh the switches (two small collections: `AvatarVersionPurge`, the app accounts), write queued deletions to the small `HolonPurge` collection, make ONE query for due entries (index on status + purgeAfter, `limit = BatchSize`), then purge each after re-checking it. It never reads the `Holon` collection with a scan. With `Enabled` false the loop does nothing at all. A burst of deletions causes one cycle, not one each. A crash can lose only the few seconds of queue; the explicit endpoints are the backstop.
- Restore: no hook is needed. At due time a holon that is no longer deleted has its entry cancelled.
- Purge (all versions): `IHolonPurgeProvider` on the Mongo provider removes the holon's whole document (current version and archive, majority + journal write concern) whether or not it is deleted; `HolonManager.PurgeHolonStoresAsync` runs it on the primary provider and on every auto-replicating provider, reporting each. Providers without the interface are reported as `NotPurged`.
- Sealed chains purge as a whole (the document is removed). `GET api/data/holon-seal/verify/{id}` for a purged holon returns status `Purged` (to its owner) instead of 404 or "broken".
- Audit: one insert-only record per purge in `HolonPurgeAudit`: holon id, time, who (or the worker), trigger, app, versions removed, store names. No content.

### Endpoint contract (all need a Bearer token; same permission everywhere: the holon's creator, or an avatar with a Delete grant)

`DELETE api/data/purge-holon/{id}?allowLive=false`
- Purges every saved version of one holon. By default the holon must already be deleted (`NotDeleted` / HTTP 409 otherwise).
- Result (in `result.result`): `{ holonId, status, message, versionsRemoved, purgedAtUtc, stores:[{store,state,versionsRemoved,message}], unpurgedStores:[...], backupNote }`.
- `status` / HTTP: `Purged` 200, `PurgedPartially` 200 (read `unpurgedStores`), `AlreadyPurged` 200 (owner only), `NotFound` 404, `Forbidden` 403, `Disabled` 403, `OnHold` 409, `NotDeleted` 409, `Failed` 500.

`POST api/data/purge-holons`  body `{ "ids": ["guid", ...], "allowLive": false }`
- At most 100 ids (extras ignored, counted in `ignoredBeyondLimit`). HTTP 200 whenever the batch ran; read each `results[i].status`. Also `purged`, `purgedPartially`, `refused` counts.

`POST api/data/holon-purge-hold/{id}`  body `{ "reason": "parent access request" }` and `DELETE api/data/holon-purge-hold/{id}`
- Legal hold: blocks both the worker and both purge calls until released. Allowed before or after the holon is deleted and even while the master switch is off (a hold only protects data). Statuses `HoldSet` / `HoldReleased`.

`GET api/data/holon-purge/status/{id}` -> `{ holonId, status (none|pending|purged|cancelled), hold, holdReason, purgeAfterUtc, purgedAtUtc, versionsRemoved }` (owner only).

`GET|POST api/avatar/version-purge`  body `{ "enabled": true|false|null, "days": 0..3650|null }` (null = follow default; both null clears the choice) -> `{ enabled, days, nodeAvailable, effectiveDays, effectiveDaysSource, globalDays }`. The app-owner settings call takes `versionPurgeEnabled` and `versionPurgeDays` the same way.

### What it can and cannot promise (tell the client this)

- Removed: every saved version in MongoDB, and in each auto-replicating provider that implements the purge. The result lists any that did not.
- Not removed by OASIS: database backups and snapshots (they expire on the host's schedule: say the window in `BackupRetentionDays`), IPFS content (addressed by hash), HyperDrive V2 destination copies and replication bookkeeping (outbox rows are pruned after delivery; the result says so), and anything an app copied elsewhere.
- Concurrency: a hold set in the same instant a purge starts may lose the race (no lock across the two calls).
- MongoDBOASIS removes the whole version chain directly. Every other storage provider (the base class `OASISStorageProviderBase`) uses its own hard delete and then VERIFIES that the latest and first versions no longer load; it is reported `Purged` only if that check passes, otherwise `NotPurged` with the reason. Providers that keep earlier versions outside what their delete removes will therefore show up as not purged rather than being silently trusted. Providers that do not derive from the base class are reported `NotPurged`.
- The verified path (Mongo primary, explicit and scheduled purge) is what PIPER relies on; the generic path is best effort.
