# Relationship delta comparison

20 October 2026. Canonical membership comparison is implemented; committed per-save deltas remain open.

`HolonChildRoleDelta.Compare` compares two fully observed manifests belonging to the same parent. It returns immutable entries ordered by child ID, recording previous/current role and index. Membership added or removed is reported as linked/unlinked. A retained child whose role or index changes is reported as moved. Index shifts caused by another member's removal/addition are included as changed positions; this is not a minimal edit script.

Unlinking never implies deletion of the child entity. Different parent IDs require explicit cross-parent evidence and are rejected by this local comparison. Unobserved/partially hydrated collections cannot be supplied as empty relationship state. The comparison does not invoke providers, mutate tracking or produce a save receipt.

Both Core targets build. Full Core1,135/1,135 passes (`TestResults/CosmicMongoCurrent/role-delta-core-current.trx`), including three new cases covering link/unlink/role/order differences, detached no-op membership and cross-parent rejection.

Stage7 remains open for committed before/after capture, operation/revision/actor/authority binding, field/metadata/nested changes, deletion/restoration, authorization/redaction/encryption, atomic persistence, uncertain acknowledgements and original-delta replay. The manifest itself still needs provider commit/transport/hydration integration. Do not present this comparator as the requested completed Save delta API.

## Canonical value comparison follow-on

`HolonValueDelta.Compare` now compares supplied canonical persistent objects. Entries distinguish missing values from explicit JSON null, use escaped JSON-pointer paths, and detach previous/current values as JSON strings. Nested object properties are compared in ordinal name order; arrays are ordered whole-value replacements rather than index-level edit scripts. Empty object/type changes are retained, and equivalent property ordering produces no delta.

Both Core targets build; full Core1,139/1,139 passes (`value-delta-core-current.trx`) with four new value-comparison cases. This implements value comparison only. Caller-supplied comparisons are neither authorized audit evidence nor committed-save receipts; before/after selection, redaction, transaction ownership and replay remain open. No provider or store is read by this helper.

## Local pending-value preview

`HolonChangeTracking.PreviewValueChanges(holon)` now compares current canonical values against the private accepted tracking baseline and returns `OASISResult<IReadOnlyList<HolonValueDelta>>`. It does not read providers, accept changes or claim a save occurred. Untracked values return `HOLON_TRACKING_BASELINE_MISSING`; failed canonical capture returns `HOLON_VALUE_PREVIEW_FAILED`. Graph membership is not included in this value-only API.

```csharp
// After a successful tracked load/save:
holon.Name = "Updated name";
var preview = HolonChangeTracking.PreviewValueChanges(holon);
if (!preview.IsError)
    foreach (var change in preview.Result)
        Console.WriteLine($"{change.Path}: {change.Change}");
```

Full Core1,142/1,142 passes (`value-preview-core-current.trx`), both targets build. Three new cases verify immunity to compatibility `Original` mutation, detached preview values, remaining pending edits after acknowledgement, clean no-op and missing-baseline errors. Values in the preview belong to the local caller; this is not a remotely authorized committed audit API. Stage7's committed evidence/persistence/replay gates remain open.

Focused native integration:2/2 SQLite cases pass (`value-preview-native-current.trx`) for sync/async V2 loads and saves. An independently loaded entity starts with an empty preview, metadata edits produce the accepted before/current after values, and successful native save advances the baseline to an empty preview while prior preview entries remain detached. Independent provider read confirms storage. Producer/consumer hashes matched before startup: Core `C3D4C11AE69846BBAB73181B4644509F1002DC5DE634597FA2907403B342CE24`, Shared `92758099BC5143520707809505DFB88115A8A5CE268ADAA09321DB7A4CAD22D4`. Owned disposable database is absent. This was a focused two-case run, not a new full-suite713 result; earlier full711 evidence remains separately scoped.


## Exact local numeric snapshots — 20 October 2026

