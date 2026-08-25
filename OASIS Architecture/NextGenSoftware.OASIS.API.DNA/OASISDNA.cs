using System;
using System.Collections.Generic;
using NextGenSoftware.ErrorHandling;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Configuration;

namespace NextGenSoftware.OASIS.API.DNA
{
    public class OASISDNA
    {
        public OASIS OASIS { get; set; }
    }

    public class OASIS
    {
        //public string CurrentLiveVersion { get; set; }
        //public string CurrentStagingVersion { get; set; }
        //public string OASISVersion { get; set; }
        public string Terms { get; set; }
        public LoggingSettings Logging { get; set; }
        public ErrorHandlingSettings ErrorHandling { get; set; }
        public SecuritySettings Security { get; set; }
        public EmailSettings Email { get; set; }
        public StorageProviderSettings StorageProviders { get; set; }
        public OASISHyperDriveConfig OASISHyperDriveConfig { get; set; }
        public Web6Settings Web6 { get; set; } = new Web6Settings();
        
        // HyperDrive mode switch: "Legacy" or "OASISHyperDrive2"
        public string HyperDriveMode { get; set; } = "Legacy";
        
        // Enhanced HyperDrive Configuration
        public ReplicationRulesConfig ReplicationRules { get; set; } = new ReplicationRulesConfig();
        public FailoverRulesConfig FailoverRules { get; set; } = new FailoverRulesConfig();
        public SubscriptionConfig SubscriptionConfig { get; set; } = new SubscriptionConfig();
        public DataPermissionsConfig DataPermissions { get; set; } = new DataPermissionsConfig();
        public IntelligentModeConfig IntelligentMode { get; set; } = new IntelligentModeConfig();
        
        public ONETConfig ONET { get; set; } = new ONETConfig();

        public string OASISSystemAccountId { get; set; }
        public string OASISAPIURL { get; set; }
        public string NetworkId { get; set; } = "onet-network";
        /// <summary>Directory where OASIS persists runtime state (quota counters, discovered ONET peers, etc.). Relative paths are resolved against the working directory.</summary>
        public string DataDirectory { get; set; } = "oasis-data";
        public Guid SettingsLookupHolonId { get; set; } = Guid.Empty;
        // Stats caching controls
        public bool StatsCacheEnabled { get; set; } = false;
        public int StatsCacheTtlSeconds { get; set; } = 45;
    }

    /// <summary>
    /// Settings for WEB6 - the unified AI abstraction/aggregation layer (NextGenSoftware.OASIS.Web6.*), covering
    /// the multi-provider completion router (AIProviderManager), the FAHRN reasoning-network controller agent
    /// (FAHRNManager) and the Holonic BRAID shared reasoning-graph memory (HolonicBraidManager).
    /// </summary>
    public class Web6Settings
    {
        /// <summary>Default AIProviderType (e.g. "Auto", "OpenAI", "Anthropic", "OpenServ") used by /v1/complete when CompletionRequest.Provider is "auto" or not set.</summary>
        public string DefaultProvider { get; set; } = "Auto";

        /// <summary>Default model id used when CompletionRequest.Model is "auto" or not set and the resolved provider is OpenServ.</summary>
        public string DefaultOpenServModel { get; set; } = "gpt-5.4";

        /// <summary>Routing priority used by AIProviderManager.ResolveProviderCandidates when Provider is "auto" and Routing.Priority is not set on the request: "quality", "latency", or "cost" (default).</summary>
        public string DefaultRoutingPriority { get; set; } = "cost";

        /// <summary>When true, AIProviderManager.CompleteAsync fails over to the next configured provider candidate on error (overridable per-request via CompletionRequest.Routing.Fallback).</summary>
        public bool DefaultRoutingFallbackEnabled { get; set; } = true;

