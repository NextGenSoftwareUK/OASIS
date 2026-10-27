# WEB4 indexed frozen preparation

27 October 2026. Server binding, query planning and reservation-bound preparation
implemented/tested. HTTP route/admission wiring and end-to-end HTTP acceptance are
not implemented by this checkpoint; do not mark indexed WEB4/demo rows green.

## One captured intent and grant

CqlIndexedV1Request accepts an explicit server alias, bounded row Query and optional
MaximumLagSlots/MaximumHeadAgeSeconds. Unknown fields reject; callers cannot send
endpoints, keys, prices, checkpoint/head evidence or raw server grants. Query.Provider
and Graph are rejected because the alias owns the source and this profile is leaf-only.

ICqlIndexedV1Bindings resolves one authenticated actor/alias without activation or
failover. Implementations must not perform unadmitted storage I/O. It returns a
CqlIndexedV1Binding with registered reader/observer, exact schema2 Solana/SQLite
tariffs, server minimum finalized position/freshness baseline, actor/source read
grant and maximum results. Server alias registration/deployment remains explicit.

CqlIndexedV1Preparation.Prepare captures all caller-owned fields before invoking
the resolver once. Caller mutation during resolution cannot change the captured
query, alias, output metadata flag or freshness. Returned plan/read requirements
are immutable; execution must use this preparation instead of resolving again.
The source/index fields are admitted against the actual reader binding/capabilities.

Freshness requests can tighten maximum lag/head age; they cannot weaken the server
baseline or raise its result bound. Minimum finalized position is server-owned in
this initial HTTP wire profile. Unsupported metadata/graph shapes reject during
planning before any query transport. All ordinary canonical filters/order/paging
use the existing wire compiler. An explicit indexed-only compiler entry admits
Solana intent without widening native provider enrollment or inventing a SQLite
logical source.

MaximumServiceCostUsd is the conservative bound:

`MaximumPhysicalReads * max(configured per-operation service tariff)`

It covers the full admitted physical-read budget even if a reader is erroneous;
actual billing still uses acknowledged operations only. This is a service-cost
reservation bound, not a native gas estimate or invoice. The host must authorize
and reserve it before constructing the prepared usage handle. Creating this
preparation does not itself create usage admission, signing authority or execute I/O.

## Host preparation sketch

```csharp
// actor is established by authentication; bindings is server-owned configuration.
var captured = CqlIndexedV1Preparation.Prepare(actor, request, bindings);
if (captured.IsError)
    return Reject(captured.ErrorCode);

var prepared = captured.Result;
var reservationBound = prepared.MaximumServiceCostUsd;
// Next: authorize/reserve against this exact frozen preparation and retain it in
// the request context. Execute prepared.Plan with prepared.Binding's registered
// reader/observer and exact tariff policy. Never resolve the alias a second time.
```

Next implementation step: bind this frozen object to the HTTP request/usage handle,
validate captured wire intent at controller dispatch, execute the same admitted
plan, return bounded public rows plus source/execution checkpoint/receipts, and
prove real HTTP routing/admission/uncertainty/no-dispatch semantics. Production
JWT/outbox/settlement, native source coverage and full ORM parity remain separate gates.

## Verification

10 new cases prove capture-before-callback, resolver exactly once, no reader I/O,
conservative reservation bound, stricter freshness, wrong actor, weaker freshness,
result bound and unsupported metadata rejection, invalid alias/duplicate provider/
graph preflight, and unknown endpoint/price wire rejection. Production compiler,
DTO and preparation sources are linked directly into the existing Core tests.

118 focused chain/preparation checks and511 combined chain/WEB4/compiler regression
checks pass, zero skips. No source/index/query operations, runtime/environment
creation, restore/download or service launch. Full ONODE host build and HTTP
execution are not certified by these linked-source tests. Existing scoped green
features/federation cursors were not expanded or hardened.

- `TestResults/ProtocolCqlCurrent/index-web4-preparation-core.trx` SHA256 `C01C344C32458DABA92CB2200B1C05E7996C8B5DDAFABA75A054E8827FA0A124`.
- `TestResults/ProtocolCqlCurrent/index-web4-preparation-regression.trx` SHA256 `0C35677677FB69F79158A17EACBBB21A930C3A5477C26DD96EB53F455FDD8AB9`.
