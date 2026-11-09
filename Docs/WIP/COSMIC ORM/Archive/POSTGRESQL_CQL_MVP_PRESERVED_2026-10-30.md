Superseded by ../POSTGRESQL_CQL_MVP.md for current integration; preserved f89cf0085 history.

# PostgreSQL CQL MVP admission

Updated30October2026. **Identity MVP and native/restart content queries verified; local content federation/HTTP/browser verified; deployed sandbox remains open.**

The adapter uses the existing PostgreSQL connection and canonical record reader.
One parameterized SELECT applies the server-authorized catalogue (at most100IDs),
native business predicates and UUID paging before materialization. The original
identity-only proof remains separate from new content-query acceptance. Decoded
response budget remains4MiB; it is not a pre-allocation bound.

PostgreSQL-compatible aliases retain their own identities and do not inherit this
capability admission. PostgreSQL storage is canonical JSON; no application encryption
or hosted-service certification is claimed. Federation observes providers separately.

## Verified controls

Eight tests passed, zero failed/skipped: captured catalogue immutability and authority
intersection, unsupported shapes/bounds, inactive provider, cancellation and alias
capability isolation. Producer and consumer builds passed using cached dependencies.

- Test: `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests/PostgreSqlCqlTests.cs`.
- Evidence: `TestResults/PostgreSqlCqlCurrent/postgres-cql-controls.trx`.
- SHA256: `4A1612248B969CC449F47763B9B7A37EB145FB7A392F17996452632AFE89CFEC`.

## Native and restart proof

The retained PostgreSQL service passed the native test twice (one test, zero failures
or skips per run): initial 751 ms, restart 324 ms. Three acknowledged saves in the
dedicated schema survived service restart with identical JSON hashes and native
`xmin` row versions. Selection returned three/two/zero rows; native paging returned
the expected UUID; a one-row result budget rejected without partial results.

- Fixture: `PostgreSqlCqlNativeTests.cs` in PostgreSQLOASIS.IntegrationTests.
- Initial evidence: `TestResults/PostgreSqlCqlCurrent/postgres-cql-initial.trx`.
- Initial SHA256: `1E68F21F658CFB3382F2225FC7A6DCF9673F8A099A516C954AEFE30F9A80EBBA`.
- Restart evidence: `TestResults/PostgreSqlCqlCurrent/postgres-cql-native.trx`.
- Restart SHA256: `72EFD2A2CE3293EB41FE64B041C137182A2059834DF4C0E40333D1BB06872521`.
- Persistent seed SHA256: `9B5BEFAA86549FAC075191C9418182E81F9013500A9612F938584AFD55B2A33F`.
- Runner: `Scripts/TestHosts/Run-CosmicPostgreSqlCqlNative.ps1`.

The first run exposed a runner ownership-flag scope bug: native verification passed,
but cleanup did not receive the flags. The runner now uses one shared lifecycle
object. The exact owned container was stopped, the restart proof passed, and final
container state was verified `exited`. No source/schema was reset.

## Mongo-paired teaching preset

The C# walkthrough, HTTP endpoints and browser all returned five physical rows
(three PostgreSQL plus two Mongo copies), then four for the two-ID subset.
Provider provenance remains explicit; duplicates are preserved. Forged actor and
unknown preset reject; an inactive PostgreSQL target returns unavailable without
partial rows. The temporary three-record Mongo fixture was verified and dropped.

- Test: `PostgreSqlMongoCqlNativeTests.cs` in PostgreSQLOASIS.IntegrationTests.
- Evidence: `TestResults/PostgreSqlCqlCurrent/postgres-mongo-federation.trx`.
- Result: one passed, zero failed/skipped, 20 seconds.
- SHA256: `60D807085515EE6CD73A2F2BF4959B746A352CB572DC4CFADC413A52BE7A09F4`.
- Browser evidence: `TestResults/PostgreSqlCqlCurrent/postgres-mongo-browser.jpg`.
- Final state: runner exit0, PostgreSQL exited, owned Mongo/HTTP listeners absent,
  fixture journal removed, original source retained.