        /// <summary>
        /// When true and CompletionRequest.Provider is "auto", route through the OpenServ SERV gateway instead of
        /// calling providers directly. OpenServ reaches OpenAI, Anthropic, Google, xAI, Qwen and DeepSeek behind a
        /// single SERV_API_KEY / ApiKeys.OpenServ. Overridable per-request via CompletionRequest.Routing.UseOpenServ.
        /// </summary>
        public bool PreferOpenServ { get; set; } = false;

        /// <summary>
        /// When true, /v1/complete automatically runs the FAHRN reasoning-network dispatch step before calling the
        /// AI provider, injecting the returned Mermaid execution plan into the system context. This improves
        /// multi-step reasoning at the cost of extra latency. Overridable per-request via CompletionRequest.UseFAHRN.
        /// </summary>
        public bool EnableFAHRN { get; set; } = false;

        /// <summary>
        /// When true, /v1/complete fetches the shared Holonic BRAID reasoning graph for the request's task type
        /// and injects it into the system context before calling the provider. Reasoning patterns compound globally
        /// across all users. Overridable per-request via CompletionRequest.UseHolonicBraid.
        /// </summary>
        public bool EnableHolonicBraid { get; set; } = false;

        public OpenServSettings OpenServ { get; set; } = new OpenServSettings();
        public FAHRNSettings FAHRN { get; set; } = new FAHRNSettings();
        public HolonicBraidSettings HolonicBraid { get; set; } = new HolonicBraidSettings();
        public HolonicMemorySettings HolonicMemory { get; set; } = new HolonicMemorySettings();
        public LeelaAISettings LeelaAI { get; set; } = new LeelaAISettings();

        /// <summary>
        /// AI provider API keys. Environment variables always take priority (OPENAI_API_KEY, ANTHROPIC_API_KEY, etc.)
        /// and override these values at runtime. Set keys here for local development so you don't need to configure
        /// environment variables on your machine. Leave blank in production and use environment variables instead.
        /// </summary>
        public Web6ApiKeysSettings ApiKeys { get; set; } = new Web6ApiKeysSettings();

        /// <summary>Base URL for Web4 API — used by avatar context injection and StarnetContextManager. Env var WEB4_API_BASE_URL takes priority.</summary>
        public string Web4BaseUrl { get; set; } = "https://api.oasisomniverse.one";

        /// <summary>Base URL for Web5 API — used by avatar context injection and StarnetContextManager. Env var WEB5_API_BASE_URL takes priority.</summary>
        public string Web5BaseUrl { get; set; } = "https://api.star.oasisomniverse.one";

        /// <summary>When true, inject avatar context (Web4 karma + Web5 quests) into every completion request that has an AvatarId set. Overridable per-request via CompletionRequest.InjectAvatarContext.</summary>
        public bool InjectAvatarContext { get; set; } = false;

        /// <summary>When true, Web6 registers itself as an MCP orchestrator in its own registry on startup so FAHRN agents can call back into any OASIS tool.</summary>
        public bool SelfRegisterAsOrchestrator { get; set; } = true;

        /// <summary>Per-avatar monthly USD spend limit. 0 = no limit. CompletionController returns 429 when exceeded.</summary>
        public double DefaultMonthlyBudgetUSD { get; set; } = 0;

        /// <summary>Per-avatar daily token limit (prompt + completion). 0 = no limit.</summary>
        public int DefaultDailyTokenLimit { get; set; } = 0;

        /// <summary>Settings for the semantic response cache (Priority 13).</summary>
        public Web6CacheSettings Cache { get; set; } = new Web6CacheSettings();
    }

    public class Web6CacheSettings
    {
        /// <summary>Default TTL in seconds for cached completions (0 = disabled globally). Overridable per-request via CompletionRequest.CacheTtlSeconds.</summary>
        public int DefaultTtlSeconds { get; set; } = 3600;

        /// <summary>Cosine similarity threshold (0–1) above which a cached response is returned. Overridable per-request via CompletionRequest.CacheSimilarityThreshold.</summary>
        public double SimilarityThreshold { get; set; } = 0.95;

