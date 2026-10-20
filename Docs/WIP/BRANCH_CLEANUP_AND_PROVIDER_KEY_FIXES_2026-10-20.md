# Branch cleanup, feature ports and provider key fixes (2026-10-18 to 2026-10-20)

All branches across the parent repo and its 8 submodules are now merged or archived; only in-use branches remain. Along the way, real defects in Development were fixed and pushed. This report records what was done, where everything went, and what is still open.

## 1. Branch state now

| Repo | Remaining remote branches |
| --- | --- |
| OASIS (parent) | `Development`, `master`, `codex/hyperdrive-dual-mode-route-matrix` (Codex, in use), `promotion/development-to-master` and `submodule-sync/*` (release workflows) |
| API.Core | `Development`, `main`, `codex/hyperdrive-dual-mode-route-matrix` (in use) |
| STAR ODK, WEB6, ONODE.Core, ONODEManager, OGEngineClient, OASIS Hub, HoloNET-ORM | `Development` and `main`/`master` only |

Local branches: parent keeps `Development`, `master` and two worktree branches (`claude/dreamy-goldstine-7c7161`, Codex route-matrix). WEB6 keeps four worktree branches (`codex/fix-mcp-karma-contract`, three `worktree-agent-*`); remove the worktrees first if those are no longer needed.

## 2. What was merged

| Merge | Repo | Notes |
| --- | --- | --- |
| `codex/hyperdrive-dual-mode-route-matrix` (and follow-ups) | parent, API.Core, ONODE.Core | Storage admission core APIs; subscription conflicts kept Development's multi-currency catalogue |
| `feature/hyperdrive-v2-storage-admission` | WEB6, STAR ODK | WEB6 NBitcoin aligned to 10.0.7 (required by the Aptos SDK) |
| `codex/holochain-0.7-wire-mapping` | HoloNET-ORM | Combined DateTime ISO and collection mapping; temp `MockData` stub removed |
| `codex/fix-web5-jwt-runtime` | STAR ODK (4107047) | Plain POST create endpoints for 16 STAR resources called `UpdateAsync` instead of the create lifecycle; quest errors carry `InnerMessages`; STAR Postman collection added |
| Newtonsoft.Json 12.0.2 → 13.0.2 (Dependabot) | parent | Archived HoloNET client, known advisory |

## 3. Branches removed and how they are preserved

- Parent: about 175 branches deleted (89 fully merged, 81 obsolete Dependabot bumps, the rest superseded). Branches with unique history are kept as `archive/<branch>` tags.
- The retired `main` branch is preserved in full in the private `OASIS-main-archive` repo (tip `305ef3711`) and as `archive/main`.
- Submodules: 40 branches deleted (36 fully merged; 4 reviewed: one merged, two identical patches already in Development, one superseded and kept as `archive/codex/shared-inventory-geonft-id` in API.Core).
- Local: 70 parent and 31 submodule branches pruned; each was merged, on `master` by identical patch, or held in an archive tag.

## 4. Projects moved to their own repos

Created 19 Oct 2026 (private): MNEEHackathonSubmission, UniversalAssetToken, OASISMauiDemo, AztecBridge, HolonicVisualizer, DoomBrowser. `PlatoMusicFrontend` duplicates the existing `PlatoMusic` repo and can be deleted.

Already split on 11 Oct 2026: TimoRides, Pangea, RWA, RWA-Backend, UniversalAssetBridge, AssetRail, Kleros, x402, NFTMintFrontend, TelegramCRM and others; newer files are on their `from-oasis-main` branches.

Not moved: Max's personal material (CV, job application, political one-pager), copies of third-party repos (zashi, zcash-devtool), test-validator leftovers.

## 5. Features from Max's branches

Full detail: `Docs/WIP/MAX_BRANCH_FEATURES_COMPLETENESS_2026-10-19.md` and the live doc linked there.

| Feature | Outcome |
| --- | --- |
| SPL token creation | Shipped: `IOASISTokenIssuerProvider`, Solana implementation, `POST api/wallet/create-token` |
| A2A agents | Rebuilt in WEB6 on durable holons: agent cards with no secret fields, messages, delegated tasks, WalletManager token payments (MNEE = its ERC-20 address), JSON-RPC; GraphQL and gRPC protocol tasks now enforce ownership |
| Liquidity pool, RWA oracle, Zcash/Aztec stablecoin, contract generator | Not shippable (no custody, mocks, placeholder contracts, missing service); recorded as rebuilds |

## 6. Provider key and address derivation fixes

Each fix was verified against published vectors or real chain data.

| Provider | Was | Now | Verified by |
| --- | --- | --- | --- |
| Starknet | SHA-256 "keys", 16-word System.Random phrases, simulated tx hashes reported as success | starknet.js 10.8.0 bridge: BIP-39, Stark keys, OpenZeppelin accounts, status, fee, deploy; unsigned submit now errors | Stark generator vector; two real mainnet deployments; live Sepolia reads |
| Radix | SHA-256 public key; secp256k1 BIP-32 bytes reused as Ed25519 | SLIP-10 CAP-26 path, RET keys and `account_rdx1`/`account_tdx_2_1` addresses | SLIP-10 vector 1 |
| EOSIO | "EOS" + Ethereum address; GUID seed; text hashed into keys | eos-sharp WIF/EOS keys; restore accepts only real private keys | EOSIO dev key |
| MultiversX | Bitcoin keys; SHA-512 half as public key; brain-wallet restore | BIP-39, SLIP-10 `m/44'/508'/0'/0'/i'`, Ed25519, `erd1` | RFC 8032; alice/bob test wallets |
| Cardano | SHA-256 base64 "address"; brain-wallet restore | CIP-19 enterprise addresses (Blake2b-224, bech32); restore only from the issued seed | CIP-19 vectors |
| Zcash | Node address paired with an unrelated Bitcoin key | `z_getnewaddress`, `z_exportkey`, `z_exportviewingkey` | RPC contract tests |
| Stacks | "SP" + Bitcoin address; Base58Check | c32check encode/decode; signer key suffix fixed | c32check vectors |

Shared helpers added to API.Core: `Helpers/Crypto/Bech32.cs` (BIP-173, no 90-char limit) and `Helpers/Crypto/Slip10Ed25519.cs`. The first commit of these broke the netstandard2.1 target; fixed in 485e810.

## 7. Still open

- Full rebuilds: ThreeFold, Miden and Aztec providers call invented APIs on hosts that do not resolve.
- Signed transaction submission: MultiversX, Cardano, Stacks and Starknet writes are unsigned or in the wrong format.
- Cardano CIP-3 (Yoroi/Daedalus) mnemonic restore.
- One funded Starknet account deployment on Sepolia (about 0.08 STRK).
- A2A extras: OpenSERV workflows (credentials via KeyVaultManager), karma awards, agent NFT trading.
- `Docs/Devs/STAR_CLI_SessionHandoff.md` has been missing from Development for a long time; five docs, including `AGENTS.md`, still reference it.
- AllProviders test harness references a non-existent `ZkSyncOASIS` namespace (pre-existing).
