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
Next implement one explicitly reviewed continuation of the original pre-persistence fixture with the same IDs and fresh no-DB check, preserving first failure; capture readiness detail before writes. Local/hosted HTTP acceptance remain unproved; no TON deploy/grant.