- Launcher check: 8 single/16 plans, 21 paired/42 plans, 28 paired rejections,
  3 archive rejections; listeners unchanged.

```powershell
# Inspect the ready preset without launching services.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider PostgreSQL -Federated -ShowPlan

# Run C# and HTTP checks, then keep the browser demo available on loopback5112.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider PostgreSQL -Federated

# Automated verification with owned service teardown.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider PostgreSQL -Federated -VerifyOnly
```

Use the page's stop button to complete owned fixture cleanup. Persistent source is
read-only on subsequent runs. This proves bounded identity filtering/native paging,
not arbitrary content predicates, managed cloud products, aliases, production
registration or full PostgreSQL LINQ parity.

## Native content-query scope and remaining demo work

The single native compiler now admits Name/Description equality and Contains,
string metadata equality (including nested object paths), Boolean constants, Id
equality/membership and AND/OR. All values/JSON path segments are parameters.
Native predicates run before the existing UUID order/Skip/Take; there is no
managed filtering or unrestricted collection read. A business OR cannot enlarge
the complete server-authorized catalogue.

Text is case-sensitive, with PostgreSQL C collation for equality. Contains uses
strpos, so percent/underscore/backslash are literal characters rather than LIKE
wildcards. Metadata must traverse objects and finish at a JSON string; missing,
null, non-string and array-parent paths do not match a string equality. Keys are
case-sensitive, including numeric-looking object keys. NUL inputs reject. Native
quote/wildcard/Unicode values, nested object paths and array exclusion are proved
by the17-query native fixture below.

Name/metadata ordering, numeric metadata, null predicates, StartsWith/EndsWith,
NOT, graph queries and arbitrary-source authorization remain unsupported. Aliases
which explicitly opted into their earlier native identity scope retain only that
scope; none inherits PostgreSQL content capability certification. The existing
Compile helper remains an identity-algebra API; provider execution uses the one
CompileNative command owner, without fallback dispatch.

Producer/unit builds pass.16 controls pass/no skips:8 retained plus8 content,
parameter ownership, authority containment and rejection cases. Evidence:
TestResults/PostgreSqlCqlCurrent/content-controls.trx; SHA256 895972ac28790ac99bd1bb34e2565282ef4e285de8f5bc573dd4d9d5ee9a35d8.
These controls are compiler proofs; native SQL/JSON and restart evidence follow.
They do not establish content federation/browser/sandbox acceptance.

### Native and restart acceptance

One native test containing17 query cases passed twice: initial991ms and after
service restart499ms, no skips/failures. Four acknowledged records are retained
in the separately owned cosmic_cql_content schema on the existing database.
Three IDs are authorized; a fourth matching record remains excluded even through
business OR. Original cosmic_cql rows remain unchanged. Content JSON hashes and
xmin row versions match across both runs; source is never reseeded on repeat.

| Feature | Native content proof |
|---|---|
| Name equality / Contains / case-sensitive negative | ✅ exact Espresso, ss, lowercase miss |
| Description Contains | ✅ two coffees; literal percent/underscore/Unicode/quote |
| String metadata equality | ✅ two coffee records; exact Unicode value |
| Nested object keys | ✅ London matches two; numeric-looking object key works |
| Missing/null/wrong-type/array parents | ✅ no string match; array indexing excluded |
| AND/OR and authorization | ✅ mixed positives; matching unauthorized record excluded |
| Native paging | ✅ stable UUID order, one selected coffee after Skip/Take |
| Content federation / C# / HTTP / browser | ✅ four native presets,2/4/4/2 physical rows, both providers |
| Deployed WEB4 sandbox | ❌ tenant authorization/admission/registration and UI enrollment remain |

