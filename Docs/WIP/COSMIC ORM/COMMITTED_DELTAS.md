# Committed per-save deltas

20 October 2026. First native transaction owner: SQLite conditional saves/deletes. Generic immutable capture/validation lives in Core `HolonCommittedDelta`; this is not the completed Stage7 provider/authorization rollout.

## Commit ownership

SQLite acquires its native writer reservation, verifies all expected provider revisions, and detaches prior authoritative records before changing models/history. After assigning actual committed logical/provider revisions, it captures changes from those prior records and the acknowledged flat records. The delta is part of the same checksummed primary receipt inserted in the same transaction as records, history and any enrolled outbox obligations. Before COMMIT, the receipt and delta are decoded and checked against the original operation, actor, authority, selected identities and acknowledged revisions.

Failed writes/conflicts return no committed delta. Replaying the original operation returns its original summary, even after newer revisions, process/context restart or a change to the history setting. Live caller edits cannot change the captured JSON strings. A native gated encryption test edits the caller while the save is pending and verifies those edits remain dirty locally, while storage and the delta retain the staged write.

New primary envelopes are format3 and carry the version1 delta as a JSON string, avoiding accidental date/number coercion by an enclosing JSON reader. The existing Shared graph codec remains format3 inside the envelope; replica receipts retain their separate existing protocol. Valid prior primary format2 receipts replay their original acknowledgement with `COSMIC.CommittedDeltaUnavailable=ReceiptPredatesDelta`; no new delta is inferred from current state. Primary format1 still requires [explicit receipt reconciliation](SQLITE_RECEIPT_INTEGRITY.md).

## Reading a Save result

```csharp
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

row.Name = "Updated name";
var saved = await manager.SaveHolonAsync(
    row, actorId, new CosmicWriteOptions(Guid.NewGuid()),
    saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
if (saved.IsError) throw new InvalidOperationException(saved.Message);
if (saved.MetaData != null && saved.MetaData.TryGetValue(HolonCommittedDelta.ResultMetadataKey, out var summary))
    Console.WriteLine(summary);
```

The manager propagates the provider's descriptor summary. A no-op manager save issues no transaction/receipt/delta; absence of this key is not an instruction to query newer storage or invent an event. Ordinary unconditional CRUD, other native providers and replica transfer capture are not yet enrolled.

Each record includes entity ID, Created/Updated/SoftDeleted/HardDeleted/Restored classification, previous/current existence, prior/committed physical revisions and logical version IDs/numbers. Paths are escaped JSON pointers into canonical stored record values, including existing typed collection envelopes such as `/MetaData/array/$values`. Arrays are whole-value replacements, not index edit scripts. Missing and explicit JSON null remain distinct; exact numeric comparison reuses the shared value owner. The capture model can classify restoration, but broader native restore acceptance is still open.

## Values, encryption and membership

Every new SQLite conditional commit captures a redacted descriptor set. When the existing native `IsVersionControlEnabled` policy is true at the transaction boundary, the stored delta additionally retains prior/current JSON values, matching the existing archival policy. When false, those values and historical membership identities/roles are omitted. Public Save summaries always omit values and membership identity/role details, regardless of that flag. `valuesRetained` describes the stored policy, not permission to fetch values. The summary's `view` is `Summary`; the stored-data decoder intentionally rejects that as a full stored packet.

Stored values are exact JSON strings (`previousJson`/`currentJson`), not values reparsed through double. They reflect authoritative storage representation: already encrypted metadata remains ciphertext; this feature does not decrypt historical private partitions or expose plaintext through result metadata. Existing clear/reserved metadata storage/query behavior is unchanged. An authorized history-value read/decryption API, old-key access policy, configurable retention and operator tooling remain required before the full requested audit feature can be declared complete. A checksum is not a keyed signature or an authorization grant.

Membership compares persisted role manifests only when both sides are fully observed, or a missing entity side is authoritative. Unknown membership is reported as `relationshipsObserved=false`, never inferred from an empty loaded Children collection. Changes distinguish link/unlink/move from physical entity deletion; moved positions include shifts induced by another link's removal. Stored membership details follow the same history-value policy. Summaries report only the observed flag and change count. An unselected child remains stored and does not acquire a deletion delta.

## Codec correction discovered by native coverage

Stored typed integer arrays could fail during canonical projection: JSON-element numbers became Raw tokens, which typed array readers cannot consume. `StoredRecordJson.DeserializeToken` now normalizes only exactly representable numeric tokens through the existing numeric admission owner before materialization; `Project` uses that same decoder. Capture/serialized wire bytes are unchanged, unrepresentable untyped numbers remain raw, and numeric strings stay strings. The dedicated Core case fails against the original decoder. This fixes the shared boundary rather than a SQLite-only array path.

## Verification

Original native delta gate2/2 fails because result metadata has no committed delta. The next gate exposes the typed-array codec bug; its dedicated Core red report is `core-delta-array-projection-red.trx`. A subsequent assertion is corrected to the actual canonical typed-array path rather than promising a new alias schema. Final focused native matrix11/11 passes: history on/off, exact numbers, null/missing, array change, original replay after later commits/restart/policy change, conflict/receipt-insert rollback, soft/hard sync/async delete, manager propagation, encryption/no-op, format2 absence, revision rebinding and in-flight edits.

Exact committed Core `b3c74d44370a243894a46230c46e491bc7f9e771` builds both targets; full Core1,546/1,546, Shared895/895 and SQLite native1,089/1,089 pass, zero skips. `TestResults/SQLiteCurrent/committed-delta-artifacts.json` verifies identical Core/Shared assemblies across those consumers: Core `C6EBDDC3CD9094212B77B56F3A3862FC9481FA72523B66E56135D1B58A0771A8`, Shared `65F42190B6347FFEE3A2FCA02611E96D3D8C8BA728D03F99E1147F71C67F8BCE`, SQLite `9EEE976620EFB1D8598569B78CDA09E680F0F9127F5ABCADE3E6730181DA8B75`. The fixed SQLite fixture is absent; free disk space is retained in the report.

Additional affected-codec regressions pass MongoDB84/84 selected native and Neo4j13/13 conditional cases, zero skips. Both use the same committed Core hash above and Shared `CDAF436EC115919EC1F871F380EEDAB894F2C5A78AFBBCE880D522B1405EACB3`, recorded in `TestResults/CosmicMongoCurrent/committed-delta-cross-provider-artifacts.json`. This is the later parent6ee02af4b producer epoch, distinct from the first three-suite cohort; no matching-five-suite claim is made. Successful runs remove their fixtures and stop owned MongoDB/Neo4j; the Mongo log is bounded to a65KB tail. These verify shared codec compatibility, not delta capture on those providers. The exact Core commit is remotely verified and paired with the parent gitlink/dependency manifest; no Development/master deployment is included.

Reports under `TestResults/SQLiteCurrent`: `sqlite-committed-delta-red.trx`, `sqlite-committed-delta-focused.trx`, `sqlite-committed-delta-codec-focused.trx`, `sqlite-committed-delta-final-focused.trx`, `sqlite-committed-delta-matrix.trx`, `sqlite-committed-delta-full.trx`. Core reports live under `TestResults/CosmicMongoCurrent`, including `core-committed-delta-committed-full.trx`; Shared has `TestResults/SharedCurrent/shared-committed-delta-full.trx`. Complete atomic retention, authenticated value access, generic provider adoption, restore/fault acceptance and the remaining roadmap stay open.