        /// <summary>Maximum number of cached entries to keep in memory (LRU eviction when exceeded).</summary>
        public int MaxEntries { get; set; } = 1000;
    }

    /// <summary>
    /// API keys for every AI provider supported by WEB6's AIProviderManager.
    /// Environment variables (OPENAI_API_KEY, ANTHROPIC_API_KEY, etc.) always win over these values.
    /// These are the fallback used when the corresponding environment variable is absent or empty.
    /// Do NOT commit real keys to source control — use environment variables in production.
    /// </summary>
    public class Web6ApiKeysSettings
    {
        public string OpenAI { get; set; } = "";
        public string Anthropic { get; set; } = "";
        public string Gemini { get; set; } = "";
        public string Groq { get; set; } = "";
        public string Mistral { get; set; } = "";
        public string Cohere { get; set; } = "";
        public string XAI { get; set; } = "";
        public string DeepSeek { get; set; } = "";
        public string HuggingFace { get; set; } = "";
        public string AzureOpenAI { get; set; } = "";
        public string StabilityAI { get; set; } = "";
        /// <summary>OpenServ SERV gateway key — reaches OpenAI, Anthropic, Google, xAI, Qwen, DeepSeek behind one key.</summary>
        public string OpenServ { get; set; } = "";
        /// <summary>Leela AI API key. Env var LEELA_API_KEY takes priority.</summary>
        public string LeelaAI { get; set; } = "";
    }

    /// <summary>Settings for Leela AI — spiritual intelligence / karmic-pattern reasoning provider.</summary>
    public class LeelaAISettings
    {
        /// <summary>Leela AI Lambda endpoint. Env var LEELA_BASE_URL takes priority.</summary>
        public string BaseUrl { get; set; } = "https://namozyqyvwf62hqxpzujt7e5hq0njhge.lambda-url.eu-west-1.on.aws/";
    }

    /// <summary>
    /// Settings for the OpenServ provider - the SERV inference gateway that reaches every model in the SERV
    /// catalog (OpenAI, Anthropic, Google, xAI, Qwen, DeepSeek) behind one SERV_API_KEY via an OpenAI-compatible
    /// chat/completions endpoint. See https://docs.openserv.ai/serv-reasoning/sdk-integration
    /// </summary>
    public class OpenServSettings
    {
        /// <summary>The OpenAI-compatible chat/completions base URL for the SERV inference gateway.</summary>
        public string BaseUrl { get; set; } = "https://inference-api.openserv.ai/v1/chat/completions";

        /// <summary>Default model id used when none is specified on the request (kept in sync with OpenServCatalog.DefaultModel in NextGenSoftware.OASIS.Web6.Core).</summary>
        public string DefaultModel { get; set; } = "gpt-5.4";

        /// <summary>The full list of model ids in the SERV catalog. Mirrors NextGenSoftware.OASIS.Web6.Core.Models.OpenServCatalog.Models and the OASIS IDE's OPENSERV_MODELS list - kept here too so the catalog is configurable/overridable per deployment without a code change.</summary>
        public List<string> AvailableModels { get; set; } = new List<string>
        {
            "gpt-5.5", "gpt-5.4", "gpt-5.4-mini", "gpt-5.4-nano",
            "o3", "o3-mini", "o3-pro", "o4-mini",
            "claude-opus-4.6", "claude-sonnet-4.6", "claude-haiku-4.5",
            "gemini-flash-latest", "gemini-pro-latest", "gemma-4-26b-a4b-it", "gemma-4-31b-it",
            "grok-4.3", "grok-4.20",
            "qwen3.6-flash", "qwen3.6-max-preview",
            "deepseek-v4-pro", "deepseek-v4-flash"
        };
    }

    /// <summary>
    /// Settings for FAHRN - the Fractal Adaptive Holonic Reasoning Network controller agent (FAHRNManager) that
    /// scores, routes (Serial/Parallel/Decomposed), runs loop detection over, and learns from every registered
    /// reasoning agent.
    /// </summary>
    public class FAHRNSettings
    {
        /// <summary>The smoothing factor for the Exponential Moving Average used to update agent scores after every dispatch outcome (mirrors FAHRNManager.EMAAlpha). 0-1, higher reacts faster to recent outcomes.</summary>
        public double EMAAlpha { get; set; } = 0.2;

