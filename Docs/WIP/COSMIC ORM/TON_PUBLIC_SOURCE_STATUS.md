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

- Reviewed parent fb29aa936870dcfb3182102642e5997b2cedd90f / Core 2ac915f709ded7aabcd28a9db02aaea92fc52d97 deployed successfully: job 571cb5f0-85f4-40fa-9d99-568339833b3e SUCCESS. Combined integration controls 32/32 pass.
- Two concurrent baseline changes were preserved in reviewed revision ton-public-duration-oland. Three TON values/public review bytes matched the tested candidate; publisher controls 5/5 pass.
- Exact HEAD/live-release guard and full baseline check passed. All three settings applied once with skip-deploys; per-key ACK hashes and complete candidate matched. No source/wallet DNA changes.
- One journalled settings job e3ae497c-3f19-45b2-a577-b0fc40a7ae6b is BUILDING at exact fb29aa936. Original rollout intent/protected outcome retained under LocalState/COSMIC/HostedSourceRolloutCurrent. Never replay apply or deployment.
- Earlier ton-public and ton-public-duration snapshots are stale. Revised before/candidate snapshots and all three mutation intents/ACKs are preserved. No TON hosted acceptance session yet. Website task25733b4 stages the spaced TON command and pinned fb29aa936 guide/C# links; not published to main.
- Observe this exact settings job. After SUCCESS verify whole candidate still matches, then run Verify-HostedQuerySource.py TONOASIS --prepare once on the task branch. Preserve the original cohort across failures; only explicitly reconcile unfinished stages.
- Prove native 3/2, paired 5/4, reuse 0/2, privacy 0/3 and exact SQLite/Neo4j copies, record completion ACK, then publish the TON card with pinned local demo/guide links. Hosted count stays 38 until proved.

## Evidence

Raw reports remain in TestResults/TonPublicSourceCandidateCurrent; durable source/identities/ACKs remain in LocalState/COSMIC. Failed activation, paired 5s deadline and leaf 5s deadline outcomes are preserved in the archive and original reports.

- native-duration-admission.trx: 15 controlled passes, 25574 bytes, SHA256 e9063eb27327a51c6f60f70a1636fc9f16037b8984edd2cb1a05371819304b23.

- ton-mongo-http-native-budget-continuation.trx: 1 real local HTTP pass, 3341 bytes, SHA256 777e05b8e0f4f545e0d6dde2ebc0470ae8dfe2c5e33963c115479672c6f2fff8.

- merged-duration-admission.trx: 16 combined passes, 27168 bytes, SHA256 6777b1b5002255e3db86c873b07d34cb4967bf21a466aabe1a21da2a0afbf773.

Production billing, native replication, browser interaction and hosted TON success are not inferred from local HTTP checks. Current hosted provider count stays 38.

[Guide and C# demo](../../Devs/TON_PUBLIC_HEADER_CQL_GUIDE.md) | [Current task](CURRENT_STATUS.md) | [Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Preserved history](Archive/TON_PUBLIC_SOURCE_HISTORY_2026-11-01.md) | [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

- descendant-integration.trx: 32 combined passes, zero failures/skips; 50597 bytes, SHA256 abf100d2eb73fd3c65c966182f55ea9e308636e1c7ed07ae0cfe03b476abc7fb.

Completed preparation procedure (do not replay): prepare-revision oland created new protected snapshots without overwriting prior evidence; it refuses any prior TON mutation intent/ACK. The completed apply used COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT to the reviewed SHA, check out that exact commit in this worktree, call unchanged require_matching_release(configuration), then configuration.apply_values("ton-public-duration-oland", KEYS) through the existing generic publisher. KEYS are exactly CQL_TON_PUBLIC_SOURCE_JSON, PREPARED_STORAGE_ADMISSION_JSON, CQL_HOSTED_QUERY_EXAMPLES__TONOASIS. Returned to task branch before docs. Apply is complete; do not invoke it again or weaken the guard.

- Revision publisher controls: 5 passed; revision-publisher-controls.log, 208 bytes, SHA256 9daf9fa68c78d7e0c79733c7f2df7575c44c5cdf468677377d8c663e7704b711. Sanitized configuration-revision-review.json confirms exact scope/current baseline, zero mutation intents.

Acceptance runner now explicitly admits TONOASIS; --help check passes without creating any session or mutation. Execute its --prepare flow only after exact code/settings success. Existing completed cohorts remain frozen.
