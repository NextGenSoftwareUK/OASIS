# Apache Druid CQL MVP

The reviewed read profile uses the Druid SQL API with a bound VARCHAR
parameter on a dedicated `oasis_cql_public_holons` datasource. It reads
only the document column for the exact ID and public flag. Duplicate
public rows or an inconsistent payload reject the entire query. The
canonical public record boundary suppresses tombstones and children.

The task-branch native job uses the
Apache Druid 38.0.0 binary release and pinned SHA-512 on an isolated Linux
runner. It ingests three synthetic live public rows, one public tombstone
and one private row. The test also requests an absent key and expects
3/2/0 results. The retained report will distinguish the SQL source count
from the visible CQL row count.

Mongo federation and hosted acceptance remain open.
Append-only write reconciliation and the provider's complete interfaces
remain open.

[Apache Druid 38.0.0](https://druid.apache.org/downloads/) ·
[Druid SQL API](https://druid.apache.org/docs/latest/api-reference/sql-api/) ·
[Native batch ingestion](https://druid.apache.org/docs/latest/ingestion/native-batch/).

The first Linux run verified the pinned Druid 38.0.0 version, completed
ingestion and confirmed all five source records and four public flags.
The pre-existing provider test referenced a removed `GetProviderVersion`
method and prevented test compilation. The provider now initializes its
name and description, and the identity suite tests the supported category
contract. The Linux fixture checks the native version through Druid's
`/status` API. [First Linux run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42259837438).

The second Linux run reached CQL and observed four rows. The fixture had
written the derived `isDeleted` Boolean instead of the stored
`deletedDate` field that defines a canonical holon tombstone. The seed
now uses `deletedDate`, and a controlled check covers tombstone and
absent-key omission. [Second Linux run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42260416387).

The third Linux run passed. Druid 38.0.0 ingested exactly five source
records and four public flags. The real CQL query returned 3/2/0 after
excluding the private row, tombstone and absent key. The single test
passed with zero skips. The retained CI artifact is 2,090 bytes, SHA-256
`eb843cdd3d97485d006bf913d54ff7cd3ad0c808bc43528d7444f368357ece0b`.
[Passing Linux run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42261110911).

## WEB4 read-only admission

Set the reviewed origin and probe in `CQL_DRUID_QUERY_ORIGIN` and
`CQL_DRUID_QUERY_PROBE_ID`. Keep the restricted Basic credentials in
`CQL_DRUID_QUERY_READER_USERNAME` and `CQL_DRUID_QUERY_READER_PASSWORD`.
The HTTPS transport sends credentials only to `/druid/v2/sql` at that
origin. The Druid reader must have only `DATASOURCE READ` on
`oasis_cql_public_holons`. Verify denial against a separate test table
before hosted use.

The provider passed 11 checks. WEB4 passed 22 checks including the
Safe, Proof of Humanity and Oracle Object Storage regressions. Both
ran at Core `ac8c6409421017f80f40242a50482029df11ea20`. Provider TRX
SHA-256 `124b31dcd751b35143358186dc3d863a406a61d4675d8e802368939b6ebd3fef`.
WEB4 TRX SHA-256
`e9292152d178c976b5f28fe4d8915fc5368fb9a15165ba4f0bc1ed0f93f7150f`.
All 36 protected exact-key configuration tests passed.
[Apache Druid authorization](https://druid.apache.org/docs/latest/operations/security-user-auth/).

The Linux 3/2/0 test also passed after adding the protected reader path.
[Native regression run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42264837608).
