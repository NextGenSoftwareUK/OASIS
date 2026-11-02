# Stacks / BlockStack native qualification

Updated 2 November 2026. Local CQL source and actual manager/LINQ3/2 pass;
Local HTTP/Mongo/admission and prepared presets pass; hosted publication and
sandbox proof remain pending.
Totals now53integrated/48hosted-functional/5browser/43open; Stacks is not hosted-certified yet.

## Verified native scope

- Anonymous `https://api.hiro.so/v2/info` returned HTTP200, mainnet network1 and synchronized status. The captured tip was height9394169.
- Derived explicit block ID `ccb149a1faca3a57f68fb0ed0b9474931c8036ba6d7f4eeffb9de61dd177b8b5` using the reference node's SHA-512/256 over block hash followed by consensus hash. Plain SHA-512 truncation is a different algorithm.
- Three explicit `proof=0&tip={block_id}` account GETs returned HTTP200 for `SP000000000000000000002Q6VF78` and its `.pox-4`/`.pox-5` contract principals. Exact returned spendable micro-STX balances3176656859/110000/0; all locked/unlock-height/nonce values0. No signing, mutation, scan, account setup or source owner created.
- Original node-info response was reused for qualification; no refetch or tip switch. Both exclusive intents are consumed. Preserve original evidence; no repeated native run to obtain a new green checkpoint.
- Six offline tests cover captured accepted block-ID vector, malformed selectors, wrong network/unsynced status, full uint128/uint64 boundaries, incomplete data and duplicate JSON properties. The fixed expected block-ID vector is independent of the test implementation.

## Implemented local source contract

`StacksPublicPrincipalMetadataSource` is implemented under existing BlockStackOASIS,
with `StacksPrincipal` equality/membership over one bounded configured catalogue.
It uses shared HolonManager planning/deferred LINQ and complete-selection paging.
Each read captures synchronized mainnet info once, derives one valid explicit
block ID and uses it for every selected principal. Reject invalid selectors
before I/O: the node can silently select its default for malformed tip strings.
Do not use `tip=latest`, which can include unconfirmed microblocks.

Preserve uint128 balance/locked and uint64 nonce/unlock-height as exact decimal
strings. Native zero state does not prove that an account/contract exists.
These are trusted node-reported explicit-tip observations: no independent MARF
proof, consensus/finality, permanent canonicality, Gaia storage or full ORM
certification. No proof data or media links are fetched.

Actual WEB4 assets resolve BouncyCastle.Cryptography2.7.0, already cached.
The adapter uses its SHA-512/256 implementation through an explicit compile
alias because the legacy dependency graph also exposes an older library with
the same type name. No dependency was removed or crypto substitute added.
Original failed build retained; corrected provider and console builds pass.

## Local CQL and console evidence

