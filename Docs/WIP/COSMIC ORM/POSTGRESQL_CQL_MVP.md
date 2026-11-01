# PostgreSQL CQL current integration

Updated 1 November 2026. Scoped restoration is in progress; native and hosted qualification for this release are not yet proved.

## Current result

- Restored four existing files from preserved work/cosmic-cql-sandbox-integration (237403a75): PostgreSQL adapter/compiler, shared bounded-identity compiler, and focused CQL controls. No broad branch/Core merge.
- All 16 focused controls pass against current Core2ac915f, zero skips. Canonical storage baselines were not rerun.
- Scope: explicit authorized catalogue (up to 100 IDs), ID equality/membership, bounded name/description equality and Contains, string metadata equality/nested paths, Boolean AND/OR and paging through one parameterized SELECT before materialization.
- No managed predicate scan, unbounded catalogue, arbitrary aliases, graph expansion, full EF parity or hosted grant is implied. PostgreSQL-compatible aliases must opt in independently.
- Current hosted provider count stays 39; implemented-qualified count stays 41 until current native qualification passes.

## Reuse and next step

1. Reuse stopped retained PostgreSQL container cee58d44cc44aace8863ca53ffa955deed78a710a9800b750a484bb478206a69, image79bd7c99e923138f136f8009d6bffa66e21e9d4fda5c0c561b00fc9c90cfe537, volume oasis-postgres-data, loopback15432. Container is present/exited; no start or mutation performed in this checkpoint.
2. Existing LocalState/COSMIC/PostgreSqlCql contains the original 15 metadata/seed/snapshot/ACK/lock files (historically1605bytes), with no pending setup files observed. Preserve actor/IDs, both cosmic_cql and cosmic_cql_content schemas, source snapshots and write ACKs. Do not reset/reseed or expose credentials.
3. Review/port the preserved native runner and its full dependencies before execution; qualify restored code with the original read-only content/restart checks, verifying before/after JSON/xmin and metadata fingerprints. Do not partially expose unsupported federation/serve options.
4. Inspect hosted placement separately: selected OASIS DNA PostgreSQL/DynamoDB/AWS sections were absent in a private read-only configuration check. That is not proof no other account/configuration exists. No new hosting or spend is authorized by this inspection.
5. DynamoDB also has preserved bounded CQL code; inspect/reuse it later, never rebuild it from scratch or relabel local DynamoDB as hosted AWS.

## Evidence

- TestResults/PostgreSqlCqlCurrent/preserved-port-controls.trx: 16 passed/0failed/0skipped, 25961bytes, SHA256d638b8943ec69b471f0e68feae0c3aeaf132c49c63c9454f3d4d66b9e830f697.
- Build log: TestResults/PostgreSqlCqlCurrent/preserved-port-controls.log; existing SDK/Npgsql/NuGet/assets reused, no restore.
- Sanitized hosted-configuration-presence.json records only section/field-presence booleans, no credential values.
- Historical native/HTTP/browser evidence remains on preserved branch237403a75 in POSTGRESQL_CQL_MVP.md and the resource ledger. Historical success is not substituted for this release's native gate.

[Current programme](CURRENT_STATUS.md) | [Provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
