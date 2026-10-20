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
