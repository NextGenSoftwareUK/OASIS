# Apache Pinot CQL MVP

The Pinot broker reads exact canonical GUIDs from the dedicated
`oasis_cql_public_holons` table. It selects only the document column and
requires the public flag. Broker exceptions, partial responses,
incomplete server results, duplicate rows and inconsistent payloads
reject the entire query. Canonical tombstones and children are omitted.

The first native qualification is pending on the task branch. The Linux
job uses the official Pinot 1.6.0 image at Linux x64 digest
`sha256:a87cd2ba3dc10a08620a8a7414ef0ea161bca3d8fb73adc5cb5aefef8e255655`.
It creates a disposable offline table, ingests three public records, one
canonical tombstone and one private row, and tests 3/2/0 including an absent
key. WEB4 admission, Mongo federation and the complete storage interface
remain open.

[Apache Pinot release](https://pinot.apache.org/download/) ·
[Broker Query API](https://docs.pinot.apache.org/reference/api-reference/query-api) ·
[Batch ingestion](https://docs.pinot.apache.org/build-with-pinot/ingestion/batch-ingestion/batch-ingestion).

The official broker passed 3/2/0 through the .NET CQL adapter after
ingesting five source records with four public flags. The test passed
with zero skips and verified omission of the private row, canonical
tombstone and absent key. The 84,075-byte CI artifact SHA-256 is
`5db76972fe4693965fabd217aeb0ae4807ab305224cc791a2dea216f42b4ec8d`.
[Passing Linux run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42262964991).

## WEB4 read-only admission

Set the approved broker origin and public probe through
`CQL_PINOT_QUERY_ORIGIN` and `CQL_PINOT_QUERY_PROBE_ID`. Store the
restricted reader in `CQL_PINOT_QUERY_READER_USERNAME` and
`CQL_PINOT_QUERY_READER_PASSWORD`. The broker role is limited to the
teaching table and its `is_public = 1` row policy. Hosted verification
includes the policy marker and denial against a separate test table.

The shared protected SQL path passed 13 Pinot provider, 11 Druid
provider and 13 WEB4 checks at Core
`f3730a52d04ad8e19ce4889e4fa9dc39e50f5ad1`. All 36 protected
exact-key configuration checks passed. The Pinot provider TRX SHA-256
is `1d6e980574d714b4f203d1e22c3db3755642588523cbafd2f9e23f38b8f3dcd3`.
The WEB4 TRX SHA-256 is
`9807691ed548a68ec2f0292a53b8da44243c902c262385a3628363cecb7c23ed`.
Authenticated hosted acceptance remains open after LocalFile.
[Pinot broker ACLs](https://docs.pinot.apache.org/operate-pinot/security/authentication/basic-auth-access-control)
and [row-level security](https://docs.pinot.apache.org/operate-pinot/security/access-control).

The native 3/2/0 check passed after the shared protected SQL reader
change. The 84,024-byte artifact SHA-256 is
`c2f38179f505d5ce67896a4ef64a5c13812d1b849a34e3d190944b8b76878ead`.
[Shared reader native regression](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42268028673).
