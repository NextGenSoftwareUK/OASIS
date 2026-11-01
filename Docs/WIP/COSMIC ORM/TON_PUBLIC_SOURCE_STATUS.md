# TON public-source status

Updated 1 November 2026. Local CQL, HTTP federation and privacy checks pass. Hosted rollout and browser acceptance remain open; full ORM parity is incomplete.

## Verified scope

- Native CQL and actual HolonManager/C# demo: 3 records and a 2-key subset.
- Decoder controls: 32; pacing/query controls: 13; bounded HTTP-error controls: 3.
- Local HTTP: native 3/2, TON + Mongo 5/4, foreign private 0/public 3, nine query receipts.
- Original two private Mongo saves acknowledged at version 1. Read continuations performed zero saves.
- Success-only cleanup verified the owned fixture database absent and Mongo port 27029 stopped. Never replay the cleaned fixture.
- Explicit HTTP leaf/federation duration: 1..30000 ms, default 5000 ms. All four TON presets request 30000 ms.
- API duration/admission/schema controls: 15 passed. Combined Development plus actual revised preset controls: 16 passed, zero skips. Publisher controls: 3 passed.
- Website 299c376: browser timer and derived setup-read duration corrections published; 90 client tests pass. Public HTML and both CQL scripts match committed blobs. This is not browser interaction proof.

## Dataset and semantics

- Three explicit Mainnet descriptors: 102765318, 102765319, 102765320.
- Active catalogue: LocalState/COSMIC/HostedTonPublicSourceCurrent/source.json, 542 bytes, SHA256 dcb8332a908dd9cbb6d686e3df444eaca46cc53f48d1b593a73dc31f4b1f762e.
- Original archive catalogue preserved: source.archive-10000000.json, 539 bytes, SHA256 deade2f66fb678ff1945460b87124bcc18a3cd6488c50d612d6107d84b42efa3.
- Explicit reviewed dataset promotion preserved the source owner. No fallback to the old catalogue, wallet operation or chain transaction.
- Read-only native-key equality/membership and admitted paging; arbitrary Name/Description scans and owned TON writes are not implemented by this profile.
- Official signed Int32 validator hash domain preserved as string metadata; exact counters/logical times retained.
- One endpoint-wide gate waits 1.1 seconds after completion. No automatic retries or alternate endpoint.
- Complete selected headers validated before paging. Bounded operator error detail retained without partial rows.
- Gateway observations do not independently prove consensus or universal archive availability.

## Deployment and exact next step

1. Tested code published: parent 24f548550dd2caaf99de00c2bf44003b2a2f8219 / Core 2ac915f709ded7aabcd28a9db02aaea92fc52d97. Remote heads verified.
2. Original app job 4f6eba6c-5ee4-4afe-95a5-07849a92d7ec is SUCCESS; do not resubmit.
3. New descendant fb29aa936870dcfb3182102642e5997b2cedd90f preserves TON code/Core/ONODE pins, adds OLAND contracts. Job 571cb5f0-85f4-40fa-9d99-568339833b3e is QUEUED. Integrated into task99b4f080; changed contracts/schema/admission/actual presets pass 32/32, zero skips.
4. Latest observed successful app is 24f548550/job 4f6eba6c; newer fb29aa936/job 571cb5f0 remains queued and the unchanged latest-release guard prevents apply. No TON settings have been applied.
5. Latest reviewed prepare-only prefix: ton-public-duration-oland. Earlier ton-public and ton-public-duration candidates are stale. Protected before/candidate snapshots live under LocalState/COSMIC/HostedSourceRolloutCurrent; public review is TestResults/TonPublicSourceCandidateCurrent/ton-duration-oland-prepared-review.json. Old ton-public candidate is stale and must never be applied.
6. Observe existing jobs. Two concurrent baseline changes are preserved in the new oland revision; all three TON values and public review bytes match the tested candidate. Five publisher controls pass. Never apply a stale snapshot. After exact reviewed code SUCCESS, temporarily check out that exact commit in this worktree for the release guard. Apply revised settings once, preserving per-key intents/ACKs and all unrelated live configuration. Return task branch before doc commits.
7. Journal one settings rollout; reconcile unknown outcomes, never blindly replay. Then run one original hosted acceptance cohort and publish the TON card only after its gates pass.

## Evidence

Raw reports remain in TestResults/TonPublicSourceCandidateCurrent; durable source/identities/ACKs remain in LocalState/COSMIC. Failed activation, paired 5s deadline and leaf 5s deadline outcomes are preserved in the archive and original reports.

- native-duration-admission.trx: 15 controlled passes, 25574 bytes, SHA256 e9063eb27327a51c6f60f70a1636fc9f16037b8984edd2cb1a05371819304b23.

- ton-mongo-http-native-budget-continuation.trx: 1 real local HTTP pass, 3341 bytes, SHA256 777e05b8e0f4f545e0d6dde2ebc0470ae8dfe2c5e33963c115479672c6f2fff8.

- merged-duration-admission.trx: 16 combined passes, 27168 bytes, SHA256 6777b1b5002255e3db86c873b07d34cb4967bf21a466aabe1a21da2a0afbf773.

Production billing, native replication, browser interaction and hosted TON success are not inferred from local HTTP checks. Current hosted provider count stays 38.

[Guide and C# demo](../../Devs/TON_PUBLIC_HEADER_CQL_GUIDE.md) | [Current task](CURRENT_STATUS.md) | [Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Preserved history](Archive/TON_PUBLIC_SOURCE_HISTORY_2026-11-01.md) | [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

- descendant-integration.trx: 32 combined passes, zero failures/skips; 50597 bytes, SHA256 abf100d2eb73fd3c65c966182f55ea9e308636e1c7ed07ae0cfe03b476abc7fb.

Revision procedure: prepare-revision oland creates new protected snapshots without overwriting prior evidence; it refuses any prior TON mutation intent/ACK. For apply after exact code SUCCESS, set COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT to the reviewed SHA, check out that exact commit in this worktree, call unchanged require_matching_release(configuration), then configuration.apply_values("ton-public-duration-oland", KEYS) through the existing generic publisher. KEYS are exactly CQL_TON_PUBLIC_SOURCE_JSON, PREPARED_STORAGE_ADMISSION_JSON, CQL_HOSTED_QUERY_EXAMPLES__TONOASIS. Return task branch before docs. Do not invoke original stale apply or weaken the guard.

- Revision publisher controls: 5 passed; revision-publisher-controls.log, 208 bytes, SHA256 9daf9fa68c78d7e0c79733c7f2df7575c44c5cdf468677377d8c663e7704b711. Sanitized configuration-revision-review.json confirms exact scope/current baseline, zero mutation intents.
