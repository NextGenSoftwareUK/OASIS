# Oracle CQL demo admission

28 October 2026. User added Oracle to demo priority2. Selected implementation:
OracleDBOASIS(native Oracle Database); OCI Object Storage/hosted OCI are separate.
Storage52/52 is existing native storage evidence, not CQL or hosted-cloud proof.

## Implemented foundation

OracleDBOASIS now implements ICosmicQueryStorageProvider. Its first explicit profile
supports canonical Id equality/membership, Boolean/And/Or composition and a bounded
post-selection window. A complete authoritative Oracle readable-ID catalogue is
required from trusted host code; client-selected IDs alone must never become that
catalogue. Owner/public authorization remains unenrolled; new native JSON content
filtering is a compiler candidate pending the native proof below.

The native SQL owner binds GUIDs as named RAW16 parameters, intersecting the business
predicate and captured authoritative catalogue in the same SELECT. No full-store
scan/download or managed business filtering is used. Existing canonical history,
identity and native-column validation are reused before output. Selected records are
fully validated before Skip/Take publication; no partial rows on source error.
Children are stripped; graph/depth options are rejected at planning.

Bounds:128 selected records,512 bound identity parameters,1MiB characters/record and
4MiB characters total. Native RAW-ID order defines this profile's unspecified-order
page; arbitrary OrderBy is not advertised. Complete page over result budget errors.
Cancellation and explicit activation precede connection access. Existing storage
mutation/conditional/history behavior is unchanged.

## Verification

Provider builds.7 focused CQL cases pass; all473 existing+new Oracle unit tests pass.
Tests cover frozen ID/auth snapshots, bound GUID parameters, empty catalogue FALSE,
unsupported Name/metadata/order/graph, inactive provider and pre-cancelled dispatch.
Native CQL acceptance now passes (1 test, no skips): four real records, trusted authorization returns three, native membership subset returns two, stable page, Take(0), result-budget rejection without partial rows, canonical names/provider keys and exact envelope-checked cleanup.
Proof: TestResults/OracleCurrent/oracle-cql-native-current.trx; SHA256 4F4BF7EBBB95E40D8BB0DAAB29304C1AF1D6712E4A6D39BA41D4E17A758A9ED0.
[Test](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests/OracleCqlNativeTests.cs). Container stopped (143, no OOM); persistent fixture journal absent.
Raw proof:TestResults/DemoBreadthCurrent/oracle-cql-unit-regression.trx
SHA2564AE87058D9B651EFA6FF0C3B81A5D22F36B7A77F974496A2C00599E6563A9C4C.
Unit assets regenerated from local cached packages only; no download/install/container.

## Exact next step

Mongo+Oracle SDK/native pairing now passes the commented
[IdentityFederationWalkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs):
5 physical rows (Mongo2/Oracle3), then4 subset, unrelated Mongo record excluded.
Each provider receives its own trusted readable-ID catalogue; HolonManager dispatches
both native reads through an explicit V2 registry. Exact3 Mongo/4 Oracle records
removed, journals absent, both services stopped (Oracle143/no OOM).
Paired TRX:TestResults/OracleCurrent/oracle-cql-native-current.trx;
SHA2567F4D1FF8D328B0A4047833BB5F5D8CDCEC44D8082B9BDF8571A5ECC8DE4B667F.
Run `Scripts/TestHosts/Run-ReusableOracleMongoTests.ps1` (owns one build lease).
First preserve reviewed prior evidence; never replay pending fixtures.

Initial test setup omitted explicit V2 DNA/registry and failed before query dispatch.
Corrected the fixture configuration; original records reconciled without replay:
TestResults/OracleCurrent/oracle-cql-manager-reconciliation.trx,
SHA256439C115E1E96B324573AB40AB3F4FDF7F4E3F30833CFF49E763EE82ADAB359D5.
Failed original evidence remains in TestResults/OracleCurrent/Archive.

HTTP/browser teaching preset now verified through
[IdentityFederationApiHost](../../../Examples/CosmicCqlDemo/IdentityFederationApiHost.cs).
Run `Scripts/TestHosts/Run-ReusableOracleMongoTests.ps1 -Serve`, then open
http://127.0.0.1:5090/ . Native SDK/HTTP acceptance precedes interactive use;
click **Stop demo and clean fixtures** to finish and verify owned fixture teardown.
The browser submits only `{ "preset": "all" }` or `{ "preset": "subset" }` to
`POST /api/cql/federation`. Actor/catalogues/providers stay server-owned.
Unknown presets and forged actor fields return400. Native source failure returns503
without connection details or partial rows. This loopback teaching host is not a
production WEB4 authentication/billing endpoint.

