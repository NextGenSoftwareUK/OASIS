# Story CQL MVP

Progress (25 Nov 2026, cloud-cql-C): anonymous public balance source merged to Development 4142d82b36; controlled CQL tests pass (58/58 EVM CQL suite, WEB4 builds). Waiting on Railway enrollment by the local CQL sandbox session: see [enrollment handoff](CLOUD_CQL_C_ENROLLMENT_HANDOFF_2026-11-25.md).

## Scope

- `StoryProtocolOASIS` reads native balances for three fixed public addresses on chain 1514 (`0x5ea`), `https://mainnet.storyrpc.io/`. It reads by finalized block hash, with EIP-1898 `requireCanonical`.
- The source uses the shared `EvmPublicAccountMetadataSource` with the default finalized policy. It has no wallet, keys, signing or writes, and is separate from the legacy Story API client.
- It is wired through `CQL_STORY_PUBLIC_SOURCE_JSON` in `Startup.cs`. Presets: native 3 and 2 records, plus Mongo-paired federation.

## Not claimed

Writes, history, private data and full EF/NHibernate parity.
