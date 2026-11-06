# Apache Druid CQL MVP

The reviewed read profile uses the Druid SQL API with a bound VARCHAR
parameter on a dedicated `oasis_cql_public_holons` datasource. It reads
only the document column for the exact ID and public flag. Duplicate
public rows or an inconsistent payload reject the entire query. The
canonical public record boundary suppresses tombstones and children.

The first native qualification is pending. The task-branch job uses the
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
