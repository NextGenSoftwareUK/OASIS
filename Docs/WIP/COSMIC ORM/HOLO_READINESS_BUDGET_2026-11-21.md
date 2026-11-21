# Persisted Holo cell readiness budget — 21 November 2026

David explicitly requested doubling the current timeout. The current shipping owner is HoloOasisDevelopmentConductor.PersistedCellReadinessBudget, increased from 120 to 240 seconds. This waits for the exact persisted app cell; it does not replace native readiness with a timer, retry provisioning, create another interface, or reset conductor/lair data.

Historical commit918da95d740fb296f0417369d2380f70ae3a4036 separated this deadline from HoloNET admin binding after an observed 73-second retained DHT initialization. That change remains present; it was not lost. The recurrent new failure exceeded the retained 120-second budget.

This is a code constant, not an OASIS DNA setting. No DNA setting or Railway variable needs changing for this increment. Source publication does not change the running timeout. Actual WEB4 compilation and a qualified Development image rollout with retained-state readiness observation remain required. The already built snapshot2d114754 image still contains120 seconds and is not evidence of the new budget.

No staging/production deployment, PIPER operation, local build/install/download or conductor state deletion. Disposable scoped Git index removed; existing active delivery journals retained. A longer budget alone is not proof that the network join issue is resolved.

## Frozen delivery candidate

Candidate91423bce2e32fd664074784522020b85d2282e14 (tag cosmic-holo-readiness-240-91423bce2e32) is directly on the package-qualified snapshot2d114754 and changes exactly one conductor source file:120 to240 seconds. Core stays13bd050; unqualified range SDK work is excluded. Current shared Development remains606e1a52.

Image run44946308915 is confirmed in progress. Composition compile44945969209/source029545dc is confirmed in progress; range compatibility44945637242/sourcee2161fb6 also remains in progress. Observe these same handles; no duplicate producer. Image/package acceptance and exact Development startup observation remain open.

The original snapshot2d package44945668173 remains accepted as packaging evidence, not as evidence for the new timeout. Its immutable source is preserved; no stale image is deployed merely to synchronize branches.
