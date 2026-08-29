# OASIS DNA Reference Guide

Complete documentation for every setting in `OASIS_DNA.json`. Status column indicates whether the setting is actively wired up and taking effect in the current codebase.

**Status key:**
- ✅ Wired up and active
- ⚠️ Partially wired / read but logic incomplete
- 🔧 Config read, feature not yet fully implemented
- ❌ Not wired up / placeholder / null

---

## Top-Level Fields

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `IsProduction` | `false` | ✅ | Switches between dev and production behaviour throughout the API. Set `true` on Railway live. |
| `Terms` | Lorem ipsum | ⚠️ | Terms of service text served to clients. Currently placeholder — replace with real terms before launch. |
| `OASISSystemAccountId` | `40361570-...` | ✅ | GUID of the OASIS system/admin avatar used for internal operations and system-level data. |
| `OASISAPIURL` | `https://dev.api...` | ✅ | The base URL of this API instance. Used in email verification links, redirect URLs, and ONET registration. **Must be the live URL in production.** |
| `NetworkId` | `onet-network` | ✅ | Identifier for the ONET peer-to-peer network this node belongs to. |
| `SettingsLookupHolonId` | `00000000-...` | 🔧 | Future: load DNA settings from a Holon stored on the network instead of from the file. All zeros = disabled, use file. |
| `StatsCacheEnabled` | `false` | ✅ | Caches global stats (avatar count, karma totals etc.) to reduce DB hits. |
| `StatsCacheTtlSeconds` | `45` | ✅ | How long cached stats are considered fresh (seconds). |

---

## Logging

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `LoggingFramework` | `Default` | ✅ | Logging backend to use. Options: `Default`, `NLog`, `Serilog`. |
| `AlsoUseDefaultLogProvider` | `false` | ✅ | When using a third-party framework, also pipe output to the built-in .NET logger. |
| `FileLoggingMode` | `0` | ✅ | `0` = Off, `1` = Errors only, `2` = All. |
| `ConsoleLoggingMode` | `1` | ✅ | `0` = Off, `1` = All. Currently logging everything to console. |
| `LogToConsole` | `true` | ✅ | Master switch for console output. |
| `ShowColouredLogs` | `true` | ✅ | ANSI colour codes on console output. |
| `DebugColour` / `InfoColour` / `WarningColour` / `ErrorColour` | `15/10/14/12` | ✅ | Console colour codes (Windows ANSI). |
| `LogToFile` | `false` | ✅ | Master switch for file logging. Disable in Railway (use console → log aggregator instead). |
| `LogPath` / `LogFileName` | `C:\Users\USER\...` | ⚠️ | Local dev path only — irrelevant when `LogToFile` is false. Update if enabling file logging. |
| `MaxLogFileSize` | `1000000` | ✅ | Max bytes before log file rotates (1 MB). |
| `NumberOfRetriesToLogToFile` | `3` | ✅ | Retries on file write failure before giving up. |
| `RetryLoggingToFileEverySeconds` | `1` | ✅ | Delay between file write retries. |
| `InsertExtraNewLineAfterLogMessage` | `false` | ✅ | Adds blank line between log entries for readability. |
| `IndentLogMessagesBy` | `1` | ✅ | Number of spaces to indent log message body. |

---

## ErrorHandling

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `ShowStackTrace` | `false` | ✅ | Include stack traces in error responses. **Keep false in production** — exposes internals. |
| `ThrowExceptionsOnErrors` | `false` | ✅ | Re-throw exceptions after logging. False = swallow and return error result. |
| `ThrowExceptionsOnWarnings` | `false` | ✅ | Re-throw on warnings. |
| `LogAllErrors` | `true` | ✅ | Log every error regardless of caller handling. |
| `LogAllWarnings` | `true` | ✅ | Log every warning. |
| `ErrorHandlingBehaviour` | `1` | ✅ | `0` = Silent, `1` = Log only, `2` = Throw. |
| `WarningHandlingBehaviour` | `1` | ✅ | Same enum as above, applied to warnings. |

---

