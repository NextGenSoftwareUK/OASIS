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