        /// <summary>Default DispatchMode ("Serial", "Parallel", or "Decomposed") used when a DispatchRequest does not specify one.</summary>
        public string DefaultDispatchMode { get; set; } = "Serial";

        /// <summary>When true, the WEB6 host automatically calls FAHRNManager.SeedDefaultOpenServAgentsAsync() once at startup so the reasoning network has agents to score/dispatch against without a manual seed call.</summary>
        public bool AutoSeedOpenServAgentsOnStartup { get; set; } = true;

        /// <summary>Maximum number of agents considered "leads" in Decomposed dispatch mode (mirrors the Take(3) used in FAHRNManager.DispatchDecomposedAsync).</summary>
        public int MaxDecomposedSubProblems { get; set; } = 3;

        /// <summary>Global default token ceiling for a full dispatch run (null = unlimited). Per-request MaxTotalTokens takes priority.</summary>
        public int? DefaultMaxTotalTokens { get; set; }

        /// <summary>Global default USD cost ceiling for a full dispatch run (null = unlimited). Per-request MaxCostUsd takes priority.</summary>
        public decimal? DefaultMaxCostUsd { get; set; }

        /// <summary>Global default token ceiling per individual agent call (null = unlimited). Per-request MaxTokensPerAgent takes priority.</summary>
        public int? DefaultMaxTokensPerAgent { get; set; }
    }

    /// <summary>Settings for the Holonic BRAID shared reasoning-graph memory (HolonicBraidManager) - persisted per task-type Mermaid execution plans that any agent can re-use at zero generation cost.</summary>
    public class HolonicBraidSettings
    {
        /// <summary>When true, a winning dispatch's Mermaid plan is persisted as the shared graph for its task type if none exists yet (mirrors the behaviour in FAHRNManager.DispatchAsync).</summary>
        public bool AutoPersistWinningPlan { get; set; } = true;
    }

    /// <summary>Settings for the Holonic Memory hierarchy (HolonicMemoryManager) - the fractal User/Agent/Session memory levels that FAHRN records dispatch outcomes into.</summary>
    public class HolonicMemorySettings
    {
        /// <summary>Default RetentionPolicy applied to newly-created HolonicMemory holons when none is specified.</summary>
        public string DefaultRetentionPolicy { get; set; } = "Default";

        /// <summary>When true, FAHRNManager.DispatchAsync records a session memory item for every dispatch outcome (mirrors RecordSessionMemoryAsync).</summary>
        public bool RecordDispatchOutcomes { get; set; } = true;
    }

    public class SecuritySettings
    {
        public bool HideVerificationToken { get; set; }
        public bool HideRefreshTokens { get; set; }
        public string SecretKey { get; set; }
        public int RemoveOldRefreshTokensAfterXDays { set; get; }
        /// <summary>JWT (access token) expiration in minutes. Industry standard 5–60; default 15. Used when issuing tokens on authenticate/refresh.</summary>
        public int JwtTokenExpirationMinutes { get; set; } = 15;
        /// <summary>Refresh token expiration in days. Industry standard 1–30; default 7. Used when issuing refresh tokens and setting auth cookie expiry.</summary>
        public int RefreshTokenExpirationDays { get; set; } = 7;
        public EncryptionSettings AvatarPassword { get; set; }
        public EncryptionSettings OASISProviderPrivateKeys { get; set; }
        /// <summary>Encryption settings applied to holon MetaData at rest. All layers are reversible (no BCrypt). Overridable per SaveHolon call via Holon.DataEncryptionOverride.</summary>
        public EncryptionSettings HolonDataEncryption { get; set; }
        /// <summary>When true each avatar is assigned a W3C DID (did:oasis:&lt;avatarId&gt;) and the DID is included in issued JWT tokens.</summary>
        public bool DIDEnabled { get; set; }
        /// <summary>DID challenge nonce store configuration.</summary>
        public DIDChallengeStoreSettings DIDChallengeStore { get; set; } = new();
        /// <summary>
        /// When true, all non-exempt requests must include the X-OASIS-API-Key header.
        /// Toggle on when you want to lock down the API to known clients only.
        /// Defaults to false so existing deployments are unaffected.
        /// </summary>
        public bool RequireApiKey { get; set; } = false;
        /// <summary>
        /// The expected value of the X-OASIS-API-Key header when RequireApiKey is true.
        /// OASIS_API_KEY environment variable takes priority over this at runtime (use for Railway secrets).
        /// Leave empty to disable the key check even when RequireApiKey is true.
        /// </summary>
        public string ApiKey { get; set; } = "";
        /// <summary>Per-IP rate limiting. Configurable per deployment; defaults provide sensible protection out of the box.</summary>
        public RateLimitingSettings RateLimiting { get; set; } = new RateLimitingSettings();
    }