Actual browser clicks prove5rows then4, both source provider labels, and shutdown.
Screenshot capture now works; inspected readable section/line/button spacing.
No durable screenshot archive was created; raw pixels were inspected in the browser tool.
Final SDK/HTTP/browser acceptance1/1pass/no skips; exact4Oracle/3Mongo records removed,
journals absent, Mongo27029/host5090 listeners absent, Oracle stopped143/no OOM.
TRX TestResults/OracleCurrent/oracle-cql-native-current.trx SHA256
AF6DE716288A6A662AC6A6FC13E4CA618124AD3E4F057548E83B9E794ABAA0DE.
Original host-configuration failure was corrected by adding the in-memory
configuration source before binding the URL; no query fallback. Original records
reconciled without replay:oracle-cql-host-reconciliation.trx SHA256
0EFBA2DA17BA1AAEECF38044767E5B571D411E8F5FD45BF78EDE8C20453647B3.

Next breadth gate:Mongo+Solana mixed HTTP/browser preset. Hosted cloud, richer
Oracle business predicates and full EF parity remain separate open gates.
Reuse the retained database and runner `Run-ReusableOracleTests.ps1 -Cql` under
one build lease. Persistent journal: LocalState/COSMIC/OracleCqlCurrent;
raw evidence: TestResults/OracleCurrent. Never overwrite prior evidence or replay
pending fixture intent. Richer business fields/scalars, runtime outage/cancellation
and history-specific CQL corruption gates remain open; this is not full EF parity
or hosted OCI proof.

