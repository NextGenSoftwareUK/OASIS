# Solana public account query profile

Updated 31 October 2026. Adapter, host profile resolution, admission/preset planning and native endpoint dispatch verified; hosted integration and browser acceptance pending.

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
| Explicit host profile activation/resolution, failed activation, cancellation, duplicate rejection | 2 passed | `solana-profile.trx` | `D627D679D54CAC0225F8D59249B570FDE73F4F41C2A764D6BE29113DA87D9E95` |
| Existing discovery, Oracle admission and shipping GraphQL composition | 19 passed | `solana-profile-composition.trx` | `76F068515A4C7EB66DC7DE586FB050970C4659F26A4F4331EDBCC793AB4A805A` |
| Solana admission/presets plus Oracle/profile/discovery regressions | 22 passed, zero skipped | `solana-admission.trx` | `DD07BEBC423DB15134B3060D344DA975CCE97395FB7A46CDC1FA93B6EC6B65F3` |
| Real mainnet through shipping endpoint plus scoped composition regressions | 25 passed, zero skipped; native three/two and two validated query receipts | `solana-native-endpoint.trx` | `0A4962C20DF4264F0F9A90F29D8E42DEDB3499FAE666F8C38F25A1C90E74CFF5` |
| Broad CQL release regressions | 733 passed, zero skipped | `solana-release-core.trx` | `3A597944CCDE5807ABD997E8E39786222BA6EEE64BC5B4B05842B827B4CF4DB7` |

Three prior assertions assumed Solana was never a read profile. They now check independent permission and case-sensitive names; `CqlSolanaUnsignedEnrollmentTests` separately rejects every ordinary storage operation tariff and implicit provider selection. Original failed run (729 passed/3 failed) remains `solana-release-core-stale-expectations.trx`, SHA256 `AEB2DE01C5AB34A3BB39ADED611C3CD4C18C69123C59B30B68707DA61C58966F`.

The first endpoint test reproduced a constructor-ownership bug: supplying the query representation to `HolonManager` registered it in the canonical registry. CQL now constructs an unbound manager; the admitted plan and explicit adapter snapshot own query dispatch. This also avoids per-query storage error-event subscriptions. Failed evidence remains `solana-native-endpoint-registry-failure.trx`, SHA256 `1E8117D2850A4E6E96FF08105C0EFA2775565B2D0DA3225EA4F9C141797805B5`.

`CqlSolanaNativeEndpointTests` invokes the shipping endpoint directly with a controlled in-memory usage authority/outbox and real native HTTP RPC. It proves native dispatch, projection, unchanged empty canonical registry and two query receipts. It does not prove public authentication, production billing persistence, browser rendering or Mongo federation. It uses the same shipping query-manager factory as production.

Native test category: `SolanaPublicAccountsNative`; set `OASIS_SOLANA_PUBLIC_SOURCE` to the retained configuration path. Ordinary controls exclude that category. Use existing build outputs and bounded build workers; do not create a validator or wallet.

## Next acceptance gates

Hosted rollout candidate is prepared, **not applied**: `Scripts/Sandbox/Publish-HostedSolanaPublicConfiguration.py`. It changes only `CQL_SOLANA_PUBLIC_SOURCE_JSON`, `PREPARED_STORAGE_ADMISSION_JSON` and `CQL_HOSTED_QUERY_EXAMPLES_JSON`; canonical DNA remains byte-for-byte unchanged. Protected before/candidate snapshots are retained in `LocalState/COSMIC/HostedSourceRolloutCurrent/solana-public-*-variables.dpapi`. Two pure builder tests (including six rejection subcases) pass. Apply uses the existing per-variable intent/ACK and concurrency checks with deployment disabled; interrupted/unknown outcomes require reconciliation, never replay. Deploy the verified code/pins as one batch before normal visitor acceptance; do not advertise readiness from candidate creation.

The normal visitor verifier now accepts `SolanaOASIS`: `python Scripts/Sandbox/Verify-HostedQuerySource.py SolanaOASIS --prepare`. It creates a journalled owner/foreign cohort only when explicitly invoked; wait until deployed discovery is ready. The real client preparation uses the existing generic metadata-key recipe, with no Solana-specific hashing or save path.

1. Host profile configuration/resolution is implemented and controlled-tested via `CQL_SOLANA_PUBLIC_SOURCE_JSON` (the retained source schema). DI owns its disposable client. Explicit provider requests resolve the configured profile, including when inactive; activation failure cannot select the canonical provider. Unspecified/default requests keep their existing resolution. Nothing is deployed or enrolled merely by adding this configuration. Native shipping endpoint dispatch now passes with controlled usage admission; production usage persistence remains to be verified.
2. Query-only tariff admission, discovery and three/two native presets with Mongo-paired presets now pass controlled planning tests (`CqlSolanaHostedAdmissionTests`). The ten-provider host tariff catalogue still requires independent permission and pricing; it does not enlarge the eight-target request limit or grant signing/storage writes. Task Core `759b49a529a86abbfc50e36b26343fdb894295b4` is pushed, not deployed. Five/four actual paired counts remain unverified for Solana.
3. Hosted native queries, normal visitor preparation, repeat-zero-write behavior, foreign-private isolation and replica observation.
4. Rendered sandbox browser acceptance and local-demo links. Until then, this adapter is not hosted demo coverage.

Full canonical Solana ORM writes, memo decoding and history recovery remain separate unfinished scopes. Hosted Azure and Oracle remain explicitly deferred by David.
