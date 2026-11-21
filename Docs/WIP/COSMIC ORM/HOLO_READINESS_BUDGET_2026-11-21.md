# Persisted Holo cell readiness budget — 21 November 2026

David explicitly requested doubling the current timeout. The current shipping owner is HoloOasisDevelopmentConductor.PersistedCellReadinessBudget, increased from 120 to 240 seconds. This waits for the exact persisted app cell; it does not replace native readiness with a timer, retry provisioning, create another interface, or reset conductor/lair data.

Historical commit918da95d740fb296f0417369d2380f70ae3a4036 separated this deadline from HoloNET admin binding after an observed 73-second retained DHT initialization. That change remains present; it was not lost. The recurrent new failure exceeded the retained 120-second budget.

This is a code constant, not an OASIS DNA setting. No DNA setting or Railway variable needs changing for this increment. Source publication does not change the running timeout. Actual WEB4 compilation and a qualified Development image rollout with retained-state readiness observation remain required. The already built snapshot2d114754 image still contains120 seconds and is not evidence of the new budget.

No staging/production deployment, PIPER operation, local build/install/download or conductor state deletion. Disposable scoped Git index removed; existing active delivery journals retained. A longer budget alone is not proof that the network join issue is resolved.

## Frozen delivery candidate

Candidate91423bce2e32fd664074784522020b85d2282e14 (tag cosmic-holo-readiness-240-91423bce2e32) is directly on the package-qualified snapshot2d114754 and changes exactly one conductor source file:120 to240 seconds. Core stays13bd050; unqualified range SDK work is excluded. Current shared Development remains606e1a52.

Image run44946308915 is confirmed in progress. Composition compile44945969209/source029545dc is confirmed in progress; range compatibility44945637242/sourcee2161fb6 also remains in progress. Observe these same handles; no duplicate producer. Image/package acceptance and exact Development startup observation remain open.

The original snapshot2d package44945668173 remains accepted as packaging evidence, not as evidence for the new timeout. Its immutable source is preserved; no stale image is deployed merely to synchronize branches.

## Compilation accepted

Run44945969209/job136973233689 succeeded on029545dc: actual WEB4 build0 errors4m34.95s and WEB5 build0 errors28.14s. Strict RAM verification of artifact14723533241 (351bytes, SHA2566ac639a3ae82c8da61c0af6200dde23b2057e18327f495ad5ea774aa9a4f2daf) confirmed compiled.json exact source/Core3b69/ONODE9c8/STAR677, both compilation flags, zero behavior cases/database writes. This compile source includes range registration; frozen delivery91423bce deliberately retains qualified Core13bd. The image itself must independently compile/package that exact frozen graph. No retained-cell runtime success claimed.

Development advanced fcb6e088 with SmartBrick ONODE34a8685e/STAR872f0be8 pins. Preserve those pins and ancestry in the existing task branch; do not deploy stale91423bce over them. Original image44946308915 continues without replacement/replay. A combined frozen graph must pass its own image/package gate; prior behavior/contract evidence is retained.

## Current integrated candidate

Frozen7c5f0a164cf18f55903d1fa3e009dc5eaec93a1f/tagcosmic-holo-readiness-240-7c5f0a164cf1 is directly on current Developmentfcb6e088. Eight paths: the seven already qualified snapshot changes plus the240-second conductor deadline. It preserves ONODE34a8685e and STAR872f0be8 from Development, Core13bd050 from the accepted snapshot, and all other dependency/settings bytes. Official manifest sync/ref validator passed. Source body for the timeout equals compile-qualified029545dc.

The existing COSMIC feature integrated Development ancestry in2c2298c02b82e61416ef432bbc3a59980bd87202. New exact image dispatch acknowledged once; observe same candidate/source. Prior91423bce image44946308915 remains in progress and is preserved as superseded-base evidence, not authorized for stale Development replacement. Both image handles and original candidates remain in the compact handoff journal. No new branch/worktree/environment or local image/download.

Combined image/package/live readiness remain pending. Compilation/native behavior acceptance from prior source is retained without rerunning unrelated accepted cohorts.

Read-only Railway preflight: saved Development WEB4 load balancing=true; active peer46ca1820 BUILDING, fcb6e088 QUEUED, last successful606e1a52. Preserve peer rollouts. No new rollout while active ones remain. These are deployment/saved-config observations, not loaded in-memory Holo cell readiness.

Original91423bce image44946308915/job136974290676 terminalSUCCESS, digest sha256:10ffc6fe0487678b25b8f52e43e4151705831244cbaf37ccb9883f3f0ce1438e. Retain remote artifact as superseded-base provenance; no separate package run or deployment. Current combined7c5f0a16 image44946907870/job136977414701 confirmed queued. Native range44947147551/job136976863629 is in progress at its selected test step with isolated Mongo started; not yet accepted.

## Corrected current release candidate

42a13288100085314041e7305bbbdca400f11fed/tagcosmic-holo-readiness-240-42a132881000 is directly on Developmentd40364ed, preserving ONODE689a89bd and STAR72b38266. Exact nine paths: seven accepted snapshot paths, the240-second conductor deadline and MapController constructor call repair. Core stays13bd050; UpdateRange/RemoveRange source7165 is excluded. Official manifest/ref validation passed. Image dispatch acknowledged once; package and live startup pending.

Feature7cd5605f integrates Developmentd403 ancestry and dependency fixes without losing pending COSMIC source. Replacement combined Core5/native3 qualification44948216478/job136980131747 is queued on fixedfb43 source; its older ONODE34 constructor contract equals latest689 contract, while the exact current image independently builds current dependency graph. Failed native44947147551 remains preserved as compile-failure evidence, not a native failure.

## Known dependency regression prevents shipping the current frozen image

Peer current status identifies missing qualified Map/shallow/hotspot files in recently advanced ONODE/STAR package pins and prepares combined restoration1fe579cf/1de76d2d. Direct comparison confirms MapManager(HolonManager,Guid), present in accepted9c8, is absent in689. Our caller fix matches current contracts but does not restore those independently lost features.

Own queued image44948470462 cancellation acknowledged before build. This is cancellation for a confirmed source regression, not queue duration or observation timeout. Do not start another pin-chasing image while restoration is unqualified. Preserve current42a132 source and earlier successful91423 image proof; combine timeout/snapshot with the restored graph only after inspecting peer compile44948349449 (queued). Native session44948941338 remains independent and does not qualify missing Map functionality. No peer jobs/deployments cancelled, no production or data mutation.

## Peer restoration compilation accepted

Run44948349449/job136980553768 terminalSUCCESS. Logs attest exact rootf8bee8a4c8ca5a5598759aa4aeb409b45292f86a, ONODE1fe579cf5e385b4a6ecce12934a7dd7578c2340c and STAR1de76d2d5b1428ac3c645139823757224a1e2ca3. Actual WEB4 compiled0errors5m12.99s; WEB5 compiled0errors32.66s. This was compile-only: no native/browser fixture or new behavior acceptance.

Fresh shared Development now17f2f1e83577961d21cb0c458dc2412b918d4cad, so inspect peer fresh-head composition before using those qualified pins. Preserve package metadata/reference changes and accepted Map/hotspot source together. No stale overwrite or automatic full branch merge authorized by this compile result. Timeout/snapshot source remains prepared; no duplicate image producer.
