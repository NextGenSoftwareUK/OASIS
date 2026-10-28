# Native Mongo + Solana mixed federation proof

28 October 2026. Shared V2 CQL federation; retained local validator and explicit SQLite
index. No hosted-chain, repeatable snapshot, replica agreement or production auth claim.

## Verified

`Run-ReusableSolanaMongoTests.ps1 -ArchiveCompletedDemo` passes1 native acceptance case.
A journaled synthetic Mongo DB holds a matching copy and an unselected control.
The chain uses retained schema3/index86d676b6-c2c5-4c8f-98b2-a88e0fb8bb11/coverage3325.
No source write, new seed, replacement ledger/key/index or dependency restore occurs.

One Name-equality query executes Mongo natively and Solana through its explicit
registered index reader plus independent source observer. Per-provider authoritative
read catalogues bind actor/allowed ID before windows. The shared result preserves
2 physical copies of logical ID a03306d4-649f-4871-8f27-8e13869b59a1:

- Mongo1, no fabricated chain evidence.
- Solana1, original commit signature/version identity plus source/checkpoint/head.
- Indexed read source remains SolanaOASIS, execution SQLLiteDBOASIS.
- Indexed checkpoint3859/finalized head3863 and source genesis/index identity verified.
- The unrelated Mongo document is excluded; PreserveCopies asserts no causal agreement.

Exact native Mongo record owners/names/IDs and collection counts are read back before
removing only the previously absent owned DB. No pending journal remains. Mongo27029,
Solana18899 and demo5089 listeners absent; solana-test-val process absent after teardown.
Retained source ledger/ack/index/keys remain intact. Existing50 HTTP admission cases
also pass; HTTP sourceWrites0/submittedNewSeed=false. Mixed HTTP/browser preset pending.

## Evidence

C:/Source/OASIS-cosmic/TestResults/SolanaCurrent/solana-indexed-metadata-native.trx
SHA256 FA2AB49B7FE16DEE14F595A7B893BAA011C752DCFCC571BCBC14299BB3B2C3B1.
Companion JSON SHA256 A5DF41903396897813971D7EED97CA5F5BEB22F8D850AA92316FE9144237AA59.
Summary mixedFederation:2physical/Mongo1/Solana1, retainedEvidence=true,
sourceWrites0/sourceReseeded=false/unselectedMongoExcluded=true/exactMongoCleanupVerified=true.

Initial compile-only issues(import and explicit Mongo capability interface) corrected
before validator/native fixture execution. No failed native source write or replay.
Exact helper:SolanaMixedFederationNative.cs; it uses shared planner/manager/executors.
No local native-shaped adapter shim or chain-evidence metadata injection.

## Next

Add a clearly labelled mixed HTTP/browser preset retaining per-copy chain evidence and
separate server-owned policies. Prove actual clicks, unsupported admission, cleanup and
sourceWrites0. Hosted Azure independently requires free-account setup; user confirmed
no existing free subscription. Do not create paid resources/accept billing terms.