## Security

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `HideVerificationToken` | `true` | ✅ | Strips email verification token from API responses. Token is sent by email only. |
| `HideRefreshTokens` | `true` | ✅ | Strips refresh token from most responses. Prevents token leakage in logs/responses. |
| `SecretKey` | `7B6A835F-...` | ✅ | JWT signing secret. **Never share or commit the live value.** Rotate if compromised. |
| `RemoveOldRefreshTokensAfterXDays` | `0` | ⚠️ | Purge expired refresh tokens from storage after N days. `0` = never purge automatically. Should be set (e.g. `7`). |
| `JwtTokenExpirationMinutes` | `15` | ✅ | Access token lifetime. 15 min is standard — short enough to limit blast radius if stolen. |
| `RefreshTokenExpirationDays` | `7` | ✅ | Refresh token lifetime. User stays logged in for 7 days without re-authenticating. |
| `AvatarPassword.BCryptEncryptionEnabled` | `true` | ✅ | BCrypt hash applied to avatar passwords. |
| `AvatarPassword.Rijndael256EncryptionEnabled` | `true` | ✅ | AES-256 layer on top of BCrypt. |
| `AvatarPassword.Rijndael256Key` | `F4C1A9...` | ✅ | AES encryption key for passwords. **Must be kept secret and rotated if compromised.** |
| `AvatarPassword.QuantumEncryptionEnabled` | `true` | 🔧 | Quantum-resistant layer — wired up but algorithm implementation is in progress. |
| `OASISProviderPrivateKeys.BCryptEncryptionEnabled` | `true` | ✅ | BCrypt applied to stored provider private keys (Solana, Ethereum etc.). |
| `OASISProviderPrivateKeys.Rijndael256EncryptionEnabled` | `true` | ✅ | AES-256 applied to provider private keys. |
| `OASISProviderPrivateKeys.Rijndael256Key` | `A7F3D9...` | ✅ | AES key for provider private key encryption. **Keep secret.** |
| `OASISProviderPrivateKeys.QuantumEncryptionEnabled` | `true` | 🔧 | As above — partially implemented. |
| `RequireApiKey` | `false` | ⚠️ | When `true`, every request must include a valid API key header. Currently disabled — consider enabling per-route for WEB6 endpoints. |
| `ApiKey` | `""` | ⚠️ | The API key value to validate against when `RequireApiKey` is true. Empty = not set. |
| `RateLimiting.Enabled` | `true` | ✅ | Global rate limiting on all endpoints. |
| `RateLimiting.RequestsPerWindow` | `100` | ✅ | Maximum requests allowed per time window per client. |
| `RateLimiting.WindowSeconds` | `60` | ✅ | Duration of the rate limit window in seconds. So: 100 requests per minute. |
| `RateLimiting.WindowSegments` | `6` | ✅ | Sliding window subdivisions (smoother than fixed window). 6 segments × 10 sec each. |
| `RateLimiting.QueueLimit` | `0` | ✅ | Requests beyond the limit are queued up to this count. `0` = reject immediately (429). |

---

## Email

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `EmailFrom` | `anorak@oasisomniverse.one` | ✅ | Sender address for all system emails. |
| `SmtpHost` | `mailuk2.promailserver.com` | ✅ | SMTP relay host. |
| `SmtpPort` | `25` | ✅ | SMTP port (25 = unauthenticated relay — check if TLS/587 is available on this host). |
| `SmtpUser` | `anorak@oasisomniverse.one` | ✅ | SMTP login username. |
| `SmtpPass` | `` `ipmq0...` `` | ✅ | SMTP password. Stored in plain text here — consider moving to env var. |
| `ResendKey` | `re_5Urnt9...` | ✅ | Resend.com API key — used as the modern email path. Takes priority over SMTP when configured. |
| `DisableAllEmails` | `false` | ✅ | Master kill switch — disables all outbound email without removing config. Useful for testing. |
| `SendVerificationEmail` | `true` | ✅ | Whether to send email address verification on registration. |
| `OASISWebSiteURL` | `https://dev.oportal...` | ✅ | Base URL embedded in email links (verify, reset password etc.). **Must be live URL in production.** |

---

## StorageProviders

### Global switches

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `LogSwitchingProviders` | `true` | ✅ | Logs every time the active provider changes (failover/load balance). |
| `ProviderMethodCallTimeOutSeconds` | `99` | ✅ | Max seconds to wait for any provider operation before timing out. |
| `ActivateProviderTimeOutSeconds` | `10` | ✅ | Max seconds to wait when activating a new provider connection. |
| `DectivateProviderTimeOutSeconds` | `10` | ✅ | Max seconds to wait when gracefully closing a provider connection. |
| `AutoReplicationEnabled` | `false` | ✅ | When `true`, writes are replicated to all providers in `AutoReplicationProviders`. Currently off — enabling will increase write latency and cost. |
| `AutoFailOverEnabled` | `true` | ✅ | When a provider fails, automatically switches to the next available one. |
| `AutoLoadBalanceEnabled` | `true` | ✅ | Distributes read requests across providers based on performance/availability. |
| `AutoLoadBalanceReadPollIntervalMins` | `10` | ✅ | How often (minutes) the load balancer re-evaluates provider health for reads. |
| `AutoLoadBalanceWritePollIntervalMins` | `10` | ✅ | Same for writes. |
| `AutoReplicationProviders` | Long list | ✅ | Comma-separated providers that participate in auto-replication (when enabled). |
| `AutoLoadBalanceProviders` | Long list | ✅ | Providers included in load balancing rotation. |
| `AutoFailOverProviders` | Long list | ✅ | Providers tried in order on failover. |
| `AutoFailOverProvidersForAvatarLogin` | `MongoDBOASIS` | ✅ | Only MongoDB for login failover — keeps auth fast and reliable. |
| `AutoFailOverProvidersForCheckIfEmailAlreadyInUse` | `MongoDBOASIS` | ✅ | Registration uniqueness check always hits MongoDB only. |
| `AutoFailOverProvidersForCheckIfUsernameAlreadyInUse` | `MongoDBOASIS` | ✅ | Same. |
| `AutoFailOverProvidersForCheckIfOASISSystemAccountExists` | `MongoDBOASIS` | ✅ | System account bootstrap check — MongoDB only. |
| `OASISProviderBootType` | `Warm` | ✅ | `Warm` = providers are pre-initialised at startup. `Cold` = lazy-init on first use. Warm reduces first-request latency. |

