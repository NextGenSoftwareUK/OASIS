# PostgreSQL CQL current integration

Updated 1 November 2026. Scoped restoration now passes native and local paired HTTP checks. Hosted admission/placement and fresh-checkout setup remain open.

## Current result

- Restored four existing files from preserved work/cosmic-cql-sandbox-integration (237403a75): PostgreSQL adapter/compiler, shared bounded-identity compiler, and focused CQL controls. No broad branch/Core merge.
- All 16 focused controls pass against current Core2ac915f, zero skips. Restored native content17cases and paired SDK/loopback HTTP2/4/4/2 plus identity5/4 pass. Canonical storage baselines were not rerun.
- Scope: explicit authorized catalogue (up to 100 IDs), ID equality/membership, bounded name/description equality and Contains, string metadata equality/nested paths, Boolean AND/OR and paging through one parameterized SELECT before materialization.
- No managed predicate scan, unbounded catalogue, arbitrary aliases, graph expansion, full EF parity or hosted grant is implied. PostgreSQL-compatible aliases must opt in independently.
- Current integrated query-type count is42; hosted remains39. Local loopback HTTP uses the retained synthetic owner/catalogue, not hosted visitor authentication, production billing or browser verification.

## Reuse and next step

1. Reuse stopped retained PostgreSQL container cee58d44cc44aace8863ca53ffa955deed78a710a9800b750a484bb478206a69, image79bd7c99e923138f136f8009d6bffa66e21e9d4fda5c0c561b00fc9c90cfe537, volume oasis-postgres-data, loopback15432. Container is present/exited; no start or mutation performed in this checkpoint.
2. Existing LocalState/COSMIC/PostgreSqlCql contains the original 15 metadata/seed/snapshot/ACK/lock files (historically1605bytes), with no pending setup files observed. Preserve actor/IDs, both cosmic_cql and cosmic_cql_content schemas, source snapshots and write ACKs. Do not reset/reseed or expose credentials.
3. Complete preserved runner/dependencies now restored and tested once with -Content -Federated. Original PostgreSQL JSON/xmin snapshots and all15metadatafiles/1605bytes remain unchanged. Temporary3record Mongo fixture verified/dropped, no pending journals; owned Mongo stopped and PostgreSQL exited,15432/27029/5114 absent. Runner refuses missing original provenance before activation or any seed branch. Preserve source; do not rerun these green gates without a code change/bug.
4. Inspect hosted placement separately: selected OASIS DNA PostgreSQL/DynamoDB/AWS sections were absent in a private read-only configuration check. That is not proof no other account/configuration exists. No new hosting or spend is authorized by this inspection.
5. DynamoDB also has preserved bounded CQL code; inspect/reuse it later, never rebuild it from scratch or relabel local DynamoDB as hosted AWS.

## Evidence

- TestResults/PostgreSqlCqlCurrent/preserved-port-controls.trx: 16 passed/0failed/0skipped, 25961bytes, SHA256d638b8943ec69b471f0e68feae0c3aeaf132c49c63c9454f3d4d66b9e830f697.
- Build log: TestResults/PostgreSqlCqlCurrent/preserved-port-controls.log; existing SDK/Npgsql/NuGet/assets reused, no restore.
- Sanitized hosted-configuration-presence.json records only section/field-presence booleans, no credential values.
- Historical native/HTTP/browser evidence remains on preserved branch237403a75 in POSTGRESQL_CQL_MVP.md and the resource ledger. Historical success is not substituted for this release's native gate.

[Current programme](CURRENT_STATUS.md) | [Provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Current native/local demo

Run in this assigned worktree only, against the retained source:

```powershell
Scripts/TestHosts/Run-CosmicPostgreSqlCqlNative.ps1 -Content -Federated -Serve
```

Local page: http://127.0.0.1:5114/. Four content presets return2/4/4/2 physical copies; ID presets return5/4. The runner preserves original PostgreSQL source and removes only its journal-owned temporary Mongo fixture after completion. This is not a fresh-checkout bootstrap: missing original provenance fails rather than inventing source identities. The completed bounded run omitted -Serve; browser interaction was not repeated.

## Next placement work

David explicitly deferred hosted PostgreSQL on1November2026: "Leave hosted PostgreSQL pending". No owner setup or new paid resource is needed now; continue other demo providers and do not re-ask. Local native/SDK/HTTP proof stays valid in its declared scope. Revisit hosted placement/admission/trusted catalogue/visitor acceptance only when David resumes it; never call the local demo hosted PostgreSQL.

- TestResults/PostgreSqlCqlCurrent/postgres-content-native.trx:1passed/0failed/0skipped, 2783bytes, SHA256546a6bd65f62b9e7d170d96d51a05ed8e02cda3ffa8a4ba0a6b0fcea287d4fb2.
- TestResults/PostgreSqlCqlCurrent/postgres-content-mongo-federation.trx:1passed/0failed/0skipped, 11478bytes, SHA2564b51c1eecf189d1ff8877810c7f0d955750fa798cc0c79a9c1b549fc8439fa4f.

Hosted placement observation: the OASIS Railway project inventory lists9services and no PostgreSQL service. David answered the existing central action once: leave hosted PostgreSQL pending. Its status is now explicitly deferred. No paid resource, account, database or setting was created. Continue the other demo providers.
