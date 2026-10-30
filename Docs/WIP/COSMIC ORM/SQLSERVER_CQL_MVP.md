# SQL Server CQL MVP admission

Updated30October2026. **Identity demo and native/restart content queries verified; content federation/browser/sandbox remain open.**

## Implemented scope

The adapter binds the shared bounded identity compiler to SQLServerDBOASIS. It
admits an explicit authoritative catalogue of at most 100 UUIDs, then uses one
parameterized native SELECT with OFFSET/FETCH. UUID text ordering uses a binary
collation. Empty logical selections return without opening a connection, after
capability/cancellation/activation checks. The new content candidate is described
below; child traversal and arbitrary ordering still reject instead of scanning. A 4 MiB aggregate
decoded response budget is checked after string allocation; no pre-allocation bound
is claimed. Errors publish no partial page. Native isolation follows database
configuration; federation observes providers separately.

The provider now marks successful activation and clears activation on activation
failure/deactivation. Previously the activation method returned success without
setting the public lifecycle state. The existing save/load JSON codec is reused;
its native three-record save/load round trip now passes. No encryption, Azure SQL, cloud, alias or
production registration claim follows from these controls.

## Verified controls

Nine passed, zero failed/skipped, 127 ms. Covers captured catalogue immutability,
authority intersection, unsupported query shapes/bounds, inactive/pre-cancelled
requests, alias isolation, empty selection without native I/O and deactivation.
Producer and unit consumer built using cached dependencies.

- Test: `Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS.UnitTests/SqlServerCqlTests.cs`.
- Evidence: `TestResults/SqlServerCqlCurrent/sqlserver-cql-controls.trx`.
- SHA256: `673C37F69FBBD64CB0AD96D55BCB54EFC5BB87335DB159AC5DB0993403AD6DB1`.

## Native and restart verification

Installed LocalDB17.0.4025.3 is reused through one Windows-user-private task instance,
`CosmicCql`, and database `cosmic_cql_demo`. No runtime was downloaded/installed.
Database files, admission, seed, three write receipts and native snapshot remain in
`LocalState/COSMIC/SqlServerCql`. The named-pipe fixture uses Windows authentication;
this is local SQL Server, not Azure SQL or a remotely exposed service.

Initial native test1 passed891ms; restart test1 passed (including final-source rerun); zero failures/skips.
Three acknowledged saves load the expected IDs, names and creator; selection returns
three/two/zero rows; paging returns the expected UUID; result-budget failure returns
no partial records. Native JSON hashes and modified timestamps remain unchanged
through queries and instance stop/start. Final instance state: **Stopped**.

SaveHolonAsync previously returned success without IsSaved/SavedCount. It now requires
one acknowledged SQL upsert row, then sets IsSaved=true/SavedCount=1. Native save
assertions verify this at the write boundary; wider save/concurrency parity is open.

- Fixture: `SqlServerCqlNativeTests.cs` in SQLServerDBOASIS.IntegrationTests.
- Runner: `Scripts/TestHosts/Run-CosmicSqlServerCqlNative.ps1`.
- Initial evidence: `TestResults/SqlServerCqlCurrent/sqlserver-cql-initial.trx`.
- Initial SHA256: `3943CCEB9752274478C16E7948108D478ED6975F25226DF3A3533C466CA49EC1`.
- Restart evidence: `TestResults/SqlServerCqlCurrent/sqlserver-cql-native.trx`.
- Restart SHA256: `BFC39406CE22D9530EECCC8777093F8B0AA29756EAD31005F7E9C181CFDD451E`.
- Seed SHA256: `E8845E29EB703BD1495C51FE9CFCA734C3DB624D2B599952C82C002F39AAC914`.

## Mongo-paired teaching demo

The shared C# walkthrough, HTTP endpoints and browser return five physical rows
(three SQL Server plus two Mongo copies), then four for the two-ID subset. Copies
remain labelled by provider. Forged actor and unknown preset reject; an inactive
SQL Server target returns unavailable without partial rows.

- Test: `SqlServerMongoCqlNativeTests.cs` in SQLServerDBOASIS.IntegrationTests.
- Evidence: `TestResults/SqlServerCqlCurrent/sqlserver-mongo-federation.trx`.
- Result: one passed, zero failed/skipped, 32 seconds.
- SHA256: `52DCCB5C526E06A43C02AD6F7A00FE6C0BC04DD279368DFA66D972B92039F235`.
- Browser: `TestResults/SqlServerCqlCurrent/sqlserver-mongo-browser.jpg`.
- Final state: runner8528 exit0; LocalDB **Stopped**, Mongo/HTTP listeners absent;
  exact temporary Mongo fixture verified/dropped and ownership journal removed.
- Launcher routing: 8 single/16 plans, 22 paired/44 plans, 30 paired rejections,
  3 archive rejections; listeners unchanged.

```powershell
# Inspect the preset without starting services.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SQLServer -Federated -ShowPlan

# Run SDK/HTTP verification, then serve the browser demo on loopback5113.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SQLServer -Federated

# Automated verification and teardown.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SQLServer -Federated -VerifyOnly
```

