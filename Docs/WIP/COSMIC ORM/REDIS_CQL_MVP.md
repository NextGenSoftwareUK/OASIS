# Redis + Mongo CQL MVP - native/restart/paired/browser verified

## Current-branch integration —30 October2026

Selective port from preserved f89cf0085. Eight controls, one retained-native scenario
and one Mongo federation scenario passed with zero skips. Independent browser5/4
verified. Original Redis DUMP snapshot, seed and save acknowledgements unchanged.

```powershell
Scripts/TestHosts/Run-CosmicRedisCqlNative.ps1 -Federated -Serve
```

Open http://127.0.0.1:5113/. Run both query buttons, then stop locally and wait for
runner cleanup. Dedicated5113 avoids Aptos5111. Existing source/container required;
producer builds before consumer. Successful reports archive by hash; unresolved
failures stop a new run. No database flush, replay or reseed.

Current raw evidence under `TestResults/RedisCqlCurrent/`:

| Report | SHA256 |
|---|---|
| redis-cql-current-controls.trx (8passed) |6A36BC2ED23EE268D3E41ABB215477107B44B281288C8CA0CF17E5181BF61260 |
| redis-cql-native.trx (1passed) |8D81D7416B0C4AA50F350D732CF7CFB6ED65241BEEA7F401404329D7B02627A1 |
| redis-mongo-federation.trx (1passed) |7065D76E0A16EB5A85575F6EC797FE60A6DFD36C7E0060B2D774BAA19192D0AF |

Native browser proof: `redis-mongo-subset-current.png`. Exact temporary Mongo fixture
verified/dropped, pending/ready gone, runner exit0. Same original Redis container
confirmed exited;18679/27029/5113 absent. Retained metadata8files830B unchanged.
No download/install/new environment; shared volume and other Redis databases retained.

Public sandbox section19 verified: Web4Site maine016ee698d6216753a989e94ad3903f10f188077,
Production7452576646success. Expanded command/fixed5113link visible. Screenshot
`TestResults/RedisCqlCurrent/redis-catalogue-public.png`, SHA256
FB08A56A24D2B5BAC92AB3F34B79259210E7379B514C2ACD0D8174604C1C8F4A.
Backend remains task-branch only; no new Railway deployment. Hosted selector remains
Mongo/SQLite/Neo. Historical proof below retains its old5111 port, superseded by5113.
Query, alias, registration and full HyperDrive/parity limits below remain open.

29 October 2026. RedisOASIS implements ICosmicQueryStorageProvider over its existing
encrypted key-value backend/StackExchange.Redis transport and shared ID compiler.

## Scope

Exact Redis authority/catalogue; ID equality/membership, Boolean algebra and paging,
at most100 selected keys. Unsupported metadata/content/order/children fail before
I/O. Reads use canonical RecordKey and original encrypted backend, never scan the
keyspace to evaluate a CQL predicate. Any failure/cancellation discards all rows.
Result/post-decode4MiB budgets apply; no database-wide/distributed snapshot claim.

Derived aliases retain their actual QueryProviderType and receive no query
capabilities until separately admitted. KeyDB/Valkey/etc do not automatically gain
Redis CQL support or certification from inheritance. No central BootLoader active
Redis case was found; direct demo registration and production registration are
separate gates. No full ORM/history/HyperDrive parity claim.

## Verified controls

RedisCqlTests:8/8,zero skips,342ms. Frozen authority catalogue and caller selection,
unsupported shapes, inactive/pre-cancelled requests never resolve storage keys,
and unadmitted alias identity/capability rejection. No native Redis was started.
Report `TestResults/RedisCqlCurrent/redis-cql-controls.trx`, SHA256
`4C0DD5727D57B40550F5E8D46B2946D6D27585C6965C7B889E983D05839AC400`.

## Native/restart/paired proof and commands

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Redis -Federated
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Redis -Federated -VerifyOnly
```

Browser localhost5111. One HolonManager query reads encrypted Redis3+Mongo2=5
physical rows; selecting two source IDs yields4, with provider provenance.
Shared C# and HTTP/browser teaching files retain clear spacing/comments.

Native first admission required database11 empty and journaled the3 IDs before
saves. No existing database was flushed/adopted. CQL3/2/missing passes1/1 zero skips;
private content decrypts correctly and is absent from native DUMP data. Every native
key/DUMP hash remains unchanged by queries. Full container restart then compares
against a persisted original ciphertext snapshot and passes1/1,221ms, zero skips.
Reports under TestResults/RedisCqlCurrent:
- `redis-cql-native.initial.trx` initial1/1,571ms, SHA256
  `84BD52FF95D9CFD3E3DDD49536000795F987E3079BA1FB7DE8856E26D95FA42E`.
- `redis-cql-native.snapshot-baseline.trx` baseline1/1,231ms, SHA256
  `59E2EC029E84DB032B89A6A3E97ABF90B2F95E907FC7CD6D593D9DA85C539A30`.
- Latest `redis-cql-native.trx` preflight1/1,219ms, SHA256
  `2928E3831FD30FDEED3C1953A3AFA3AF3F1D0D65C1C11622C6F66E2E2A99EE7B`.

RedisMongoCqlNativeTests:1/1 zero skips,35s. SDK/HTTP5/4 and provenance, unrelated
Mongo identity excluded, forged actor/unknown preset rejected, inactive Redis
returns HTTP503/no rows. Browser independently verified5/4/provenance.
`redis-mongo-federation.trx`, SHA256
`28FAF34BDBD4578DD48A69D55834E8B6D9E4C51DC1E51C41B1AE41367D507B66`.
Screenshot `redis-mongo-browser.jpg` in the same TestResults directory.
Hub routing8 single/16 plans,20 paired/40 plans,26 paired rejections,3 archive
rejections passes with listeners unchanged (`hub-routing.log`).

## Retention and cleanup

Same original Redis8.2.2 container48458f978518783740065970cccbf74f6f8626a97660c625467fa561bf4d8005,
image5d79a9ce29f8b3fe50fd5c07ee1bf654b5cec557b895835825bf6825d525e4f2,
volumeoasis-redis-data, loopback18679. Retain source DB11/prefixcosmic-cql/; other
existing databases are untouched. Protected auth/storage-key DPAPI files reused
from LocalAppData/OASIS/ReusableProviderTests/Redis, never printed/copied into docs.

LocalState/COSMIC/RedisCql retains187byte seed,448byte original native snapshot,
3 save acknowledgements and locks:830bytes. Seed SHA256
`059913E2BDADF59152DE18A19FCE4E2617C1B7C3239E3B62C3E9E29A6485464C`.
Missing/partial original source must be reconciled, never reseeded. Never delete
shared volume/other databases for CQL cleanup. Retain source and original key together.

After browser shutdown same runner24190 exited0; exact temporary Mongo DB
`cosmic_cql_mongo_redis_demo` content/identity-validated and dropped, pending/ready
journals removed. Owned Mongo/Redis stopped; no18679/27029/5111 listeners. Startup
ownership is recorded before invoking the reviewed helper so partial startup failure
is also reconciled in teardown. No new runtime/download/service environment.
Generated outputs are regenerable; native source stays outside TestResults.

Continue other red provider MVPs. Production BootLoader registration and Redis-family
aliases remain separate gaps; this direct-registration demo does not certify them.

[Status](CURRENT_STATUS.md) / [inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md) /
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
