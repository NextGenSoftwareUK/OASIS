# Bittensor public chain-header demo scope

Updated 2 November 2026. Native readiness and C# HolonManager/LINQ CQL passed; local HTTP,
hosted admission and sandbox entry remain unimplemented/unverified. Totals
remain60integrated query types/56hosted functional proofs/5browser verified.

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
equality/IN with bounded paging over1–8configured keys. It checks configured
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

## Exact next step

Add Bittensor to the actual demo CLI and HTTP compiler/source activation,
read-only tariff/admission and hosted configuration tooling. Verify one
Mongo-paired local HTTP cohort, then deploy the verified batch and first hosted
cohort before publishing its sandbox card. Preserve the existing source owner,
keys and all consumed intents. Current integrated HTTP/live totals unchanged.
No AI inference/subnet metrics/native storage or independent consensus claims.
No David action is needed.