Proving test: Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests/PostgreSqlCqlContentNativeTests.cs.
Evidence under TestResults/PostgreSqlCqlCurrent:
- postgres-content-initial.trx: SHA256 70bfc064953bb319f7daa7a62a9323cd211176abaefd64e119a59891acb8cab4.
- postgres-content-native.trx: SHA256 c424c2cfe8ce0b0aeb9c68b3fc6a7e9c7b843590d501c0cb6729f24b8274f2fd.

```powershell
# Reuses the exact retained container/volume; no restore/download/install.
Scripts/TestHosts/Run-CosmicPostgreSqlCqlNative.ps1 -Content
```

The runner requires original identity source, refuses pending content setup,
checks container/image/port/volume, locks the source and stops the owned service
on verified success. Content source/acknowledgements/snapshots stay under
LocalState/COSMIC/PostgreSqlCql/content-*; logs/TRX stay disposable TestResults.
Final container state was exited. No schema reset, source rewrite or new runtime.

### Mongo-paired content teaching demo

The retained content schema now pairs with a verified disposable three-record
Mongo fixture. Four content presets run through the same captured CQL query,
capability planner and HolonManager federation owner in C#, HTTP and browser:

| Preset | Predicate | Physical rows (PostgreSQL + Mongo) |
|---|---|---|
| name | Name == Espresso | 2 (1+1) |
| description | Description.Contains(coffee) | 4 (2+2) |
| metadata | category == coffee | 4 (2+2) |
| combined | (Name == Espresso OR Tea) AND category == coffee | 2 (1+1) |

Every preset preserves provider copies/provenance and fails if a native source
fails. Trusted per-source catalogues stay server-owned; request actor injection
and unknown presets reject. The original ID-query5/4 presets remain available.
C# source: Examples/CosmicCqlDemo/ContentFederationPresets.cs; the shared
IdentityFederationWalkthrough.RunQueryAsync owns planning and execution for both
identity and content examples. No separate HTTP query evaluator or managed scan.

```powershell
# Automated native and paired C#/HTTP checks, followed by owned teardown:
Scripts/TestHosts/Run-CosmicPostgreSqlCqlNative.ps1 -Content -Federated

# Same checks, then the teaching page at http://127.0.0.1:5112/:
Scripts/TestHosts/Run-CosmicPostgreSqlCqlNative.ps1 -Content -Federated -Serve
```

Use the page stop button to finish cleanup. The general hub's existing PostgreSQL
preset still launches the ID demo; use the explicit -Content command above for
new content buttons. All4 browser buttons were clicked and returned2/4/4/2 with
both native provider labels. Final runner exit0; PostgreSQL exited, Mongo/HTTP
listeners absent, exact temporary database dropped and content journal removed.
Original PostgreSQL source, content seed/ACKs and snapshots remain retained.

Evidence under TestResults/PostgreSqlCqlCurrent:
- postgres-content-mongo-federation.trx: SHA256 a167dd98c799344c4b2230d5788ecf20655b482dc481ab7bf6a5ad316a848a98.
- postgres-content-browser.json: SHA256 ec1a92dfacb9a839a265312c589ca7c7d467159ad9f46f11295fef8d88ecaec9.
- postgres-content-mongo-browser.png: SHA256 e739dfd452a85ac88bf4b3c8ec6817e0c6354e185fbff68309a46bd305f57545.
- postgres-content-result-browser.png: SHA256 7e13d61a34cc11e7b21648cbdc989feae4ccb9c1588572462654e6b0133a4d6b.

Exact next: deployed sandbox integration requires actual tenant authorization,
provider/admission registration and UI enrollment. The native bounded catalogue
must never be replaced by a client-provided ID list or all tenant rows. Until
those gates pass, this is a loopback teaching host with a controlled identity,
not a hosted WEB4 authentication/usage or full PostgreSQL-parity certification.

## Programme priority

MVP breadth now covers **all providers** before deferred parity depth. Each concrete
provider must independently prove its native scope; shared transport or inheritance
is not evidence. See [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) and
[current status](CURRENT_STATUS.md). Hosted/credential/funding gates stay explicit;
free-only infrastructure policy remains in force.
