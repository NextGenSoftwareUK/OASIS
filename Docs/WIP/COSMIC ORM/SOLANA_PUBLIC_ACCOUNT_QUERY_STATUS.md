# Solana public account query profile

Updated 31 October 2026. Adapter verified; hosted integration and browser acceptance pending.

## Verified scope

`SolanaPublicAccountMetadataSource` offers explicitly selected, finalized public account metadata through the shared CQL interfaces. It does not replace the existing signed Solana storage provider or recover the original memo/history dataset.

- Queries use `SolanaAccountAddress` equality or membership over one to eight configured addresses, with bounded paging. Unsupported content predicates fail during planning.
- Each read verifies the configured genesis hash, then calls `getMultipleAccounts` with a zero-length data slice. No wallet, transaction, funding or account-data download is needed.
- Native responses are bounded to 64 KiB and validated completely before paging. Cancellation reaches HTTP reads; failures never publish partial rows.
- Slot, lamports, rent epoch and space are invariant decimal strings, preserving UInt64 precision through JavaScript clients.
- Stable holon identities include the source mapping owner, chain genesis and native address. Rows are public; this source has no write capability.

The retained configuration is `C:/Source/OASIS-cosmic/LocalState/COSMIC/HostedSolanaPublicSourceCurrent/source.json`. Preserve its mapping owner between runs. It contains public source settings, no signer credentials.

## Evidence

Proving test: `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests/SolanaPublicAccountMetadataTests.cs`.

| Gate | Result | Regenerable evidence under worktree `TestResults/HostedCqlIntegrationCurrent/` | SHA256 |
| --- | --- | --- | --- |
| Controlled native boundary, rejection, precision and cancellation | 15 passed, zero failed/skipped | `solana-public-controls.trx` | `75600B2ACCC299AA642B809062186927F03B906D6135F5F00B8F22AD531B4F3D` |
| Real official mainnet adapter query | 1 passed, zero failed/skipped; three accounts and two-account subset | `solana-public-native.trx` | `D586D05EF4F80D41A5DE1C82DADA23D417362AA5272084A96890D45C8A5FA490` |

Native test category: `SolanaPublicAccountsNative`; set `OASIS_SOLANA_PUBLIC_SOURCE` to the retained configuration path. Ordinary controls exclude that category. Use existing build outputs and bounded build workers; do not create a validator or wallet.

## Next acceptance gates

1. Explicit host query-profile configuration and resolution, isolated from canonical Solana signer/storage/index bindings. Prove that configured failures cannot select a different source.
2. Query-only tariff admission, discovery and three/two native presets with one Mongo-paired five/four preset.
3. Hosted native queries, normal visitor preparation, repeat-zero-write behavior, foreign-private isolation and replica observation.
4. Rendered sandbox browser acceptance and local-demo links. Until then, this adapter is not hosted demo coverage.

Full canonical Solana ORM writes, memo decoding and history recovery remain separate unfinished scopes. Hosted Azure and Oracle remain explicitly deferred by David.
