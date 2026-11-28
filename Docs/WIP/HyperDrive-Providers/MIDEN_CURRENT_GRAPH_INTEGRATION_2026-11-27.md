# Miden current-graph integration — 2026-11-27

Reused Claude’s native implementation0289f57c (52/52 CI45753725053) rather than rewriting it. Applied only its17Miden files to Development6426cde3, preserving all existing Docker memory/on-demand and other-provider work. Core differs (526ebc2b →41e46d93), so current-graph native acceptance is required.

Shipping gaps fixed: normal constructor reads existing fixture-named environment settings; all7API Docker builds install the locked official SDK into the published bridge; NuGet assets include worker/component/manifests/installer. Native CI uses the same installer and runs from published test output, through the shipping constructor. SDK process remains lazy/long-lived; no startup download is introduced. No DNA setting or deployment secret changed. Hosted account/node/Kubo/encryption-key admission remains separate.

Current CI pending: no100% provider or live acceptance claimed. Temporary npm cache, local-network containers/volumes, fixture and published output have explicit cleanup. No laptop build/download/env/database/test output or shared Atlas calls. Private Git indexes removed in finally.
