# COSMIC ORM / CQL

1. **[Current status — start here after compaction](CURRENT_STATUS.md)**
- [Fresh hosted acceptance: all seventeen additions](HOSTED_PUBLIC_SOURCE_ACCEPTANCE_2026-11-01.md)
- [Full provider/CQL/live-demo status — all229provider projects](PROVIDER_CQL_AND_SANDBOX_STATUS.md)

2. [Hosted demo expansion](CQL_DEMO_EXPANSION_ROADMAP.md): active priority before full parity. [Solana next-provider admission](CQL_SOLANA_DEMO_ADMISSION.md) and [chain query contract](CQL_CHAIN_QUERY_CONTRACT.md). [fresh reusable environments](CQL_DEMO_ENVIRONMENTS.md), [IPFS admission](CQL_IPFS_DEMO_ADMISSION.md) and [protocol admission](CQL_PROTOCOL_DEMO_ADMISSION.md) and [verified Mastodon CQL point profile](CQL_ACTIVITYPUB_NATIVE_PROFILE.md).
3. [CQL parity checklist](CQL_PARITY_CHECKLIST.md): one feature row, independent proof per database; red rows only.
4. [CQL completion roadmap](CQL_COMPLETION_ROADMAP.md) and [COSMIC ORM roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md).
5. [Implementation status](IMPLEMENTATION_STATUS.md) and [active projected-All/source contract](CQL_PROJECTED_ALL_EXECUTION_CONTRACT.md).
6. [CQL manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
7. [Historical reports/reference](Archive/README.md) and [previously committed evidence](Evidence/README.md).

Recovery: read CURRENT_STATUS, the two short roadmaps, IMPLEMENTATION_STATUS and the active contract; together they stay below 300 lines.
Update CURRENT_STATUS at every milestone. Do not append a chronological work log to the active roadmaps.
New logs/test output belong in the worktree's TestResults, never Docs. Reports record counts, hashes and evidence paths; no new raw evidence commits.
Persistent, irreproducible fixture state belongs in ignored LocalState/COSMIC and is not disposable test output.

- [SOLID CQL record-point profile and verification](CQL_SOLID_NATIVE_PROFILE.md).

- [Interactive IPFS/SOLID point demo launch and limits](CQL_INTERACTIVE_POINT_DEMO.md).

- [Indexed metadata policy and schema3 implementation plan](CQL_INDEXED_METADATA.md): verified shared policy and SQLite schema3 rows/cardinality; declared-range live/HTTP/interactive acceptance now passes; production limits remain explicit.

- [Indexed SQL canonical rows: explicit schema, privacy, evidence and remaining integration](CQL_INDEXED_SQL_ROWS.md).

- [Indexed physical-read billing](CQL_INDEXED_BILLING.md): exact server tariffs, durable SQL/RPC attempts and current acceptance limits.

- [WEB4 indexed frozen preparation](CQL_INDEXED_WEB4_PREPARATION.md): server aliases, immutable intent and reservation bound; stock HTTP wiring verified; metadata live HTTP remains pending.

- [Indexed WEB4 HTTP checkpoint](CQL_INDEXED_HTTP.md): frozen reservation/intent execution, precise receipts and current deployment limits.

- [Three-database hub routing and retained-fixture preflight](CQL_DATABASE_DEMO_HUB.md): discovery verified; native launch pending reconciliation.

- [Replica baseline and recovery: native implementation and remaining hosted gates](REPLICA_BASELINE_AND_RECOVERY.md).

- [After-demo backlog: preserved deferred work](AFTER_DEMO_BACKLOG.md).

Public read-profile guides: [S3](../../Devs/AWS_S3_PUBLIC_CQL_GUIDE.md), [Solana](../../Devs/SOLANA_PUBLIC_ACCOUNT_CQL_GUIDE.md), [Google](../../Devs/GOOGLE_PUBLIC_METADATA_CQL_GUIDE.md), [Ethereum](../../Devs/ETHEREUM_PUBLIC_METADATA_CQL_GUIDE.md), [Polygon](../../Devs/POLYGON_PUBLIC_METADATA_CQL_GUIDE.md), [Arbitrum](../../Devs/ARBITRUM_PUBLIC_METADATA_CQL_GUIDE.md), [Cosmos](../../Devs/COSMOS_PUBLIC_METADATA_CQL_GUIDE.md), [Polkadot](../../Devs/POLKADOT_PUBLIC_METADATA_CQL_GUIDE.md), [NEAR](../../Devs/NEAR_PUBLIC_METADATA_CQL_GUIDE.md). [EVM balances](../../Devs/EVM_PUBLIC_BALANCE_CQL_GUIDE.md), [Bitcoin](../../Devs/BITCOIN_PUBLIC_HEADER_CQL_GUIDE.md), [XRPL headers](../../Devs/XRPL_PUBLIC_HEADER_CQL_GUIDE.md), [Stellar native headers](../../Devs/STELLAR_PUBLIC_HEADER_CQL_GUIDE.md). Guides describe contracts; CURRENT_STATUS distinguishes hosted and local proof.

The bounded EVM and Bitcoin profiles have hosted HTTP proof within their recorded scope. XRPL and Stellar also have hosted HTTP/Mongo/isolation/replica proof; browser gates remain open. [All-provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md), [XRPL next steps](XRPL_PUBLIC_SOURCE_STATUS.md).

[Algorand current-account guide](../../Devs/ALGORAND_PUBLIC_ACCOUNT_CQL_GUIDE.md): native CQL/shared-manager/CLI3/2 plus hosted HTTP/Mongo/privacy/replica proof verified; browser remains open. [Proof](ALGORAND_PUBLIC_SOURCE_STATUS.md).

[Aptos account guide](../../Devs/APTOS_PUBLIC_ACCOUNT_CQL_GUIDE.md): native CQL/shared-manager/commentedCLI3/2 and hosted API/Mongo/privacy/replicas verified; browser remains open. [Proof](APTOS_PUBLIC_SOURCE_STATUS.md).

[Hedera mirror account guide](../../Devs/HEDERA_PUBLIC_ACCOUNT_CQL_GUIDE.md): native CQL/shared-manager/commentedCLI3/2 and local API/Mongo verified; hosted HTTP is verified; browser verification remains open. [Proof](HEDERA_PUBLIC_SOURCE_STATUS.md).

[Sui public object guide](../../Devs/SUI_PUBLIC_OBJECT_CQL_GUIDE.md): checkpoint-scoped native CQL and commented C# CLI verified; HTTP/sandbox gates pending.