### Individual provider configs

| Provider | Status | Notes |
|---|---|---|
| `MongoDBOASIS` | ✅ **Primary active provider** | Currently pointing to DEV DB (`OASISAPI_DEV`). Live has `OASISAPI_LIVE` uncommented. Connection string contains credentials — move to env var. |
| `HoloOASIS` | 🔧 Points to `localhost:8888` | Holo conductor not running in prod. `UseLocalNode: true` means it will try localhost — will silently fail and be bypassed by failover. |
| `EOSIOOASIS` | ⚠️ Test keys exposed | `localhost:8888` connection — not live. Keys are labelled as needing replacement before use. |
| `TelosOASIS` | 🔧 | Configured against mainnet but no active usage in prod flows. |
| `SEEDSOASIS` | 🔧 | Hypha node configured, not actively used. |
| `ThreeFoldOASIS` | ❌ | Empty connection string — not configured. |
| `EthereumOASIS` | ⚠️ Test keys exposed | Pointing to Nethereum test chain — not mainnet. Keys labelled as needing replacement. |
| `ArbitrumOASIS` | ⚠️ Test keys exposed | Sepolia testnet. Shared private key with Rootstock/Polygon — all need new keys before mainnet. |
| `RootstockOASIS` | ⚠️ Test keys exposed | RSK testnet. Same shared private key. |
| `PolygonOASIS` | ⚠️ Test keys exposed | Amoy testnet. Same shared private key. |
| `SQLLiteDBOASIS` | ✅ | Local SQLite file — used as local fallback/test. Works in dev, not useful in Railway. |
| `IPFSOASIS` | 🔧 Points to localhost | `localhost:5001` — IPFS daemon not running in prod. Bypassed by failover. |
| `Neo4jOASIS` | 🔧 Points to localhost | Local Bolt connection — not prod. |
| `SolanaOASIS` | ⚠️ | Devnet connection string. Private/public keys differ between DEV and LIVE backups — verify which is correct before mainnet use. |
| `CargoOASIS` | ❌ | All fields empty — not configured. |
| `LocalFileOASIS` | ✅ | `wallets.json` flat file — used as lightweight local fallback. |
| `PinataOASIS` | ✅ | IPFS pinning via Pinata. JWT and API key are in the connection string — consider moving to env var. |
| `BitcoinOASIS` through `GoogleCloudOASIS` | ❌ | All `null` — providers registered but not configured. Will be skipped by all provider selection logic. |

---

## OASISHyperDriveConfig

The HyperDrive is the intelligent multi-provider orchestration layer — it sits above the raw `StorageProviders` config and applies scoring-based provider selection.

> **Note:** `HyperDriveMode` is set to `"Legacy"` at the top level, which means HyperDrive is bypassed and the older `StorageProviders` auto-failover/load-balance logic is used instead. All settings below are read but not actively applied until `HyperDriveMode` is changed to `"HyperDrive"`.

