# Railway and OASIS DNA configuration reference

Snapshot taken 2026-11-23 from Railway project `712f50f6-3b7b-46d4-b229-44b49922f16b`
(environments `production`, `dev`, `staging`) and the `Development` branch.
This file lists names and meanings only. Never put values in it.

## 1. Where configuration comes from

A running OASIS API reads configuration from three places, in this order:

1. **OASIS DNA.** One JSON document (`OASISDNA` class). On Railway it is the whole
   `OASIS_DNA_JSON` variable. Locally it is `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Default/OASIS_DNA.json`
   (or `OASIS_DNA.Development.json`). Providers, security, email, HyperDrive, subscriptions and so on all live here.
2. **Plain environment variables** read directly by code with `GetEnvironmentVariable` or `configuration["NAME"]`.
   These are operator switches, secrets that are deliberately kept out of DNA, and per-provider CQL catalogues.
3. **ASP.NET section overrides** using a double underscore, for example `OLAND__Enabled` maps to
   `OLand:Enabled`. These bind to app settings, not to DNA.

Railway also injects its own `RAILWAY_*` variables (project, environment, service, domains). Code does not
need them; leave them alone.

## 2. Railway variables by service

### All WEB5 to WEB10 services (STAR, AI, SYMBIOTIC, IGN, SINGULARITY, THE SOURCE)

The same set in production and dev:

| Variable | Secret | Purpose |
|---|---|---|
| `OASIS_DNA_JSON` | yes | The service's whole DNA (see section 3). |
| `GITHUB_PAT` | yes | Build-time only: lets the Docker build clone pinned private submodules (`Docker/clone-pinned-oasis-dependencies.sh`). |
| `ENABLE_GENERIC_EXCEPTION_HANDLING` | no | `true` turns unhandled exceptions into generic error results (`OASISControllerBase`, `ServiceExceptionInterceptor`). Currently `false`. |
| `OASIS_OFFLINE_GRANT_SIGNING_PRIVATE_KEY` | yes | Signs offline session grants. DNA `OfflineSessionGrants.SigningPrivateKeyEnvironmentVariable` names this variable. |
| `OASIS_PAUSE_USAGE_SETTLEMENT` | no | `true` pauses the usage-settlement worker (`Core/Services/Subscriptions/Web4UsageExecution.cs`). Queued usage is kept. |
| `STRIPE_PUBLISHABLE_KEY`, `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET` | yes (publishable is public) | Stripe for subscriptions (`OASISDNA.Payments.cs`, `SubscriptionStripeConfiguration.cs`). |
| `SUBSCRIPTION_SERVICE_KEY_WEBn` | yes | The service's key for reporting usage to WEB4 (`Web4UsageLedgerMiddleware`). WEB4 holds the full set, WEB5 to WEB10. |
| `SUBSCRIPTION_USAGE_ENABLED` | no | `true` turns on usage metering. |
| `WEB4_API_BASE_URL` | no | Where the service calls WEB4. |
| `WEB6_API_KEY` (WEB6 only) | yes | **Not read by any code.** Candidate for removal. |
| `STRIPE_WEBHOOK_TEST_TOKEN` (staging only) | yes | Used only by the test harness and tests. Do not add it to production. |

### WEB4 OASIS API

About 290 variables. Grouped:

**Operator switches (no secrets).** Pauses never delete or acknowledge queued work.

| Variable | prod | dev | Purpose |
|---|---|---|---|
| `OASIS_PAUSE_HYPERDRIVE_WORKERS` | false | true | Pauses HyperDrive replication delivery (`HostedMongoWorkerControl.cs`). While `true`, nothing reaches SQLite or Neo4j. |
| `OASIS_PAUSE_USAGE_RECONCILIATION` | true | true | Pauses usage reconciliation. |
| `OASIS_PAUSE_USAGE_SETTLEMENT` | true | true | Pauses usage settlement. |
| `OASIS_PAUSE_APPLICATION_MAINTENANCE` | false | true | Pauses app maintenance jobs. |
| `OASIS_POOLED_APP_QUOTAS` | true | true | Apps share one quota pool (`AppAccounts.cs`). |
| `SUBSCRIPTION_USAGE_ENABLED` | true | true | Usage metering on. |
| `ENABLE_GENERIC_EXCEPTION_HANDLING` | false | false | As above. |
| `OASIS_ENVIRONMENT` | set | set | Environment name used by `ProgrammaticVerificationPolicy`. |
| `OASIS_ALLOW_PROGRAMMATIC_VERIFICATION` | unset | true | Lets tests verify avatars without email. Must stay unset in production. |
| `OASIS_INCLUDE_TEACHING_SIDECARS` | true | true | Build-time: puts the teaching sidecars into the image (`Dockerfile.web4`). |
| `OASIS_SANDBOX` | set at the shell | set at the shell | Only set by hand when running the repair commands `--cosmic-repair-baseline` and `--cosmic-rearm-held-head`. Never set it on the service. |

