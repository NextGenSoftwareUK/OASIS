Superseded by [current TON status](../TON_PUBLIC_SOURCE_STATUS.md); historical investigation and failure evidence retained.

# TON public-source qualification

Updated 1 November 2026. Research only; no TON CQL or hosted admission claim.

The existing TON provider is a candidate after Arweave's hosted rollout. Exactly two anonymous bounded GET requests to toncenter.com succeeded with HTTP200 and protocol ok=true: masterchain info (557bytes) and explicit historical header10000000 (859bytes). Existing guarded HTTP/Core/PowerShell runtime reused; no wallet, account, source owner, database or environment created. Requests were separated by1.1seconds for the documented anonymous1RPS limit; no retry or alternate endpoint.

Evidence stays outside Docs: TestResults/TonPublicSourceCandidateCurrent/shape-observation.json and two public response files. Identity response SHA256ccca9d9f6d0c75218577c57c7bbe7ce195935f4b7deccb67fa461e88be294f7e; header SHA25620849b0ad32e148b902e3428f361983f46116971430e5813cf14761944cecf00.

## Contract observations and next gates

- Historical header reports workchain-1, shard -9223372036854775808, seqno10000000 and global_id-239. Its logical times are decimal strings; sequence/time/hash-short fields are JSON integer tokens. Preserve exact domains, canonical padded32byte base64 hashes and complete requested block identity.
- Mainnet identity init root/file hashes match the official public global-config zero-state hashes. However, masterchain-info init.shard reports string0 while the official zero-state shard is numeric -9223372036854775808. Record this discrepancy; do not claim complete descriptor equality or silently normalize it. Establish the intended state-ID contract before implementation.
- Qualify three explicit historical headers and a two-key selection, requested root/file identity and parent relationships. Do not discover/scan arbitrary chain history or create a node/wallet.
- Then add capability-aware read-only CQL, controlled/native tests, spaced shared-manager C# demo, API/tariff admission, local HTTP/Mongo federation/privacy and actual candidate checks before any hosted release.
- RPC-reported headers do not independently verify consensus. No native transaction or full ORM/HyperDrive completion is implied.

