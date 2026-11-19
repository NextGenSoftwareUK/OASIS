Promoted 19 November 2026: [COSMIC property entries](../../Devs/COSMIC_Property_Entries.md) is the maintained developer guide. S05 is accepted on MongoDB/SQLite/Neo4j by the linked audit; full ORM parity remains open. The earlier draft below is retained as history.

# COSMIC property entries - usage guide draft

This is a source-contract draft for the S05 property-entry gate, not a claim of full EF/NHibernate or live deployment parity. See [the acceptance audit](S05_PROPERTY_ENTRY_ACCEPTANCE_AUDIT_2026-11-19.md) for exact provider evidence and remaining gates. Base-holon property inspection/editing, selected owned saves, encryption, replay and bound replication have scoped native evidence. Typed relationship-role composition has retained MongoDB, SQLite and Neo4j native evidence. Extraction14 and nullable-entry8 controls are accepted, as are all three protected custom-property native cases; plain-mode historical replay remains under qualification.

The fragments assume an authorized loaded holon, its manager, actor and explicit provider authority. Import `NextGenSoftware.OASIS.API.Core.Holons` for tracking/property values and `NextGenSoftware.OASIS.API.Core.Interfaces` for `CosmicWriteOptions`. The manager is `NextGenSoftware.OASIS.API.Core.Managers.HolonManager`; provider authority is `NextGenSoftware.OASIS.API.Core.Enums.ProviderType`.

The APIs below live in `NextGenSoftware.OASIS.API.Core.Holons.HolonChangeTracking` and return `OASISResult<T>`. Check every result. Local tracking information grants no permission to read or write a record.

## Inspect a property

Start with a holon loaded or acknowledged through the application's authorized provider flow. Inspection requires an accepted local baseline; an arbitrary detached object has none.

```csharp
var inspected = HolonChangeTracking.GetPropertyValues(holon, "/Description");
if (inspected.IsError)
    throw new InvalidOperationException(inspected.Message, inspected.Exception);

HolonPropertyValues entry = inspected.Result;
Console.WriteLine(entry.OriginalJson);
Console.WriteLine(entry.CurrentJson);
Console.WriteLine(entry.IsModified);
```

`HolonPropertyValues` is a detached view. Its JSON strings cannot mutate the private baseline. `IsValueModified` compares persistent values; `IsExplicitlyModified` records force-write intent; `IsModified` is their union.

| Value state | Exists flag | JSON string |
| --- | --- | --- |
| Missing metadata key | `false` | C# `null` |
| Present JSON null | `true` | `"null"` |
| Declared nullable CLR slot omitted by the canonical null writer | `true` | `"null"` |
| Present string `hello` | `true` | `"\"hello\""` |

The nullable-slot rule applies only to an exact declared, nullable canonical root whose writer proves null omission. It does not invent metadata keys, unknown/ignored properties, nested values or conditionally suppressed members. Inspection normalizes logical null without changing the stored JSON. Eight focused controls in run44546555815 verify this rule, including reset, explicit intent, aliases and original rebasing.

Paths are exact RFC 6901 pointers: `/Name`, `/MetaData/status`, or `/MetaData/a~1b` for the metadata key `a/b`. Escape `~` as `~0` and `/` as `~1`. Arrays are whole-value properties, not writable index paths. Persistent serializer rules determine valid names and types. Do not parse large integer JSON through JavaScript `Number` when exact values matter.

## Change current values, mark intent, or reset

```csharp
var changed = HolonChangeTracking.SetCurrentPropertyValue(holon, "/Description", "Reviewed");
if (changed.IsError)
    throw new InvalidOperationException(changed.Message, changed.Exception);
```

The typed setter validates the persistent contract and round trip before publishing the change. It does not save the holon or edit its original comparison value.

`SetPropertyModified(holon, path, true)` requests a write even when current and original values compare equal. `ResetPropertyValue(holon, path)` restores that property's comparison original and clears its explicit subtree flags. **`SetPropertyModified(holon, path, false)` also performs this reset**; it does not merely hide an unsaved edit. Other properties remain independent.

Flags are bounded and process-local. A save clears only the intent captured for that save. A flag reasserted or a value edited while a save is in flight must survive its acknowledgement. Flags are not serialized authorization, client-supplied ownership or a durable operation receipt.

## Edit comparison originals

```csharp
var rebased = HolonChangeTracking.SetOriginalPropertyValues(holon,
    new Dictionary<string, object>
    {
        ["/Description"] = "Previously reviewed",
        ["/MetaData/status"] = "draft"
    });
if (rebased.IsError)
    throw new InvalidOperationException(rebased.Message, rebased.Exception);
```

`SetOriginalPropertyValue<TValue>` edits one original. `SetOriginalPropertyValues` atomically edits a bounded dictionary of non-overlapping paths. `RemoveOriginalPropertyValue(holon, "/MetaData/status")` records original metadata absence; it differs from assigning null. Scalar originals must be assigned, not removed.

These operations change local comparison values only. They preserve current values, explicit modification tokens, native revision observations and child-view completeness. They do not refresh a stale record, grant Manage/Write, or turn a partial graph into a complete one. Changing the compatibility `Original` view is also not a way to supply a new native concurrency observation.

## Save selected fields

For a holon with a real previously observed native revision:

```csharp
var saved = await manager.SaveOwnedHolonAsync(
    holon,
    actorId,
    new CosmicWriteOptions(Guid.NewGuid()),
    new[] { "/Description" },
    saveChildren: false,
    providerType: authority);

if (saved.IsError)
    throw new InvalidOperationException(saved.Message, saved.Exception);
```

