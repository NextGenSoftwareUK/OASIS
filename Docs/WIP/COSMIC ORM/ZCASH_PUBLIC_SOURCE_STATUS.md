# Zcash public block-summary demo qualification

Updated 2 November 2026. Native read readiness passes; CQL adapter, manager/CLI,
HTTP admission, hosted/Mongo acceptance and sandbox card remain unimplemented.
Counts unchanged:61integrated types/57hosted proofs/5browser verified/52open.

## Selected public interface

[ZecBlock's operator documentation](https://zecblock.com/docs) explicitly allows
account-free production reads at `https://api.zecblock.com/v1`. Public blocks
support bounded height filters, order and cursor pagination; previews/paid/private
routes are separate. No account, token, preview header or paid service was used.
The old [ZcashBlockExplorer service](https://www.zcashblockexplorer.com/) states
it shut down in2024; it was not selected or probed. No alternate endpoint tried.

Pin [Kenbak/zecblock116b8a72](https://github.com/Kenbak/zecblock/tree/116b8a72f4d0aac6bafeecac34422246a75cc8ed).
OpenAPI exposes the v1envelope/network/units/filters; its broad domain schemas
are supplemented by the actual route, v1adapter, SQL JSON projection and manifest
at that pin.5source files/1,019,086bytes retained in sdk/, no SDK/node install.
Mainnet genesis hash is cross-checked against the upstream
[Zcash chain parameters](https://github.com/zcash/zcash/blob/master/src/chainparams.cpp),
not learned from the selected indexer:
`00040fe8ec8471911baa1db1266ea15dd06b4a8a5c453883c000b031973dce08`.

## First native readiness

`Scripts/Sandbox/Probe-ZcashPublicReadiness.ps1` uses the existing PS7/.NET HTTP
client without redirects, auth or custom User-Agent.3fixed GETs,>=1s spacing,
15s linked timeout and256KiB per response. No dependency restore/new environment.
`Validate-ZcashPublicReadiness.py` validates each response offline before the
next native request.7controlled tests and PS syntax check pass: network/genesis,
metadata projection, exact keys/counts, errors/private payloads, lossy numbers,
duplicate JSON and full UInt32 timestamps beyond signed Int32.

| Native GET /v1/blocks selection | HTTP | Bytes | Result |
|---|---:|---:|---|
| min_height0/max_height0/limit1/orderoldest | 200 | 984 | Mainnet envelope and pinned genesis match |
| min_height1/max_height3/limit3/orderoldest | 200 | 1386 | Exactly heights1,2,3 |
| min_height1/max_height2/limit2/orderoldest | 200 | 1110 | Exactly heights1,2; same records as first selection |

All3responses declare freshness `unknown`; this is preserved, not called fresh
or independent network availability. Hashes/heights/timestamps are node/indexer-
reported; genesis identity matches, but no Equihash/PoW/chain-finality proof or
shielded-wallet/private-note verification is claimed. Only public block-list
metadata was requested; no transaction detail/body or shielded wallet route.

Exclusive consumed intent/ACK/manifest/native copies under
`LocalState/COSMIC/ZcashPublicQualificationCurrent/`.3raw responses3480bytes
hash-match report digests and are retained in native/ to survive cleanup.
Raw initial reports also TestResults/ZcashPublicSourceCurrent. Never remove the
intent or repeat qualification to recreate green. No source owner, fixture,
Mongo session/copy, permissions, tariff, hosted settings or card exists yet.

| Durable artifact | Bytes | SHA256 |
|---|---:|---|
| `qualification.intent.json` | 243 | `a789aabe7e758cb5d70b541d721e9ba62735989681870b3bfad2a2a775e6f1a4` |
| `qualification.json` | 2875 | `b8b03ae09872d0796e48611a76069091ea6c5a4edbd3a220fadf8d7ac71ac215` |
| `qualification.acknowledged.json` | 422 | `7404cc06779801bfe3637a438003ce0b541a11bcf75f38c19d9b1c75b27beb42` |
| `sdk/manifest.json` | 977 | `ed47ce67164b85ab6f7c7926457284585db35ad6be054c09f5910d7e3ecfb085` |

## Exact next step

Implement a read-only `ZcashBlockHash` equality/IN+paging source over1–8explicit
configured hash-to-height mappings. Use the summary list endpoint with min/max
height equal to each selected height and limit1; never fetch a transaction-body
endpoint or scan the full chain to answer hash filters. Revalidate mainnet/
genesis and exact expected native hash at each mapped height; no silent canonical
replacement or partial rows. Stable IDs bind owner/provider/endpoint/genesis/
native hash. Whitelist height/hash/UInt32 timestamp, Name/Description and honest
indexer/unknown-freshness/no-PoW-verification labels. Save/Delete must be refused.
Add provider tests and actual manager/C#/CLI3/2 before local Mongo/HTTP3/2+5/4,
then guarded hosted admission/configuration, original cohort/audit and sandbox
card. Keep existing RPC wallet/bridge code unchanged; it is not this readonly
profile's acceptance. No full node or account setup is required for the next step.

## Adapter controlled milestone — 2 November 2026

Read-only ZcashPublicBlockMetadataSource builds using cached dependencies. Dedicated MSTest filter passed **22/22**, zero skipped/failed (782 ms). Covers point/IN queries, pagination and budget; genesis/hash/height/network/units/timestamp validation; duplicate and oversized responses; HTTP403/429/500 with no retry; cancellation; zero-limit failure visibility; inactive/unknown keys before network; write rejection without mutation. Native CQL and hosted integration remain unverified. Evidence: `TestResults/ZcashPublicSourceCurrent/adapter.trx`, SHA256 `8be1dee42b807217ae7118ea04339b7cb62c1dea896c041378a958547bdc3eb0`; regenerable controlled tests, not a replay of qualification. Next: configuration controls and HolonManager/CLI demonstration.

Configuration follow-up:27/27 pass (zero skipped), including invalid endpoints, unknown/duplicate fields, negative height and uppercase hash rejection. Evidence TestResults/ZcashPublicSourceCurrent/adapter-config.trx SHA256 76dd4c89a989a9576211f809f4594345387e8925b2bfe7226f71957dd8662655. C# route --zcash-public and shared metadata key mapping added; full demo build running, native execution not yet accepted.

Full shared demo cached restore/build passed (2m01s); --zcash-public compiles through HolonManager/shared LINQ walkthrough. No native execution yet.

## Real manager/LINQ/CLI acceptance — 2 November 2026

Original compiled --zcash-public walkthrough passed3full/2filtered native rows, matching exact preserved heights1/2/3 and hashes. Source configuration uses one persistent projection owner; no account, wallet, transaction or Mongo save. Anonymous HTTPS genesis/mainnet/hash checks execute inside the adapter. Freshness remains indexer-reported unknown; no independent consensus proof. Durable source/consumed intent/ACK/compressed output: LocalState/COSMIC/HostedZcashPublicSourceCurrent. Output SHA256 `49a75c08519986410a0079e0ec96ae3d6300247ae8e1bcc18b7c1478e1d52b9d`. HTTP/API/compiler/tariff/federation/hosted/browser gates remain unverified and counts61/57 unchanged.

## HTTP admission controls — 2 November 2026

Explicit CQL_ZCASH_PUBLIC_SOURCE_JSON registration, compiler provider scope and independent read tariff added.3/3API tests and1/1Core tariff test pass, zero skipped. Four native/Mongo3/2 presets plan; missing permission/tariff and write tariff rejected. No deployment/settings changes or local native HTTP/Mongo cohort yet. Core task1e9b2409897eac62a1dd2eade3840e000e4ef87f pushed and verified remotely.

Evidence `TestResults/ZcashPublicSourceCurrent/api-admission.trx` SHA256 `ab133508c911d65fb94db468dccbce1558546eeb92f55d2eaf0e35df87c0b767`.

Evidence `TestResults/ZcashPublicSourceCurrent/core-tariff.trx` SHA256 `361b2263c745ede8b686bca43f7374d46eb84fd23da0aa66b440e495b2c56615`.

## Original local HTTP/Mongo acceptance — 2 November 2026

Actual loopback shipping CQL endpoints pass native3/2 and Mongo-paired5/4. Foreign avatar sees0private copies and3public native records. Two SDK-owned version1 saves and9query receipts verified; this does not certify production billing or HyperDrive replication. One native test passed in26s, zero skips. Exact ownership-marked cosmic_cql_zcash_http_demo database removed after consumers stopped; fixture.cleaned.json retained and owned Mongo process stopped. Original source unchanged. Durable web4-local-qualified.acknowledged.json and compressed evidence manifest in HostedZcashPublicSourceCurrent; fixture journal in ZcashMongoHttpCurrent. No replay/reseed permitted. Hosted counts remain61/57.

`TestResults/ZcashPublicSourceCurrent/mongo-http-native.trx`: 3353bytes SHA256 `81c28f2ac496d887196c83928114b5d990712bc7605b4ad99eacfecfba291fc2`.

`TestResults/ZcashPublicSourceCurrent/zcash-mongo-native-summary.json`: 223bytes SHA256 `4a6cf2a3701d1983cb46cfe9ec96c65212c36ec53fe28d9f7a64ff8c816bf0d2`.

Developer guide: [Zcash public block CQL](../../Devs/ZCASH_PUBLIC_BLOCK_CQL_GUIDE.md). Next: hosted candidate, publisher controls, code release and exact hosted acceptance before browser card.

## Hosted candidate milestone - 2 November 2026

Protected live baseline/candidate prepared: exactly3changed values, free tariff,4presets, DNA unchanged.9publisher controls pass; actual shipping compiler test passed1/1 for4presets. No settings application/deployment yet. Development advanced to0df90e062/Coree920d14 (Worldscape); merge/preserve before combined verification. Recursive fetch reports unrelated unavailable HoloNET-ORM ref85287e59; inspect deployment relevance.

Evidence TestResults\ZcashPublicSourceCurrent\prepared-presets.trx SHA256 9ca97706f62f34339bd1355c2e5d33c3238929fe20905127e72ea20c53743d04.

Combined Development merge4e1f712c2/Core5b47c0b preserves Worldscape.11API tests (new Worldscape plus Zcash) and1Core tariff pass, no skips. Manifest/gitlinks7Dockerfiles pass. Generic hosted verifier now accepts Zcash; no hosted cohort created yet.

Evidence TestResults\ZcashPublicSourceCurrent\combined-api.trx SHA256 cae75dea4d114f615505e62222962f438b48537966902e27ee279062ae93bc79.

Evidence TestResults\ZcashPublicSourceCurrent\combined-core.trx SHA256 5cc2899e5d0c67be3b391842041c452de39021d734c964fb67ca1aaf1e6a7d2a.

Development publication: parent9e98a4278c91a9d01caaabdfbed447d38120a6f2/Core5b47c0b60b027092e51e486050b898ba147a4194 pushed and ls-remote verified. Code job624285a4-15a7-419d-8dad-e8d8daf90bc8 observed QUEUED. Original intent/job tuple retained HostedZcashReleaseCurrent. Follow same job; no restart/new build or source settings before exact SUCCESS.

Independent hosted audit implemented with2controlled tests/16negative scenarios passing (no network). Verifies native ownership/source/hash/height/genesis/UInt32timestamp/freshness/proof claims, selected keysets, private owner/version,5/4federation, exactSQLite/Neo4j replicas, foreign0/3visibility,2acknowledged normal saves/repeat0new/pending0. Original code deployment624285a4 remains authoritatively QUEUED; do not infer failure or start another job. No hosted Zcash cohort/settings yet.

Reporting reconciliation: Zcash counts as the62nd locally integrated query type (55bounded profiles+3canonical+4local cloud/database implementations). Hosted functional/tariff counts remain57; browser5/52open. Earlier61/57 was the pre-Zcash integrated total, not a claim that local acceptance was absent. Source code remains in queued deployment; no settings activation.

## Shared C# federation demo regression

Reproduced actual failure: NativeKeySetWalkthrough.RunFederatedAsync rejected Zcash before planning because its historical six-provider list was stale. Removed provider-name admission in favor of distinct providers and the existing required native-membership capability/planner. Same controlled test now passes4physical rows (3Zcash+1controlled Mongo), exactly1Mongo query. No live accepted cohort replay. Original red/green logs/TRX retained under TestResults/ZcashPublicSourceCurrent. This is a reproduced demo fix, not green-provider hardening.

TestResults\ZcashPublicSourceCurrent\federated-demo-red.trx SHA256 1154ac40007e9b1826ddaa6a6e429b635b89b0b96e35bfca08a1e9f5b9d72756.

TestResults\ZcashPublicSourceCurrent\federated-demo-green.trx SHA256 dc2c9ded850a50a32e38b01b07d6b5336de087eeea54228873c4e8baaa0853bd.

## Hosted settings activation

Code job624285a4 SUCCESS at9e98a4278. Prepared source/read admission/examples3values applied exactly once through publisher, per-key ACKs/exact complete candidate equality/DNA unchanged. Settings ACK/activation intent/protected submission/job tuple retained HostedZcashReleaseCurrent. One activation66d2ccea-7d77-4f76-816a-12ed810fe6ed BUILDING, same exact code. Do not resubmit or create cohort before SUCCESS. Hosted functional counts remain57; Zcash not yet accepted.

Reproduced publisher bug: Python boolTrue compared equal to configured integerheight1; original regression failed before fix. Added explicit integer-type validation,10publisher tests now pass. Protected original applied candidate revalidated equal after fix; no configuration change, reapplication or deployment. Red evidence TestResults/ZcashPublicSourceCurrent/publisher-boolean-red.log. Activation66d2ccea remains BUILDING, hosted cohort not started.

## Hosted acceptance complete - 2 November 2026

Activation66d2cceaSUCCESS at9e98a4278. Source transitioned fromstartupNOT_READY toavailable4presets; reused original session, no native/private dispatch before readiness. Original hosted run and independent audit pass: native3/2, Mongo5/4, repeat0new/2existing, foreign0private/3public, exactSQLite/Neo4j2each.2normal APIversion1saves acknowledged/pending0. No continuation/reseed/replacement owner. Cohort/source/config/intents/ACKs frozen. Functional source proof, not fullORM/nativewalletwrite/reliability/browser certification. Counts62integrated/58hosted/5browser/53open. Next sandbox card/publicasset verification.

ZcashOASIS-native-http.json:14515bytes SHA256 `1cde3624b4e259b3df6fdd3ca01a2580706d22bd90f3b7e0a6f4ab742e519bb8`; compressed original retained HostedQueryAcceptance-ZcashOASIS.

ZcashOASIS-client-http-flow.json:38239bytes SHA256 `67eeae24490e719f1292f5c2ce41aab68416017117fdaa88ed114a5e4d0c647a`; compressed original retained HostedQueryAcceptance-ZcashOASIS.

ZcashOASIS-isolation-http.json:5581bytes SHA256 `eeb9e894732b3b3ce222a54a0f26ae8e22c7c213104cfef762316dc5be1716b5`; compressed original retained HostedQueryAcceptance-ZcashOASIS.

## Sandbox card publication

Website main/task3141cca9491d585ed0b1c8aab7a52c7dd5bbc948 publishes cql-zcash-catalogue with native/Mongo preset directions, spaced local command and pinned guide/C# links. HTML parser/uniqueDOMID checks pass. Public HTML199130bytes SHA256583e921e1fa2901a6737e63c7c8ba095235bd84d73e2336f6d411e81d8e6dd8c and both unchanged JS assets match exact Git blobs. No JS changes/repeated green tests/browser interaction. Publication ACK retained HostedZcashPublicSourceCurrent. Hosted cohort remains frozen. Browser/reliability/fullORM gates remain open.

11 November UTC: ZcashOASIS public block-summary presets returned 3 full and 2 filtered rows in the live Development WEB4 CQL Playground. Original-owner paired browser verification remains open.