    public class RateLimitingSettings
    {
        /// <summary>When false, rate limiting is completely disabled regardless of other settings.</summary>
        public bool Enabled { get; set; } = true;
        /// <summary>Maximum requests allowed per IP within the sliding window.</summary>
        public int RequestsPerWindow { get; set; } = 100;
        /// <summary>Sliding window size in seconds.</summary>
        public int WindowSeconds { get; set; } = 60;
        /// <summary>Number of segments the window is divided into (higher = smoother limiting).</summary>
        public int WindowSegments { get; set; } = 6;
        /// <summary>Requests that exceed the limit are queued up to this depth; 0 = reject immediately with 429.</summary>
        public int QueueLimit { get; set; } = 0;
    }

    public class DIDChallengeStoreSettings
    {
        /// <summary>
        /// Which backing store to use for DID challenge nonces.
        /// "InMemory" (default) — single-node, zero dependencies.
        /// "Redis"    — multi-node; requires <see cref="RedisConnectionString"/>.
        /// </summary>
        public string Provider { get; set; } = "InMemory";

        /// <summary>StackExchange.Redis connection string, e.g. "localhost:6379" or "redis.myhost.com:6379,password=secret".</summary>
        public string RedisConnectionString { get; set; }

        /// <summary>Redis key prefix to namespace OASIS nonces. Defaults to "oasis:did:challenge:".</summary>
        public string RedisKeyPrefix { get; set; } = "oasis:did:challenge:";

        /// <summary>Nonce TTL in seconds. Defaults to 300 (5 minutes).</summary>
        public int NonceTtlSeconds { get; set; } = 300;
    }

    public class ErrorHandlingSettings
    {
        public bool ShowStackTrace { get; set; }
        public bool ThrowExceptionsOnErrors { get; set; }
        public bool ThrowExceptionsOnWarnings { get; set; }
        public bool LogAllErrors { get; set; }
        public bool LogAllWarnings { get; set; }

        /// <summary>
        /// An enum that specifies what to do when an error occurs. The options are: `AlwaysThrowExceptionOnError`, `OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent` & `NeverThrowExceptions`). The default is `OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent` meaning it will only throw an error if the `OnError` event has not been subscribed to. This delegates error handling to the caller. If no event has been subscribed then OASIS will throw an error. `AlwaysThrowExceptionOnError` will always throw an error even if the `OnError` event has been subscribed to. The `NeverThrowException` enum option will never throw an error even if the `OnError` event has not been subscribed to. Regardless of what enum is selected, the error will always be logged using whatever ILogProvider's have been injected into the constructor or set on the static Logging.LogProviders property.
        /// </summary>
        //public ErrorHandlingBehaviour ErrorHandlingBehaviour { get; set; } = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent;