**Sandbox sidecars** (`Docker/entrypoint.onode.sh` starts them inside the container):
`OASIS_SANDBOX_DAPR`, `_DUCKDB`, `_GUN`, `_IPFS`, `_NATS`, `_OPENVINO`, `_ORBITDB`, `_POUCHDB`, `_SOLID`, `_WAKU`
(each `true`/`false`), plus their bootstrap settings `OASIS_SANDBOX_*_BOOTSTRAP_JSON` and `OASIS_SANDBOX_ORBITDB_ADDRESS`.

**Secrets held outside DNA:**
`OASIS_OFFLINE_GRANT_SIGNING_PRIVATE_KEY` and `_PUBLIC_KEY`, `OASIS_IPFS_STORAGE_ENCRYPTION_KEY` (`IPFSOASIS.PrivateStorage.cs`),
`OASIS_SOLID_STORAGE_KEY` (`OASISDNA.Solid.cs`), `STRIPE_*`, `SUBSCRIPTION_SERVICE_KEY_WEB5` to `WEB10`, `GEOAPIFY_API_KEY`
(places search), `GITHUB_PAT` (build only), and the CQL sidecar tokens `CQL_DAPR_QUERY_SIDECAR_TOKEN`, `CQL_GUN_QUERY_READER_TOKEN`,
`CQL_ORBITDB_QUERY_READER_TOKEN`, `CQL_NATS_PUBLIC_SOURCE_USERNAME` and `_PASSWORD`.

**CQL read catalogues (no secrets, about 210 variables):**
- `CQL_<PROVIDER>_PUBLIC_SOURCE_JSON` and `CQL_<PROVIDER>_QUERY_SOURCE_JSON` or `_OWNER`: the public data source each provider's CQL demo reads.
- `CQL_HOSTED_QUERY_EXAMPLES_JSON` and `CQL_HOSTED_QUERY_EXAMPLES__<Provider>OASIS` (99 of them): the example queries shown per provider (`CqlHostedQueryExamples.cs`).
- `CQL_PUBLIC_READ_CATALOGUES_JSON` (`CqlPublicReadCatalogue.cs`), `CQL_LOCALFILE_PUBLIC_BOOTSTRAP_JSON`, `CQL_DUCKDB_QUERY_CONNECTION_STRING`,
  the `CQL_*_PROBE_ID` and `CQL_*_SIDECAR` values, and `CQL_POUCHDB_QUERY_SOURCE_DIGEST`.
- `CQL_HOLOOASIS_LOCAL_DEMO__Enabled` and `__UseBootLoader`: run the HoloOASIS conductor inside WEB4 (`HoloOasisDevelopmentConductor.cs`).
- `PREPARED_STORAGE_ADMISSION_JSON`: storage admission rules for prepared requests (`Startup.cs`).
- `HOLONET_CONDUCTOR_FOLDER`: used only by the CI signing workflow. Not needed on the service.

**OLand commerce** (ASP.NET section `OLand`, `OLandCommerceRegistration.cs`): `OLAND__Enabled` (dev only), `OLAND__PublicApiBaseUrl`,
`OLAND__SuccessUrl`, `OLAND__CancelUrl`, `OLAND__StripeWebhookSecret` (secret, dev only), and `OLAND__Chains__SolanaOASIS__*`
(ChainId, NativeSymbol, NativeDecimals, NativeAmountUnit, NativePaymentEvidenceEnabled, NftStandard, TreasuryAddress).
These duplicate DNA `OASIS.OLand`. The `OLAND__` value wins.

**Staging only:** `SUBSCRIPTION_LEDGER_INITIAL_STATE` and `SUBSCRIPTION_MONGODB_DATABASE` point staging at its own subscription ledger
(`SubscriptionMongoConfiguration.cs`).

### Neo4j

`NEO4J_AUTH` (secret: user/password for the database image), `NEO4J_server_memory_heap_max__size`,
`NEO4J_server_memory_pagecache_size`, and `SANDBOX_NEO4J_PASSWORD` (secret: read by `Scripts/Sandbox/build_sandbox_dna.py`
when it builds the dev DNA). The Neo4j password must match `OASIS.StorageProviders.Neo4jOASIS.Password` in WEB4's DNA.

### function-bun

Only Railway's own variables.

## 3. OASIS DNA sections

All keys sit under `OASIS`. Each setting has a typed default in its C# class in
`OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASISDNA*.cs`, where the XML comments describe every key.