- 43 source tests pass, 0 failed/0 skipped: reference block-ID vector, canonical C32 checksums/mainnet versions, exact amounts, one tip per selection, point/subset/window ordering, malformed/failed last rows before paging, cancellation, unknown keys, unsupported predicates and native writes.
- Actual `--stacks-public` console through HolonManager/deferred LINQ passes3full/2filtered. One source owner/configuration/intent/ACK is retained under `C:/Source/OASIS-cosmic/LocalState/COSMIC/HostedStacksPublicSourceCurrent/`. Do not replace the owner or rerun qualification.
- Source `source.json`:220bytes/SHA256 `aa5fe1f4934e1ed9670aee3064527a7becccee2332c3d416615288a93de634d1`.
- Unit evidence `TestResults/StacksPublicSourceCurrent/Offline/stacks-source.trx`:79052bytes/SHA256 `f7502f6a1b92f3d87000540e1efa37f729abaa1e4c8b72e4d5936df16b654df4`.
- Actual console log `TestResults/StacksPublicSourceCurrent/console-qualification.log`:1284bytes/SHA256 `e98ae186ca6e596a7360370b9a4bd12a6ec0a4051667dc31916ebedd013b5ec5`.
- [Configuration/C#/limits guide](../../Devs/STACKS_PUBLIC_PRINCIPAL_CQL_GUIDE.md) and [spaced/commented walkthrough](../../../Examples/CosmicCqlDemo/StacksPublicWalkthrough.cs). HTTP/compiler admission subsequently passed its local gate below; no hosted count added.

## HTTP/admission milestone

- Core query-only tariff control1, API admission/preset/identity controls3 and publisher9pure tests pass. Native signing/writes remain outside this source admission.
- Actual local shipping HTTP endpoints pass1native fixture test: native3/2, paired Mongo5/4, foreign-private0/public3, two SDK-owned version1 snapshots and9query receipts. Copies retain original exact amounts/tip metadata; later native observations can have different tips.
- Owned `cosmic_cql_stacks_http_demo` dropped success-only after exact marker/count checks; owned Mongo stopped/27029absent. Original actor/copy/operation/save/cleanup ACKs retained in `LocalState/COSMIC/StacksMongoHttpCurrent`; native/local source ACKs retained in HostedStacksPublicSourceCurrent. Never resave/reseed this fixture.
- Prepared protected baseline/candidate `LocalState/COSMIC/HostedSourceRolloutCurrent/stacks-public-*` changes exactly3settings, preserves canonical DNA/native write permissions and uses free query-only pricing. Actual4presets pass1shipping native/Mongo planner test. No configuration apply or deployment yet.
- Source-specific preparation test confirms exact principals, private Mongo-only copies and repeat0new/2existing. All102client tests pass; frontend task68214963cf076409be03fcf9661d9dbd1ee1c6b8 pushed, no app JS logic changed or public card added.
- Core task `6aa17e79e12ec3c54fbc3cf9611fdaa8adfe8dbf` pushed and remote ref verified; parent gitlink and Docker manifest advance together before verified integration.

| Evidence under `TestResults/StacksPublicSourceCurrent/` | Count | SHA256 |
|---|---|---|
| `Http/core-admission.trx` | 1/1 | `7b90ee539d1f83d3d404d9bd517b6dfed7c1b9a3b6672e67748be23e971dad19` |
| `Http/api-admission.trx` | 3/3 | `d2648b64b7c0187a202260269bb83ae3b8f02ba453567352bda0ef863f0df5f8` |
| `mongo-http-native.trx` | 1/1, no skips | `fa8981661e660448af53b48ff5a23e802aca0ac5d581c8c213e8e6277953a1dd` |
| `Http/prepared-presets.trx` | 1/1, four presets | `4f9d230588222a62a834164769c3818bd43e5b43ccc100e7deedb7aa11544b2f` |
| `prepared-review.json` | four presets, not deployed | `5f32a9830ad5dfd2663fa8c69f0436832643f11399c3303f41de44f24686071a` |

## Code publication

Parent `3768c2050ec7b0c60677ae3367bda450440b71ad` and Core
`6aa17e79e12ec3c54fbc3cf9611fdaa8adfe8dbf` published to Development;
remote refs verified after fetch/ancestry checks. Manifest/seven Dockerfiles/
parent gitlinks validator passes. Existing Railway code job
`8f13e4b3-5113-4b74-a9f8-8ace730b2654` observed QUEUED at that exact parent.
Publication intent/observation: `LocalState/COSMIC/HostedStacksReleaseCurrent`.
No hosted source settings applied and no Stacks hosted proof counted.

Next read-only observation; preserve the same job after any timeout:

```powershell
python Scripts/Sandbox/Observe-HostedDeployment.py `
  8f13e4b3-5113-4b74-a9f8-8ace730b2654 `
  --expected-commit 3768c2050ec7b0c60677ae3367bda450440b71ad `
  --output TestResults/StacksPublicSourceCurrent/code-deployment.json
```

Wait for exact SUCCESS. The publisher's release gate requires HEAD to match:
from a clean task branch temporarily check out3768c2050, set the expected full
SHA, apply the existing protected candidate once with skip-deploys, restore
work/cosmic-cql-ton-public, and trigger one journalled settings activation.
Preserve all existing intents/ACKs; do not reprepare, restart or replay saves.
Then verify hosted native3/2, Mongo5/4, reuse/privacy/replicas before publishing
the card. Native zero state is not account-existence, consensus or Gaia proof.

Legacy BlockStack client includes Gaia/user-directory paths and synthetic user
results; it is not used for this proof and remains unchanged. Document legacy
gaps only. The source owner is now retained; HTTP/host admission remains pending.

## Evidence outside docs/git

`C:/Source/OASIS-cosmic/TestResults/StacksPublicSourceCurrent/` measured121613bytes,
including reusable public specification/source snapshots and native observations.

| Artifact | Bytes | SHA256 |
|---|---|---|
| `info-response.json` | 1089 | `9258107e644f4956e4bcb45fdc65a0b031eeea36b93b7db19270534f091ac9d7` |
| `qualification.json` | 1223 | `2d2e5530b6948b70707a0f5673b778c50dab57587341b6a610e5f156961c3fd9` |
| `account-0.json` | 122 | `11613b2c05d331648586008b48c559468eb3a493ac79e0e13caaec35c0d53617` |
| `account-1.json` | 122 | `7b9b9a661e80a218571b46f8b5b37fd7f56d99098b8deaebe3c13bc6f4c4fa7c` |
| `account-2.json` | 122 | `75f624e95c238102a55e16788c5bde66c3414670ca354f1f7d684a00530f4587` |

Offline command: `python -m unittest discover -s Scripts/Sandbox -p test_stacks_public_probe.py`.
Do not rerun native intents. Raw outputs are not committed in docs.

Primary references: [node RPC contract](https://github.com/stacks-network/stacks-core/blob/main/docs/rpc-endpoints.md),
[OpenAPI](https://github.com/stacks-network/stacks-core/blob/main/docs/rpc/openapi.yaml),
[tip parameter](https://github.com/stacks-network/stacks-core/blob/main/docs/rpc/components/parameters/tip.yaml),
[block-ID derivation](https://github.com/stacks-network/stacks-core/blob/main/stacks-common/src/types/chainstate.rs),
[mainnet service](https://www.hiro.so/stacks-api).
