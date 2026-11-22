# COSMIC ORM / CQL — current handoff

Updated 22 November 2026. Work only on `work/cosmic-cql-ton-public`; website uses its existing `work/cosmic-cql` branch.
The physical checkout contains unrelated concurrent changes: use the established scoped Git-object publisher; do not reset, switch, stage or overwrite them.

## Live Development

- Root **a96652767c3b382cdbd9e4ccaa80516358abff53**, Core **eef9f1a1d6bca1041c0971fadf764c2664f64d26**. Normal fast-forward from reviewed peer warning-email commit c3f70353; peer changes preserved.
- All seven WEB4–WEB10 deployments succeeded. Exact running source and startup **AutoLoadBalanceEnabled=true** verified on every service. [Runtime evidence](TEXT_COALESCE_COMBINED_RUNTIME_ACCEPTANCE_2026-11-22.json).
- SQL text coalescing through the exact website compiler passed on real MongoDBOASIS, SQLLiteDBOASIS and Neo4jOASIS: one exact replacement cell/provider, 18 paced API calls, three private fixture creates, conditional acknowledged-revision deletes and exact absence. Visitor revoked; credential files removed. [Live evidence](TEXT_COALESCE_HOSTED_ACCEPTANCE_2026-11-22.json).
- Selected-column SQL Concat previously passed on all three providers; preserve this evidence rather than replaying it. [Selected sets](PROJECTED_SET_HOSTED_ACCEPTANCE_2026-11-22.json).
- HoloOASIS previously passed a real native live CQL read. Conductor readiness budget is 480 seconds (formerly240), returning immediately when ready; admin bind remains separate. Recovery does not prove intermittent conductor joining can never recur. No conductor/lair reset or user-data migration.
- Last full startup diagnostic reported105 readers/102 verified, with CosmosBlockChainOASIS, HashgraphOASIS and ArweaveOASIS unavailable. This is an earlier startup snapshot, not proof of their current availability or of a current Holo failure.
- No staging/production promotion, PIPER data mutation or DNA-setting change in these query slices.

## Next release: conditional text

**Typed Boolean metadata candidate:** root **01de83ad1705423bd7f60e9a8fdb35e2dbf033ed**, Core **74d4f046b2f49893a19c40e161e73999772ab412**.

- Native qualification44995661257/job137126021674 passed three real-provider journeys, six checks/provider: canonical and typed metadata labels, branch identity, selected filtering, private-row exclusion and invalid-domain/code refusal. Four isolated fixture records/provider; exact cleanup and containers stopped. [Native evidence](BOOLEAN_METADATA_CASE_NATIVE_ACCEPTANCE_2026-11-22.json).
- API qualification44995661351/job137126022382 passed three controls and actual WEB4 compilation. [Wire evidence](BOOLEAN_METADATA_CASE_WIRE_ACCEPTANCE_2026-11-22.json).
- Website **5f60b8a3d4afbf7948f5f3076e6a988f517cd2ac**: four changed SQL parser controls and one focused Ask review UI journey passed. CASE accepts IsActive/IsPublic or a known CAST(metadata.flag AS BOOLEAN); literal text branches only, bounded512characters. Metadata is required: invalid/null/missing values must error rather than choose false.
- Exact preview HTML/compiler/playground/Ask bytes accepted, responses inspected in RAM. [Preview evidence](BOOLEAN_METADATA_CASE_PREVIEW_SOURCE_ACCEPTANCE_2026-11-22.json). No rendered-browser or real WEB6 inference acceptance.
- Exact image **44996549738/job137128701744** is running at last authoritative observation. Package inspection, Development rollout and live conditional SQL remain pending.
- Earlier canonical-only candidate565aab06 has accepted image44994753282 and package44995695068; [package evidence](BOOLEAN_TEXT_CASE_PACKAGE_ACCEPTANCE_2026-11-22.json). It is not deployed. Prefer one qualified batch containing both canonical and metadata conditions when ready, rather than two unnecessary rollouts.
- Last own Development push completed **06:35:52.487838UTC**. Next ordinary push **not before07:35:52.487838UTC**, and only after image/package acceptance plus fresh shared-branch/peer review. Never force-push or sweep unqualified peer changes into deployment.
- Prepared hosted verifier: `Scripts/Sandbox/Verify-HostedOwnedNativeSets.py`, explicit `--selected-columns --boolean-text-case --boolean-metadata-case`, fresh bounded fixture name and exact source/website/deployment. Requires its own accepted release journal, unique ready source descriptions before writes, immutable checkpoints and exact cleanup. Not executed for conditional metadata yet.
- Durable private journals under `LocalState/COSMIC`: `boolean-metadata-case-*.json`, `boolean-text-case-image-handoff.json`, `text-coalesce-combined-image-handoff.json`. Preserve unknown-outcome journals; never replay a dispatch, create, delete or session issuance blindly.

