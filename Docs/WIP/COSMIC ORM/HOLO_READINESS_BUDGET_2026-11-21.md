# Persisted Holo cell readiness budget — 21 November 2026

David explicitly requested doubling the current timeout. The current shipping owner is HoloOasisDevelopmentConductor.PersistedCellReadinessBudget, increased from 120 to 240 seconds. This waits for the exact persisted app cell; it does not replace native readiness with a timer, retry provisioning, create another interface, or reset conductor/lair data.

Historical commit918da95d740fb296f0417369d2380f70ae3a4036 separated this deadline from HoloNET admin binding after an observed 73-second retained DHT initialization. That change remains present; it was not lost. The recurrent new failure exceeded the retained 120-second budget.

This is a code constant, not an OASIS DNA setting. No DNA setting or Railway variable needs changing for this increment. Source publication does not change the running timeout. Actual WEB4 compilation and a qualified Development image rollout with retained-state readiness observation remain required. The already built snapshot2d114754 image still contains120 seconds and is not evidence of the new budget.

No staging/production deployment, PIPER operation, local build/install/download or conductor state deletion. Disposable scoped Git index removed; existing active delivery journals retained. A longer budget alone is not proof that the network join issue is resolved.
