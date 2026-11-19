# Solana schema3 browser verification

28 October 2026. Existing local validator/source and explicit SQLite index; no hosted
chain, production authentication or general LINQ parity claim.

## Verified browser interactions

Hub command: `Run-CosmicCqlDemoHub.ps1 -Provider SolanaMetadata -ArchiveCompletedDemo`.
Page: `http://127.0.0.1:5089/`. Actual browser AX/control observations:

- Load rows: HTTP200/settled, one retained coffee holon
  `a03306d4-649f-4871-8f27-8e13869b59a1`.
- Filter nested metadata: Status.category=coffee AND Status.amount>9007199254740992,
  one matching row, source receipt retained.
- Select metadata columns: identity, coffee, exact string `9007199254740993`, null.
  Int64 and nullable absence survive browser JSON handling.
- LongCount: HTTP200/settled, Int64 value string `1`, authenticated-index-snapshot.
- Response explicitly identifies SolanaOASIS source, SQLLiteDBOASIS execution,
  schema3/index86d676b6-c2c5-4c8f-98b2-a88e0fb8bb11, coverage3325,
  checkpoint and independently observed finalized head. Rows/projections use trusted
  source-observation assurance and source receipts; no false native-SQL-chain claim.
- Stop demo and validator button visibly returned Shutdown requested; runner then
  completed1/1 native acceptance with no skipped cases and closed owned services.

## Evidence and limits

Native report: `C:/Source/OASIS-cosmic/TestResults/SolanaCurrent/solana-indexed-metadata-native.json`.
50 acceptance requests:21accepted/21replay rejections/8unsupported before execution;
4 additional interactive queries,19ingestion advancements, final checkpoint3844.
`submittedNewSeed=false`, `sourceWrites=0`. Original ack/index/source identities retained.
No5089/18899 listeners and no solana-test-val process after shutdown; no pending seed.

TRX SHA256 C56B42AD16755F38E0D4A7EFC4F60EDEED0CE92EFD4F1996E75477E8346CB45F.
Summary SHA25656EDD1A0A8BD22D2B964E63B724643334D01498B46E782B69A3FD90EEB72B6BA.
Screenshot API returned Unable to capture screenshot. Functional browser interaction
is proved; screenshot/layout QA remains unverified, no screenshot artifact invented.

Retained WSL ledger currently1.3GiB, index32768bytes, ack511bytes. Preserve original
ledger/keys/acknowledgements; no reset/GC is authorized to replace missing original
history. Raw evidence remains TestResults; lifecycle ledger contains resource details.
Mongo+Solana federation preset and hosted-chain acceptance remain open.