The example uses the holon's retained native observation. Applications can supply explicit original expected revisions through `CosmicWriteOptions`; they must not replace a stale condition with a newly read value merely to make an update succeed. Use the existing authorized conflict-resolution flow after a genuine conflict.

For batches, `SaveOwnedHolonsAsync` accepts `IReadOnlyDictionary<Guid, IReadOnlyList<string>> propertySelections`. Each selected record must already exist and carry its original authority condition. Other nodes in the explicit graph retain full owned-save semantics. An empty/missing selection, duplicate/overlapping paths, unsupported storage owner, or invalid property is an error rather than permission to downgrade to a whole-record save.

Only selected persistent fields are merged with the private authoritative record. Excluded local edits remain local. The acknowledgement advances the actual native condition and selected comparison state while preserving excluded values, excluded originals, later edits and reasserted flags. It must not publish private authoritative siblings or hidden provider metadata.

Permissions are enforced against actual stored ownership and access rules. Field Write/Delete may be sufficient without Read, but changing access metadata still requires Manage. Encryption uses the existing metadata owner and logical/stored selection binding; callers must not manipulate encrypted envelope fields themselves.

## Graphs and partial views

`saveChildren: false` requests scalar persistence without replacing relationship membership. A partial child view is not authority to replace the complete stored graph. `AcceptChanges`, edited originals, local collections and a scalar-save acknowledgement cannot grant that authority.

Child-selecting saves require an admitted complete membership observation. New child attachments require AddChildren where the authoritative parent restricts attachments, including when the parent is in the batch or the child has a caller-supplied ID. Parent lookup failures cannot be treated as successful permission checks. Native transaction conditions still decide conflicts and atomicity.

Application-specific runtime contracts and multiple typed child roles must use the registered typed materialization contract. Retained native evidence covers typed scalar preservation, partial-view restrictions, explicit role changes and historical replay across MongoDB, SQLite and Neo4j (runs44526382452 and44531399889). This is scoped composition evidence, not completion of the whole session/relationship/parity programme.

## Custom properties and their stored metadata cells

Custom CLR property names follow their canonical serializer contract in logical property paths. The actual extraction owner stores their values in metadata under the CLR property name; a custom JSON alias can therefore differ from the stored metadata key. Selection preparation must map the logical path to the cell written by that same extraction pass, then apply the encryption owner's storage rules. Callers should use the logical property path rather than manually target encrypted envelopes.

Native metadata cells may be CLR values or canonical JSON token wrappers. Compare their canonical type and value using the public `StoredRecordJson.CaptureRuntimeToken` helper (`NextGenSoftware.OASIS.API.Core.Serialization`), rather than boxed CLR equality or lossy numeric/string conversion. This preserves exact integers and null/absence distinctions.

Exact numeric tokens may be `JRaw`: use the same public `StoredRecordValueEquality.IsNumber` and `CanonicalNumberText` contract as receipt fingerprinting. Requiring `JTokenType.Integer` or converting through floating point rejects valid exact numbers or loses precision. A quoted numeric string is not a numeric token.

A nested custom property encoded as one metadata value requires merging that entire encoded cell. Selecting both a logical custom property and its backing metadata cell (or all metadata) is refused as overlapping storage intent. Whole collections remain whole-value properties.

Actual extraction/projection controls14 passed in run44542276995. Run44546555815 then passed nullable-entry controls8 and all three protected native custom cases, each with eight groups, five logical writes and all ten scalar/null/collection/replay/stale/cleanup markers. The three plain cases reached historical replay and failed intent binding. Run44550931624 accepted the eight shared fingerprint controls and Mongo/Neo plain cases; SQLite alone still failed replay. SQLite selected-domain6 routing and four new SDK guards are prepared; its plain and encrypted modes need qualification because the local receipt owner changed. No whole custom-property acceptance is claimed yet.

## Operation identity, replay and local acceptance

A new write intent gets a new `CosmicWriteOptions.OperationId`. When recovering an uncertain outcome, preserve the original operation, original condition and selected logical intent, and use the existing outcome/recovery API. Do not manufacture a fresh operation or perform an ordinary save as a retry. Historical receipt replay is not a refresh: a returned historical acknowledgement must not overwrite newer native values or relationship membership.

`AcceptChanges`/`RejectChanges` are local tracking operations. They do not contact storage, prove ownership, verify a remote revision, or grant a complete child observation. Do not call `AcceptChanges` merely to make an unsaved or conflicting object appear persisted.

## Executable contract examples and evidence

The short fragments above illustrate public calls, not standalone applications. The following source contains compiled/executed examples rather than new untested sample copies:

- [Original comparison editing controls](<../../../OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicOriginalPropertyEditingTests.cs>) cover atomic rebasing, null versus absence, refusal and in-flight originals; accepted20 controls are recorded in the audit.
- [Native original editing](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlV1Web4NativeTests.OriginalEditing.cs) composes those calls with actual shipping-provider writes, permissions and replay; run44502674000 accepted all three stores.
- [Nullable property entries](<../../../OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/CosmicNullablePropertyEntryTests.cs>) provide eight accepted examples of the nullable-slot behavior above.
- [Native custom property cases](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlV1Web4NativeTests.SelectedCustom.cs) exercise the actual selected-save overload and typed storage. Protected cases passed44546555815; plain replay remains pending.

## Draft status

Code examples illustrate the implemented signatures; they are not independently compiled sample applications. The final S05 audit must reconcile this guide with the qualified public API and provider results before promoting it to a lasting developer guide. S05 and full parity remain open. No deployment, shared-database call, local build, runtime installation or artifact download was performed to write this draft.
