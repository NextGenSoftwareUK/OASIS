# COSMIC ORM / CQL — current handoff

Updated22November2026. Implementation branch: **work/cosmic-cql-ton-public**; website: **work/cosmic-cql**. Keep the full roadmap active. The physical checkout contains concurrent changes: use scoped Git-object publication; never reset, switch or stage unrelated files.

## Accepted live demo evidence

- Last fully source-verified seven-service cohort: root **09d61416ea4ed2926bf044e211fc73502ab4adbe**, Core **3696bd32ff84ee49db4b189dfcea946da7172dbd**. WEB4–WEB10 running revisions and loaded AutoLoadBalanceEnabled=true were verified. This is retained evidence, not an assertion that later shared rollouts still run09d. [Runtime proof](COMPUTED_SCALARS_RUNTIME_ACCEPTANCE_2026-11-22.json).
- Combined Boolean metadata CASE and nullable Int32 COALESCE passed live through website2973da77 on real MongoDB, SQLite and Neo4j providers: exact text/integer cells,18paced calls,3private creates, conditional acknowledged-revision deletes and exact absence. Visitor revoked; credentials removed. [Hosted proof](COMPUTED_SCALARS_HOSTED_ACCEPTANCE_2026-11-22.json).
- Earlier text COALESCE and selected-column SQL Concat live proofs remain accepted; do not repeat them. [Text](TEXT_COALESCE_HOSTED_ACCEPTANCE_2026-11-22.json), [sets](PROJECTED_SET_HOSTED_ACCEPTANCE_2026-11-22.json).
- HoloOASIS freshly returned one live CQL row at09:18UTC22November. Four paced calls, visitor revoked, credentials removed; no Holon writes or PIPER access. Exact running source was not established by this probe. Its completed checkpoint directory is removed; do not replay. [Fresh proof](HOLOOASIS_FRESH_READ_ACCEPTANCE_2026-11-22.json).
- Conductor readiness budget is480seconds (formerly240), returning immediately when ready. Peer HoloNET changes separately describe a900-second hApp-install ceiling. These are different timeouts. No conductor/lair reset or user-data migration.
- Last aggregate startup observation was105readers/103verified; it did not identify the remaining unavailable readers. Do not infer every provider is healthy from this aggregate or from the Holo probe.

## Active release — mapped computed rows

Current image candidate **c25ce0b7ac7d6b1de734e31d7bbc2c706de7c384** / **Corebabf7b82936f07d9decff57fac52b08ed527eb35** preserves reviewed Developmentf7516ea9: acknowledged session foundation, explicit Enumerable.Reverse framework fix, retired protocol-test include removal, ONODEManagerfa6c50a shared clients/port and WEB6 model catalogue0348ded2. The paired manifest is validated, including correction of the shared ONODEManager gitlink/pin mismatch. No Development push from this batch. [Candidate record](MAPPED_COMPUTED_RELEASE_CANDIDATE_2026-11-22.json).

**All three native/mixed journeys passed**, seven representative checks/provider:
- SQLite and Neo4j: retained45004657247/source5d39eeddbc29/Core5240d3bd.
- Mongo:45010074469/job137170251121/sourcec2c158a0/Core1d627b36; ZIP14750456409/1699bytes/SHAf201ea7888a3e0b8c66ee0088b763c257c3c5ec585ffdd0dc4c411be9d76ee9c verified in RAM.
- Native CASE/text/Int32 COALESCE, distinct literals, null/empty/zero, wrong-domain refusal, real JWT mixed full/virtual HTTP, stale saved-copy reconciliation, computed global paging, permissions, unchanged source/cache and exact cleanup accepted. Actual per-provider source refs remain in the [aggregate proof](MAPPED_COMPUTED_NATIVE_ACCEPTANCE_2026-11-22.json); no case is relabelled as executing on this new binary.
- Mongo's earlier failures were owned fixture errors: a64-character DB name, then projecting reserved _id as metadata. Its corrected ordinary payload key preserves native identity and provider guards. SQLite/Neo were not replayed.

