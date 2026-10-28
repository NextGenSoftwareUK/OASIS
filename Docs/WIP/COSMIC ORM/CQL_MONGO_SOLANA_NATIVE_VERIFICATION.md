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


## Mixed HTTP/browser milestone

The commented [MixedFederationWalkthrough](../../../Examples/CosmicCqlDemo/MixedFederationWalkthrough.cs)
now owns the common SDK/HTTP plan; [MixedFederationApiHost](../../../Examples/CosmicCqlDemo/MixedFederationApiHost.cs)
provides a loopback teaching UI. Run:

```powershell
& Scripts/TestHosts/Run-ReusableSolanaMongoTests.ps1 -ArchiveCompletedDemo -Serve
```

Open http://127.0.0.1:5091/ . Matching returns2physical copies (Mongo1/Solana1)
with the original receipt/version. Empty returns0 while retaining indexedReads
(sourceSolana, executionSQLite, original index ID, checkpoint/finalized head).
Browser submits only a named preset; actor/catalogues/source grants stay server-owned.
Unknown presets/forged actor fields reject400. Failed source/index admission returns503,
never partial substitute rows. This teaching host does not certify production WEB4
mixed-route authentication/billing; stock indexed WEB4 acceptance remains separate.

Live automated SDK/HTTP gates and actual browser matching/empty/shutdown clicks pass.
Matching browser checkpoint3929/head3930; empty3940/head3946. Read-level assurance is
preserved for both shapes; strings preserve slot precision. Screenshot successfully
inspected: readable section/code spacing. No durable screenshot artifact was created.
An explicit5second ingestion schedule advances acknowledged checkpoints independently
of requests;45second bounded observations,500slot freshness ceiling, fail-stop/no retry.
Stock indexed HTTP runs before mixed interactive ingestion so its acknowledged cursor
cannot be reused after a later publisher has advanced it.

Final native1/1pass/no skips. Original chain seed/signature/version/index retained;
sourceWrites0/submittedNewSeedfalse. Exact2Mongo records and owned DB removed;
fixture journal absent,27029/18899/5089/5091 listeners absent after teardown.
Current TRX TestResults/SolanaCurrent/solana-indexed-metadata-native.trx SHA256
7FF331309600E357CDE3E911DD6154C3C49113E94860002BE0D66CB88BCF713F.
Summary JSON SHA25664AC9E7C2A3314BFDEB000DBD7551CD6EBA378F07C1A440D8751B6C2F9BDD86F.
The summary checkpoint fields describe their SDK gates before interactive ingestion;
browser positions above are later observations, not a shared snapshot.
Previous passed evidence was hash-preserved under TestResults/SolanaCurrent/Archive.
