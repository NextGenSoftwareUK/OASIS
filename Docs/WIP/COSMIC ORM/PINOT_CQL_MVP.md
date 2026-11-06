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