        /// <summary>
        /// An enum that specifies what to do when an warning occurs. The options are: `AlwaysThrowExceptionOnWarning`, `OnlyThrowExceptionIfNoWarningHandlerSubscribedToOnWarningEvent` & `NeverThrowExceptions`). The default is `OnlyThrowExceptionIfNoWarningHandlerSubscribedToOnWarningEvent` meaning it will only throw an error if the `OnWarning` event has not been subscribed to. This delegates error handling to the caller. If no event has been subscribed then OASIS will throw an error. `AlwaysThrowExceptionOnWarning` will always throw an error even if the `OnWarning` event has been subscribed to. The `NeverThrowException` enum option will never throw an error even if the `OnWarning` event has not been subscribed to. Regardless of what enum is selected, the error will always be logged using whatever ILogProvider`s have been injected into the constructor or set on the static Logging.LogProviders property.
        /// </summary>
        //public WarningHandlingBehaviour WarningHandlingBehaviour { get; set; } = WarningHandlingBehaviour.OnlyThrowExceptionIfNoWarningHandlerSubscribedToOnWarningEvent;

        /// <summary>
        /// An enum that specifies what to do when an error occurs. The options are: `AlwaysThrowExceptionOnError`, `OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent` & `NeverThrowExceptions`). The default is `OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent` meaning it will only throw an error if the `OnError` event has not been subscribed to. This delegates error handling to the caller. If no event has been subscribed then OASIS will throw an error. `AlwaysThrowExceptionOnError` will always throw an error even if the `OnError` event has been subscribed to. The `NeverThrowException` enum option will never throw an error even if the `OnError` event has not been subscribed to. Regardless of what enum is selected, the error will always be logged using whatever ILogProvider's have been injected into the constructor or set on the static Logging.LogProviders property.
        /// </summary>
        public ErrorHandlingBehaviour ErrorHandlingBehaviour
        {
            get
            {
                return ErrorHandling.ErrorHandling.ErrorHandlingBehaviour;
            }
            set
            {
                ErrorHandling.ErrorHandling.ErrorHandlingBehaviour = value;
            }
        }

        /// <summary>
        /// An enum that specifies what to do when an warning occurs. The options are: `AlwaysThrowExceptionOnWarning`, `OnlyThrowExceptionIfNoWarningHandlerSubscribedToOnWarningEvent` & `NeverThrowExceptions`). The default is `OnlyThrowExceptionIfNoWarningHandlerSubscribedToOnWarningEvent` meaning it will only throw an error if the `OnWarning` event has not been subscribed to. This delegates error handling to the caller. If no event has been subscribed then OASIS will throw an error. `AlwaysThrowExceptionOnWarning` will always throw an error even if the `OnWarning` event has been subscribed to. The `NeverThrowException` enum option will never throw an error even if the `OnWarning` event has not been subscribed to. Regardless of what enum is selected, the error will always be logged using whatever ILogProvider`s have been injected into the constructor or set on the static Logging.LogProviders property.
        /// </summary>
        public WarningHandlingBehaviour WarningHandlingBehaviour
        {
            get
            {
                return ErrorHandling.ErrorHandling.WarningHandlingBehaviour;
            }
            set
            {
                ErrorHandling.ErrorHandling.WarningHandlingBehaviour = value;
            }
        }
    }

    public class LoggingSettings
    {
        public string LoggingFramework { get; set; } = "Default";

        /// <summary>
        /// If the LoggingFramework is set to anything other than 'Default' then you can set this flag to true to also log to the Default LogProvider below.
        /// </summary>
        public bool AlsoUseDefaultLogProvider { get; set; } = false;

        /// <summary>
        /// This passes through to the static LogConfig.FileLoggingMode property in [NextGenSoftware.Logging](https://www.nuget.org/packages/NextGenSoftware.Logging) package. It can be either `WarningsErrorsInfoAndDebug`, `WarningsErrorsAndInfo`, `WarningsAndErrors` or `ErrorsOnly`.
        /// </summary>
        public LoggingMode FileLoggingMode
        {
            get
            {
                return LogConfig.FileLoggingMode;
            }
            set
            {
                LogConfig.FileLoggingMode = value;
            }
        }

