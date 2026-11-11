# Current HyperDrive/provider release gates

Updated 2026-11-11. Start with [current provider status](CURRENT_STATUS.md), the [provider matrix](PROVIDER_MATRIX.md) and [Actions for David](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).

## Current phase

Provider implementation and native capability verification precede global routing checks. The provider matrix tracks each storage baseline, capability check, hosted dependency and remaining acceptance gate.

## Open gates and next action

| Gate | Current blocker / next action |
|---|---|
| SOLID storage | An isolated Community Solid Server pod passed the shared storage suite. Access to the original protected fixture is still required for verification against its retained data. Hosted storage and COSMIC acceptance have separate evidence. |
| Cloudflare KV |52controlled core checks/25controls; native credentials absent at inspected locations. Eventual consistency, same-key rate limit and no CAS/transactions remain contract gates; see CLOUDFLARE_KV_STORAGE_BASELINE.md |
| Oracle cloud | Local Oracle 52/52 is not Autonomous Database or OCI Object Storage acceptance. Need protected OCI profile/key, region/namespace/test bucket and database connection/wallet |
| Azure Blob hosted acceptance | Local automatic baseline52/52 +111controls passes; graceful stop persists0blobs/blocks. Real Azure account/IAM/cloud durability acceptance remains separate; see AZURE_BLOB_STORAGE_BASELINE.md |
| AWS S3 / hosted AWS | Need explicit test bucket/region, protected credentials and persisted storage key; billable-request policy/configuration remains separate. Local DynamoDB 52/52 is not hosted acceptance |
| EVM family | AvalancheGo and BSC Parlia passed the shared storage suite. BSC also passed native NFT mint, transfer and burn plus the standard Hardhat deployment. Hosted writable fixtures, older deployments and original uncertain outcomes remain open. |
| IPFS | Fresh Kubo fixtures passed restart and concurrent publication. The original Kubo repository and protected key are needed for historical-data recovery and compatibility checks. |
| Cosmos blockchain | Fresh native wasmd passed 52/52 through CosmJS with no fixture keys remaining. The original retained wasmd fixture and hosted writes remain separate acceptance gates. |
| Solana | Fresh Agave 4.3.0 passed both shared storage modes and CI confirmed fixture cleanup. The original memo-chunk recovery and hosted writes remain open. |
| SQLite remaining interfaces | SQLite passed 52/52 and focused import, graph and encrypted-wallet checks. See the provider matrix for the exact test scope. |
| Redis-compatible storage | Valkey 9.0.4 passed 52/52. KeyDB and Dragonfly passed native smoke and separate CQL restart and federation checks. Their full storage lifecycle runs remain pending. |
| Firebase Realtime Database | Both test projects compiled and four activation-response cases passed. Native RTDB stopped at graph save after nine async checks. The atomic graph-save candidate is awaiting native verification. Hosted permissions and rules remain open. |
| Raw evidence | Writers redirected by `d70625e86`; unpublished ActivityPub proofs moved to TestResults. Linked legacy evidence remains; existing working-tree line-ending hashes are not Git-blob hashes |
| Integration | ActivityPub published separately as `00235929a`; batch verified scoped changes into Development, preserving user/Core/submodule work |

## Remaining verification

- All relevant provider interfaces, hosted credentials/IAM/durability and federation/authenticated social actions need their own evidence.
- Global V2 failover, load balancing and replication follow acceptance of the eligible providers.
- Contract/fee/crash-recovery depth follows breadth. Legacy implementation gaps remain documentation-only.
- Carry-forward owner actions not revalidated in this docs checkpoint: CoinGecko credential rotation/live quotes, Hedera test funding, authenticated ActivityPub search token. Consult their scoped audits when that work resumes; no secret values belong in docs/chat.

## Historical checkpoints

- Dynamo path relocation (`f446b70a2`): AWS/Dynamo 52/52 each, factory27, all five raw tables empty; stopped DB compacted to 43,008 bytes.
- LocalFile native52/52 and20controls pass; both current directories empty, original failed fixtures preserved; details in LOCALFILE_STORAGE_BASELINE.md.
- SOLID canonical build passed, both schedules blocked before native by missing protected files; no identity/key/ACL change.
- ActivityPub `00235929a`: capability5/5 and controlled291; source/PDB and final proof hashes reverified. Generic record storage unsupported; authenticated/social mutation gates open.

Keep this summary below 150 lines and roll old milestone entries into the archive. The complete pre-trim chronology is preserved verbatim in [Archive/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md](Archive/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md); read it only for a specific historical issue. [Archive hashes](Archive/ARCHIVE_INDEX.md).
