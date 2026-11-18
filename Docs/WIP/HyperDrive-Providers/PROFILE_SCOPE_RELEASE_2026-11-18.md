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

2026-11-18 running acceptance: all7 are sole-active SUCCESS on `f19a373947d28049bd9ac7ab960541e91ca7a13b`. Exact process source/deployment, mapped Core, settlement literal, both new `AVATAR_PROFILE_ROUTE_REQUIRED` and `HYPERDRIVE_SCOPED_PROVIDER_SHUTDOWN_DENIED` literals, true settlement hold and1 positive paused startup marker were verified per host. This is selected process/assembly/startup evidence; no API or database request.

| Service | Deployment | Running instance | API PID |
|---|---|---|---|
| WEB4 | `d0ad6756-4ca0-485c-a246-b191f3b0c889` | `2ec3c39d-a2f5-4895-be69-d9a44f066374` | 230 |
| WEB5 | `01112432-c58b-46c6-9329-69ebfd78ceb8` | `4bce742d-ca53-4b64-b954-a90bf1b155d0` | 1 |
| WEB6 | `1b8454f3-6b2f-4fc1-bd87-e26f4251f089` | `d9a803db-092b-400a-a8f6-13d9152d446d` | 1 |
| WEB7 | `da4d704b-bd19-4b49-8dbb-87914787f13d` | `08a1fb1a-d03b-43a1-a835-480757e8ff2b` | 1 |
| WEB8 | `e29750f2-eb55-4f09-9159-4d5cb9dc1426` | `a0bd0211-7eed-4ab6-b0d8-67eb9eec13f3` | 1 |
| WEB9 | `83c264ad-6455-4743-89e5-7eb5ff35fc33` | `1289c8dd-bc26-4474-855a-4ff8dd12ec35` | 1 |
| WEB10 | `f9c1d673-9fa4-425c-a8b0-42f422d34d14` | `af9df3b5-8c20-4867-a8aa-cd65f3c01bc0` | 1 |

WEB4 also verified all4 actual holds=true, mapped API, maintenance literal and the retained audit refresh/atomic seal/complete-policy projection feature markers. Selected saved fields show WEB4's4 applicable holds=true and WEB5–10's settlement hold=true; the other3 keys are intentionally WEB4-only (per the runbook). Foreground SUBSCRIPTION_USAGE_ENABLED remains saved true on all7. No settings were changed to satisfy a checker; the initial blanket four-key expectation was corrected to the documented applicability. Staging/production and535/543 remain unchanged.

The operator inspector's optional `-VerifyProfileScope` checks both fixed new markers in the mapped Core file and never emits full SSH/environment/DNA/assembly contents. PowerShell syntax check and all7 actual inspections passed. Default settlement-only use remains unchanged. Running code/control acceptance does not complete profile API orchestration, concurrent write/uniqueness proof or OPORTAL end-to-end behavior.
