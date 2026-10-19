# Cosmos DB retained record history

## Implementation state

Scoped feature implemented and verified, 2026-10-18. `CosmosEmbeddedHistory` implements the format and delegates numbering/linkage to existing `StoredRecordVersions`. All ordinary SDK record writers share the canonical conditional writer; repositories and public reads/saves/conditional soft deletion are integrated. `IsVersionControlEnabled` begins retaining history (default false), while an existing archive always remains retained even after disablement. Native36/36 and unit152/152 pass. Do not advertise complete provider certification: remaining native race/size/restart, interface options and hosted auth/cost gates are listed below. Direct property enablement is proven; OASISDNA/hosted configuration binding is not.

The existing containers partition by `/id`. Different document IDs therefore cannot be assumed to share a transactional batch. This implementation retains the current canonical DTO and its archives in one document under the existing logical/native ID, allowing one native atomic create or conditional replacement. There is no second container, ledger, serializer or journal.

## Format and invariants

The current canonical OASIS fields remain at the root, beside native `id`. Reserved infrastructure field `_oasis_record_history` contains schema `1` and `records`, ordered by historical version. Archives contain canonical domain DTOs, including private fields already present in that record, but no Cosmos native system properties or recursive history envelopes.

- Version zero means latest; positive versions select exact stored versions. Missing versions do not return latest.
- An earlier unversioned record numbered zero can be retained internally when history is first enabled; zero still selects latest in the public API, not that unversioned prehistory. No silent renumbering/migration is performed. The native activation tests enable history before the first save.
- Shared version stamping determines Version, VersionId and PreviousVersionId; exhausted counters reject rather than roll over.
- Current/archive IDs must agree. Archived versions must be unique and precede the current version. Malformed/unknown schemas and conflicting records fail, including when the caller requests latest.
- Caller objects and the prior JSON document remain unchanged during staging. Caller state is not published by the SDK primitive.
- No history is silently trimmed. Native document capacity remains a hard limit. Item-size rejection must leave the previous committed version intact; native size-rejection evidence remains required.
- Version filtering applies to selected roots only. Descendants retain independent versions; a numbered root is not a point-in-time graph snapshot.

## SDK ownership

The writer captures a detached canonical proposed record before its first awaited read. It reads the current item using the explicit matching `/id` partition and requires a native ETag for replacement. Staging builds a complete current-plus-history document.

For an absent item it calls `CreateItemStreamAsync`, not blind upsert. For an existing item it calls `ReplaceItemStreamAsync` with `IfMatchEtag`. Concurrent creation/update conflicts are returned, not retried. There is exactly one mutation attempt. Transport or response-body uncertainty preserves the existing reconciliation exception; it is not permission to replay.

Success requires whole-document acknowledgement equivalence: root ID, current domain fields and the complete history envelope must match. Only native `_etag`, `_rid`, `_self`, `_attachments`, `_ts` fields are excluded. A valid current record paired with a missing/altered archive is not accepted as success.

## Required integration work

1. Implemented and scoped-native verified: repositories use detached canonical DTOs; comparisons accept only SDK-owned version changes and compare all application fields. IDs/keys/version linkage publish after acknowledgement, including selected graph sources.
2. Implemented and scoped-native verified: ordinary writers are envelope-aware; existing archives force retention after configuration disablement. All existing-item writes now pre-read and conditionally replace with ETag, including nonhistory writes. This adds RU cost even before history is enabled. Three controlled modes and all three native public datatype saves cover preservation. Known native409/412 conflicts classify as terminal lifecycle conflicts, not availability failover; a read outage before mutation dispatch can safely fail over. Post-ACK uncertainty stays fenced.
3. Historical reads now use the shared exact-version and graph/search helpers. Continue verifying every datatype/selector/options combination and error classification. Authentication/bootstrap selectors must remain latest-only; old credentials are not valid authentication merely because retained history contains them. Repository positive-version field selectors scan/decode historical records before matching (O(n)); the low-level SDK field query separately remains a current-record Version filter. New IRepository ID/list overloads require source/binary consumers implementing this infrastructure contract to rebuild; existing signatures remain intact.
4. Avatar soft-delete history is native-proven; profile/detail pair failures remain explicitly partial, not cross-container atomic. Hard deletion removes the item including its embedded archive. Broader hard-delete/retention policy acceptance and full pair/concurrency matrices remain gates.
5. Actual native competing-update races are now verified with history both off/on: before-read mismatch dispatches no PUT; after-read competing commit causes actual412 and preserves winner version2/original archive. Remaining: concurrent creation, lost/altered ACK after a real versioned mutation, native item-size rejection, restart retention and broader provider options/hosted behavior. A controlled SDK mock is not native failure injection.

