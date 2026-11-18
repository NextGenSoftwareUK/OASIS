# Profile request scope and borrowed-provider lifecycle — verified release batch

Qualification source: `b3bb74ce95056f95e9ed7d0a557c575610dddedb`; Core: `30458c760e4e6c0ecbd8cd7496c4ad8f9ed6a04a`. Integration base: Development `0f7ebae449039c6580acd0f25c9709dc41ead529`.

The initial account read uses normal HyperDrive routing. Its canonical response-owned serving-provider fact pins the detail read and subsequent manager operations to the same primary. The context retains replication policy, isolates local routing changes and owns mutable save collections; it is not an authorization grant or a two-record transaction. Missing detail never produces a fabricated blank record or an unrelated replica detail.

Native qualification found a legacy default-selection path switching to the first inherited failover entry, then deactivating a shared Mongo provider on restoration. ProviderManager now resolves scoped selection through its existing resolver, honors the captured default, rejects scoped global selection/shared shutdown and avoids reinitializing borrowed active instances. The host registry/selection remains unchanged. Offline admission remains enforced before resolver activation.

Core changes from deployed `30c89aaf55ff7f2eae25d1217b2736e50fd0452a` are confined to `AvatarManager-Save.cs` (exact existing774c credential fix), `AvatarAuthResponse.cs` (profile-only detachment helpers), new `AvatarManager-ProfileRequestContext.cs`, and `ProviderManager.Register.cs` (scoped lifecycle ownership). Preserve newer COSMIC/CQL changes when reconciling; do not replace whole differing Core/project/DNA files.

Evidence:

- [Input contract44313299801](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/44313299801): API+16/16 pure presence/validation cases.
- [Field application44316490165](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/44316490165): API+4/4 real Mongo/SQLite field/state-preservation and refusal cases.
- [Existing credential fix44321827257](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/44321827257): API+4/4 legacy/V2 x sync/async, each real owned Mongo/SQLite.
- [Final scope/lifecycle44333019866](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/44333019866): API+9/9, artifact14440871670/2542bytes, zero failed/skipped, finally absence assertions and container/volume cleanupSUCCESS. The earlier44329355181 failed6/7 and is retained as root-cause evidence, not acceptance.

OPORTAL's independent optional-field completeness correction is live on dev `9630d828c5ee6636a23714ff1daba6f0d2b3d8d5`:9 local shipping-JS cases; exact public script/page delivery verified without a backend call. The rest of the profile save operation still requires orchestration, ownership/uniqueness/partial-outcome/concurrent-state proof and SDK/frontend migration. No complete OPORTAL/provider/HyperDrive acceptance is claimed.

All shared Atlas stand-down controls and deliveries535/543 remain unchanged. No synthetic login, refresh, health, shared database test, queued-delivery replay or runtime DNA change. This release's running-host acceptance must be recorded separately after actual source/process verification.

Published Development: `f19a373947d28049bd9ac7ab960541e91ca7a13b`, non-forced fast-forward/readback from0f7. Runtime/CI/test bytes matchb3 exactly and paired dependency pins passed `--check-ref`; the public-manager CSV also matches.18 scoped paths; shared status/cleanup history retained.8 of the9 final cases exercise real Mongo/SQLite and1 verifies detached collection ownership.

Initial normal dev rollout metadata (new sourcef19, old0f still running/accepted): WEB4d0ad6756-4ca0-485c-a246-b191f3b0c889 QUEUED; WEB501112432-c58b-46c6-9329-69ebfd78ceb8 BUILDING; WEB61b8454f3-6b2f-4fc1-bd87-e26f4251f089 QUEUED; WEB7da4d704b-bd19-4b49-8dbb-87914787f13d BUILDING; WEB8e29750f2-eb55-4f09-9159-4d5cb9dc1426 BUILDING; WEB983c264ad-6455-4743-89e5-7eb5ff35fc33 QUEUED; WEB10f9c1d673-9fa4-425c-a8b0-42f422d34d14 QUEUED. These are deployment handles, not running acceptance. No duplicate/manual deployments or synthetic/backend requests. Staging/production unchanged.