| Setting | Status | Description |
|---|---|---|
| `IsEnabled` | 🔧 (bypassed by Legacy mode) | Master switch for HyperDrive. |
| `DefaultStrategy` | `Auto` | How to pick a provider: `Auto`, `Performance`, `Cost`, `Geographic`, `RoundRobin`. |
| `AutoFailoverEnabled` | `true` | HyperDrive-level failover (separate from StorageProviders failover). |
| `AutoReplicationEnabled` | `true` | HyperDrive-level replication. |
| `AutoLoadBalancingEnabled` | `true` | HyperDrive-level load balancing. |
| `MaxRetryAttempts` | `3` | Retries per operation before giving up. |
| `RequestTimeoutMs` | `5000` | Per-request timeout (5 sec). |
| `HealthCheckIntervalMs` | `30000` | How often providers are health-checked (30 sec). |
| `MaxConcurrentRequests` | `100` | Global concurrency cap across all providers. |
| **Scoring weights** | | Control how providers are ranked for selection. All should sum to 1.0 within each group. |
| `PerformanceWeight` | `0.4` | How much raw speed matters. |
| `CostWeight` | `0.3` | How much per-operation cost matters. |
| `GeographicWeight` | `0.2` | How much proximity to user matters. |
| `AvailabilityWeight` | `0.1` | How much historical uptime matters. |
| `LatencyWeight` | `0.5` | Within performance scoring — latency vs throughput. |
| `ThroughputWeight` | `0.3` | Within performance scoring. |
| `ReliabilityWeight` | `0.2` | Within performance scoring. |
| `MaxLatencyThresholdMs` | `200` | Providers over this latency are deprioritised. |
| `MaxErrorRateThreshold` | `0.05` | Providers with >5% errors are deprioritised. |
| `MinUptimeThreshold` | `99.0` | Providers below 99% uptime are deprioritised. |
| `EnabledProviders` | MongoDB, SQLite, Ethereum, IPFS | Which providers HyperDrive manages. |
| **PerProviderConfigs** | | Per-provider weight, timeout, error threshold. MongoDB=80, SQLite=90 (highest priority), Ethereum=70, IPFS=75. |
| **GeographicConfig** | | Regions defined: US-East, US-West, Europe, London, Asia/Tokyo. Used to route reads to nearest provider. Currently `IsEnabled: true` but bypassed by Legacy mode. |
| **CostConfig** | | Per-provider cost per GB/hour/transaction. SQLite cheapest (0.06/op), Ethereum most expensive (0.81/op). Used for cost-optimised routing. |
| **PerformanceConfig** | | Thresholds: max 1000ms response, max 5% error rate, min 99% uptime, min 10 Mbps. |
| **SecurityConfig** | | Requires encryption, auth, and authz on all HyperDrive operations. Session timeout 5 min, max 10 concurrent sessions. `AllowedIPs`/`BlockedIPs` are empty (open). |
| **MonitoringConfig** | | Metrics collected every 30 sec, real-time monitoring enabled, cost tracking enabled. No monitoring endpoints configured (`MonitoringEndpoints: []`). |

---

## HyperDriveMode

| Value | Effect |
|---|---|
| `"Legacy"` | **Current setting.** Uses the StorageProviders auto-failover/load-balance logic. HyperDrive config above is ignored. |
| `"HyperDrive"` | Activates the full intelligent provider orchestration above. |

---

## ReplicationRules

Controls when and how data is replicated across providers. Active when `AutoReplicationEnabled: true` in StorageProviders (currently false) or when HyperDrive mode is enabled.

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `Mode` | `Auto` | ✅ | `Auto` = system decides when to replicate. `Manual` = explicit trigger only. |
| `IsEnabled` | `true` | ⚠️ | Rules are defined but replication itself is off (`AutoReplicationEnabled: false`). |
| `MaxReplicationsPerMonth` | `1000` | ✅ | Hard cap to prevent runaway costs. |
| `CostThreshold` | `0.0` | ⚠️ | Maximum cost (USD) per replication before it's skipped. `0` = no threshold (replicate regardless of cost). Should be set. |
| `FreeProvidersOnly` | `false` | ✅ | When `true`, only replicates to providers with zero gas/transaction fees. |
| `GasFeeThreshold` | `0.01` | ✅ | Skip replication to blockchain providers if gas fee exceeds $0.01. |
| `ReplicationTriggers` | Test placeholder | ⚠️ | Rules defining *when* to trigger replication (e.g. on write, on schedule). Currently just a test entry with no condition or action. |
| `ProviderRules` | `[]` | ❌ | Per-provider replication rules. Not configured. |
| `DataTypeRules` | `[]` | ❌ | Replicate only certain data types. Not configured. |
| `ScheduleRules` | Test placeholder | ⚠️ | Scheduled replication jobs. Currently just a test entry. |
| **CostOptimization** | | |
| `MaxCostPerReplication` | `$0.01` | ✅ | Hard cap per individual replication operation. |
| `MaxCostPerMonth` | `$10.00` | ✅ | Monthly replication budget. |
| `AvoidHighGasProviders` | `true` | ✅ | Skip blockchain providers with high gas. |
| **IntelligentSelection** | Algorithm: Intelligent | 🔧 | ML-style provider selection for replication targets. Weights: cost 30%, performance 30%, reliability 20%, security 10%, geography 5%, availability 5%. Learning enabled. Not fully active in Legacy mode. |

---

## FailoverRules

