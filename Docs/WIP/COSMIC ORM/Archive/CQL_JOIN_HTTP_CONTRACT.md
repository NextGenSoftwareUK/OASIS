> Superseded by [current status](../CURRENT_STATUS.md); retained historical and technical reference.

# CQL join HTTP contract

Date:25 October2026. Status: **endpoint implemented; signed native acceptance
passes on MongoDB, SQLite and Neo4j**. Latest evidence is below; earlier dated
compiler sections preserve prerequisite history.

## Intent

The `POST /api/data/query-cql/join` request selects one host-eligible
provider (or its current default), an `Inner`/`Left` kind, independent `Left`/`Right`
source filters/order/windows,1–8 typed key pairs and1–32 selected columns.
`CqlJoinV1Request` and nested objects reject unknown members. Actors and provider
eligibility come from admission; sources cannot override them. Requests contain
bounded descriptions, never C#, SQL, Cypher, expression strings or provider plans.

Each key has an alias and explicit left/right `CqlScalarV1Selection` paths.
Output columns name `Left`, `Right` or `RightPresence`. A path column requires a
selection; `AllowMissing` applies only to right outputs. `WhenMissing` applies only
to presence. Right required value types on a left join need explicit absence
lifting. Source metadata nullability and right-row absence are independent.

Maximum input rows are1–1000 per side, pair work1–10000, output rows1–100.
Left/right/output payload limits are each1MiB. These limits reject overflow rather
than implement a result page. Source Skip/Take have the existing CQL source
semantics. Result composition/paging and distributed joins remain separate work.

## Verified compiler controls

The linked production sources pass18 new controls in full Core2,771, zero skips.
Both actor/provider bindings, independent windows/budgets, required versus lifted
right paths, malformed/ambiguous shapes, unknown JSON members and mutable request
isolation are covered. Core runtime named bindings reuse the common join owner.

## Historical compiler checkpoint: required before exposure

- Register one authorized controller/endpoint action and exact query-route/tariff leaf.
- Reuse existing prepared admission/provider eligibility and physical query-attempt owner.
- Plan against actual adapter capabilities before dispatch; reject unsupported graph/snapshot domains.
- Serialize bounded columns/rows with existing exact scalar wire tags and independent right presence.
- Cover unauthorized/foreign actor/provider, malformed/oversized requests, cancellation and budgets.
- Run signed native SDK/HTTP acceptance on MongoDB, SQLite and Neo4j, with receipt/replay checks.
- Update commented C#/HTTP/browser/Swagger examples, runbook, capability matrix and evidence.

SDK flat joins are already available on all three stores. This compiler checkpoint
does not claim the planned HTTP endpoint exists or close the full CQL roadmap.

## Join HTTP endpoint implementation — 25 October 2026

`POST /api/data/query-cql/join` is implemented in the authorized production
controller. It reuses prepared admission, tariff/provider eligibility, actual
adapter capability planning and the generic V2 join execution/receipt owner.
The shared exact route registry now includes the join leaf. Results expose
bounded column descriptors, exact scalar wire values and independent `hasRight`
presence. Escaped JSON output is checked against the existing response byte cap.

Full Core/linked WEB4 tests pass **2,785/2,785**, zero skips. The first test run
found four mock packets missing the compiler's automatic ID ordering tie-break;
fixtures were corrected without weakening production validation. Original red
and final green TRX evidence are retained under `TestResults/CoreCurrent`.
The linked native HTTP host now includes the join model/compiler/endpoint and
builds with **zero errors**; existing warnings remain. Its browser authorization
route list includes `/join`. Evidence:
`TestResults/CosmicCqlDemo/join-http-host-build.log`.

**Still required:** signed native HTTP join acceptance on MongoDB, SQLite and
Neo4j, durable receipt/replay checks, annotated SDK/HTTP/browser/Swagger join
examples and final regression certification. This endpoint implementation is
not yet a claim of production transport certification or full CQL completion.

## Signed native join HTTP acceptance — 25 October 2026

The authorized join action passes the real-Kestrel signed-JWT acceptance flow
on MongoDB, SQLite and Neo4j: **1/1, zero skips**. Fifteen joins and fifteen
replay probes cover inner/left selection, unmatched presence/null values, exact
Int64 strings, owner/visitor visibility and hidden right inputs sharing matching
stock keys. Every join has one durable attempt and receipt; replay returns409
without another dispatch. The full flow reconciles **195 settled operations /
190 physical attempts**, including existing row/scalar/projection regressions.
This does not add joins to the interactive87-example demo or close the broader
join certification matrix.

Mongo's compiler now explicitly retains null right-ordering fields after an
unmatched lookup. Decoder shape validation remains strict. The original frozen
failure passes with the producer fix; exact ownership and durable settlement
checks precede teardown. A later test-only wire assertion was corrected to the
existing column-described exact string representation. Original red reports
are retained separately.

Evidence: `TestResults/CosmicCqlDemo/join-http-native-final.log`,
`TestResults/Neo4jCurrent/neo4j-cql-v1-demo-current.trx` and
`TestResults/Neo4jCurrent/cql-v1-web4-native-evidence.json`. Owned fixtures were
removed and ports27029/17687 verified absent after teardown. No new environment.
Annotated interactive join demos, composition, federation, extended scalar/
policy coverage and remaining final certification gates stay open.

Final Mongo regression after the ordering-proof fix passes **510/510 provider
units** and **328/328 native CQL tests**, zero skips. The native run takes4m29s;
no additional producer changes followed the signed three-provider HTTP gate.
Reports: `TestResults/SubscriptionUsageCurrent/join-null-order-unit.trx` and
`TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx`.
Interactive join examples and the broader roadmap remain open.

## Annotated join demos verified — 25 October 2026

Examples30/31 now demonstrate left/inner joins in the shared C# walkthrough,
signed HTTP walkthrough, browser presets and generated Swagger. C# uses the
generic Core/V2 planner/executor on every provider; HTTP binds actor policy to
both independent sources. Comments explain absence, nullable right outputs,
exact Int64 values and native matching. Browser presets retain editable,
indented JSON and separate explanations.

Fresh dedicated native demo passes **93 SDK /93 HTTP**, **288 settled operations
/283 physical attempts**, joint test1/1 zero skips. Actual generated Swagger
join route/schema checks pass. Production browser JavaScript and six provider/
kind presets pass offline; that is request-generation evidence, not rendered
browser interaction. Standalone demo build passes with zero errors.

Run `Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1` for verification, or add `-Serve`
and open `http://127.0.0.1:5088/demo` (Swagger at `/swagger`). Use **Finish demo**
for verified settlement/cleanup. The console transcript is
`TestResults/CosmicCqlDemo/v1-walkthrough.txt`; summary `v1-demo-result.json`;
log `join-demo-native.log`. The earlier87/87 cohort remains historical.

Owned fixtures are removed and test ports stopped. Broader composition,
federation, expanded policy/scalar and final certification gates remain open.