        /// <summary>
        /// This passes through to the static LogConfig.ConsoleLoggingMode property in [NextGenSoftware.Logging](https://www.nuget.org/packages/NextGenSoftware.Logging) package. It can be either `WarningsErrorsInfoAndDebug`, `WarningsErrorsAndInfo`, `WarningsAndErrors` or `ErrorsOnly`.
        /// </summary>
        public LoggingMode ConsoleLoggingMode
        {
            get
            {
                return LogConfig.ConsoleLoggingMode;
            }
            set
            {
                LogConfig.ConsoleLoggingMode = value;
            }
        }

        /// <summary>
        /// Set this to true (default) if you wish HoloNET to log to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public bool LogToConsole { get; set; } = true;

        /// <summary>
        /// Set this to true to enable coloured logs in the console. NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public bool ShowColouredLogs { get; set; } = true;

        /// <summary>
        /// The colour to use for `Debug` log entries to the console NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public ConsoleColor DebugColour { get; set; } = ConsoleColor.White;

        /// <summary>
        /// The colour to use for `Info` log entries to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public ConsoleColor InfoColour { get; set; } = ConsoleColor.Green;

        /// <summary>
        /// The colour to use for `Warning` log entries to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public ConsoleColor WarningColour { get; set; } = ConsoleColor.Yellow;

        /// <summary>
        /// The colour to use for `Error` log entries to the console. NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public ConsoleColor ErrorColour { get; set; } = ConsoleColor.Red;

        /// <summary>
        /// Set this to true (default) if you wish HoloNET to log a log file. NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public bool LogToFile { get; set; } = true;

        /// <summary>
        /// The logging path (will defualt to AppData\Roaming\NextGenSoftware\OASIS\Logs). NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public string LogPath { get; set; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\NextGenSoftware\\OASIS\\Logs";

        /// <summary>
        /// The log file name (default is OASIS.log). NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public string LogFileName { get; set; } = "OASIS.log";

        /// <summary>
        /// This is the max file size the log file can be (in bytes) before it creates a new file. The default is 1000000 bytes (1 MB).
        /// </summary>
        public int MaxLogFileSize { get; set; } = 1000000;

        /// <summary>
        /// The number of attempts to attempt to log to the file if the first attempt fails. NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public int NumberOfRetriesToLogToFile { get; set; } = 3;

        /// <summary>
        /// The amount of time to wait in seconds between each attempt to log to the file. NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public int RetryLoggingToFileEverySeconds { get; set; } = 1;

        /// <summary>
        /// Set this to true to add additional space after the end of each log entry. NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public bool InsertExtraNewLineAfterLogMessage { get; set; } = false;

        /// <summary>
        /// The amount of space to indent the log message by. NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public int IndentLogMessagesBy { get; set; } = 1;
    }

    public class EncryptionSettings
    {
        public bool BCryptEncryptionEnabled { get; set; }
        public bool Rijndael256EncryptionEnabled { get; set; }
        public string Rijndael256Key { get; set; }
        /// <summary>Enables AES-256-GCM post-quantum symmetric encryption as the outermost password layer.</summary>
        public bool QuantumEncryptionEnabled { get; set; }
        /// <summary>Passphrase used to derive the AES-256-GCM key for the quantum encryption layer. Generate a long random string.</summary>
        public string QuantumEncryptionKey { get; set; }
        /// <summary>
        /// Extra MetaData keys that must remain in plain text so they can be used in
        /// LoadHolonsByMetaData queries. Add any [CustomOASISProperty] key names or
        /// application-level MetaData keys your app queries on. The built-in system
        /// keys (CreatedByAvatarId, Active, HolonType, _versionStamp, data) are always
        /// exempt and do not need to be listed here.
        /// </summary>
        public List<string> AdditionalQueryableKeys { get; set; } = new List<string>();
    }
}