Decimal review exposed a correctness gap in the delta prerequisite: default JObject JSON parsing converts fractional object values to double, so nearby high-precision decimals could appear unchanged. Local tracking now captures canonical tokens directly, and save staging, local clones, rollback and concrete projection use those tokens without a text-reader round trip. `StoredRecordJson.CaptureRuntimeToken` is the shared trusted-runtime capture owner. `CaptureInputToken<T>` performs one validated input capture with the declared root contract and existing delegate/cycle rejection, preserving exact scalar values for downstream graph staging.

Mutable byte-array token values need explicit copying: Newtonsoft token DeepClone can retain byte[] references. Capture detaches those values, and private-baseline restoration clones tokens and byte scalars before conversion. This preserves both high-precision decimals and full double range without choosing a lossy global numeric parser policy.

Nine Core cases cover JSON value kinds, tiny custom/nested decimal edits and exact previews, rollback, raw JsonElement numeric values, projection, byte-array ownership and validated input rules. Original five decimal failures and two byte ownership failures are preserved. The first broader run exposed remaining save-staging reparsing; that layer now consumes the same token representation. Final Core1,370/1,370 passes, zero skips, and both targets build with zero errors. Native precision acceptance is recorded separately; local tests alone do not prove external JSON/ciphertext readers or persisted delta correctness.

The provider session owns companion shared graph-planning/publication guard changes using these APIs. Stage7 remains open for atomic committed delta persistence, operation/revision/actor binding, authorization/redaction/encryption, history, deletion/restoration and idempotent original-delta replay. This fixes prerequisite ownership and precision, not the complete committed Save delta API.

JSON element follow-on: strings, booleans, null and containers now retain structural value kinds when captured as tokens; numeric raw lexemes remain exact. Metadata encryption serializes its payload through the same canonical serializer so raw numeric tokens are encoded as numbers. The first native950 run had948pass/2encrypted-publication failures; the original encryption assertions remain intact. Focused final6/6 passes across decimal sync/async plaintext/encrypted variants plus independent root/child key contexts. Final native regression is recorded separately.

Final precision/ownership acceptance: Core1,370/1,370 passes9s; Shared847/847 passes on matching Core/Shared copies; focused native6/6 passes2s; fullSQLite952/952 passes121s, zero skips. The owned fixture is absent after normal teardown. Native cases exercise actual sync/async saves, reloads, exact decimal edits and plaintext/encrypted metadata. Generic text/ciphertext/receipt numeric parsing and decimal-safe conditional fingerprints remain explicit follow-on gates; this does not close committed delta persistence/replay or the full roadmap.


## Exact stored receipt metadata — 20 October 2026

`StoredRecordJson.DeserializeStoredHolon(string)` reads canonical holon metadata through its raw JSON-value codec, excluding that field before restricted typed decoding of the remaining record. Metadata therefore has one decoder owner and never becomes a rounded temporary double. Metadata values use the same JsonElement domain as native SQLite reads, preserving numbers beyond decimal precision/range, wide floating-point lexemes, strings, nested containers and explicit null. Root type binding remains restricted; metadata type-name text remains data. Missing metadata keeps the normal empty dictionary. Ambiguous case-insensitive metadata fields and non-object metadata reject as corrupt data.

SQLite conditional receipt replay now uses this decoder for each stored root/child record. Receipt format1 and its original record JSON are retained; no schema rewrite or alternative receipt fallback is added. Two native restart/replay cases originally fail on precise decimal metadata, then pass unchanged: replay returns original root/child values and original logical/provider revisions while the current database keeps its later update. Wide-double/null/string controls remain intact. Four new codec cases pass in full Core1,374/1,374, zero skips; both Core targets build with zero errors.

This certifies the selected SQLite receipt boundary, not every provider/replica/text reader. Shared replica receipt adoption is coordinated with the provider session. Full committed delta generation, atomic persistence, authorization/redaction and replay remain unfinished, together with the complete original roadmap.

Final receipt gate: Core1,374/1,374 passes11s, focused native2/2 passes2s and fullSQLite954/954 passes117s, zero skips (`receipt-precision-core-full.trx`, `sqlite-receipt-precision-focused.trx`, `sqlite-receipt-precision-full.trx`). Fixture absent after normal teardown. These reports certify the captured SQLite receipt epoch; other decoders and the full roadmap remain open.
