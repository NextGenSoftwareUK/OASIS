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

WEB4 admission, Mongo federation and hosted acceptance are separate.
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
