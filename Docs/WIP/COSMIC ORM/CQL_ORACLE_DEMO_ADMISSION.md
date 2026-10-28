# Oracle CQL demo admission

28 October 2026. User added Oracle to demo priority2. Selected implementation:
OracleDBOASIS(native Oracle Database); OCI Object Storage/hosted OCI are separate.
Storage52/52 is existing native storage evidence, not CQL or hosted-cloud proof.

## Implemented foundation

OracleDBOASIS now implements ICosmicQueryStorageProvider. Its first explicit profile
supports canonical Id equality/membership, Boolean/And/Or composition and a bounded
post-selection window. A complete authoritative Oracle readable-ID catalogue is
required from trusted host code; client-selected IDs alone must never become that
catalogue. Owner/public/native JSON-field filtering is not enrolled yet.

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
