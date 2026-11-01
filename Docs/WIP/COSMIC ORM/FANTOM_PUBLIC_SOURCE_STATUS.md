# Fantom Opera public-source qualification

Updated 1 November 2026. Protocol qualification passes for an explicit latest-canonical-at-read contract. CQL controls, manager/LINQ native3/2 and actual console3/2 pass. Hosted enrollment remains open; one new local-qualified type makes programme45integrated/40hosted.

## Native evidence

- Official RPC https://rpcapi.fantom.network/ confirms chain250 (`0xfa`) and genesis0x0000000000000000c20dbfb2ec18ae20037c716f3ba2d9e1da768a9deca17cb4.
- Original finalized-tag probe failed with JSON-RPC-32602/HTTP200. Original intent and three wire responses remain preserved; no runtime retry/fallback.
- Separately selected latest-canonical-at-read probe passes7requests: chain/genesis, latest header, three EIP-1898 balances at one hash with requireCanonical=true, then exact-height/hash recheck. Balances remain exact UInt256 quantities; no source write/transaction/body scan.
- Stable source LocalState/COSMIC/HostedFantomPublicSourceCurrent/source.json created once only after the7request qualification. Retain owner/addresses and both original probe intents.
- Raw latest proof TestResults/FantomPublicSourceCurrent/LatestCanonical/qualification.json, SHA256846464a7d387ab8bb30040fe601b7bc3666745388353cd71c51a37fc5de6af82. This is protocol qualification, not yet a manager/HTTP/browser certificate.

## Verified implementation

- FantomPublicAccountMetadataSource binds the shared EVM engine to immutable latest-canonical-at-read policy. JSON cannot choose finality or override network identity. Existing profiles retain finalized defaults and the original seven-parameter descriptor constructor; no binary shim.
- All selected balances use EIP-1898 requireCanonical at one captured hash. Exact height/hash is rechecked before any page is published; a changed hash or height fails without retry/partial results. Metadata and console explicitly label latest-canonical-at-read, with no finality/independent-consensus promise.
-278controlled EVM tests pass, zero skips, including21Fantom cases, reorg/height errors, precision and the preserved constructor contract. Existing native cohorts were not rerun. Source code/runtime outputs were synchronized under one lease.
- New manager/LINQ native test1pass/0skips and actual console3/2 pass. Reproduced copied finalized console label corrected, with native output assertion. Retained original stale-consumer, missing copied source-variable and misleading initial console evidence; corrected reports have distinct filenames. No source records written.

| Current proof under TestResults/FantomPublicSourceCurrent | Result | SHA256 |
|---|---|---|
| controlled-stable-contract.trx | 278pass/0fail/0skip | 4bfbfdc6ca0c152ee0b741efc84e0d1d792b1fa6a07735642a0592961ad7f995 |
| fantom-native-presentation-fixed.trx | 1pass/0fail/0skip;3/2 | fe19863cc115d32a7f835777115c8da331d2aa03ac80c541bb8caab8ee197cb2 |
| console-presentation-fixed.log | actual CLI3/2; canonical-at-read | 2cd86d06f63252b1c5215b0dd355c58014533dd648f46033d87fe9694176a9a1 |

Source source.json322bytes/SHA25686b7ba36bc665a74cd77fa0469c6276156c95967a480e9eadbb1aa09389a5550; durable native-qualified.acknowledged.json binds current proof to it. Preserve owner/addresses and both original diagnostic intents.

## WEB4/local HTTP checkpoint

-40controlled permission/tariff/discovery/preset regressions pass0skips. New explicit CQL_FANTOM_PUBLIC_SOURCE_JSON registration does not alter canonical wallet DNA or default routing.
- Local shipping HTTP1test passes0skips: native3/2, Mongo5/4, foreign-private0/public3,2SDK-owned private saves/9query receipts. HTTP asserts latest-canonical-at-read metadata on every returned source/copy. Exact ownership/count checks precede verified DB deletion; Mongo stopped/27029absent.
- Durable web4-local-qualified.acknowledged.json binds both results to the unchanged source. Native/HTTP ACKs keep separate proof fields; no replay of original fixture.
- Raw web4-controls.trx66591bytes/SHA256650c34c1c86ebae5912592c9c8ee8ac7076ff08f62a9b191a3389591f5ce722c; mongo-http-native.trx3359bytes/SHA256f54b02961294e213c905af4aa75636bf39f6694df1dce10d4b7b8c8308cfea5d, under TestResults/FantomPublicSourceCurrent.
-8pure publisher controls pass. Protected fantom-public before/candidate snapshots prepared once; public prepared-review.json contains exactly3changed keys/4free30-second presets, original source/DNA/unrelated values preserved. No live mutation yet.

## Next

WEB4/local HTTP is qualified. Fetch/integrate current Development (new OLAND checkout quote changes must be preserved), verify merged controls, then publish the exact code before applying the reviewed3settings once. Complete first hosted native/federation/reuse/privacy/replica cohort before adding sandbox card. Counts45integrated/40hosted/browser5/open35; no hosted Fantom or full ORM claim yet. [Local C# guide](../../Devs/FANTOM_PUBLIC_ACCOUNT_CQL_GUIDE.md).

## Current primary references

[Fantom official RPC endpoints](https://docs.fantom.foundation/build-on-opera/api/public-endpoints), [Sonic Labs23June2026 reversal: Opera stays through at least year-end](https://www.soniclabs.com/blog/the-first-1/), [EIP-1898 canonical hash reads](https://eips.ethereum.org/EIPS/eip-1898). The June announcement supersedes the April shutdown plan; Sonic chain146 must never be substituted for Opera250.

[Programme](CURRENT_STATUS.md) | [Provider matrix](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Resources](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