Primary contracts: [masterchain info](https://docs.ton.org/api/v2/rpc/get-masterchain-info), [header](https://docs.ton.org/api/v2/rpc/get-block-header), [official Mainnet config](https://ton.org/global-config.json). [Current task](CURRENT_STATUS.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Three native historical headers observed

Headers10000000/10000001/10000002 all report Mainnet global_id-239/masterchain-1 and exact requested sequence numbers. Returned parent descriptors match consecutive selected block IDs, and root/file hashes are canonical32byte padded base64; logical-time strings are exact UInt64 decimal values. Protocol ok=true and HTTP200 for all three. This is native wire evidence, not CQL or independent proof verification.
- header-10000001: 860bytes; SHA25643f159f14d80097d32f1848c3a3c53f0aca8bb57a4f4c4c9632d802d7bf91266.
- header-10000002: 860bytes; SHA2562e0f5b287c691c1f5469411752829bb949f6a5715df7bddeb8ea9e303a32f7d5.
The official tonlib source confirms masterchain info converts a zeroStateIdExt into its HTTP-facing BlockIdExt representation; the exact conversion overload remains to inspect before declaring the shard discrepancy resolved. Official source SHA2566d2688e8b00555b1d2539735df47cd59f190b976ad390d069c92c527d00b015c,306736bytes, inspected in memory only.

## Identity representation resolved; native3/2 complete

The official tonlib conversion overload explicitly represents zeroStateIdExt as BlockIdExt with shard0/seqno0 while retaining workchain/root/file hashes. This explains the observed init descriptor without normalizing it into a real masterchain block. A future adapter must validate the explicit wire representation plus the pinned Mainnet workchain/root/file anchors, and independently validate real header shard/seqno/root/file/global_id. This is an upstream representation contract, not independent consensus verification.

Both explicit two-record subset requests included documented root_hash/file_hash parameters and returned payloads exactly equal to their original historical headers. Native3/2 wire qualification now passes; CQL remains unimplemented. TestResults/TonPublicSourceCandidateCurrent/wire-qualification.json records source hash and conversion line6037. [Official conversion implementation](https://github.com/ton-blockchain/ton/blob/master/tonlib/tonlib/TonlibClient.cpp).
- subset-10000000: 859bytes; SHA256da859821f9ef0150a279b771130634a5ca55145ee066881f161c765820d4114a.
- subset-10000002: 859bytes; SHA256b22dd7affda8775210c9ed73c0bdebcb21c415bf29d043f53652d35e7773023d.

Implementation has not started: the large proposed editing command was rejected by automatic tool review with only "blocked by policy", no detailed reason. Subsequent git status and exact target absence confirm no TON source/project edits or environments were created. This is agent-owned tooling work; no David action. Continue with small reviewable source edits after the Arweave hosted gate.

## Exact header contract implemented and tested

Smaller apply_patch source edits passed tool review. TonPublicHeaderReference validates pinned Mainnet zero-state wire identity, the complete requested block descriptor, predecessor shape, native UInt32 fields/UInt64 logical-time strings and duplicate JSON properties.23controlled tests and1decoder test over the original native identity/three headers/two pinned subset responses pass,0skips. No new HTTP requests. This is the decoder foundation, not a connected CQL adapter, so implementation/hosted counts are unchanged.
- TestResults\TonPublicSourceCandidateCurrent\contract-controls.trx: 23passed, 35629bytes, SHA256b32a0899e24afce880ea0b2cdf98683d7805affd6f08f4ca5e0501a2e7cf3e45.
- TestResults\TonPublicSourceCandidateCurrent\contract-captured.trx: 1passed, 2791bytes, SHA2569a9955e0619aa070f2d69d0613e293f5346bb807735a5dc3402972a5e2fdebec.
Next: connect this tested decoder to bounded read-only native key selection with one endpoint-wide anonymous pacing owner, then shared planner/manager tests and spaced demo before API/hosted admission. Keep Arweave rollout separate.

## CQL candidate: controls pass, live HTTP429 gate remains red

Bounded native root-hash selection now uses complete configured sequence/root/file descriptors, the tested decoder, capability admission, deterministic paging after validation and a process-wide1.1second request-start gate.12new controlled query checks pass,0skips (previous green decoder tests not rerun). Actual native activation failed on a selected-header request with HTTP429 after Mainnet identity passed; no automatic retry, alternate endpoint or native-success claim. Preserve original failed response evidence. Implementation/hosted totals remain40/37 until the separate Arweave milestone updates hosted totals.

The candidate is isolated on work/cosmic-cql-ton-public and must not be merged/deployed as qualified TON support. Next establish an evidenced admissible free native endpoint/access policy before another explicit live check; then finish manager/CLI/API/hosted gates. No David request has been made; this is agent-owned provider qualification.
- TestResults\TonPublicSourceCandidateCurrent\cql-controls.trx: 12passed/0failed, 19892bytes, SHA25685c654599ef9b201d277d4524e5cf7f0d46be02e79d50f375058148a3731c0a8.
- TestResults\TonPublicSourceCandidateCurrent\cql-native.trx: 0passed/1failed, 4564bytes, SHA25645215a8d8cca58b6d5d0bd1a116729ac820ea0476d826eda14cc9340e81f6187.

Failure-location correction: original stack ReadAsync line240 in candidate2764d64e1 is the getBlockHeader request; identity validation is line231 and had returned. Earlier summary wording incorrectly called this the identity request. Raw failure/TRX are unchanged; derived summary corrected. Existing generic Web3 signed-provider scaffolding is not proof of TON-native storage; the independent read candidate grants no signed-write certification.

## Reproduced pacing defect fixed; native HTTP500 still red

A slow-response regression failed on the old gate: measuring from dispatch lets DNS/TLS/server latency consume the next admission interval. The single endpoint owner now waits1.1seconds after the preceding request completes; cancellation while waiting does not reset that clock. The affected query suite passes13/13 with0skips; earlier green decoder suite was not rerun. This is a root timing fix, not retry/backoff or a new endpoint.

One explicit native verification after the code change failedHTTP500 on a header request. A bounded diagnostic for the first exact descriptor then returned200/matching payload; one readiness recheck still failed500 and further full-flow checks stopped. Individual remaining descriptor diagnostics also returned200/matching original payloads (859bytes each). Thus the complete runtime flow remains unproved despite independent record availability. Do not infer that all prior429s were caused by the fixed pacing defect.
- TestResults\TonPublicSourceCandidateCurrent\pacing-repro.trx: 0passed/1failed, 3254bytes, SHA256041bc25c16ab782474bdbad04b683849367bcf986e438510a6072d4731843260.
- TestResults\TonPublicSourceCandidateCurrent\pacing-fixed-controls.trx: 13passed/0failed, 21372bytes, SHA2567e9e3a85b7ad69a3c3cb80d74f19060896fab72cb80854eb394900adbdb168c4.
- TestResults\TonPublicSourceCandidateCurrent\native-after-pacing-fix.trx: 0passed/1failed, 4568bytes, SHA256706ea35022b8cc981b6137dd25dbd8c6a1d6c0ddf7a595c23995aa932cc6ad7f.
- TestResults\TonPublicSourceCandidateCurrent\native-after-diagnostic-ready.trx: 0passed/1failed, 4568bytes, SHA256a6b4450f1b3e5c17f1d9e9d07aa65962b7569cfbaa22a21335919d580db1fc36.
Next: capture/compare exact native-flow request URI and bounded error detail against successful independent reads, without changing source identity, replacing records, retry loops or alternate endpoints. Keep candidate isolated; hosted38/implemented40/browser5/open33 unchanged. No David action or new environment is required for this investigation.

## Native CQL3/2 verified; manager/CLI remains red

A one-activation runtime diagnostic observed identity plus all three exact headers200 through the same adapter/decoder. Actual URI strings match independently successful .NET requests, including encoded root/file hashes; HTTP1.1 was observed. No successful response/body was modified. The diagnostic records activation only, not CQL readiness.

After that new readiness evidence, one complete FromJson/shared-planner native verification passed3/2 with0skips. All earlier429/500 failures remain retained; no retry or alternate endpoint was added. The new spaced shared-manager console walkthrough builds with0errors but its one actual run failedHTTP500 during activation, before manager queries. Therefore the full local demo/API/hosted gates remain open and TON is not added to qualified provider totals.
- TestResults\TonPublicSourceCandidateCurrent\native-transport-diagnostic.trx: 1, 2811bytes, SHA25668dc5ad43fee30cf84152ca76e10f4ece2c7d43f023ff21d6f51097d693f5a68.
- TestResults\TonPublicSourceCandidateCurrent\native-after-activation-observation.trx: 1, 2771bytes, SHA2565b7dbccd12032e8c123d6fc0865c4cde43cee12fcf68dd138f9221f33109f8f5.
- TestResults\TonPublicSourceCandidateCurrent\cli-native.log: HTTP500 during activation, manager queries not reached, 1811bytes, SHA256cdae8e916c0e9b634ac0c9052bd0a541765042bda979db6ea05d8c3ca2d80d85.
[Spaced setup and C# examples](../../Devs/TON_PUBLIC_HEADER_CQL_GUIDE.md). Next capture bounded status/route/error detail from the actual failing runtime path, then fix the evidenced cause; do not cycle full-flow rechecks or replace the source catalogue. Candidate remains on work/cosmic-cql-ton-public. Qualified40/hosted38/browser5/open33 unchanged.


## Superseded detailed API, continuation and publication checkpoints

Superseded by ../TON_PUBLIC_SOURCE_STATUS.md. Original failures and milestones are preserved below.

# TON public-source status

Updated 1 November 2026. Native CQL and shared-manager/C# demo pass; API and hosting are next. Full ORM/parity, signed writes and browser acceptance are not certified.

## Verified

- Native3/2 and actual HolonManager/deferred-LINQ console3/2 pass on explicitly pinned recent Mainnet descriptors102765318/319/320; build0errors,0test skips.
- 32 decoder controls,13pacing/query controls and3bounded HTTP-error controls passed in their scoped runs. Earlier green suites were not broadly rerun.
- The official wire schema exposes validator_list_hash_short as signedInt32. A captured negative value reproduced the wrong unsigned-domain check; boundaries/malformed tokens now pass/reject correctly. Its raw signed value is preserved as string metadata, never normalized into an unsigned number.
- One endpoint-wide owner waits1.1seconds after completion; a slow-response regression proves dispatch timing cannot consume the next admission interval. No retry or alternate endpoint is added.
- Exact public route/status and up to4096bytes of JSON-escaped operator error detail are retained. Bodies remain bounded; errors return explicit OASIS results without partial rows.

## Explicit dataset migration

The original2021 block10000000 triggered a server-side Merkle-proof/hash rejection. Successful cached/individual reads do not erase that failure; original records are not used as a runtime fallback.

The recent dataset was separately qualified with the native decoder, complete CQL3/2 and actual manager/CLI3/2 before promotion. Stable owner preserved; no TON snapshot cohort/native writes/hosted settings were created or changed.

- Active: LocalState/COSMIC/HostedTonPublicSourceCurrent/source.json,542bytes, SHA256dcb8332a908dd9cbb6d686e3df444eaca46cc53f48d1b593a73dc31f4b1f762e.
- Original: source.archive-10000000.json,539bytes, SHA256deade2f66fb678ff1945460b87124bcc18a3cd6488c50d612d6107d84b42efa3.
- Proposal, source-migration.intent.json and source-migration.acknowledged.json remain beside them. Never recreate the owner or repeat an uncertain promotion.
- This proves the declared recent dataset, not universal archive availability or independent consensus verification.

## Evidence

- TestResults\TonPublicSourceCandidateCurrent\error-context-controls.trx: 3, 5895bytes, SHA256e0c18542cf2a9b78c7ed7a460fa6836e5c910a048b298c30d30ba387d0b5e652.
- TestResults\TonPublicSourceCandidateCurrent\signed-hash-controls.trx: 32, 48924bytes, SHA256fbb194baf1cfce7d8690af67f350f14a7dae7a7cd3279a68d1f3c5e2e635adb3.
- TestResults\TonPublicSourceCandidateCurrent\recent-native-cql.trx: 1, 2771bytes, SHA2564735aa366f0a73cb23f77e00a2fc2b8db21c7ae13930e8589ba0f590d9ae4d55.
- TestResults\TonPublicSourceCandidateCurrent\cli-recent-candidate-native.log: [3, 2], 1350bytes, SHA256c0771a49018802d2d72232e567a70a19aed13f80931fd58262c257e1f022861c.

Raw evidence remains TestResults/TonPublicSourceCandidateCurrent; durable catalogue/ACKs remain LocalState. [Investigation history](Archive/TON_PUBLIC_SOURCE_HISTORY_2026-11-01.md) retains original429/500 failures, failed qualifiers and root-cause evidence.

## Next exact step

On work/cosmic-cql-ton-public, add independent read-only Core/API admission, four native/Mongo presets, local HTTP/Mongo/private-visitor checks and actual protected candidate tests. Then select a verified release, guarded configuration rollout, original hosted acceptance cohort and spaced sandbox card. No TON hosted count until those gates pass.

Query-type inventory41 (TON locally verified), hosted38/tariffs38/browser5/open33. No David action, new account/spend, node, wallet, database or runtime is required for this phase.

[Setup/manual and commented demo](../../Devs/TON_PUBLIC_HEADER_CQL_GUIDE.md) - [current task](CURRENT_STATUS.md) - [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

Primary wire domain: [official tonlib schema](https://github.com/ton-blockchain/ton/blob/master/tl/generate/scheme/tonlib_api.tl).

## API admission checkpoint

API/schema3/3, actual protected four-preset planning1/1 and pure publisher3/3 pass. Read-only Core admissionddabbf5447257a2136fa02aff3d4ca1c81035f3c is pushed/ls-remote verified on task only; parent gitlink/manifest advance together. Protected3-setting candidate prepared from acknowledged recent catalogue, no host changes.

Real local HTTP/Mongo fixture stopped during source activation before first persistence; original intent/actor/copy/operation IDs retained in LocalState/COSMIC/TonMongoHttpCurrent. Original assertion discarded the activation exception, so the failure cause is not established. Assertions now include actual exception detail. pre-persistence-failure.review.json ties original intent/TRX/source hashes and failure line100 to the code boundary; DB absence must be verified by the owned runner before any explicit continuation. Never delete/reset/reseed the intent or create replacement identities. Mongo runner stopped and port27029 is absent.
- TestResults\TonPublicSourceCandidateCurrent\api-admission.trx: 3passed/0failed, 6523bytes, SHA25679d3a81e4d6d268036db1e511c0d715fc29b78107490cb2d2983f2e0e1040690.
- TestResults\TonPublicSourceCandidateCurrent\actual-prepared-candidate.trx: 1passed/0failed, 3323bytes, SHA256e7c9b74d5a950c4922e046421d9063307ee97404053e2f82f7f27d05cee8f1ca.
- TestResults\TonPublicSourceCandidateCurrent\ton-mongo-http.trx: 0passed/1failed, 4831bytes, SHA25668e4b902498996563945d54da1d38804ba0be3394692c072a4bf34a2f1d08969.
Explicit same-ID continuation is implemented with original intent/TRX/catalogue hashes, fresh no-DB check and a one-shot journal. First build failed with CS0136 (overlapping original variable names); renamed intent document to originalIntent. Rebuild and native continuation remain pending; no native operation started. Preserve the first failure and capture readiness detail before writes. Local/hosted HTTP acceptance remain unproved; no TON deploy/grant.

## Original-cohort continuation checkpoint

Corrected build passed. The one-shot reviewed continuation verified DB absence, reused all original identities, activated the source, and acknowledged two private Mongo saves at version1. Native HTTP3 passed; paired3 returned HTTP504 under the default5000ms federation budget. Remaining paired2/privacy/receipt/cleanup gates were not reached. Owned Mongo stopped; port27029 absent; fixture DB, intent and save ACKs preserved. No repeat of the seeding test is allowed.

Core already accepts explicit1..30000ms federation durations; HTTP capture previously capped requests at5000ms. Aligning that boundary and requesting30000ms only in TON paired presets; default5000ms unchanged. Six lower/upper/default boundary controls added; publisher3 controls pass. A separately reviewed read-only continuation over the existing DB/ACKs is next, after controlled verification. Prepared protected candidate predates this budget change and must not be applied as-is.

Original continuation evidence: TestResults/TonPublicSourceCandidateCurrent/ton-mongo-http-prepersistence-continuation.trx, 0passed/1failed, 6652bytes, SHA25668043846418e989590925ceceecaaff0b1580152d934e143a6cf934e003d2e49. Original failure and both save ACKs retained; no hosted acceptance claimed.

Duration/admission controls verified: 8passed/0failed/0skipped (six duration boundaries plus existing two TON admission controls); TestResults/TonPublicSourceCandidateCurrent/duration-admission.trx, 14423bytes, SHA256fd33bcf0be7bfef05eeac2c5f18ed7dd20c94bf1fd309800fa443d85285d312b. Pure publisher controls3/3pass. This does not establish native paired/hosted success.

## Leaf deadline checkpoint

Reviewed read continuation built and validated original DB marker/count, two ACKs, actor/copy/operation IDs, source provenance, versions1/private visibility and identity metadata without saves. It stopped on native3 HTTP504/CQL_QUERY_CANCELLED under the leaf endpoint fixed5s deadline before testing paired budgets. No cleanup; existing DB/intents/ACKs preserved, owned Mongo stopped/27029absent. This is a distinct leaf boundary from the preceding paired3 timeout.

Added CqlV1Request.MaximumDurationMilliseconds (default5000, admitted1..30000), copied/validated before callbacks, consumed by the leaf endpoint cancellation source. TON native and paired presets explicitly request30000. Six leaf boundary/detachment controls plus schema/admission tests are running. This does not yet prove real paired success; original read intent cannot be rerun. Next use a separately reviewed read-only continuation tied to this failed outcome and the same DB/IDs, never Save/reseed. Fresh Developmentb175fe4/Corec9af864 must be integrated before any release.

Read continuation: TestResults/TonPublicSourceCandidateCurrent/ton-mongo-http-read-continuation.trx, 0passed/1failed, 7490bytes, SHA2569ba60c08e7229f65d676bd885a91bcebad7d10c355e9657e4325c670a5cbc96d. Native source activation/query preparation succeeded; no hosted acceptance claimed.

## Local HTTP milestone verified

Native-budget read-only continuation passes 1/1 with original identities: native 3/2, paired 5/4, foreign-private 0/public 3 and 9 receipts. No repeat Save was executed. Original two saves remain acknowledged version1. Success-only marker/count checks and teardown verified DB absent; owned mongod stopped and port27029 absent. Do not replay the cleaned fixture. Neither production billing, replication nor browser acceptance is inferred.

Duration/admission/schema controls pass 15/15 with zero skips; publisher controls pass 3/3. Single and paired TON presets explicitly request30000ms, default remains5000ms. Earlier paired/leaf deadline failures remain retained, not overwritten. Next revise prepare-only protected candidate (retain stale candidate), verify actual new presets, integrate fresh Development b175fe4/Core c9af864 and shipping checks, then scoped hosted rollout. No TON live grant/deploy yet.

- TestResults/TonPublicSourceCandidateCurrent/native-duration-admission.trx: 25574bytes, SHA256e9063eb27327a51c6f60f70a1636fc9f16037b8984edd2cb1a05371819304b23.
- TestResults/TonPublicSourceCandidateCurrent/ton-mongo-http-native-budget-continuation.trx: 3341bytes, SHA256777e05b8e0f4f545e0d6dde2ebc0470ae8dfe2c5e33963c115479672c6f2fff8.
- TestResults/TonPublicSourceCandidateCurrent/ton-mongo-native-summary.json: 219bytes, SHA256d2c639fafa1f306fa8269f42e01d1615cb7d3671fdb52ff76d4b64938649e767.

## Combined Development candidate

Parent b26724b integrates Development6b5db934; Core2ac915f integrates c9af864 and TON read-only admission. Both upstream work and manifest/gitlinks preserved; committed-pin validator and five SDK bridge manifests pass. Core task push verified by ls-remote. Revised prepare-only prefix ton-public-duration preserves stale ton-public candidate and stages four explicit30s presets without mutating host settings. Combined API duration/admission/schema and actual revised preset gate pass16/16, zero skips; publisher3/3pass.

- TestResults/TonPublicSourceCandidateCurrent/merged-duration-admission.trx:16passed/0failed/0skipped, 27168bytes, SHA2566777b1b5002255e3db86c873b07d34cb4967bf21a466aabe1a21da2a0afbf773.

Next publish the tested Core/parent Development batch, verify exact deployed commit, then apply only revised candidate with existing per-key journal/ACK and a single observed settings rollout. No TON settings apply or live acceptance has occurred yet.

## Development code publication

Parent24f548550dd2caaf99de00c2bf44003b2a2f8219 and Core2ac915f709ded7aabcd28a9db02aaea92fc52d97 published to Development after fresh fetch/ancestry checks; both remote heads verified. Exact app deployment4f6eba6c-5ee4-4afe-95a5-07849a92d7ec observed QUEUED (not success). Observe this same ID with Observe-HostedDeployment.py and expected commit24f548550dd2caaf99de00c2bf44003b2a2f8219; do not trigger another code build. Revised TON settings remain prepared only, no apply/intent/ACK. After exact code SUCCESS, use ton-public-duration candidate and existing guarded per-key apply, then one journalled settings deployment.

Queued deployment wait also exposed frontend15s timer conflicting with explicit30s API budget. Fixed at website7a02a23, all89client controls pass including actual shipped run() timer/one dispatch. Public HTML and versioned JS match exact commit blobs; no browser control or TON card. App4f6eba6c still QUEUED; observe same ID. Guarded settings apply must run with exact reviewed24f548550 checked out (temporarily detach in this existing worktree, then return task branch before docs); do not weaken release guard or apply stale ton-public candidate.

Preparation budget omission reproduced before saves and fixed at website299c376: derived native read inherits validated top-level paired duration.90client tests pass; public HTML/preparation/query scripts exactly match committed blobs. No TON card/grant/cohort. Queue inventory confirms original4f6eba6c and newer571cb5f0 both QUEUED. New parentfb29aa936 is a descendant of24f548550, preserves Core2ac915f/ONODE308a786 pins, changes OLAND contracts/David list. Review/integrate before selecting exact successful release for guarded settings apply. Latest successful app remains6b5db934/job5191ee53; no restart requested.


## Superseded cumulative handoff before TON settings rollout

Superseded by ../CURRENT_STATUS.md; preserve dated observations below.

# COSMIC ORM / CQL — current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demo. Deep LINQ parity remains paused.

## Current verified milestone

**38 providers have scoped hosted HTTP evidence; 5 have browser evidence and 33 browser gates remain open.** Arweave passes native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3 and matching SQLite/Neo4j replicas2/2. Two private saves were acknowledged once. Source writes and browser interaction are not inferred.

Latest hosted acceptance: parent37a530a87e601397f187430728bb9fb02b224f87/Core5fd361db11f5d54e3e99282536d7c7be842edf86, job3da7f986 SUCCESS. This docs-only descendant preserves testedb158bda61 code; original settings jobec59010e was removed and never replayed. Original Arweave cohort completion ACK retained. Website61e85b75a933004d1f005f85f1318fa05ad139de publishes Arweave with guided089a778c/C#b158bda61. Public HTML155229bytes/SHA2566fda659c10e90bb99d7636e04b62d3dc343b3a0c36cd85bb5b3f3c3e3c5c94f3 matches the committed git blob. Browser/real-expiry gates remain separate.

There are **41 implemented query types**, including locally verified TON: 3 canonical adapters,36bounded public/source profiles and2local Azure/Oracle implementations. TON native/manager 3/2 and local HTTP native 3/2, Mongo-paired 5/4 and privacy 0/3 pass on the qualified recent dataset; hosted verification is next. Hosted38/tariffs38/browser5/open33 remain unchanged. Full ORM/EF parity/HyperDrive certification remains incomplete.

## Exact next step

1. Radix card published/byte-verified at website0228db6; setup guide74ccc3752/C#74aaa9b4d linked. Original HostedQueryAcceptance-RadixOASIS completion ACK exists: do not replay saves or create a new owner.
2. Session lifecycle HTTP proof complete on original avatar, completion ACK retained: do not replay. Frontend9e18b7d published and HTML/JS byte-verified; browser gates remain open. [Proof and limits](SESSION_RENEWAL_STATUS.md).
3. Arweave hosted acceptance complete on original cohort; completion ACK blocks replay. Arweave card61e85b7 is published/byte-verified with updated guide/C# links. Continue breadth; TON hosted gates remain pending, other provider candidates next. [Proof](ARWEAVE_PUBLIC_SOURCE_STATUS.md).
4. Browser gates:5verified/33open. CUA transport remains closed; David already opened the browser. No OS control resumed or repeated owner request.
5. TON local HTTP passes native 3/2, paired 5/4, privacy 0/3 and 9 receipts, zero repeat saves; fixture cleaned. Development fb29aa936 merged in task99b4f080/Core2ac915f; changed contracts/schema/admission/actual presets pass32/32. Original app4f6eba6c is SUCCESS, descendant571cb5f0 is QUEUED; observe existing jobs. Live settings drifted2values; explicit ton-public-duration-oland revision prepared preserving them. Same three TON values/public review bytes match tested candidate; five publisher controls pass. Old candidates excluded. Await exact latest successful release guard before apply. No TON host mutation. [Evidence](TON_PUBLIC_SOURCE_STATUS.md).

## Guarantees and limits

- MongoDB/SQLite/Neo4j checklist50/82each; original demo set green. Work only on red rows unless a bug is reproduced.
- Added30hosted profiles retain60private demo copies and120presets. Original earlier eight proofs were not broadly rerun. Copies carry name/description/native key, not complete native chain state.
- HyperDrive scoped selection/failover/recovery proofs remain recorded; public source demos do not certify full provider writes, authority migration or universal uptime.
- Atomic billing handoff publishes settlement evidence and inline lease in one Mongo CAS. Genuine delivery failures remain durable/pending; lease expiry preserves worker recovery.
- Radix native/federation evidence began at1f39016fa; remaining reuse/isolation/replica checks completed at74aaa9b4d using the same cohort. Original failure and credential recovery are retained, not hidden.
- Azure and Oracle hosting deferred by David; no spending/signup re-asks. No production/master promotion.
- Full correctness/history/deltas, migration/placement/scale and broader ORM/EF parity remain open; legacy gaps documentation only.

## Recovery and commands

- Work only C:/Source/OASIS-cosmic; active candidate work/cosmic-cql-ton-public, Core work/cosmic-cql-google-public. Verified release work/cosmic-cql-public-release excludes unqualified TRON/TON query candidates; TON source must stay isolated until its gates pass. Frontend C:/Source/Web4Site work/cosmic-cql.
- Live app37a530a87/Core5fd361d job3da7f986 SUCCESS, exact tested codeb158bda61 plus David-list update. Frontend299c376 duration/setup fixes published/byte-verified; Arweave card preserved. No Development push solely for docs.
- Raw outputs TestResults/. Durable source/config/identities/ACKs LocalState/COSMIC/. Original Radix settlement-continuation manifest hashes are preserved; acceptance completion ACK now blocks reruns.
- Reuse pinned environments; inventory/cleanup in ENVIRONMENT_AND_CLEANUP_LEDGER.md. Unknown root logs preserved. Owned diagnostic DLLs removed; no new runtime or test service retained.
- Build lease: Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1; /m:2 /nr:false /p:UseSharedCompilation=false. Explicit bounded build/test, then run frozen DLL; do not forward build flags to dotnet run.
- Validate committed pins: python -B Scripts/validate_railway_dependency_manifest.py --require-gitlinks.
- SDK packaging: python -B Scripts/validate_all_provider_sdk_publish_manifests.py.
- Continuation tests: node --test Scripts/Sandbox/test_hosted_source_continuation.cjs. Completed hosted cohorts stay frozen.
- Exact settings observation: python -B Scripts/Sandbox/Observe-HostedDeployment.py 3da7f986-6e65-4fcf-b8f8-4916e688a9b6 --expected-commit 37a530a87e601397f187430728bb9fb02b224f87 --output TestResults/ArweavePublicSourceCandidateCurrent/settings-deployment-observation.json. No restart on observation timeout.
- Fetch before integration, preserve concurrent changes, non-force Development pushes allowed for live demo verification. Red TRON adapter/test changes remain excluded from releases.

[Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) · [manual](CQL_MANUAL.md) · [architecture](CQL_ARCHITECTURE.md) · [checklist](CQL_PARITY_CHECKLIST.md) · [backlog](AFTER_DEMO_BACKLOG.md) · [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).

Browser query timeout bug fixed and published: website7a02a23 aligns timeout with explicit server budget (up to30s plus5s transport allowance, minimum15s), no retry. All89client tests pass, including actual run() timer/one-dispatch regression; four stale session test harnesses corrected. Public HTML and versioned JS exactly match git blobs. Backend deployment4f6eba6c still QUEUED; observe same job, no duplicate build. New descendant fb29aa936 has another queued job571cb5f0-85f4-40fa-9d99-568339833b3e. It preserves TON code/Core pins but adds OLAND contracts; integrate/review it before applying against the latest successful exact release. Never weaken release guard or replay a deployment.

Preparation duration regression reproduced and fixed at website299c376: derived native setup query now carries validated paired budget before saves. All90client tests pass; live HTML/preparation/query assets match commit blobs. No TON card or new hosted cohort. Backend job4f6eba6c remains QUEUED; latest descendant job571cb5f0 also QUEUED; current successful app is6b5db934/job5191ee53. Next review/integrate fb29aa936 while observing existing jobs, no extra code build submission.

Latest checkpoint: descendant integration32/32pass, no skips; committed pins validate. Configuration baseline observation reports2changed values, TON source absent, zero host mutation. Do not print secret values or overwrite snapshots; prepare an explicit reviewed revision preserving current live settings and previous evidence. TON recovery report consolidated to50lines; detailed history preserved in Archive/TON_PUBLIC_SOURCE_HISTORY_2026-11-01.md.

Reviewed configuration revision ready: ton-public-duration-oland before/candidate DPAPI snapshots preserve all live unrelated values; exactly3TONkeys differ and their values equal the tested candidate. No mutation intents/ACKs. New pure revision guards reject unsafe names and any prior mutation evidence. Future apply uses unchanged release guard plus generic apply_values with this exact revision prefix, never original stale CLI apply. Source owner/catalogue unchanged.

Code deployment milestone:4f6eba6c at exact24f548550 is SUCCESS. Newer descendant571cb5f0 at fb29aa936 remains QUEUED, so unchanged latest-release guard still blocks settings apply. Observe that same job, then recheck revised baseline immediately before guarded apply. No TON settings or acceptance sessions created; no duplicate deployment.

While latest TON release queues, TRON access diagnostic returned PublicNode HTTP403 before any validated header; stopped after one read, unchanged source/owner/no grant. Native429 gate remains red. No guessed sleep/retry or provider count change. [Diagnostic and sources](TRON_PUBLIC_SOURCE_STATUS.md#bounded-diagnostic-while-ton-rollout-waits).
