# Stacks / BlockStack native qualification

Updated 2 November 2026. Native principal-state qualification passes;
CQL source, HTTP admission, hosted settings and sandbox proof remain pending.
Totals remain52integrated/48hosted-functional/5browser/43open.

## Verified native scope

- Anonymous `https://api.hiro.so/v2/info` returned HTTP200, mainnet network1 and synchronized status. The captured tip was height9394169.
- Derived explicit block ID `ccb149a1faca3a57f68fb0ed0b9474931c8036ba6d7f4eeffb9de61dd177b8b5` using the reference node's SHA-512/256 over block hash followed by consensus hash. Plain SHA-512 truncation is a different algorithm.
- Three explicit `proof=0&tip={block_id}` account GETs returned HTTP200 for `SP000000000000000000002Q6VF78` and its `.pox-4`/`.pox-5` contract principals. Exact returned spendable micro-STX balances3176656859/110000/0; all locked/unlock-height/nonce values0. No signing, mutation, scan, account setup or source owner created.
- Original node-info response was reused for qualification; no refetch or tip switch. Both exclusive intents are consumed. Preserve original evidence; no repeated native run to obtain a new green checkpoint.
- Six offline tests cover captured accepted block-ID vector, malformed selectors, wrong network/unsynced status, full uint128/uint64 boundaries, incomplete data and duplicate JSON properties. The fixed expected block-ID vector is independent of the test implementation.

## Required source contract

Implement `StacksPublicPrincipalMetadataSource` under existing BlockStackOASIS,
with `StacksPrincipal` equality/membership over one bounded configured catalogue.
Use shared HolonManager planning/deferred LINQ and complete-selection paging.
Each read captures synchronized mainnet info once, derives one valid explicit
block ID and uses it for every selected principal. Reject invalid selectors
before I/O: the node can silently select its default for malformed tip strings.
Do not use `tip=latest`, which can include unconfirmed microblocks.

Preserve uint128 balance/locked and uint64 nonce/unlock-height as exact decimal
strings. Native zero state does not prove that an account/contract exists.
These are trusted node-reported explicit-tip observations: no independent MARF
proof, consensus/finality, permanent canonicality, Gaia storage or full ORM
certification. No proof data or media links are fetched.

For SHA-512/256, inspect/reuse the already cached BouncyCastle package/version
used by the actual dependency graph. Versions2.5.1/2.6.2/2.7.0 are present;
do not install a new runtime or implement a truncated-SHA512 substitute.
Then add appropriate source tests and spaced/commented C#3/2 walkthrough;
only after that add HTTP/Mongo5/4/privacy/admission/hosted/card gates.

Legacy BlockStack client includes Gaia/user-directory paths and synthetic user
results; it is not used for this proof and remains unchanged. Document legacy
gaps only. No source owner or CQL admission has been minted yet.

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
