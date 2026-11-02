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
