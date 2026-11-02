# Bittensor public chain-header demo scope

Updated 2 November 2026. Native readiness and C# HolonManager/LINQ CQL passed; CLI and local HTTP/Mongo pass. Hosted deployment and sandbox entry remain
unverified. Totals61integrated query types/56hosted functional proofs/5browser verified;
the extra integrated type is currently on the task branch, not deployed.

## Selected interface and scope

[OnFinality's operator documentation](https://www.onfinality.io/en/networks/bittensor-finney)
publishes anonymous HTTPS `https://bittensor-finney.api.onfinality.io/public`
and5RPS limit. This probe used one request per second, no API key/account.
[Official Bittensor docs](https://preview.bittensor.com/docs/concepts/network)
identify Subtensor as a Substrate chain. The independent
[polkadot-js registry](https://github.com/polkadot-js/common/blob/9281daa14c2c2a41708c3489de8770a064525577/packages/networks/src/defaults/genesis.ts)
pins Finney genesis
`0x2f0555cc76fc2840a25a6ea3b9637146806f1f44b090c175ffde2a7e5ab36c03`.
Registry source was cached and checked after qualification against the same
previously selected constant; this did not change/replay the native request.

Existing BittensorOASIS targets a legacy Taostats path; its current
[operator RPC docs](https://taostats.io/docs/api-reference/rpc-live/post-rpc-http)
require an API key. Legacy code unchanged; this anonymous Subtensor scope is
separate. No paid account, node, SDK, wallet, signing or AI request was created.

## First native readiness evidence

`Scripts/Sandbox/Probe-BittensorPublicReadiness.py` reused the existing offline
Polkadot SCALE/Blake2b codec without changing/invoking its network probe.
4tests in `test_bittensor_public_probe.py` passed: valid ancestry; wrong anchor,
tampered content, parent/count mismatches; strict RPC id/errors; duplicate JSON
and noncanonical height rejection. These are qualifier controls, not CQL tests.

Exactly5HTTP200responses: genesis, reported finalized head, three parent-linked
headers. All three hashes recomputed locally; heights consecutive. Total native
response bytes2169. No retries, block bodies, storage scans or metadata downloads.
The finality anchor is node-reported: no GRANDPA proof, independent consensus,
AI inference, miner/subnet metrics, balances or native writes are certified.

Exclusive consumed intent/ACK under
`LocalState/COSMIC/BittensorPublicQualificationCurrent/`;5raw responses
are hash-matched and retained in `native/`. Raw reports also at
`TestResults/BittensorPublicSourceCurrent/` (6files/6237bytes at qualification).
Do not delete intent or run this as a fresh qualification to recreate green.

| Durable artifact | Bytes | SHA256 |
|---|---:|---|
| `qualification.intent.json` | 347 | `6397ebceac1c400e1b2e8964f5491b82d87697cf4380d73e7a8999f17a2d0f24` |
| `qualification.json` | 4068 | `26bcc6e7d643597d8b3910ef516fe2b40d32eff150671cca6d550cd08c020c27` |
| `registry-genesis.ts` | 7981 | `cecf733de00b6f8d681e912cd5a2190026a20943e7cece7086b24450c784ac74` |

Per-response SHA256 values are retained in the qualification ACK. No raw evidence
is committed to Docs. Environment/retention recorded in the existing ledger.

## Read-only CQL adapter milestone

`BittensorPublicHeaderMetadataSource` implements `BittensorBlockHash` metadata
equality/IN with bounded paging over1â€“8configured keys. It checks configured
Finney genesis, SCALE/Blake2b header hashes and RPC-reported canonical finalized
height membership. Unknown keys fail before network, malformed/partial native
reads fail the whole result, and Take(0) cannot hide a source failure.

Guarded factory transport,256KiB/response,15s linked per-request deadline,
cancellation and a shared one-request-per-second gate bound public RPC use.
Stable IDs bind projection owner/Bittensor provider/genesis/native block hash.
Public Name/Description and whitelisted header metadata include an explicit
node-reported policy and `BittensorConsensusVerified=false`. Holon Save/Delete
are rejected at the boundary without native requests or input mutation.

23controlled adapter cases pass, including four independent SCALE width
vectors, tampering/network/canonical-history/envelope/size errors, paging,
unknown keys, cancellation before/during reads, endpoint validation and writes.
1first native CQL test passes in24seconds: actual developer walkthrough
`Examples/CosmicCqlDemo/BittensorPublicWalkthrough.cs`, linked into the provider
test host, returns3then2via shared HolonManager/LINQ. This is not merely a
qualifier or mocked transport. Source keys are exactly the original readiness
three; one persistent projection namespace is retained. No readiness replay.

Retained `LocalState/COSMIC/HostedBittensorPublicSourceCurrent/source.json`,
exclusive `native-cql.intent.json` and `native-cql.ack.json` bind original source
and first CQL test. ACK SHA256:
`2b95078006005145625dc20c425aa04f9865d38f82e8c0b87f16e89d33d5bc11`.
Raw TRX/logs: `TestResults/BittensorPublicSourceCurrent/adapter.trx` (23pass),
`native-cql.trx` (1pass). Per-file hashes are retained in ACK; no raw evidence
committed to docs. Reusable cache-only runner:
`pwsh -NoProfile -File Scripts/Sandbox/Test-BittensorPublicMetadata.ps1`.
Do not repeat its -Native mode: acceptance intent is consumed.

## CLI / local HTTP integration milestone

Actual `CosmicCqlDemo --bittensor-public <source.json>` returns3/2. Core1/API3
read-admission tests and9pure publisher tests pass. First actual local HTTP
cohort returns native3/2, Mongo-paired5/4, foreign-private0/public3. Two original
SDK-owned version1private copies and9receipts verified; exact owned fixture
cleaned and Mongo stopped. This is not replication/production-billing acceptance.

Core source commit `d3f92df63ffea195ca83f057246387b03b9f3068` pushed/remote
verified; parent gitlink and manifest advance together in this batch. Full API
and actual demo project builds pass. CLI return-type compile mistake corrected
before its first native run; original failed build log preserved. First HTTP
discovery skipped before fixture creation due a wrong copied environment key;
corrected before the first actual native cohort, skipped result retained.

Protected hosted candidate prepared only:3values/four native+Mongo presets,
DNA unchanged. Actual exported query inputs pass4shipping compiler/capability
plans in1test. Initial full audit-review input shape was rejected; runner now
exports the two exact public input fields without altering the full review.
No native replay, service mutation, activation or deployment occurred.

Durable `local-integration.ack.json` SHA256:
`1b751fca989d31f6eb779d0c13e33b800c90bf36155b9c48a7c16b4253d12509`;
accepted original TRX compressed under `HostedBittensorPublicSourceCurrent/evidence/`
(5509bytes). CLI/config/local qualification ACKs retained beside source.json.
Protected candidates `HostedSourceRolloutCurrent/bittensor-public-` before193894
and candidate196982bytes. Raw test counts/hash paths remain outside docs.

## Exact next step

Commit this verified integration batch, validate parent gitlinks/manifest, merge
verified Core/parent work into Development and observe the exact deployment.
Only after matching successful code, apply the frozen3value candidate once with
per-key ACKs and full observed equality, then first hosted native/Mongo/reuse/
privacy/replica cohort and independent audit. Publish the sandbox card and
pinned guide/CLI links after that proof. Never replay accepted local/readiness
cohorts or change source owner/keys for green.

[Developer guide](../../Devs/BITTENSOR_PUBLIC_HEADER_CQL_GUIDE.md). Hosted/browser
remain open.188completed generated folders/2,154,947,374logical bytes retained:
automatic approval review rejected cleanup as blocked by policy; no retry or
bypass. Exact paths/sizes are in the resource ledger. No David action needed.