[Mapped API45002198772](MAPPED_COMPUTED_WIRE_ACCEPTANCE_2026-11-22.json) passed3controls/build. [Combined session/mapped45009513105](MAPPED_COMPUTED_CURRENT_COMPOSITION_ACCEPTANCE_2026-11-22.json) passed3controls plus actual WEB4/WEB5 for5b489985/Core1d627b36; exact TRX/source stamp verified. Earlier45005838375 remains accepted forfbcb99a9/Core5240d3bd.

Because current dependency pins changed, exact-source compile **45014425835** was last observed **in progress** for candidatec25ce0b7/Corebabf7b82 (workflow control6f609a0e). Its dispatch was acknowledged once; do not replay. This is a dependency-graph build, not a repeat of accepted provider journeys. Exact image producer **45014821002** was also observed **in progress** for the same immutable candidatec25ce0b7; it runs alongside compilation to shorten the critical path. Both gates plus exact package acceptance are required before Development. Producer intent is durable in mapped-computed-image-handoff.json; never dispatch it again. No image/package or rollout acceptance yet.

Release gates:
1. Accept the current exact compile artifact/source stamp; preserve all three native proofs.
2. In parallel, produce the exact five-reader image; qualify its immutable package once the image is available. Both compile and package must pass before integration.
3. Freshly review shared Development/peer source and integrate a scoped verified batch.
4. Observe required rollouts/running source, then execute the bounded owned full/virtual live check.

## Next parity family — GroupJoin source implemented, unqualified

Shared Core now captures an anonymous outer-and-matches LINQ GroupJoin and assembles complete groups from one fully validated native pair stream. Actual MongoDB, SQLite and Neo4j readers carry both whole sources under their existing snapshots/transactions. Separate left identities survive repeated keys; right source order, empty matches and group-level Skip/Take are preserved. The original immediate flattened GroupJoin route remains native. No source-query delegate is compiled and no second independent source scan or managed predicate/join fallback is introduced.

Five focused Core controls and six real-native journeys are selected in one new CI workflow (two changed journeys/provider). C# syntax for17changed files, workflow YAML and PowerShell AST pass using existing tools in memory; these checks do not prove compilation or native behavior. Grouped-result projections/filtering/ordering beyond the direct carrier and further representative key/visibility checks remain open. Q44 and61/168+4/13 stay unchanged. [Source record](GROUP_JOIN_COLLECTIONS_SOURCE_2026-11-22.json).

Exact GroupJoin qualification **45019120717/job137197874230** is confirmed **queued** on source3a972d83/Coref8fc9bb7. Five Core and six native checks, no manual duplicate dispatch. Twenty queued automatic qualifications for only this agent's superseded intermediatee6b22d6b were cancelled and all20terminal cancellations verified; latest-source qualifications, the two frozen release jobs and other agents' work remain intact. [CI handoff](GROUP_JOIN_CI_HANDOFF_2026-11-22.json).

This successor remains **unqualified** and must not be swept into the active mapped-computed release: candidatec25ce0b7/Corebabf7b82 and its existing45014425835/45014821002 handles are unchanged. Publish only a qualified scoped batch, never the feature tip merely for synchronization.

## Next SQL capability — record reselection source, unqualified

CQL composition now has an additive typed Select stage for choosing/renaming current visible aliases after native filtering, sorting or paging. Shared Core retains all original native inputs and previous result stages, refuses re-entry of dropped aliases, validates hidden cells, and constructs records with final output aliases. The WEB4 wire formatter emits only final columns/cells; actor/provider/read scope and acknowledged usage remain unchanged. This is groundwork for nested SQL/CTE capture, not a claim that that text syntax is implemented.

Three Core/API controls and one real-native journey/provider are prepared in one isolated CI workflow, plus actual WEB4 compilation. Eleven C#files, workflow YAML and PowerShell AST parse in RAM using existing tools. No C# compilation/native/live acceptance yet. The GroupJoin45019120717 source/run remains separate and must not be replayed. Both successors stay excluded from the frozen mapped release. [Source record](RECORD_RESELECT_SOURCE_2026-11-22.json).

## SQL and Ask delivery

Website **83852a7556c03f507e5f252af5246114c3c85f18** inherits the parser/Ask changes from475a4ff3 and enrolls mapped row CASE/text-COALESCE/Int32-COALESCE, Id first and selected-alias ordering. Three parser controls and two changed Ask UI controls passed in RAM. Ask preserves the selected source, refuses mapping substitution, remains free-visitor scoped and requires review before execution.

