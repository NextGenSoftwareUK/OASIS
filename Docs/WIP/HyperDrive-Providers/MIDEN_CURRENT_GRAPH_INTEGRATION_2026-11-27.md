# Miden shipping SDK and native acceptance — 2026-11-28

Reused Claude0289f57c rather than rewriting the native implementation. Published baseline4f826954/CI46104748678 passed52/52. Changed session/store lifecycle6fd58ec3/CI46106768815 passed26sync+26async plus one native worker-loss/reopen/disposal case, zero skips. These use a real disposable miden-node0.18.3network, official @miden-sdk/miden-sdk0.18.1and Kubo.

All7API Docker builds install the locked SDK into the published bridge using the shared installer. Normal constructor configuration reads the fixture-named environment settings; worker/component/manifests/installer are shipping assets. No runtime/startup download is introduced. Shared Core6613656a was used for native lifecycle qualification; later COSMIC-only5e8f22d is preserved inside907cd4b, whose WEB4 shipping build46109274477passed. Hosted credentials/account admission is not implied.

Official SDK Node stores persist under ~/.miden/stores. Random names accumulated persistent stores; the shipping worker now reuses a deterministic node/account namespace and preserves runtime signing/journal data. CI supplies absolute .db paths inside its owned fixture, including a separate deployment store. The provider owns both encrypted wrapper and physical SDK backend, and disposal waits for its actual worker exit before fixture deletion.

A lost worker invalidates session initialization. The failed operation returns an explicit error; a later operation reinitializes without replaying the failed operation. The native lifecycle case verifies an acknowledged record survives worker loss/reopen at the same version and workers exit on disposal.

Cleanup confirmed in CI: owned node/Kubo containers and volumes, SDK fixture stores and published SDK/test output removed. No laptop runtime, SDK download, environment, build or database created. Private Git indexes removed in finally. Persistent deployed account journals are not disposable cache.

Remaining hosted custody is logged once as MIDEN-HOSTED1 in ACTIONS-FOR-DAVID.md. No source DNA setting or Railway secret changed. Native core storage and SDK lifecycle acceptance do not certify all provider capabilities or global HyperDrive v2.

Official pinned SDK store contract: https://github.com/0xMiden/web-sdk/blob/v0.18.1/crates/web-client/js/node/client-factory.js