## Next source-only breadth work

Nullable Int32 coalescing captured in Core703310f2 and implemented in the three native translators through the existing coalesced-cell owner. No transport/SQL/native qualification or deployment acceptance yet; do not include this successor in the frozen01de release.

## Full scope and remaining gaps

Full EF/NHibernate parity remains incomplete: **61/168 feature rows plus4/13 separate extensions**. Representative feature checks are not full parity or hardened coverage. [Checklist](CQL_PARITY_CHECKLIST.md).

Delivery order remains:
1. Functional feature breadth across shared Core and real MongoDB/SQLite/Neo4j shipping providers, then remaining priority providers.
2. Complete SQL/CQL text and real WEB6 Ask Your Data flows.
3. Full/virtual/hybrid workflows and effective per-avatar/app OPORTAL policies.
4. Expanded edge coverage, optimisation and hardening; serious performance regressions get immediate attention.

Preserve the complete real SDK/client/API provider programme. Provider priority includes HoloOASIS, Solana, Ethereum, Polygon, Arbitrum, Near, EOS, SOLID, IPFS, ActivityPub, Cosmos, Polkadot, TON, TRON and Hedera, plus Supabase priority and the remaining inventory. LocalFile is already scoped/done and is not a current priority.

Open examples:
- General CASE predicates/multiple WHEN branches/numeric branches and numeric coalescing; computed mapped-source functions. Current Q55 stays incomplete.
- Broader selected-set domains/tuples, nested set composition and remaining general ORM feature families. Preserve accepted scalar/entity set evidence.
- Real signed-in WEB6 draft through review to actual WEB4 results; rendered hosted browser walkthrough and public website promotion. Existing UI tests/served assets do not prove those flows. Model credentials/configuration remain governed by OASISDNA and deferred owner actions; do not repeat expired grant/sign-in requests without a concrete need.
- Saved virtual definitions, full imports and mixed identity coexist. In-memory cache and full intelligent hybrid policy execution remain incomplete; do not claim memoryCacheSupported=true.
- Earlier SQLite unfiltered-snapshot hosted attempt stopped before its first save on Windows checkpoint WinError5; original cause remains unproven and its original journal must not be replayed. Mongo/Neo accepted cohorts remain preserved.
- Historical Holo hosted-listener cleanup remains a separate open item; encrypted rehearsal is not live cleanup acceptance.
- PIPER authoritative-collection/permission invariants must remain intact. Never accept a partial replica collection as complete.

## Coordination, resources and evidence

- Review Development and `C:/Source/OASIS2-hyperdrive/Docs/WIP/HyperDrive-Providers/CURRENT_STATUS.md` every30minutes and before a new provider. Peer billing/session work is separate; latest observed combined build44995824952 was pending and excluded from our deployment.
- Normal COSMIC Development pushes at most once/hour; emergency fixes may go sooner. Use one task branch, no extra worktrees/environments.
- Core gitlink and Railway dependency manifest move together; run official sync and require-gitlinks validation.
- No local builds, runtime/model installs, database containers, image downloads or archive files for these slices. Use pinned isolated CI services, bounded workers and the existing build lease.
- About44.9GB decimal free at last disk observation. Artifact/preview bodies inspected in RAM, compact hashes/reports retained. [Cleanup ledger](RECOVERY_LAPTOP_CLEANUP_LEDGER.md).
- Current coalescing fixture is fully closed; no credential files remain. Stop on an ambiguous write outcome and reconcile the original operation, never recreate it.
- Owner-only actions belong solely in [ACTIONS-FOR-DAVID](../Release%20and%20Status/ACTIONS-FOR-DAVID.md); keep secrets out of chat/source.
- Detailed accepted/failed evidence and prior chronology remain in [Full progress report](FULL_PROGRESS_REPORT_2026-11-21.md), linked JSON reports and Git history. This handoff replaces repetitive historical status paragraphs without discarding those evidence sources.
