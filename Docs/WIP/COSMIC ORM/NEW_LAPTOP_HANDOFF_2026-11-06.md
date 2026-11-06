# COSMIC ORM, CQL and HyperDrive new-laptop handoff

6 November 2026. Use the existing COSMIC worktree on the recovered
laptop. Fetch the remote refs and compare them with any unpublished
work on that machine before integrating. The old-laptop qualification
branch is `origin/work/cosmic-cql-ton-public` at
`78de5630bd433d6d284e05f3fb1e441c3b8898f1`. The handoff branch
`origin/codex/cql-new-laptop-handoff` adds the unfinished work below.
Its parent and Railway manifest pin Core
`c1354d87da2add7720f9c40d4e502195c01a3f41`, available on Core's
`origin/codex/cql-world-id-reclaim-web4`.

```powershell
git fetch origin work/cosmic-cql-ton-public codex/cql-new-laptop-handoff Development
git log --left-right --oneline HEAD...origin/codex/cql-new-laptop-handoff
```

## Resume the Development WEB4 demo first

1. Use the recovered laptop's existing authorized Railway session or
   sign in privately if its session has expired. The old laptop still
   returned Unauthorized at 15:49 Cancun. The existing owner action is
   in [ACTIONS-FOR-DAVID](<../Release and Status/ACTIONS-FOR-DAVID.md#railway-access-for-development-web4-rollout-from-this-laptop>).
2. Resume the exact LocalFile candidate at
   `c64adf4748856b0acaae02e24a7f8edd16d2a139` and image
   `ghcr.io/nextgensoftwareuk/oasis-web4@sha256:11257dfebf23acfa6a321273baf98b8eefe2f83f8b430a862ab54ed9a168a6ce`.
   Confirm the Development instance and persistent `/data` mount,
   verify protected settings and the immutable rollout, then run the
   prepared acceptance and browser demo. [LocalFile runbook](LOCALFILE_CQL_MVP.md) ·
   [Candidate image CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/41948106729).
3. Continue with PouchDB candidate
   `419a41cf2000cd4ebdb5a9b21e69dfc6f004eaf3` after LocalFile.
   The old laptop verified Development still pointed at
   `b718f287933b7e79e862edbcf7d3bc13c37ba559`. The handoff
   introduced no Development or production rollout.

## Start from accepted evidence

Coverage is 118 scoped local, 60 live HTTP and 28 live browser.
Push Protocol, LayerZero v1, Pinata, World ID v4 and Reclaim
passed native 3/2, local WEB4/Mongo 5/4, and foreign-private 0.
Pinata reused its two acknowledged saves. The exact successful
fixtures were removed and the owned Mongo process stopped.
Waku passed native Linux REST and CQL 3/2/0. Decentraland
passed native Catalyst CQL 3/2 and both rejection tests.
Use the matrix links for exact test receipts and remaining scopes.
[Current status](CURRENT_STATUS.md) ·
[Provider matrix](PROVIDER_CQL_AND_SANDBOX_STATUS.md).

The pending ENS Offchain cohort is different. The old laptop
retains the exact synthetic Mongo database
`cosmic_cql_ensoffchainoasis_http` and its two version-one
acknowledgements under
`LocalState/COSMIC/ReviewedPublicMongoHttpQualification/ENSOffchainOASIS/`.
Preserve that environment and reconcile those exact receipts before
any continuation. Do not replay its writes or create a replacement
cohort. Use the protected Web3.bio credential only through the
existing approved path. [ENS evidence](ENS_OFFCHAIN_CQL_MVP.md).

## Unfinished branch changes

- Decentraland WEB4 adds its explicit canonical-owner setting
  `CQL_DECENTRALAND_SCENE_QUERY_SOURCE_OWNER`, named read admission,
  three admission cases, guarded local Mongo case and fixed protected
  catalogue. The 39 publisher checks and 21 response evidence checks
  passed. The WEB4 C# build has not run. Under the build lease, create
  and verify the scoped Core read tariff for `ProviderType.DecentralandOASIS`
  in `Services/Subscriptions/PreparedStorageExecution.cs`. Run
  `CqlReviewedNetworkHostedConfigurationTests` and
  `CqlPublicPolygonGatewayHostedConfigurationTests` together and expect
  32 checks. Freeze the exact binaries and run only
  `CqlReviewedPublicMongoHttpTests.DecentralandSceneNativeHttp`
  with the existing owned loopback Mongo setup. Advance the parent
  gitlink and manifest together after verifying the scoped Core pin.
  [Decentraland native evidence](DECENTRALAND_SCENE_CQL_MVP.md).
- The Graph Subgraph Studio draft has three public indexing-status
  reads for ADEXTO on Arbitrum One and Base and Klerosboard on Gnosis.
  All three endpoints returned current `_meta` with an exact
  deployment CID, block hash and timestamp. The adapter has no tests
  yet and has not been compiled. Studio endpoints are intended for
  testing. Production gateway acceptance needs a reviewed deployment
  and its approved access. [The Graph endpoint policy](https://thegraph.com/docs/en/subgraphs/querying/from-an-application/).

## Roadmap and laptop state

Keep the MVP breadth-first. Continue to full real SDK, client or API
implementation for every provider and full EF Core, EF6 and NHibernate
parity after MVP. The Virtual Holons proposal and roadmap carry the
replicated, pointer/mapping and hybrid import modes.
[COSMIC ORM roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md) ·
[CQL roadmap](CQL_COMPLETION_ROADMAP.md) ·
[Virtual Holons proposal](OASIS_Virtual_Holons_Import_Proposal.docx).

Use the recovered laptop's existing tooling and its own ledger. The
[old-laptop cleanup ledger](RECOVERY_LAPTOP_CLEANUP_LEDGER.md) is
historical evidence for the old machine. LocalState files, cached
binaries, tokens and databases remain on that machine. Restore or
recreate only the artifacts you need, and verify the resulting
producer hashes and exact scope before native tests or rollout.