Controls automatic failover when a provider becomes unavailable.

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `Mode` | `Auto` | ✅ | Automatic failover — no manual intervention needed. |
| `IsEnabled` | `true` | ✅ | Failover is active. |
| `MaxFailoversPerMonth` | `100` | ✅ | Cap on total failover events per month (cost protection). |
| `CostThreshold` | `0.0` | ⚠️ | Max cost per failover before declining. `0` = no limit. |
| `FreeProvidersOnly` | `false` | ✅ | Can failover to paid providers. |
| `GasFeeThreshold` | `0.01` | ✅ | Skip blockchain failover targets if gas > $0.01. |
| `FailoverTriggers` | Test placeholder | ⚠️ | Conditions that trigger failover. Currently just a test entry. Real triggers come from provider health monitoring. |
| `ProviderRules` | `[]` | ❌ | Per-provider failover routing rules. Not configured. |
| `IntelligentSelection` | Same as Replication | 🔧 | ML-style target selection for failover. Same weights as replication. |
| `EscalationRules` | `[]` | ❌ | Rules to escalate (e.g. alert a human) after N failovers. Not configured. |

---

## SubscriptionConfig

Controls billing tiers, quotas, and payment integration for OPORTAL subscribers.

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `PlanType` | `null` | ✅ | The active plan for this node/instance. Null = Free. Set dynamically per avatar by the SubscriptionService. |
| `MaxReplicationsPerMonth` | `100` | ✅ | How many cross-provider replications the free plan allows per month. |
| `MaxFailoversPerMonth` | `10` | ✅ | Free plan failover cap. |
| `MaxRequestsPerMonth` | `1000` | ✅ | Free plan API call limit per month. Enforced by `SubscriptionMiddleware`. |
| `MaxStorageGB` | `1` | ✅ | Free plan HyperDrive storage cap (GB). |
| `PayAsYouGoEnabled` | `false` | ✅ | When `true`, requests beyond plan quota are charged at overage rates instead of blocked. |
| `CostPerReplication` | `0.0` | ⚠️ | Overage cost per replication (used when PayAsYouGo is on). Zero = not yet priced. |
| `CostPerFailover` | `0.0` | ⚠️ | Overage cost per failover. Zero = not priced. |
| `CostPerGB` | `0.0` | ⚠️ | Overage cost per GB over limit. Zero = not priced. |
| `Currency` | `USD` | ✅ | Currency for all billing figures. |
| `BillingCycle` | `Monthly` | ✅ | Billing period. Monthly is the only implemented cycle. |
| `UsageAlerts` | Test entry | ⚠️ | Alerts fired when usage crosses a percentage threshold. Infrastructure is wired; the test entry has `Threshold: 0` and no notification channels — not actually alerting anything yet. |
| `QuotaNotifications` | Test entry | ⚠️ | Notifications when quota types are reached. Same situation — test placeholder, no channels configured. |
| **Stripe** | All empty | ⚠️ | Payment integration. Keys must be set via Railway env vars (`STRIPE_SECRET_KEY` etc.) for checkout to work. See env var names below. |
| `Stripe.SecretKey` | `""` | ⚠️ | Set via `STRIPE_SECRET_KEY` env var. Server-side only — never expose to frontend. |
| `Stripe.PublishableKey` | `""` | ⚠️ | Set via `STRIPE_PUBLISHABLE_KEY` env var. Safe to expose to frontend for Stripe.js. |
| `Stripe.WebhookSecret` | `""` | ⚠️ | Set via `STRIPE_WEBHOOK_SECRET` env var. Used to verify Stripe webhook signatures. |
| `Stripe.PriceBronze` | `""` | ⚠️ | Set via `STRIPE_PRICE_BRONZE`. Get from Stripe Dashboard → Products → Bronze → price_xxx. |
| `Stripe.PriceSilver` | `""` | ⚠️ | Set via `STRIPE_PRICE_SILVER`. |
| `Stripe.PriceGold` | `""` | ⚠️ | Set via `STRIPE_PRICE_GOLD`. |
| `Stripe.PriceEnterprise` | `""` | ⚠️ | Set via `STRIPE_PRICE_ENTERPRISE`. |

**Plan limits enforced by SubscriptionMiddleware (hardcoded in controller):**

| Plan | Requests/Month | Storage | Overage $/req |
|---|---|---|---|
| Free | 1,000 | 100 MB | — |
| Bronze | 10,000 | 1 GB | $0.001 |
| Silver | 100,000 | 10 GB | $0.0005 |
| Gold | 1,000,000 | 100 GB | $0.0002 |
| Enterprise | Unlimited | Unlimited | $0.0001 |

---

## DataPermissions

Controls what data avatars and holons can read/write, and which providers can see it.

