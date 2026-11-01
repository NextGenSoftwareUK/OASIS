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
