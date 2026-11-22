# Sealed holon history (opt-in tamper evidence)

Off by default everywhere. With it off, the save path behaves exactly as before.

## What it does

When sealing is on for a save, the stored version gets a seal: an HMAC-SHA256 of the **previous version's seal** plus the version's own content, made with a server-held secret that is **not stored in the database**. Each version carries three optional fields: `VersionSeal`, `PreviousVersionSeal`, `VersionSealKeyId`. Old versions have none and read as "unsealed". Nothing is ever backfilled (that would stamp possibly altered history as genuine).

Seals are made inside the storage write (MongoDB: the same compare-and-swap that writes the version), from the exact stored document, so two simultaneous saves cannot fork the chain. A sealed save must be the exact next version of what is stored; a stale or repeated version is rejected with a conflict and must be reloaded and retried. Soft delete is a version (tombstone) and is sealed like any other.

## Who turns it on (all default off)

A save is sealed when ALL of these hold:

1. **Node:** `OASIS.HolonSeal.Enabled` is `true` in the DNA **and** `OASIS.HolonSeal.Key` is set.
2. **Either** the **owner avatar** (the holon's `CreatedByAvatarId`) has switched it on (OPORTAL > Settings > Security > Sealed history, `GET/POST api/avatar/holon-seal`) **or** the **calling app** has (`X-OASIS-App-Id`; set by the app's owner/creator/developer in the app-owner settings, `SealedHistoryEnabled` on `POST api/subscription/app-owner/{appId}/settings`, same permission rules as the other app settings).

Either side can add protection; neither can lower the other. Turning a switch off is honoured immediately and never rewrites anything: versions saved while it was on keep their seals. If sealing is later turned on again, the first sealed version starts a new segment and verify reports the gap as "partial".

## Checking

`GET api/data/holon-seal/verify/{id}` (same read rule as the audit trail) returns a status:

| Status | Meaning |
|---|---|
| Verified | every version from the first sealed one to the latest is sealed and the chain checks out |
| Broken | a sealed version does not match its content, a link is cut, a version in the middle was removed, inserted or forged; `brokenAtVersion` says where |
| Partial | sealed versions check out but some later versions were saved while sealing was off |
| Unsealed | no version has a seal |
| Unavailable | feature off, no key, key id not configured, or the storage provider does not carry seals |

The result also has per-version status and `latestSeal`. `GET/POST api/data/holon-audit-trail/{id}` now includes the same result as `seal`. OPORTAL shows it in the holon version view.

## Turning it on (operator)

1. In the DNA (on Railway, the `OASIS_DNA_JSON` variable), under `OASIS.HolonSeal`, set `Enabled` to `true` and `Key` to a long random secret (32+ characters). Never commit a real key; committed templates hold an empty `Key`.
2. Avatars switch it on for their own data in OPORTAL; app owners switch it on for their app.
3. To rotate: set a new `Key` and `KeyId` (for example `k2`) and move the old value into `RetiredKeys` (`{"k1": "old secret"}`). Old seals keep verifying, new ones use the new key.

## DNA keys (`OASIS.HolonSeal`)

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | `false` | node master switch; false = nothing is sealed whatever avatars/apps chose |
| `Key` | `""` | the secret; empty = sealing unavailable and verify says Unavailable |
| `KeyId` | `"k1"` | id of the current key, stored on each seal |
| `RetiredKeys` | `{}` | earlier keys by id, used only to verify old seals |
| `RefreshIntervalSeconds` | `30` | how often each node refreshes the avatar/app switches from the database |

## What it protects

Someone with database access but **without the secret** who edits a saved version's content or metadata, inserts a forged version, removes or reorders a version in the middle, or strips a seal from the middle of a chain. The version id and holon id are inside the hash, so a seal cannot be moved to another version or holon.

## What it does not protect

- **Removal of only the newest version(s):** the remaining chain is still valid. Keep an outside copy of `latestSeal` (for example in a log or a ledger) to notice this.
- **Someone with both database access and the secret:** they can recompute every seal.
- **Anything before sealing was switched on,** and versions saved while it was off (reported as unsealed/partial).
- **Hard deletion** of a whole holon (creator or Delete grant) removes the chain with it.
- **Providers other than MongoDB:** only the MongoDB provider writes and verifies seals so far; other providers report Unavailable. Replicas are not re-sealed; a copy made by replication keeps the seals it was given.
- The check proves the stored history is consistent with the key. It does not prove the data was true when saved.

## Code map

Core: `Holons/HolonSealing.cs`, `Managers/HolonManager/HolonManager-Seal.cs`. MongoDB provider: `MongoHolonSeal.cs`, `MongoNativeRecordWriter.cs`. ONODE: `Services/Seal`, `Controllers/AvatarSealController.cs`, `Controllers/DataController.Seal.cs`. Tests: `MongoHolonSealTests` (chain, tamper, switch matrix) and `HolonSealPolicyTests`.