| Section | What it controls |
|---|---|
| `StorageProviders` | Every provider's connection string and keys, timeouts, and the auto-replication, failover and load-balance lists. |
| `HyperDriveMode`, `OASISHyperDriveConfig`, `ReplicationRules`, `FailoverRules`, `IntelligentMode` | HyperDrive V1/V2 routing, replication and failover policy. |
| `Security` | JWT `SecretKey` and lifetimes, avatar-password and provider-key encryption (`Rijndael256Key`), DID, OIDC, MFA, HerzId. |
| `Email` | SMTP and Resend settings, and the verification email. |
| `SubscriptionConfig`, `UsageVisibility` | Plans, limits, Stripe publishable key, usage warnings. |
| `OfflineSessionGrants` | Offline grants. Holds the **names** of the env variables that carry the keys. This is the pattern to copy. |
| `HolonSeal` | Holon tamper seal key and retired keys. |
| `ONET` | Peer network node identity and bootstrap servers. |
| `Cql`, `NativeImport`, `Sandbox` | CQL read behaviour, native import limits and encryption key, demo sandbox. |
| `AccessLog`, `VersionPurge`, `Logging`, `ErrorHandling` | Operations. |
| `DataPermissions`, `Terms`, `Web6`, `OLand`, `DataDirectory`, `OASISAPIURL`, `OASISSystemAccountId`, `NetworkId` | Feature and identity settings. |

Secrets inside the production and dev `OASIS_DNA_JSON` today (about 37 values):
- **Email:** `Email.SmtpPass` and `Email.ResendKey`.
- **Security:** `Security.SecretKey`, `Security.AvatarPassword.Rijndael256Key`, `Security.OASISProviderPrivateKeys.Rijndael256Key` and `Security.HerzId.QeaPrivateSeed`.
- **Other keys:** `HolonSeal.Key` and `NativeImport.ConnectionEncryptionKey`.
- **Storage providers:**
  - Connection strings: MongoDB, Neo4j, IPFS, Pinata, SEEDS, Telos and the EVM chains.
  - Passwords: `Neo4jOASIS.Password`.
  - Chain private keys: Ethereum, Polygon, Arbitrum, Rootstock and EOSIO.
  - Solana: `PrivateKey` and `WalletMnemonicWords`.

DNA copies that must stay in sync (see the global rule):
- `Default/OASIS_DNA.json` and `OASIS_DNA.Development.json`
- `OASIS_DNA_SYSTEM.json`
- The test-harness copies
- The STAR ODK templates
- The `OASIS_DNA_JSON` Railway variable of every service

`Scripts/DnaSync/sync-dna-copies.js` keeps the repo copies aligned.

## 4. Where each kind of setting should live

| Kind | Best place | Why |
|---|---|---|
| Behaviour defaults (timeouts, limits, feature flags, rules) | DNA JSON in the repo, with a typed C# default | Reviewed, versioned, the same in every environment. |
| Differences between environments that are not secret (URLs, which providers are on) | The `OASIS_DNA_JSON` Railway variable for that environment | It is already the per-environment copy. Keep these changes small and documented. |
| Secrets (passwords, private keys, API keys, connection strings with credentials) | Individual **sealed** Railway variables, never in the DNA blob | Sealed variables can't be read back from the dashboard or CLI. They can be rotated one at a time, and they don't leak when someone copies or prints the DNA. Make the DNA hold the variable **name**, as `OfflineSessionGrants` already does. |
| Secrets shared by WEB4 to WEB10 (Stripe keys, offline-grant key, `GITHUB_PAT`) | Railway **shared variables** at project level, referenced as `${{shared.NAME}}` | One place to rotate instead of seven. |
| Operator switches (`OASIS_PAUSE_*`, `SUBSCRIPTION_USAGE_ENABLED`) | Plain Railway variables on each service | You can flip them without a code change. Changing one restarts the service. |
| Large non-secret catalogues (the ~210 `CQL_*` JSON variables) | JSON files in the repo (for example `ONODE/.../Configuration/cql/*.json`), with Railway keeping only a path override | Railway variables are hard to review and diff. These are data, not secrets. |
| Build-time only (`GITHUB_PAT`, `OASIS_INCLUDE_TEACHING_SIDECARS`) | GitHub Actions secrets or Docker build args | The running service doesn't need them. |

## 5. Clean-up suggested by this audit

1. Delete `WEB6_API_KEY` from WEB6 production and dev. No code reads it.
2. Move the DNA secrets listed in section 3 into sealed Railway variables and make the DNA refer to them by name.
   This needs a small DNA change: an `EnvironmentVariable` indirection, the same as `OfflineSessionGrants`.
3. Move the Stripe keys, `OASIS_OFFLINE_GRANT_SIGNING_PRIVATE_KEY` and `GITHUB_PAT` to shared variables.
4. Move the `CQL_*_JSON` and `CQL_HOSTED_QUERY_EXAMPLES__*` catalogues into repo files.
5. Choose one home for OLand: either DNA `OASIS.OLand` or the `OLAND__` overrides, not both.
6. Keep `OASIS_ALLOW_PROGRAMMATIC_VERIFICATION` and `STRIPE_WEBHOOK_TEST_TOKEN` out of production.
7. Rotate any secret that has been printed or shared in plain text. The dev Neo4j password was printed in a working session on 2026-11-23.