## Cost and privacy implications

Each versioned update adds a native pre-read plus create/replace. Authoritative absence also verifies the container. Cosmos request-unit cost and document size grow as archives grow; reads return the item including retained archives. Do not assume constant RU cost or treat emulator success as production cost evidence. Success RU attribution, hosted quota/billing and capacity policy remain release gates.

History retains previous private data and credentials under the record's storage access boundary. Rotation changes the latest value but is not erasure of older copies. Existing encryption/authorization and retention/deletion policy must apply to archives too. Metadata query exemptions are not publication permission. No new plaintext/public projection or hosted history endpoint is introduced here.

## Evidence

Nine codec cases and seven controlled SDK cases cover all three record kinds, metadata/detail XP, exact historical selection, caller isolation, schema/identity/version corruption, counter exhaustion, create/replace, ETag guards, conflicts, missing body and altered archive. Four repository/public read cases additionally cover all three record kinds and historical-root/current-child projection; three ordinary SDK modes verify archive retention when the option is off. Disappeared conditional targets and409/412 public write conflicts have controlled terminal-classification checks against compiled V2. Whole current unit suite152/152 and native36/36pass86s. Public historical holon routes and ordinary Avatar/Detail/Holon save/disablement plus Avatar soft deletion are native-proven. Exact hashes and remaining gates: `HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md`. Use the existing reusable runner and fixed reports; no new installation or database copy is needed.

## Native acknowledgement verification — 2026-10-18

Retained official Cosmos SDK/emulator suite: **41/41 passed, zero skipped, 88 seconds**. Three new cases intercept the response only after a real successful replacement: missing body, altered returned history, or response-delivery IOException. A separate healthy client verifies committed version2 and retained version1; public save must preserve reconciliation-required failure, dispatch exactly one replacement, stop child writes and leave caller version IDs unchanged. Exact inspected fixtures are removed without repeating the mutation. Report: `TestResults/CosmosDBCurrent/cosmos-native-current.trx`, SHA256 `D23A2F334D8C4813CD9C70E4188437784013AC063CD47D8B47A49EA0617CDB67`.

This is controlled response-handler failure evidence, not proof of every physical network failure/internal SDK retry path. Native capacity rejection, restart retention and remaining complete-provider/hosted/cost gates remain open. Same retained runtime/container; independently confirmed stopped after the run.

## OASISDNA configuration

Set `OASIS.StorageProviders.AzureCosmosDBOASIS.IsVersionControlEnabled` to `true` to request retained versions when BootLoader constructs the provider. Omitted or explicit `false` preserves the existing default; existing stored archives remain retained regardless. The setting adds no separate history store. Review same-item size growth, RU costs and private historical data retention above before enabling. No Railway environment value was changed by this implementation.

DNA and BootLoader cached builds passed. DNA unit suite15/15 passed (zero skipped), including omitted/false/true JSON settings. Missing test assets were regenerated from the existing local NuGet cache only, without downloads. This verifies schema parsing and compilation of the registration assignment, not a hosted BootLoader activation or Railway JSON update; those remain explicit verification gates.
