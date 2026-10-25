# CQL selected-result composition contract

25 October 2026. Immutable capture is implemented; native subquery execution is
not yet enrolled. This preserves the original full roadmap scope.

## Operation ownership

A projection keeps its original `Source` object and selected columns. Its
`ResultStages` is a frozen, bounded sequence of explicit native operations.
`Where`, `OrderBy`/`OrderByDescending`, `ThenBy`/`ThenByDescending`, `Skip` and
`Take` operate on selected aliases or an exact scalar result. Capture invokes no
result constructor/getter/setter, arbitrary method or runtime evaluator.
Grouped and projection expressions share `CosmicSelectedColumns` resolution.

Stage order is observable. Source `Take(2).Select(...).Where(...)` filters only
the two participating rows. Result `Take(2).Where(...)` cannot become
`Where(...).Take(2)`. A later OrderBy remains a new stage; ThenBy extends the
immediately preceding sort. Skip and Take remain distinct ordered stages,
including zero windows. There are at most32 stages/32 keys per ordering, and
each selected window has a count between0 and10000. Failed intent propagates
without evaluating later operations.

```csharp
// Capture only at this checkpoint: native adapters still reject this shape.
var selected = CosmicQuery<Holon>.Create()
    .OrderBy(holon => holon.Name)
    .Take(2)
    .Select(holon => new { Label = holon.Name, Revision = holon.Version })
    .Where(row => row.Revision > 1)
    .OrderBy(row => row.Label)
    .Take(1);
```

The post-selection predicate binds `Revision`, while its source path remains
`Version`. No selected predicate is pushed ahead of the captured source window.
Runtime `CosmicProjectionRecord` aliases require exact typed indexer access;
wrong domains, unknown aliases, nested computed properties and lossy casts reject.

## Native execution acceptance still required

- Independent actual-adapter composition admission: ordinary source/projection
  permissions cannot grant a subquery or selected-result operator.
- One bounded native read owner on each provider. SQLite/Mongo retain snapshot
  behavior; Neo4j retains its declared read-committed guarantee.
- Native subqueries preserve every stage and deterministic source-identity ties,
  even when identities are not selected into application output.
- Explicit input/work/byte and intermediate/output limits. Every source value
  participating in projection must be validated before a later filter/page can
  hide malformed data or excessive work. Take(0) cannot bypass validation.
- A complete detached proof contract that validates identity, stage membership,
  exact domains, ordering and windows before any DTO construction. Ordinary
  `CosmicProjectionRow` buffers do not prove composed execution.
- Global preflight and actual readmission, durable physical-attempt/receipt
  ownership, cancellation/deadline coverage and no partial publication/replay.
- Positive/negative native evidence on all three providers, signed HTTP shape,
  spaced SDK/HTTP/browser/Swagger examples and coherent regression evidence.

Current planning and raw projection materialization reject nonempty stages with
`CQL_CAPABILITY_UNSUPPORTED`. This avoids silently ignoring the new intent or
performing client-side evaluation while native owners are being implemented.
Post-join composition, federation and authoritative policy remain separate open
roadmap requirements.

The shared alias refactor also passes the fresh native three-provider developer
demo: **93 SDK /93 HTTP**, **288 settled /283 attempts**, joint1/1 zero skips.
This verifies existing operators against the new Core producer; it does not
certify native execution of the new composed stages. Evidence:
`TestResults/CosmicCqlDemo/projection-composition-demo-regression.log` and
`TestResults/CoreCurrent/projection-composition-current.trx`. Owned fixtures are
removed and test services stopped normally.
