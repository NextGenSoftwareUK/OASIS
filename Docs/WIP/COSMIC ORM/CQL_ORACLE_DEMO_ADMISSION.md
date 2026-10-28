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
No native CQL test has run yet; these do not certify Oracle SQL runtime participation.
Raw proof:TestResults/DemoBreadthCurrent/oracle-cql-unit-regression.trx
SHA2564AE87058D9B651EFA6FF0C3B81A5D22F36B7A77F974496A2C00599E6563A9C4C.
Unit assets regenerated from local cached packages only; no download/install/container.

## Exact next step

Reuse retained oasis-oracle-free/image358b32468eddc329751c28007a535cbc661f1090ab5d8a9f02267cf44ef1e3b9,
oasis-oracle-data/11521 and existing Podman secret. Inspect pending original fixtures
before writes; never reprovision/reset the schema/container/identity. Extend the
existing reusable runner with a bounded CQL mode and journal-owned3-record fixture.
Prove native all/subset/paging, access exclusion, canonical history/provenance,
cancellation/source failures/no partials and exact cleanup. Then Mongo pairing and
readable C#/HTTP/browser preset. Richer business fields/scalars remain open, not EF parity.

Native contract reference:[OracleDbType RAW](https://docs.oracle.com/en/database/oracle/oracle-database/21/odpnt/OracleDbTypeEnumerationType.html).
Hosted Azure separately requires free-account setup; user has no free subscription.
