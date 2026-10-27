# Current HyperDrive/provider release gates

Updated 2026-10-27. Start with [CURRENT_STATUS.md](CURRENT_STATUS.md). No whole-provider or global V2 completion is claimed.

## Current phase

Provider-family implementations and shared baseline verification precede global routing and depth work. Eleven storage implementations have recorded local native 52/52 passes; ActivityPub has a published 5/5 public social capability baseline (`00235929a`).

## Open gates and next action

| Gate | Current blocker / next action |
|---|---|
| SOLID storage | Original `storage-key.dpapi`, `private-local-client.dpapi`, `private-local-pod.dpapi` missing from `C:/Source/OASIS/TestResults/SolidCurrent/`; restore trusted originals under the same Windows user or locate their existing protected location. No replacement over retained encrypted records. 0/52, no HTTP/server start |
| Oracle cloud | Local Oracle 52/52 is not Autonomous Database or OCI Object Storage acceptance. Need protected OCI profile/key, region/namespace/test bucket and database connection/wallet |
| Azure Blob hosted acceptance | Local automatic baseline52/52 +111controls passes; graceful stop persists0blobs/blocks. Real Azure account/IAM/cloud durability acceptance remains separate; see AZURE_BLOB_STORAGE_BASELINE.md |
| AWS S3 / hosted AWS | Need explicit test bucket/region, protected credentials and persisted storage key; billable-request policy/configuration remains separate. Local DynamoDB 52/52 is not hosted acceptance |
| EVM family | Compatible existing contract, loopback RPC/chain ID, signer and persisted storage key absent in last inspection; inspect existing runtime/cache before any last-resort setup. Old uncertain outcomes remain unverified |
| IPFS | Original Kubo repo/config and protected storage key unavailable; do not initialize over unknown identity |
| Cosmos blockchain | Missing bridge SDK/dependency target; earlier install/repair batch was policy-rejected. Do not bypass that rejection with another wrapper |
| Solana | Recorded 4/52: finalized history references an unavailable memo chunk; preserve both original fixtures, no skip/reset to get green |
| Other families | Shared suite bindings/native configuration still missing on most rows. Four Redis-compatible facades have controlled-only passes |
| Raw evidence | Writers redirected by `d70625e86`; unpublished ActivityPub proofs moved to TestResults. Linked legacy evidence remains; existing working-tree line-ending hashes are not Git-blob hashes |
| Integration | ActivityPub published separately as `00235929a`; batch verified scoped changes into Development, preserving user/Core/submodule work |

## Deferred, not completed

- All relevant provider interfaces, hosted credentials/IAM/durability and federation/authenticated social actions need their own evidence.
- Global V2 failover, load balancing and replication wait for applicable provider baselines.
- Contract/fee/crash-recovery depth follows breadth. Legacy implementation gaps remain documentation-only.
- Carry-forward owner actions not revalidated in this docs checkpoint: CoinGecko credential rotation/live quotes, Hedera test funding, authenticated ActivityPub search token. Consult their scoped audits when that work resumes; no secret values belong in docs/chat.

## Recent checkpoints

- Dynamo path relocation (`f446b70a2`): AWS/Dynamo 52/52 each, factory27, all five raw tables empty; stopped DB compacted to 43,008 bytes.
- LocalFile native52/52 and20controls pass; both current directories empty, original failed fixtures preserved; details in LOCALFILE_STORAGE_BASELINE.md.
- SOLID canonical build passed, both schedules blocked before native by missing protected files; no identity/key/ACL change.
- ActivityPub `00235929a`: capability5/5 and controlled291; source/PDB and final proof hashes reverified. Generic record storage unsupported; authenticated/social mutation gates open.

Keep this summary below 150 lines and roll old milestone entries into the archive. The complete pre-trim chronology is preserved verbatim in [Archive/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md](Archive/HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md); read it only for a specific historical issue. [Archive hashes](Archive/ARCHIVE_INDEX.md).