Stop with the page button to finish owned fixture cleanup. The original SQL Server
source remains intact. This proves bounded UUID selection/native paging on local
SQL Server; arbitrary predicates, full LINQ parity, cloud/alias/product registration
and broader concurrency/child/history acceptance remain open.

## Exact next step

Continue the next red provider MVP from the all-provider inventory. Preserve this
source and reuse it; no further hardening of the green scope absent a reproduced bug.

All-provider [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) / [status](CURRENT_STATUS.md).

## Content-query semantics and native proof

The one native SELECT now compiles Name/Description equality and Contains,
string metadata equality, nested object paths and AND/OR inside the trusted
catalogue of at most100IDs. No unrestricted read or managed predicate scan.
Business OR remains inside the catalogue guard. Compatible aliases stay denied.

OPENJSON reads NVARCHAR(MAX) string values, avoiding JSON_VALUE's4000-character
limit. Shared CQL literal capture remains bounded to4096 UTF16 units. Canonical
record property names follow this provider's camelCase serializer; dictionary
keys retain exact spelling. Every metadata parent must be a JSON object, and
only a final string value matches; missing/null/non-string/array paths do not
satisfy string equality. Keys and values are parameters, never SQL fragments.

Equality uses Latin1_General_100_BIN2 plus DATALENGTH on both operands, preserving
ordinal trailing-space distinctions which SQL equality alone pads away. Contains
uses CHARINDEX under the same binary collation, with percent/underscore literal
rather than LIKE wildcard behavior. NUL rejects. Native whitespace/Unicode,
long-string, nested-key and authority semantics are proved by the23 cases below.

Producer and unit builds pass using cached dependencies.17 controls pass/no skips:
9 retained identity/lifecycle controls plus8 new parameterization, object/type,
authority, unsupported-shape,4001-character equality and shared capture-bound checks.
Evidence TestResults/SqlServerCqlCurrent/content-controls.trx; SHA256 670103f8b003c9664f8ef291ceda851ddb490ff2de3e48e3aef8733294843c75.
These compiler checks are supplemented by native evidence below; neither
establishes deployed sandbox/hosted-service readiness.

### Native content and restart acceptance

One native test with23 queries passes initially626ms and after instance restart573ms;
no failures/skips. The fixture includes the17 common PostgreSQL content cases plus
trailing-space distinctions for names/metadata values/keys, metadata key case,
and separate4001/4002-character string equality cases. Missing/null/non-string
and array-parent metadata do not match. Numeric-looking object keys do match.
Literal percent/underscore/quote/Unicode values, authority exclusion through OR
and deterministic native UUID paging all pass.

Existing cosmic_cql_demo3records remain unchanged. New cosmic_cql_content retains
four original content records plus two separately acknowledged long-value records.
The first run exposed a fixture omission (long fields absent), not an established
query-engine failure. Its evidence is preserved; no original source was rewritten.
An explicit append-only2row extension has its own pending/seed/ACKs. Original4row
and extended6row JSON/timestamp snapshots are both retained and unchanged across
queries/restart. No pending setup remains; no replay/reset/reseed.

| Scope | Acceptance |
|---|---|
| Native Name/Description equality + Contains | ✅ positive/negative/case/trailing/wildcards/Unicode |
| Native string metadata equality/nested object keys | ✅ missing/null/type/array exclusion and long values |
| Native AND/OR, authority containment, UUID paging | ✅23-query fixture |
| Source restart preservation | ✅ original3 + content4 + extension2 unchanged |
| Content C#/HTTP/browser federation | ❌ next scope; existing ID5/4 proof stays separate |
| Deployed sandbox/tenant auth/admission | ❌ not implied by loopback/source-catalogue tests |

Proving test: Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS.IntegrationTests/SqlServerCqlContentNativeTests.cs.
Evidence under TestResults/SqlServerCqlCurrent:
- sqlserver-content-before-long-fixture.trx: SHA256 eef72b1b1e50bb3fce5ccbb9ad5810ec2f1384352a9081e31bf4b707de526516.
- sqlserver-content-initial.trx: SHA256 13f98894b4b23ce536909747268025be8142291573b9bd032134ab6e02d88023.
- sqlserver-content-native.trx: SHA256 1c56d93505e957ccef955d5a4f9bde7150ea3eec6b691a195be2604fd919f44d.

```powershell
# Reuses the owned instance and pinned runtime, no restore/install/download.
Scripts/TestHosts/Run-CosmicSqlServerCqlNative.ps1 -Content
```

Content source files remain LocalState/COSMIC/SqlServerCql/content-source.mdf and
content-source_log.ldf, each8388608bytes (16MiB total). Same user-private instance;
no new environment/runtime. LocalDB final stateStopped. Retain attached database
files and source journals; never delete files to clean an attached database.

Exact next: add the four Mongo-paired content presets through the shared CQL
walkthrough and HTTP/browser teaching host, with explicit provenance and owned
fixture cleanup. Then address deployed sandbox tenant authorization/admission.
Do not harden completed native/identity scopes without a reproduced bug.