> **Note:** All `IsEnabled` flags are `true` and `AuthenticationRequired` is `true`, but the `Fields`, `HolonTypes`, `Providers`, `Rules`, and `AccessPolicies` arrays are all empty. The framework is wired up and enforces authentication, but no fine-grained field/holon/provider restrictions have been configured yet.

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `AvatarPermissions.IsEnabled` | `true` | ✅ | Avatar data permission layer is active. |
| `AvatarPermissions.Fields` | `[]` | ⚠️ | Which avatar fields are restricted. Empty = all fields accessible to authenticated users. Add entries to lock down e.g. `PrivateKey`, `Email`. |
| `AvatarPermissions.DefaultPermission` | `Read` | ✅ | Unauthenticated or unconfigured avatars default to read-only access. |
| `AvatarPermissions.ProviderOverrides` | `{}` | ❌ | Override permissions per storage provider. Not configured. |
| `HolonPermissions.IsEnabled` | `true` | ✅ | Holon data permission layer is active. |
| `HolonPermissions.HolonTypes` | `[]` | ⚠️ | Which holon types have restrictions. Empty = no per-type restrictions. |
| `HolonPermissions.DefaultPermission` | `Read` | ✅ | Default to read-only. |
| `HolonPermissions.ProviderOverrides` | `{}` | ❌ | Not configured. |
| `ProviderPermissions.IsEnabled` | `true` | ✅ | Provider-level permission layer active. |
| `ProviderPermissions.Providers` | `[]` | ❌ | Which providers have access to what. Empty = no restrictions beyond auth. |
| `FieldLevelPermissions.IsEnabled` | `true` | ✅ | Field-level encryption/access layer active. |
| `FieldLevelPermissions.Rules` | `[]` | ❌ | No field-level rules defined. Should eventually contain rules like "encrypt `Email` at rest" or "only admin can read `PrivateKey`". |
| `AccessControl.IsEnabled` | `true` | ✅ | Access control layer enforced. |
| `AccessControl.AuthenticationRequired` | `true` | ✅ | All data operations require a valid JWT. |
| `AccessControl.AuthorizationLevel` | `Authenticated` | ✅ | Any authenticated user can access data. Options likely include `Admin`, `Owner`, `Role`. |
| `AccessControl.EncryptionLevel` | `Standard` | ✅ | Data encrypted at standard level in transit and at rest. Options: `None`, `Standard`, `High`, `Quantum`. |
| `AccessControl.AuditLogging` | `true` | ✅ | All data access events are logged for audit trails. |
| `AccessControl.AccessPolicies` | `[]` | ❌ | Named access policies (e.g. "AdminOnly", "OwnerOnly"). None configured — everything falls back to `AuthorizationLevel`. |

---

## IntelligentMode

Cross-cutting AI/ML optimisation layer that learns from usage patterns to improve provider selection, cost, and performance over time.

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `IsEnabled` | `true` | 🔧 | Master switch. Read by the system but full learning pipeline not yet complete. |
| `AutoOptimization` | `true` | 🔧 | Automatically adjusts weights and strategies based on observed outcomes. |
| `CostAwareness` | `true` | 🔧 | Feeds cost data into optimisation decisions. |
| `PerformanceOptimization` | `true` | 🔧 | Feeds latency/throughput data in. |
| `SecurityOptimization` | `true` | 🔧 | Prefers providers with better security posture. |
| `LearningEnabled` | `true` | 🔧 | Enables the reinforcement learning loop. |
| `AdaptationSpeed` | `Medium` | 🔧 | How quickly the system shifts weights. Options: `Slow`, `Medium`, `Fast`. |
| `OptimizationGoals` | `[]` | ❌ | Named goals (e.g. "MinimiseCost", "MaximiseUptime"). None configured — uses defaults. |

---

## ONET

ONET is the OASIS peer-to-peer network that allows ONODE instances to discover and communicate with each other.

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `BootstrapServers` | `["https://dev.api.web4..."]` | ✅ | Known nodes used to bootstrap P2P discovery. Must point to live API in production. |
| `NetworkType` | `Internal` | ✅ | `Internal` = trusted private network. `Public` = open P2P. |
| `NodeId` | `""` | ⚠️ | Unique identifier for this node. Empty = auto-generated at startup. Set explicitly for stable node identity. |
| `NodePublicKey` | `""` | ⚠️ | This node's public key for P2P authentication. Empty = auto-generated. |
| `NodePrivateKey` | `""` | ⚠️ | This node's private key. Empty = auto-generated. **If set, keep secret.** |
| `TcpPort` | `38470` | ✅ | Port for direct node-to-node TCP connections. Must be open in firewall/Railway. |
| `EnableMDNS` | `true` | ✅ | Local network discovery via mDNS (useful on LAN, no effect in Railway/cloud). |
| `AutoRegisterOnBootstrap` | `true` | ✅ | Automatically announces this node to bootstrap servers on startup. |