[Latest exact preview source proof](COMPUTED_DEMO_PREVIEW_SOURCE_ACCEPTANCE_2026-11-22.json): dpl_4QjSbVPWeCHp5UgB7KUigbboBFvq serves matching HTML plus Playground/compiler/Ask bytes. A visible canonical CASE/COALESCE example now fills the editor, clears unrelated mapping selection and never auto-runs; its changed UI journey passed and its SQL compiled for all three databases. Browser cache versions advanced. This is not rendered-browser, real WEB6 inference, public website promotion or live mapped-backend acceptance. Grouped computed expressions remain refused.

Prepared hosted verifier: Scripts/Sandbox/Verify-HostedUnfilteredMapping.py --computed-rows --website 475a4ff37426c066ba4dea10a45e317276083bb1, with exact deployment/source arguments. It requires native/composition/package acceptance before issuing any visitor. It uses randomized logical aliases over approved read-only sample fields, one private mapping and full Holon/provider, immutable checkpoints, paced capped calls and conditional cleanup. Three exact SQL captures and Python AST passed offline. It has **not run**. Reconcile original outcomes rather than recreating fixtures.

## Coordination and deployment

- Shared Development last reviewed: **f7516ea95979c41b1c9df62f6bd5ab108d61486b**. Verified phase-one session foundation, worker-gate correction and corrected ONODEManager pin are preserved in the combined source. Loaded rollout acceptance remains separate from source integration.
- Peer HyperDrive status last reviewed: accepted session foundation is separately composing/compiling; strict seven-service session enforcement remains unshipped. Do not import unqualified auth/Core changes or overwrite peer work. Review C:/Source/OASIS2-hyperdrive/Docs/WIP/HyperDrive-Providers/CURRENT_STATUS.md and shared Development every30minutes/before another provider.
- Work on the one agreed feature branch. Normal COSMIC Development pushes at most once/hour, qualified batches only; critical fixes may go sooner. Last own push08:04:01UTC22November. No empty/unverified push just to meet the clock. No staging/production authorization.
- Core gitlink and dependency manifest move together; official sync and require-gitlinks validation are mandatory. No moving dependency branch pins.
- PIPER authoritative collection and caller-permission invariants remain required. No PIPER data mutation, DNA setting change or shared Atlas stress/soak/polling.

## Full scope remains incomplete

**61/168 feature rows plus4/13 separate extensions** are verified; representative feature checks do not establish full EF/NHibernate parity. [Checklist](CQL_PARITY_CHECKLIST.md). Keep functional implementation, representative verification and hardened coverage separate.

Delivery order:
1. Functional feature breadth in shared Core and real MongoDB/SQLite/Neo4j providers, then the other priority providers.
2. Complete SQL/CQL text and real WEB6 Ask Your Data flows.
3. Full/virtual/hybrid workflows and effective per-avatar/app OPORTAL policy execution.
4. Broader edge coverage, optimisation and hardening; fix serious regressions immediately.

Preserve the entire real SDK/client/API provider programme and priorities: HoloOASIS, Solana, Ethereum, Polygon, Arbitrum, Near, EOS, SOLID, IPFS, ActivityPub, Cosmos, Polkadot, TON, TRON and Hedera, plus Supabase priority and remaining popular providers. LocalFile is already scoped/done and is not a current priority.

Open examples:
- Standalone GroupJoin now has native source implementation for the direct anonymous outer-and-matches carrier, complete ordered matches and group-level paging. Compilation/native qualification and broader grouped-result composition/key/visibility acceptance remain open. No full Q44/EF parity or live acceptance is claimed.
- General CASE predicates, multiple WHEN/numeric branches, broader computed expressions and ORM/model/session/transaction families; Q55 stays incomplete.
- Broader selected-set tuples/domains and nested composition. Existing scalar/entity set proofs remain valid.
- Real WEB6 inference through reviewed SQL to WEB4 results, rendered browser acceptance and public website delivery. Deferred model/account actions stay in the owner list; do not repeat expired sign-in/grant requests.
- Memory caching and full intelligent hybrid policies are unfinished. Never claim memoryCacheSupported=true. Saved mappings, full imports and mixed identity support do not prove the whole policy engine.
- Original SQLite unfiltered-snapshot hosted attempt stopped before first save on checkpoint WinError5. Preserve its original journal; do not replay it. Accepted Mongo/Neo snapshot cohorts remain preserved.
- Historical Holo hosted-listener cleanup is separate and still open; an encrypted rehearsal is not live cleanup acceptance.

