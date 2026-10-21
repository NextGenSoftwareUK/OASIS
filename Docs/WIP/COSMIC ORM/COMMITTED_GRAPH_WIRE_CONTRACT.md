# Committed graph wire contract

Working contract, 19 October 2026. This describes the existing version1 producer format. Strict scalar admission is implemented and verified by the full713-case Shared suite; see VERIFICATION.md for exact evidence.

One serialized intent has one interpretation. Decode must validate JSON field names and token kinds before typed conversion or record deserialization. Newtonsoft's general conversion rules are not protocol negotiation. A string boolean, numeric string, floating-point depth or named enum must fail visibly before provider mutation.

| Fields | JSON token kind | Contract |
|---|---|---|
| Format | Integer | Exactly1 |
| OperationId | String | Nonempty GUID in D format |
| ActorId | String | GUID in D format; existing empty-actor semantics remain subject to host authorization |
| SourceId | String | Existing bounded, nonblank source identity rules |
| SourceAuthority | Integer | Explicit supported provider enum, validated by existing request contract |
| Operation | Integer | Existing conditional operation enum |
| SaveChildren, Recursive | Boolean | Explicit graph-selection flags |
| MaxChildDepth | Integer | Existing nonnegative depth contract; zero means unlimited |
| Roots | Array | Ordered nonempty root GUID strings in D format |
| Nodes | Array | Selected graph node objects |
| Node.RecordJson | String | Canonical stored-record JSON using the existing restricted type binder |
| Node.Children | Array | Ordered child GUID strings in D format; empty is valid |

Envelope and node objects have exactly their declared fields, with ordinal casing. Missing, extra, duplicate or incorrectly cased fields fail. GUID tokens are strings; alternative GUID text forms are not silently normalized. Existing lineage/graph validation still rejects empty or duplicate identities, missing edges, cycles, disconnected or unselected nodes, and incompatible committed revisions. No alternate decoder or coercion path is provided.

Capture and receipt fingerprints retain their current format and bytes. Valid payloads emitted by Capture remain readable. Previously persisted hand-crafted payloads outside this contract are rejected and require explicit operator investigation; no automatic rewriting of committed obligations is allowed.

This boundary does not authenticate a source, authorize a tenant/record, prove endpoint identity or establish resource limits. Bounded payload/graph sizes, queue admission, retention, authenticated recovery and broader record/type security certification remain roadmap gates. Provider native limits can reject an otherwise valid intent and must remain visible.
