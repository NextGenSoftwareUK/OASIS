# SOLID CQL native record-point profile

27 October 2026. Implementation and controlled tests verified; native CQL gate
and WEB4 HTTP/demo enrollment remain open. Native storage baseline52/52 is
separately verified and does not certify CQL.

## Current contract

`SOLIDOASIS.Cql.cs` implements the shared `ICosmicQueryStorageProvider` interface.
The common planner, executor and HolonManager discover the same capability as
other adapters; provider-specific SQL/SPARQL knowledge stays in the adapter.

Only one exact string metadata equality on synthetic `SolidRecordId` is admitted.
It must be a lowercase nonempty GUID in D format, matching the canonical native
record key. Select one explicit SOLIDOASIS target. Ordering, additional business
predicates, graph expansion, indexed sources, scalar/projection and general
metadata filters remain unsupported. No managed full-Pod scan or query fallback.

```csharp
var scope = CosmicQueryScope.Named(ProviderType.SOLIDOASIS).Result;

var source = CosmicQuery<Holon>.Create(scope)
    .Where(holon => CosmicQueryMetadata.Value<string>(holon, "SolidRecordId")
        == recordId.ToString("D")).Result;

// Bind authoritative read access through HolonManager before execution.
// Provider execution alone is an execution primitive, not an authorization grant.
```

Activation is explicit. The adapter performs one native GET against the canonical
configured Pod resource, accepting encrypted text/turtle only. Existing codec
validates RDF subject/type, authenticated context and decoded identity/history.
A4MiB RDF byte limit and shared codec expansion bound apply; cancellation reaches
HTTP and stream reads. Empty source windows need no resource read.

404 is absence.403/500/redirects, wrong media/identity, damaged ciphertext and
oversized data fail without partial results. Deleted and access-denied rows are
excluded. Children are empty under the leaf profile. The returned provider key
and SolidRecordId are authoritative; SolidRecordResource is the exact source URI.
SolidSourceETag comes only from the HTTP header; stored forged values are removed
when the source supplied no tag. An ETag is resource concurrency evidence, not a
cross-Pod transaction/snapshot guarantee.

## Verification and remaining work

22 controlled cases cover public/owner/denied/deleted/absent records, malformed
and uppercase GUIDs, pre-cancel/in-flight cancel, inactive/wrong-target/budget,
unsupported ordering, empty window, wrong media/identity, corrupt RDF, oversized
response,403/500/redirect and headerless provenance. Public case also executes
through the shared CQL executor. Full provider suite384pass, zero skips.

Next: real retained CSS Pod → adapter → HolonManager/LINQ → spaced commented C#
walkthrough gate, exact fixture cleanup, then WEB4 HTTP admission and explicit
index design for richer queries. No universal SPARQL availability claim.

| Evidence relative to worktree | SHA256 |
|---|---|
| TestResults/SolidCurrent/cql-controlled.trx (384pass) | 9D725362096851A0F3B79EF607DC8557A7DAE83E4BB955F61F60A044156EDB3A |