Native contract reference:[OracleDbType RAW](https://docs.oracle.com/en/database/oracle/oracle-database/21/odpnt/OracleDbTypeEnumerationType.html).
Hosted Azure separately requires free-account setup; user has no free subscription.


## Central launcher / post-Development-merge verification

`Run-CosmicCqlDemoHub.ps1 -Provider Oracle -Federated -VerifyOnly -ArchiveCompletedDemo`
now passes1/1native SDK/HTTP case on Corea2e9b6c. Exact4Oracle/3Mongo records
removed; both pending journals absent, no27029/5090/11521listeners; Oracle143/no OOM.
TRX TestResults/OracleCurrent/oracle-cql-native-current.trx SHA256
28CA5EE9AA3B227E2320A1A3A492369D8661B94D82FD105A45D6CF47C4360239.
The earlier AF6DE716... browser proof remains preserved under its full hash in
TestResults/OracleCurrent/Archive. ArchiveCompletedDemo admits only one complete
unskipped pass after pending-fixture checks; failed/unknown outcomes never replay.
Omit VerifyOnly for the interactive page. [Launcher guide](CQL_DEMO_LAUNCHER.md).

## Content-query compiler candidate (30 October)

The single native statement now admits Name/Description equality and Contains,
string metadata equality on nested object paths and AND/OR. The original RAW16
catalogue/business identity predicates remain in the same WHERE conjunction;
content OR cannot replace authorization. Existing128row/512parameter and canonical
envelope validation/window ownership remain unchanged. No managed business scan.

Oracle JSON_EXISTS requires a literal JSON path. Each member is JSON-quoted and
the resulting SQL string literal is escaped; predicate values use separate
PASSING VARCHAR2 binds. Explicit object checks on every metadata parent and a
string-type check on the leaf prevent array unwrapping and numeric/null coercion
in the15 native synthetic cases below; persisted-source acceptance remains open. ERROR ON ERROR retains actual native failures rather than
silently returning a nonmatch. Contains uses the literal has substring operator.

Bounds:512 total identity/text parameters; text values and complete encoded JSON
paths each at most4000UTF8bytes; NUL rejects. Null/numeric metadata, prefix/suffix,
NOT, ordering, child/graph and owner/public/inherited access remain unenrolled.
Oracle documents SQL NULL VARCHAR2 in PASSING as an empty JSON string; the adapter
uses that type for an empty captured string. Native empty/missing/null distinction,
Unicode, arrays, keys, text/trailing spaces and limits still need actual proof.

Provider/unit builds pass;13 query controls pass/no skips. They cover preserved
identity snapshots/empty catalogues, frozen text bindings, authority conjunction,
key literal containment, empty-string intent and explicit unsupported/bound cases.
Evidence TestResults/OracleCurrent/content-controls.trx; SHA256 b086e2d7476c3c3888d79a5fd536d294f9cdb4cbff53cf536075bd049900e667.
These are compiler proofs; native content and paired/browser/sandbox remain red.

Exact next: reuse the retained Oracle container/image/volume/11521 and existing
native runner. Read-only syntax/type/empty admission now passes; next use an
explicitly journalled owned saved-holon fixture for positive/
negative Name/Description/metadata, nested/numeric-looking keys, arrays, missing/
null/wrong type, literal wildcards/quotes/Unicode, AND/OR and authority exclusion.
Preserve historical pending/recovery evidence and never replay uncertain writes.
Then use the common ContentFederationPresets for Mongo-paired SDK/HTTP/browser.

References used for the candidate design:
- [Oracle SQL/JSON filter syntax](https://docs.oracle.com/en/database/oracle/oracle-database/19/adjsn/json-path-expressions.html).
- [Oracle PASSING type/NULL semantics](https://docs.oracle.com/en/database/oracle/oracle-database/26/adjsn/passing-clause-sql-functions-and-conditions.html).
Native proof on the retained pinned runtime is required; documentation from a
newer release alone does not certify that runtime's behavior.

## Read-only SQL/JSON runtime admission verified

The retained pinned Oracle image passed15 compiled-predicate cases using bound
synthetic JSON rows from DUAL. No OASIS table reads/writes, provider activation,
index initialization, DDL/DML or seeded fixture occurred. The source substitution
keeps production predicate text and OracleCqlStatement.BindParameters unchanged;
the provider now delegates to that same binding owner. Connection/parameter types
are real Oracle.ManagedDataAccess execution, not an emulated evaluator.

Cases: Name equality, Description Contains, empty equality, null-versus-empty,
empty Contains, array leaf exclusion, metadata equality, numeric type exclusion,
array parent exclusion, nested-array exclusion, numeric-looking object key versus
array step, quoted/apostrophe metadata key, trailing-space distinction and a
4000byte string. All15 pass in one native test420ms/no skips.13 compiler controls
also pass after the binding-owner extraction.

```powershell
Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-oracle-json-probe -Operation {
    Scripts/TestHosts/Run-ReusableOracleTests.ps1 -CqlContentProbe
}
```

This is runtime expression/binding admission. It does not validate persisted
canonical holons/history/native columns or the manager/federated API path. Those
content cells remain red until a saved-source fixture and paired demo prove them.
Final Oracle state exited143/no OOM; no new environment, download, restore or data.
Evidence TestResults/OracleCurrent/oracle-cql-content-probe.trx; SHA256 4ab94f7941d7b57faf04c9bb6ce40dec22036489a8bfa161e7f245c7740adaaa.
New test: OracleCqlContentProbeTests.cs in OracleDBOASIS.IntegrationTests.

## Saved-holon native content and restart verified

The actual Oracle provider path now passes21 content queries over four real,
acknowledged canonical holons, initially and after database stop/start (one test
per run, no skips/failures). Tests include Name equality/Contains/case, Description
Contains with literal percent/underscore/quote/Unicode, metadata equality/nesting,
numeric-looking object keys versus arrays, missing/null/wrong-type exclusion,
AND/OR, a matching fourth holon excluded by the trusted catalogue, stable native
RAW-ID paging, empty Contains/equality, escaped metadata keys and a4000byte value
which must not match a stored4001byte value with an extra space.

This exercises provider dispatch, canonical/history/native-column validation,
provider-key publication and the existing bounded window owner. All four source
JSON hashes and native modified timestamps remain unchanged across queries and
restart. Source seed/actor/IDs, four write acknowledgements and baseline are
retained under LocalState/COSMIC/OracleContentCql; pending setup absent, no reseed.
No schema/database/environment clone; only four owned rows in the existing store.

Proving test: OracleCqlContentNativeTests.cs in OracleDBOASIS.IntegrationTests.
Evidence under TestResults/OracleCurrent:
- oracle-cql-content-initial.trx: SHA256 8cd58a2ce146c6ddd69839f4fb75a30596d1fd32f746cef49eaf60ec9091ca99.
- oracle-cql-content-native.trx: SHA256 d2ecfe1ee04b069be03fc940aff130a036ba7b32c97043b3f5c59e2503bb3ccc.

```powershell
Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-oracle-content -Operation {
    Scripts/TestHosts/Run-ReusableOracleTests.ps1 -CqlContent
}
```

The runner preserves existing native reports: move a reviewed completed report
into retained evidence before repeat; do not bypass its guard. The initial report
is preserved byte-for-byte; an accidental duplicate copy was verified before its
disposable original was removed. That guard stopped one restart attempt before
test dispatch; the owned container was stopped, then the intended restart proof
passed. No provider write was repeated. Oracle ends stopped143/no OOM.

Native content scope is now verified. Content Mongo-paired C#/HTTP/browser and
deployed sandbox authorization/admission remain red. The approved extra daily
Development batch is the immediate priority; these new Oracle/provider changes
are excluded from that scoped deployment.