---

## Web6

WEB6 is the unified AI gateway — the intelligence layer that routes requests across AI providers.

| Setting | Current Value | Status | Description |
|---|---|---|---|
| `DefaultProvider` | `Auto` | ✅ | Default AI routing target. `Auto` = FAHRN picks the best provider per request. |
| `DefaultOpenServModel` | `gpt-5.4` | ✅ | Default model used when routing through OpenServ. |
| `DefaultRoutingPriority` | `cost` | ✅ | Primary optimisation objective: `cost`, `speed`, `quality`. |
| `DefaultRoutingFallbackEnabled` | `true` | ✅ | Try next provider if primary fails. |
| `PreferOpenServ` | `false` | ✅ | When `true`, always routes through OpenServ aggregator. When `false`, direct provider calls are preferred. |
| `EnableFAHRN` | `false` | ⚠️ | FAHRN (Federated Adaptive Holonic Routing Network) — the intelligent decomposition and routing engine. Currently disabled. Enable to activate multi-agent problem decomposition. |
| `EnableHolonicBraid` | `false` | ⚠️ | Holonic Braid — multi-model consensus voting. Currently disabled. Enable to have multiple models vote on answers and pick the best. |

### Web6.OpenServ

| Setting | Status | Description |
|---|---|---|
| `BaseUrl` | ✅ | OpenServ inference API endpoint. |
| `DefaultModel` | ✅ | `gpt-5.4` — current default via OpenServ aggregation. |
| `AvailableModels` | ✅ | Full list of models available through OpenServ (GPT, Claude, Gemini, Grok, Qwen, DeepSeek etc.). |

### Web6.FAHRN

| Setting | Current Value | Status | Description |
|---|---|---|
| `EMAAlpha` | `0.2` | 🔧 | Exponential moving average decay for performance scoring. Lower = slower to adapt. |
| `DefaultDispatchMode` | `Serial` | 🔧 | `Serial` = one sub-problem at a time. `Parallel` = concurrent. Serial when FAHRN is enabled. |
| `AutoSeedOpenServAgentsOnStartup` | `true` | 🔧 | Pre-register OASIS agents into OpenServ on boot. |
| `MaxDecomposedSubProblems` | `3` | 🔧 | Max number of sub-problems FAHRN decomposes a request into. |

### Web6.HolonicBraid

| Setting | Status | Description |
|---|---|---|
| `AutoPersistWinningPlan` | 🔧 | Saves the winning consensus response to HolonManager for future retrieval. |

### Web6.HolonicMemory

| Setting | Status | Description |
|---|---|---|
| `DefaultRetentionPolicy` | 🔧 | How long AI responses are kept in holonic memory. `Default` = system decides. |
| `RecordDispatchOutcomes` | 🔧 | Log which provider won each routing decision (feeds into FAHRN learning). |

### Web6.ApiKeys

| Provider | Status | Notes |
|---|---|---|
| `OpenAI` | ✅ Live key present | Currently the only active provider key. |
| `OpenServ` | ✅ Live key present | OpenServ aggregator key. |
| `LeelaAI` | ✅ Live key present | LeelaAI provider key. |
| `Anthropic` | ⚠️ Empty | Needed for direct Claude calls (bypassing OpenServ). |
| `Gemini` | ⚠️ Empty | Direct Gemini access. |
| `Groq` | ⚠️ Empty | |
| `Mistral` | ⚠️ Empty | |
| `Cohere` | ⚠️ Empty | |
| `XAI` | ⚠️ Empty | Grok direct access. |
| `DeepSeek` | ⚠️ Empty | |
| `HuggingFace` | ⚠️ Empty | |
| `AzureOpenAI` | ⚠️ Empty | |
| `StabilityAI` | ⚠️ Empty | Image generation. |

> **Security note:** API keys in `OASIS_DNA.json` are gitignored for the live and dev files. However, the OpenAI key is visible in this file on disk. Consider moving all WEB6 API keys to Railway env vars and loading them at runtime.

### Web6.LeelaAI

| Setting | Status | Description |
|---|---|---|
| `BaseUrl` | ✅ | Lambda function URL for LeelaAI inference. |

---

## Payments

WEB6 enforces access control through a two-axis model: **Karma tier** (earned reputation — Free / Bronze / Silver / Gold / Diamond) and **API quota** (request counts per billing cycle). `SubscriptionConfig` (above) covers the WEB4/OPORTAL Stripe integration; this section documents the WEB6-specific karma-gating and quota rules.

### Karma Tiers

