# Garnet native core-storage gate

Existing shipping provider uses the shared StackExchange.Redis transport and encrypted record implementation. Added a strict Garnet adapter for the existing26-check native suite; no fake provider/backend. Reuses the registered Redis-family workflow with a single-provider selector, so KeyDB/Dragonfly accepted cases are not rerun.

Official Microsoft Garnetv2.2.5 linux/amd64 image `ghcr.io/microsoft/garnet@sha256:87e22ffa38533c207c5210ed2c11c39ed34f5b84981837a752ac2b4ec813a674`; primary registry digest verified from the release manifest. Required Lua and transaction mode enabled, memory bounded, owned namespace/key/container, native empty DB checks,26sync+26async/zero-skip/cleanup assertions and one build lease. Reused the accepted6b546445 RESP line-read correction. XML and both Bash blocks parse locally without builds/downloads/envs. Native acceptance pending.