## Resources and records

- Last disk observation:35.99GiB free. This is an observation, not a claim this slice freed that space.
- No local builds, database/model/runtime installs, images, new worktrees or saved artifact archives. Existing Node checks and preview/artifact inspection run in RAM. CI uses isolated owned databases, bounded workers and the existing build lease.
- Keep compact evidence and uncertain-operation journals; remove owned disposable files only after processes stop and durable proof is verified. [Cleanup ledger](RECOVERY_LAPTOP_CLEANUP_LEDGER.md).
- Owner actions belong only in [ACTIONS-FOR-DAVID](../Release%20and%20Status/ACTIONS-FOR-DAVID.md); no secrets in chat/source.
- Historical outcomes, exact gate/run/hash evidence and the complete roadmap remain in [Full progress report](FULL_PROGRESS_REPORT_2026-11-21.md), linked JSON proofs and Git history. This handoff replaces stale stage descriptions; it does not discard their evidence.

Shared Development advanced to verified session foundationae341256/Corec486c152 while the worker-only composition was being prepared; stale-source guard stopped it before publication. Current source preserves its exact six Core auth paths and934manager manifest, combines only our three native-mapping Core paths, and excludes unqualified strict enforcement618c3c73. New Core1d627b36d81e53284a51455baca3e4c733a2b13a / release5b48998592d677a9a8ab020e8972593fe697f70f requires a new current-graph3control/WEB4/WEB5 compile check because C# changed. Accepted compositionfbcb99a9 remains retained under its original source. Native query/provider code is unchanged, so accepted SQLite/Neo and pending Mongo correction are preserved. Pure changed worker-gate controls passed6 in RAM; manifest verified. No image dispatch or Development push.

Mongo-only45006551717 failed at raw original-row snapshot read after native computed assertions: fixture projected reserved Mongo _id as metadata, which QueryNativeRecordsCoreAsync explicitly refuses. Corrected only Mongo fixture to keep _id as identity and expose a separate ordinary key payload; SQLite/Neo paths are unchanged. Shipping provider guard remains intact. Both owned container cleanup steps succeeded. Focused Mongo retry on current combined Core1d627b36 follows; current-graph45009513105 remains queued and source5b489985 is unchanged by this fixture-only correction.

All three mapped native journeys accepted: retained SQLite/Neo45004657247 and corrected Mongo45010074469 (seven checks each, real mixed HTTP/global paging/permissions/unchanged-data/exact cleanup). Current5b489985/Core1d627b36 composition45009513105 also passed3controls and actual WEB4/WEB5 builds; source-stamped artifact14749818775/1954bytes/SHAe94469673ef722bb9c37064e99f8ce6444b35c7c45d6d14e8d782e67afe84a7e verified in RAM. Mongo artifact14750456409/1699bytes/SHAf201ea7888a3e0b8c66ee0088b763c257c3c5ec585ffdd0dc4c411be9d76ee9c verified.

Development throughf7516ea9 adds an internal one-line explicit Enumerable.Reverse compatibility fix and removes a retired compile include from a protocol-test project. Preserved both in current Corebabf7b82936f07d9decff57fac52b08ed527eb35 / image candidatec25ce0b7ac7d6b1de734e31d7bbc2c706de7c384. Strict diffs preserve mapped code and API source. Current ONODEManager shared-client/port and WEB6 model-catalogue pins are also preserved; a current dependency-graph compile is required. Native/compile proofs keep their actual sources; this new binary is not labelled exact-source compiled by the earlier gate. A single current-graph compile will cover the new dependency pins; accepted native cases are retained. Exact image/package qualification remains mandatory. No unchanged native cases repeated, image dispatch yet or Development push.

Peer coordination22November: reviewed Developmentca4f8336 and current HyperDrive status. Peer reports WEB6 startup AWS SDK major-version mismatch and owns its ABI/startup hotfix; real Ask acceptance must wait for that verified recovery. No duplicate fix or WEB6 deployment from this task. Preserve the peer's future verified dependency pin when composing the next release.