| Tier | Min Karma | AI Providers Unlocked | Rate Limit (req/min) | Notes |
|---|---|---|---|---|
| Free | 0 | Free-tier providers only (Ollama, LM Studio, local Llama etc.) | 10 | No cost; capped to providers that have no per-call cost |
| Bronze | 100 | + OpenAI GPT-4o-mini, Anthropic Haiku, Groq, Mistral | 60 | Requires `PlanType = Bronze` in SubscriptionConfig |
| Silver | 500 | + GPT-4o, Claude Sonnet, Gemini 1.5 Pro, Cohere | 300 | |
| Gold | 2 000 | + o1-preview, Claude Opus, Gemini Ultra, xAI Grok | 1 000 | |
| Diamond | 10 000 | All 97 providers including Bittensor native subnets, Nostr DVMs | Unlimited | |

> Tier gating is enforced in `AIProviderManager` via `KarmaGateMiddleware`. The karma threshold values above are the live production defaults and can be overridden in `Web6.KarmaGates` in OASIS_DNA.json (not yet wired; middleware reads these constants directly).

### WEB6 Quota Fields (under `Web6.Quota` — not yet in DNA schema, enforced in middleware)

| Field | Default | Description |
|---|---|---|
| `FreeRequestsPerDay` | `50` | Hard cap for Free-tier avatars per 24-hour rolling window |
| `BronzeRequestsPerDay` | `500` | Bronze plan daily cap (combined across all providers) |
| `SilverRequestsPerDay` | `5 000` | Silver plan daily cap |
| `GoldRequestsPerDay` | `50 000` | Gold plan daily cap |
| `DiamondRequestsPerDay` | `Unlimited` | No cap |
| `OverageAction` | `Block` | What happens when quota is exhausted: `Block` = 429 response; `PayAsYouGo` = charge per request if `PayAsYouGoEnabled` is true |

### Stripe Env Vars Required for WEB6 Checkout

These must be set in Railway (or local `.env`) — never commit real values:

```
STRIPE_SECRET_KEY=sk_live_...
STRIPE_PUBLISHABLE_KEY=pk_live_...
STRIPE_WEBHOOK_SECRET=whsec_...
STRIPE_PRICE_BRONZE=price_...
STRIPE_PRICE_SILVER=price_...
STRIPE_PRICE_GOLD=price_...
STRIPE_PRICE_DIAMOND=price_...
```

Checkout flow: `POST /v1/billing/checkout` → SubscriptionService creates a Stripe Checkout session → user completes payment → Stripe fires `checkout.session.completed` webhook → `SubscriptionMiddleware` upgrades `PlanType` and writes updated karma tier to avatar record.

### Pay-As-You-Go Overage Rates (WEB6 AI calls)

| Tier | Per request above quota |
|---|---|
| Bronze | $0.002 |
| Silver | $0.001 |
| Gold | $0.0005 |
| Diamond | $0.0002 |

> Rates are not yet wired to the live Stripe meter API. `CostPerRequest` fields exist in `SubscriptionConfig` but are currently `0.0`. To enable metered billing, set `PayAsYouGoEnabled: true` in SubscriptionConfig and configure Stripe Meter in the Dashboard, then update the price IDs above to the metered price IDs.

---

## Summary: Things That Need Attention

| Priority | Issue |
|---|---|
| 🔴 High | **Stripe keys not set** — subscription checkout returns 500 until Railway env vars are populated |
| 🔴 High | **MongoDB connection string contains credentials** — move to env var (`MONGODB_CONNECTION_STRING`) |
| 🔴 High | **AI API keys in DNA file on disk** — consider Railway env vars for OpenAI/OpenServ/LeelaAI keys |
| 🟡 Medium | `IsProduction: false` in the file currently loaded on live — verify Railway is loading the right DNA file or overriding this |
| 🟡 Medium | `OASISAPIURL` and `OASISWebSiteURL` still point to `dev.` URLs in the ONODE DNA file — must be live URLs in production |
| 🟡 Medium | `RemoveOldRefreshTokensAfterXDays: 0` — refresh tokens never purged, will accumulate in DB |
| 🟡 Medium | `DataPermissions` framework is wired but all permission lists are empty — no field/holon restrictions enforced |
| 🟡 Medium | Solana keys differ between LIVE and DEV backups — verify which is the canonical live keypair |
| 🟠 Low | `HyperDriveMode: Legacy` — the full intelligent HyperDrive is built but not activated |
| 🟠 Low | `EnableFAHRN: false` and `EnableHolonicBraid: false` — powerful features ready but off |
| 🟠 Low | `SettingsLookupHolonId` is all zeros — DNA-from-network feature not yet wired |
| 🟠 Low | Ethereum/Arbitrum/Rootstock/Polygon all use shared test private keys — need separate mainnet keys before going live on those chains |
