# Release end-to-end proof ledger

## Historical frozen candidate

Master `28a2eb25abd2394a910b10da1a6572ba576ede0c` is the candidate. Later documentation-only Development commits do not change this evidence scope. A passing build is not proof that a new version was uploaded, installed or exercised on a physical device.

## Current integrated verification

[Global release run 38671614947](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/38671614947) was dispatched from master with `publish=false`, `nuget_plan_only=false`, Automatic versions, all first-party NuGet packages, OASIS Runtime, STAR Runtime, Native Endpoint, MCP, standalone Edge, both OGEngineClient profiles and Edge Unity selected. Optional components are `none`; the legacy OurWorld GameCI route is prohibited.

The selected workflow must prove, in one run:

1. Source/provenance and version-selection regressions pass; source-aware planning records frozen unchanged packages and exact changed candidates.
2. Selected NuGet archives pack from that frozen plan and pass whole-set immutable-content/parent-provenance preflight.
3. Required platform artifacts build and archive validation passes for selected runtime/client families.
4. Unity integration validation and distinct Unity wrapper packing pass without invoking Unity Cloud or claiming IL2CPP acceptance.
5. Artifacts and release notes are retained on the hosted runner; applicable build jobs succeed. Publication jobs must remain skipped.

**Result: FAILED**, in Edge validation because required HoloNET live tests were skipped. This historical run does not prove the current repaired source. Subsequent repairs provisioned live conductor/hApp fixtures, froze external source/artifact identities, and introduced two isolated mandatory zero-skip phases. A failure must be diagnosed in its owning layer, not marked an acceptable skip.

## Active 0.7.1 repair proof

- OASIS candidate `fc2150414374f08f01b7bec86212aac6685c2113`: [Edge/mobile/test verification 38811439032](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/38811439032), currently in progress. This includes Android runtime source pin `b01c4faa88db8d9a81e224789b2e5b23b8dca07f` and mandatory shared-conductor/auto-start TRX reports. Neither phase is yet declared accepted at this candidate.
- Mobile source resolution: [38811293124](https://github.com/NextGenSoftwareUK/android-service-runtime/actions/runs/38811293124) SUCCESS; conductor/API/types/keystore lock exactly0.7.1. Source resolution is not binary acceptance.
- iOS device/simulator build and smoke tests: [38811394578](https://github.com/NextGenSoftwareUK/android-service-runtime/actions/runs/38811394578), in progress at runtime source `b01c4faa88db8d9a81e224789b2e5b23b8dca07f`. Existing old iOS archive pins must not be relabelled as0.7.1.
- Superseded desktop-only candidate run38810772659 was cancelled after the current mobile-inclusive run started, avoiding duplicate heavy validation.
- After these pass: inspect retained zero-skip reports and version/lock/archive provenance, complete exact-source checked promotion, and run integrated publication-disabled validation on the resulting master candidate. No new public versions or duplicate uploads are authorized merely to obtain proof.

## Existing evidence, separately established

| Contract | Evidence | Limit |
|---|---|---|
| Exact planner fix | CI 38662798687 and preview 38663267084 succeeded at `1aa8cb467`; eight Python regressions plus PowerShell version checks | Preview did not pack or publish |
| Checked production promotion | PR487 exact head `13ac9234` passed 45 checks; normal exact-head merge produced the frozen master candidate | Not a pre-production staging gate |
| Post-merge master CI | Run 38666007002 succeeded at the frozen candidate | CI success does not prove every external-service/device contract |
| Deployment availability | All fourteen staging/production WEB4–WEB10 deployments reached SUCCESS/SLEEPING at the frozen candidate; twenty canonical HTTPS GETs returned200 | Availability, not complete authenticated functional acceptance |
| Packing/preflight | Earlier runs 38489087522 and 38494676244 succeeded for 220 candidates without uploads | Earlier source graph, not the new integrated run |
| Actual publication | Bulk229 run38376265788 and targeted OGEngineClient2.0.3/WEB6Core2.0.4 publicly verified; GitHub/npm families inventoried in OASIS_RELEASE_STATUS.md | Historical released versions, not every subsequent source change |

## Boundaries that prevent a universal completion claim

- Actual upload/install verification for a new changed version is different from a publication-disabled run. Existing immutable releases must not be recreated or bumped merely to make a test green. A new public-release decision must identify the reviewed candidates and intended versions.
- Optional installer/game dispatch jobs currently require `publish=true`, so they are outside this integrated nonpublishing proof. Their existing successful release runs remain separate evidence.
- The staging gate implementation is now promoted through PR488 to master91684d3d, with disabled staging/production push autodeploys and required human reviewer dellams. Its first live run38681331496 correctly stopped before deployment because the scoped Railway credential was absent. This proves a fail-closed prerequisite, not accepted staging, approval or production rollout; owner setup remains required in [the checklist](OWNER_SETUP_CHECKLIST.md).
- OurWorld Unity Cloud requires owner organization/project and scoped credentials. Physical-device, two-device and Unity IL2CPP acceptance remain unproved.

See [release status](OASIS_RELEASE_STATUS.md), [operations](RELEASE_WORKFLOW_OPERATIONS.md), [promotion policy](DEVELOPMENT_TO_MASTER_PROMOTION.md) and [Unity Cloud access](OURWORLD_UNITY_CLOUD_ACCESS.md).
