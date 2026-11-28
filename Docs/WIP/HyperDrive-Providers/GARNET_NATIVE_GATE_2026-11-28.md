# Garnet native core-storage gate

Existing shipping provider uses the shared StackExchange.Redis transport and encrypted record implementation. Added a strict Garnet adapter for the existing26-check native suite; no fake provider/backend. Reuses the registered Redis-family workflow with a single-provider selector, so KeyDB/Dragonfly accepted cases are not rerun.

Official Microsoft Garnetv2.2.5 linux/amd64 image `ghcr.io/microsoft/garnet@sha256:87e22ffa38533c207c5210ed2c11c39ed34f5b84981837a752ac2b4ec813a674`; primary registry digest verified from the release manifest. Required Lua and transaction mode enabled, memory bounded, owned namespace/key/container, native empty DB checks,26sync+26async/zero-skip/cleanup assertions and one build lease. Reused the accepted6b546445 RESP line-read correction. XML and both Bash blocks parse locally without builds/downloads/envs. Native acceptance pending.

First46122276351started official Garnet successfully, then the raw runner probe sent Redis inline commands that Garnet correctly rejected as malformed. No storage test ran; owned container/volumes/fixture removed. Probes now use standard RESP arrays for PING and DBSIZE across all products, without a second protocol path. Shipping StackExchange.Redis already uses RESP; adapter unchanged. Retry only Garnet.

## 2026-11-28 — Garnet native acceptance

CI46122538895 passed26sync+26async with zero skips on official Garnet2.2.5, native DBSIZE0 and owned container/volume/fixture cleanup. Hosted durability/restart and all-interface acceptance remain separate. Source d7a6ed18ab05. Matrix native baselines123/163; provider project inventory238/238 unchanged. First probe-only failure retained; only failed Garnet path retried. No laptop download/env/build/output/database, shared Atlas call or new David action. Owned remote resources removed; compact source receipt retained.
