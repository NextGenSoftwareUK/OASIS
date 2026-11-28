# Miden current-graph integration — 2026-11-27

Reused Claude’s native implementation0289f57c (52/52 CI45753725053) rather than rewriting it. Applied only its17Miden files to Development6426cde3, preserving all existing Docker memory/on-demand and other-provider work. Core differs (526ebc2b →41e46d93), so current-graph native acceptance is required.

Shipping gaps fixed: normal constructor reads existing fixture-named environment settings; all7API Docker builds install the locked official SDK into the published bridge; NuGet assets include worker/component/manifests/installer. Native CI uses the same installer and runs from published test output, through the shipping constructor. SDK process remains lazy/long-lived; no startup download is introduced. No DNA setting or deployment secret changed. Hosted account/node/Kubo/encryption-key admission remains separate.

Current CI pending: no100% provider or live acceptance claimed. Temporary npm cache, local-network containers/volumes, fixture and published output have explicit cleanup. No laptop build/download/env/database/test output or shared Atlas calls. Private Git indexes removed in finally.

## Accepted published baseline and lifecycle fix

Current Core6613656a/published SDK/shipping-constructor baseline passed52/52 in CI46104748678. Source4f8269545cb9f9556235fa15d42a75ede18d77fb is retained as accepted evidence.

Official SDK0.18.1 source confirms named Node stores persist at `~/.miden/stores/<name>`; the previous random name created a fresh persistent store every worker start. The fix reuses a deterministic node/account namespace and preserves runtime signing/journal data. CI supplies an absolute `.db` path inside its owned fixture. Managed provider disposal owns both crypto wrapper and physical SDK backend, so worker processes exit before fixture deletion. Session initialization is invalidated after a lost worker; a future operation reinitializes without replaying the failed operation.

One native lifecycle case verifies explicit error after an injected worker loss, subsequent read/reopen of an acknowledged record at the same version, actual worker exit on disposal, and no SDK home-directory stores in CI. Both26-check modes remain required on the changed initializer. Lifecycle acceptance pending; no global or live-provider100% claim. Official pinned source: https://github.com/0xMiden/web-sdk/blob/v0.18.1/crates/web-client/js/node/client-factory.js

## Accepted lifecycle revision

CI46106768815 on6fd58ec3 passed both26-check modes plus the native SDK lifecycle case, zero skips. Published output and fixture stores were removed. The subsequent merge preserves COSMIC-only Core5e8f22d; its shipping build is checked separately. Hosted admission is not implied.
