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
        
        // HyperDrive mode switch: "Legacy" or "OASISHyperDrive2"
        public string HyperDriveMode { get; set; } = "Legacy";
        
        // Enhanced HyperDrive Configuration
        public ReplicationRulesConfig ReplicationRules { get; set; } = new ReplicationRulesConfig();
        public FailoverRulesConfig FailoverRules { get; set; } = new FailoverRulesConfig();
        public SubscriptionConfig SubscriptionConfig { get; set; } = new SubscriptionConfig();
        public DataPermissionsConfig DataPermissions { get; set; } = new DataPermissionsConfig();
        public IntelligentModeConfig IntelligentMode { get; set; } = new IntelligentModeConfig();
        
        public string OASISSystemAccountId { get; set; }
        public string OASISAPIURL { get; set; }
    }

    public class SecuritySettings
    {
        public bool HideVerificationToken { get; set; }
        public bool HideRefreshTokens { get; set; }
        public string SecretKey { get; set; }
        public int RemoveOldRefreshTokensAfterXDays { set; get; }
        public EncryptionSettings AvatarPassword { get; set; }
        public EncryptionSettings OASISProviderPrivateKeys { get; set; }
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
        public bool QuantumEncryptionEnabled { get; set; }
    }

    public class StorageProviderSettings
    {
        public int ProviderMethodCallTimeOutSeconds { get; set; } = 10;
        public int ActivateProviderTimeOutSeconds { get; set; } = 10;
        public int DectivateProviderTimeOutSeconds { get; set; } = 10;
        public bool AutoReplicationEnabled { get; set; }
        public bool AutoFailOverEnabled { get; set; }
        //public bool AutoFailOverEnabledForAvatarLogin { get; set; }
        //public bool AutoFailOverEnabledForCheckIfEmailAlreadyInUse { get; set; }
        //public bool AutoFailOverEnabledForCheckIfUsernameAlreadyInUse { get; set; }
        public bool AutoLoadBalanceEnabled { get; set; }
        public int AutoLoadBalanceReadPollIntervalMins { get; set; }
        public int AutoLoadBalanceWritePollIntervalMins { get; set; }
        public string AutoReplicationProviders { get; set; }
        public string AutoLoadBalanceProviders { get; set; }
        public string AutoFailOverProviders { get; set; }
        public string AutoFailOverProvidersForAvatarLogin { get; set; }
        public string AutoFailOverProvidersForCheckIfEmailAlreadyInUse { get; set; }
        public string AutoFailOverProvidersForCheckIfUsernameAlreadyInUse { get; set; }
        public string AutoFailOverProvidersForCheckIfOASISSystemAccountExists { get; set; }
        public string OASISProviderBootType { get; set; }
        public AzureOASISProviderSettings AzureCosmosDBOASIS { get; set; }
        public HoloOASISProviderSettings HoloOASIS { get; set; }
        public MongoDBOASISProviderSettings MongoDBOASIS { get; set; }
        public EOSIOASISProviderSettings EOSIOOASIS { get; set; }
        public TelosOASISProviderSettings TelosOASIS { get; set; }
        public SEEDSOASISProviderSettings SEEDSOASIS { get; set; }
        public ThreeFoldOASISProviderSettings ThreeFoldOASIS { get; set; }
        public EthereumOASISProviderSettings EthereumOASIS { get; set; }
        public ArbitrumOASISProviderSettings ArbitrumOASIS { get; set; }
        public RootstockOASISProviderSettings RootstockOASIS { get; set; }
        public PolygonOASISProviderSettings PolygonOASIS { get; set; }
        public SQLLiteDBOASISSettings SQLLiteDBOASIS { get; set; }
        public IPFSOASISSettings IPFSOASIS { get; set; }
        public Neo4jOASISSettings Neo4jOASIS { get; set; }
        public SolanaOASISSettings SolanaOASIS { get; set; }
        public CargoOASISSettings CargoOASIS { get; set; }
        public LocalFileOASISSettings LocalFileOASIS { get; set; }
        public PinataOASISSettings PinataOASIS { get; set; }
        
        // Missing Blockchain Providers
        public BitcoinOASISProviderSettings BitcoinOASIS { get; set; }
        public CardanoOASISProviderSettings CardanoOASIS { get; set; }
        public PolkadotOASISProviderSettings PolkadotOASIS { get; set; }
        public BNBChainOASISProviderSettings BNBChainOASIS { get; set; }
        public FantomOASISProviderSettings FantomOASIS { get; set; }
        public OptimismOASISProviderSettings OptimismOASIS { get; set; }
        public ChainLinkOASISProviderSettings ChainLinkOASIS { get; set; }
        public ElrondOASISProviderSettings ElrondOASIS { get; set; }
        public AptosOASISProviderSettings AptosOASIS { get; set; }
        public TRONOASISProviderSettings TRONOASIS { get; set; }
        public HashgraphOASISProviderSettings HashgraphOASIS { get; set; }
        public AvalancheOASISProviderSettings AvalancheOASIS { get; set; }
        public CosmosBlockChainOASISProviderSettings CosmosBlockChainOASIS { get; set; }
        public NEAROASISProviderSettings NEAROASIS { get; set; }
        public BaseOASISProviderSettings BaseOASIS { get; set; }
        public SuiOASISProviderSettings SuiOASIS { get; set; }
        public MoralisOASISProviderSettings MoralisOASIS { get; set; }
        
        // Network Providers
        public ActivityPubOASISProviderSettings ActivityPubOASIS { get; set; }
        public GoogleCloudOASISProviderSettings GoogleCloudOASIS { get; set; }
    }

    public class EmailSettings
    {
        public string EmailFrom { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
        public bool DisableAllEmails { get; set; } //This overrides the SendVerificationEmail setting below. MAKE SURE THIS IS FALSE FOR LIVE!
        public bool SendVerificationEmail { get; set; }
        public string OASISWebSiteURL { get; set; }
    }

    public class ProviderSettingsBase
    {
        public string ConnectionString { get; set; }
    }

    public class PinataOASISSettings : ProviderSettingsBase
    {
        public string ConnectionString { get; set; }
    }

    public class CargoOASISSettings : ProviderSettingsBase
    {
        public string SingingMessage { get; set; }
        public string PrivateKey { get; set; }
        public string HostUrl { get; set; }
    }

    public class SolanaOASISSettings : ProviderSettingsBase
    {
        public string WalletMnemonicWords { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
    }

    //public class HoloOASISProviderSettings : ProviderSettingsBase
    public class HoloOASISProviderSettings
    {
        //public HolochainVersion HolochainVersion { get; set; }
        //public string HolochainVersion { get; set; }
        public bool UseLocalNode { get; set; }
        public bool UseHoloNetwork { get; set; }
        public string HoloNetworkURI { get; set; }
        public string LocalNodeURI {  get; set; }
        public bool HoloNETORMUseReflection { get; set; }
    }

    public class MongoDBOASISProviderSettings : ProviderSettingsBase
    {
        public string DBName { get; set; }
    }

    public class EOSIOASISProviderSettings : ProviderSettingsBase
    {
        public string AccountName { get; set; }
        public string AccountPrivateKey { get; set; }
        public string ChainId { get; set; }
    }


    public class SEEDSOASISProviderSettings : ProviderSettingsBase
    {
    }

    public class ThreeFoldOASISProviderSettings : ProviderSettingsBase
    {

    }

    public class EthereumOASISProviderSettings : ProviderSettingsBase
    {
        public string ChainPrivateKey { get; set; }
        public long ChainId { get; set; }
        public string ContractAddress { get; set; }
    }

    public class ArbitrumOASISProviderSettings : ProviderSettingsBase
    {
        public string ChainPrivateKey { get; set; }
        public long ChainId { get; set; }
        public string ContractAddress { get; set; }
    }

    public class PolygonOASISProviderSettings : ProviderSettingsBase
    {
        public string ChainPrivateKey { get; set; }
        public string ContractAddress { get; set; }
        public string Abi { get; set; }
    }

    public class RootstockOASISProviderSettings : ProviderSettingsBase
    {
        public string ChainPrivateKey { get; set; }
        public string ContractAddress { get; set; }
        public string Abi { get; set; }
    }

    public class SQLLiteDBOASISSettings : ProviderSettingsBase
    {
    }

    public class IPFSOASISSettings : ProviderSettingsBase
    {
        public string LookUpIPFSAddress { get; set; }
    }

    public class Neo4jOASISSettings : ProviderSettingsBase
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LocalFileOASISSettings
    {
        public string FilePath { get; set; }
    }

    public class AzureOASISProviderSettings
    {
        public string ServiceEndpoint { get; set; }
        public string AuthKey { get; set; }
        public string DBName { get; set; }
        public string CollectionNames { get; set; }
    }

    // Enhanced HyperDrive Configuration Classes
    public class ReplicationRulesConfig
    {
        public string Mode { get; set; } = "Auto";
        public bool IsEnabled { get; set; } = true;
        public int MaxReplicationsPerMonth { get; set; } = 1000;
        public decimal CostThreshold { get; set; } = 10.00m;
        public bool FreeProvidersOnly { get; set; } = true;
        public decimal GasFeeThreshold { get; set; } = 0.01m;
        public List<ReplicationTriggerConfig> ReplicationTriggers { get; set; } = new List<ReplicationTriggerConfig>();
        public List<ProviderReplicationRuleConfig> ProviderRules { get; set; } = new List<ProviderReplicationRuleConfig>();
        public List<DataTypeReplicationRuleConfig> DataTypeRules { get; set; } = new List<DataTypeReplicationRuleConfig>();
        public List<ScheduleRuleConfig> ScheduleRules { get; set; } = new List<ScheduleRuleConfig>();
        public CostOptimizationRuleConfig CostOptimization { get; set; } = new CostOptimizationRuleConfig();
        public IntelligentSelectionRuleConfig IntelligentSelection { get; set; } = new IntelligentSelectionRuleConfig();
    }

    public class FailoverRulesConfig
    {
        public string Mode { get; set; } = "Auto";
        public bool IsEnabled { get; set; } = true;
        public int MaxFailoversPerMonth { get; set; } = 100;
        public decimal CostThreshold { get; set; } = 5.00m;
        public bool FreeProvidersOnly { get; set; } = true;
        public decimal GasFeeThreshold { get; set; } = 0.01m;
        public List<FailoverTriggerConfig> FailoverTriggers { get; set; } = new List<FailoverTriggerConfig>();
        public List<ProviderFailoverRuleConfig> ProviderRules { get; set; } = new List<ProviderFailoverRuleConfig>();
        public IntelligentSelectionRuleConfig IntelligentSelection { get; set; } = new IntelligentSelectionRuleConfig();
        public List<EscalationRuleConfig> EscalationRules { get; set; } = new List<EscalationRuleConfig>();
    }

    public class SubscriptionConfig
    {
        public string PlanType { get; set; } = "Free";
        public int MaxReplicationsPerMonth { get; set; } = 100;
        public int MaxFailoversPerMonth { get; set; } = 10;
        public int MaxStorageGB { get; set; } = 1;
        public bool PayAsYouGoEnabled { get; set; } = false;
        public decimal CostPerReplication { get; set; } = 0.01m;
        public decimal CostPerFailover { get; set; } = 0.05m;
        public decimal CostPerGB { get; set; } = 0.10m;
        public string Currency { get; set; } = "USD";
        public string BillingCycle { get; set; } = "Monthly";
        public List<UsageAlertConfig> UsageAlerts { get; set; } = new List<UsageAlertConfig>();
        public List<QuotaNotificationConfig> QuotaNotifications { get; set; } = new List<QuotaNotificationConfig>();
    }

    public class DataPermissionsConfig
    {
        public AvatarPermissionsConfig AvatarPermissions { get; set; } = new AvatarPermissionsConfig();
        public HolonPermissionsConfig HolonPermissions { get; set; } = new HolonPermissionsConfig();
        public ProviderPermissionsConfig ProviderPermissions { get; set; } = new ProviderPermissionsConfig();
        public FieldLevelPermissionsConfig FieldLevelPermissions { get; set; } = new FieldLevelPermissionsConfig();
        public AccessControlConfig AccessControl { get; set; } = new AccessControlConfig();
    }

    public class IntelligentModeConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool AutoOptimization { get; set; } = true;
        public bool CostAwareness { get; set; } = true;
        public bool PerformanceOptimization { get; set; } = true;
        public bool SecurityOptimization { get; set; } = true;
        public bool LearningEnabled { get; set; } = true;
        public string AdaptationSpeed { get; set; } = "Medium";
        public List<OptimizationGoalConfig> OptimizationGoals { get; set; } = new List<OptimizationGoalConfig>();
    }

    // Supporting configuration classes
    public class ReplicationTriggerConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ReplicationConditionConfig Condition { get; set; }
        public string Priority { get; set; } = "Medium";
        public bool IsEnabled { get; set; } = true;
        public ReplicationActionConfig Action { get; set; }
    }

    public class ReplicationConditionConfig
    {
        public string Type { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
        public string Field { get; set; }
        public string ProviderType { get; set; }
        public TimeWindowConfig TimeWindow { get; set; }
    }

    public class ReplicationActionConfig
    {
        public string Type { get; set; }
        public List<string> TargetProviders { get; set; } = new List<string>();
        public List<string> DataTypes { get; set; } = new List<string>();
        public DataPermissionsConfig Permissions { get; set; }
        public decimal CostLimit { get; set; }
        public ScheduleConfig Schedule { get; set; }
    }

    public class ProviderReplicationRuleConfig
    {
        public string ProviderType { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; } = 1;
        public decimal CostLimit { get; set; }
        public decimal GasFeeLimit { get; set; }
        public List<string> DataTypes { get; set; } = new List<string>();
        public DataPermissionsConfig Permissions { get; set; }
        public List<ReplicationConditionConfig> Conditions { get; set; } = new List<ReplicationConditionConfig>();
        public ScheduleConfig Schedule { get; set; }
    }

    public class DataTypeReplicationRuleConfig
    {
        public string DataType { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<string> RequiredProviders { get; set; } = new List<string>();
        public List<string> OptionalProviders { get; set; } = new List<string>();
        public DataPermissionsConfig Permissions { get; set; }
        public decimal CostLimit { get; set; }
        public ScheduleConfig Schedule { get; set; }
    }

    public class ScheduleRuleConfig
    {
        public string Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public ScheduleConfig Schedule { get; set; }
        public List<string> DataTypes { get; set; } = new List<string>();
        public List<string> Providers { get; set; } = new List<string>();
        public DataPermissionsConfig Permissions { get; set; }
    }

    public class CostOptimizationRuleConfig
    {
        public bool IsEnabled { get; set; } = true;
        public decimal MaxCostPerReplication { get; set; } = 0.01m;
        public decimal MaxCostPerMonth { get; set; } = 10.00m;
        public List<string> PreferredFreeProviders { get; set; } = new List<string>();
        public bool AvoidHighGasProviders { get; set; } = true;
        public decimal GasFeeThreshold { get; set; } = 0.01m;
        public decimal CostAlertThreshold { get; set; } = 5.00m;
    }

    public class IntelligentSelectionRuleConfig
    {
        public bool IsEnabled { get; set; } = true;
        public string Algorithm { get; set; } = "Intelligent";
        public SelectionWeightsConfig Weights { get; set; } = new SelectionWeightsConfig();
        public bool LearningEnabled { get; set; } = true;
        public string AdaptationSpeed { get; set; } = "Medium";
        public List<OptimizationGoalConfig> OptimizationGoals { get; set; } = new List<OptimizationGoalConfig>();
    }

    public class FailoverTriggerConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public FailoverConditionConfig Condition { get; set; }
        public string Priority { get; set; } = "Medium";
        public bool IsEnabled { get; set; } = true;
        public FailoverActionConfig Action { get; set; }
    }

    public class FailoverConditionConfig
    {
        public string Type { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
        public string ProviderType { get; set; }
        public TimeWindowConfig TimeWindow { get; set; }
        public decimal? Threshold { get; set; }
    }

    public class FailoverActionConfig
    {
        public string Type { get; set; }
        public string TargetProvider { get; set; }
        public List<string> FallbackProviders { get; set; } = new List<string>();
        public decimal CostLimit { get; set; }
        public ScheduleConfig Schedule { get; set; }
    }

    public class ProviderFailoverRuleConfig
    {
        public string ProviderType { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; } = 1;
        public decimal CostLimit { get; set; }
        public decimal GasFeeLimit { get; set; }
        public List<FailoverConditionConfig> Conditions { get; set; } = new List<FailoverConditionConfig>();
        public List<string> FallbackProviders { get; set; } = new List<string>();
    }

    public class EscalationRuleConfig
    {
        public string Level { get; set; } = "Medium";
        public FailoverConditionConfig Condition { get; set; }
        public FailoverActionConfig Action { get; set; }
        public NotificationRuleConfig Notification { get; set; }
    }

    public class AvatarPermissionsConfig
    {
        public bool IsEnabled { get; set; } = true;
        public List<AvatarFieldPermissionConfig> Fields { get; set; } = new List<AvatarFieldPermissionConfig>();
        public string DefaultPermission { get; set; } = "Read";
        public Dictionary<string, List<AvatarFieldPermissionConfig>> ProviderOverrides { get; set; } = new Dictionary<string, List<AvatarFieldPermissionConfig>>();
    }

    public class AvatarFieldPermissionConfig
    {
        public string FieldName { get; set; }
        public string Permission { get; set; } = "Read";
        public bool IsEncrypted { get; set; } = false;
        public bool IsRequired { get; set; } = false;
        public List<string> ProviderTypes { get; set; } = new List<string>();
    }

    public class HolonPermissionsConfig
    {
        public bool IsEnabled { get; set; } = true;
        public List<HolonTypePermissionConfig> HolonTypes { get; set; } = new List<HolonTypePermissionConfig>();
        public string DefaultPermission { get; set; } = "Read";
        public Dictionary<string, List<HolonTypePermissionConfig>> ProviderOverrides { get; set; } = new Dictionary<string, List<HolonTypePermissionConfig>>();
    }

    public class HolonTypePermissionConfig
    {
        public string HolonType { get; set; }
        public string Permission { get; set; } = "Read";
        public bool IsEncrypted { get; set; } = false;
        public bool IsRequired { get; set; } = false;
        public List<string> ProviderTypes { get; set; } = new List<string>();
        public List<HolonFieldPermissionConfig> Fields { get; set; } = new List<HolonFieldPermissionConfig>();
    }

    public class HolonFieldPermissionConfig
    {
        public string FieldName { get; set; }
        public string Permission { get; set; } = "Read";
        public bool IsEncrypted { get; set; } = false;
        public bool IsRequired { get; set; } = false;
    }

    public class ProviderPermissionsConfig
    {
        public bool IsEnabled { get; set; } = true;
        public List<ProviderPermissionConfig> Providers { get; set; } = new List<ProviderPermissionConfig>();
    }

    public class ProviderPermissionConfig
    {
        public string ProviderType { get; set; }
        public string Permission { get; set; } = "Read";
        public List<string> AllowedDataTypes { get; set; } = new List<string>();
        public decimal CostLimit { get; set; }
        public decimal GasFeeLimit { get; set; }
        public ScheduleConfig Schedule { get; set; }
    }

    public class FieldLevelPermissionsConfig
    {
        public bool IsEnabled { get; set; } = true;
        public List<FieldPermissionRuleConfig> Rules { get; set; } = new List<FieldPermissionRuleConfig>();
    }

    public class FieldPermissionRuleConfig
    {
        public string FieldPath { get; set; }
        public string DataType { get; set; }
        public Dictionary<string, string> Permissions { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, bool> Encryption { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> Required { get; set; } = new Dictionary<string, bool>();
    }

    public class AccessControlConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool AuthenticationRequired { get; set; } = true;
        public string AuthorizationLevel { get; set; } = "Authenticated";
        public string EncryptionLevel { get; set; } = "Standard";
        public bool AuditLogging { get; set; } = true;
        public List<AccessPolicyConfig> AccessPolicies { get; set; } = new List<AccessPolicyConfig>();
    }

    public class AccessPolicyConfig
    {
        public string Name { get; set; }
        public AccessConditionConfig Condition { get; set; }
        public string Permissions { get; set; } = "Read";
        public List<string> Providers { get; set; } = new List<string>();
        public List<string> DataTypes { get; set; } = new List<string>();
    }

    public class AccessConditionConfig
    {
        public string UserRole { get; set; }
        public string SubscriptionPlan { get; set; }
        public TimeWindowConfig TimeWindow { get; set; }
        public string Location { get; set; }
        public string DeviceType { get; set; }
    }

    public class UsageAlertConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Threshold { get; set; }
        public string ThresholdType { get; set; } = "Percentage";
        public List<string> NotificationChannels { get; set; } = new List<string>();
        public bool IsEnabled { get; set; } = true;
    }

    public class QuotaNotificationConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string QuotaType { get; set; }
        public decimal Threshold { get; set; }
        public List<string> NotificationChannels { get; set; } = new List<string>();
        public List<QuotaActionConfig> Actions { get; set; } = new List<QuotaActionConfig>();
        public bool IsEnabled { get; set; } = true;
    }

    public class QuotaActionConfig
    {
        public string Type { get; set; }
        public object Value { get; set; }
        public ScheduleConfig Schedule { get; set; }
    }

    public class NotificationRuleConfig
    {
        public List<string> Channels { get; set; } = new List<string>();
        public string Message { get; set; }
        public string Priority { get; set; } = "Medium";
        public bool IsEnabled { get; set; } = true;
    }

    public class ScheduleConfig
    {
        public string Type { get; set; } = "Immediate";
        public int? Interval { get; set; }
        public string IntervalUnit { get; set; } = "Hours";
        public string CronExpression { get; set; }
        public string TimeZone { get; set; } = "UTC";
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public List<string> DaysOfWeek { get; set; } = new List<string>();
        public List<int> DaysOfMonth { get; set; } = new List<int>();
    }

    public class TimeWindowConfig
    {
        public string Start { get; set; }
        public string End { get; set; }
        public string TimeZone { get; set; } = "UTC";
        public List<string> DaysOfWeek { get; set; } = new List<string>();
    }

    public class SelectionWeightsConfig
    {
        public decimal Cost { get; set; } = 0.3m;
        public decimal Performance { get; set; } = 0.3m;
        public decimal Reliability { get; set; } = 0.2m;
        public decimal Security { get; set; } = 0.1m;
        public decimal Geographic { get; set; } = 0.05m;
        public decimal Availability { get; set; } = 0.05m;
    }

    public class OptimizationGoalConfig
    {
        public string Type { get; set; }
        public decimal Weight { get; set; }
        public decimal Target { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    // Missing Blockchain Provider Settings Classes
    public class BitcoinOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://blockstream.info/api";
        public string Network { get; set; } = "mainnet";
    }

    public class CardanoOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://cardano-mainnet.blockfrost.io/api/v0";
        public string NetworkId { get; set; } = "mainnet";
        public string ProjectId { get; set; }
    }

    public class PolkadotOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "wss://rpc.polkadot.io";
        public string Network { get; set; } = "polkadot";
    }

    public class BNBChainOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://bsc-dataseed.binance.org";
        public string NetworkId { get; set; } = "56";
        public string ChainId { get; set; } = "0x38";
    }

    public class FantomOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://rpc.ftm.tools";
        public string NetworkId { get; set; } = "250";
        public string ChainId { get; set; } = "0xfa";
    }

    public class OptimismOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://mainnet.optimism.io";
        public string NetworkId { get; set; } = "10";
        public string ChainId { get; set; } = "0xa";
    }

    public class ChainLinkOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://mainnet.infura.io/v3/YOUR_PROJECT_ID";
        public string NetworkId { get; set; } = "1";
        public string ChainId { get; set; } = "0x1";
    }

    public class ElrondOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://api.elrond.com";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "1";
    }

    public class AptosOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://api.mainnet.aptoslabs.com/v1";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "1";
    }

    public class TRONOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://api.trongrid.io";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "0x2b6653dc";
    }

    public class HashgraphOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://mainnet-public.mirrornode.hedera.com/api/v1";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "295";
    }

    public class AvalancheOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://api.avax.network/ext/bc/C/rpc";
        public string NetworkId { get; set; } = "43114";
        public string ChainId { get; set; } = "0xa86a";
    }

    public class CosmosBlockChainOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://cosmos-rpc.polkachu.com";
        public string Network { get; set; } = "cosmos";
        public string ChainId { get; set; } = "cosmoshub-4";
    }

    public class NEAROASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://rpc.mainnet.near.org";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "mainnet";
    }

    public class BaseOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://mainnet.base.org";
        public string NetworkId { get; set; } = "8453";
        public string ChainId { get; set; } = "0x2105";
    }

    public class SuiOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://fullnode.mainnet.sui.io:443";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "mainnet";
    }

    public class MoralisOASISProviderSettings : ProviderSettingsBase
    {
        public string ApiKey { get; set; }
        public string RpcEndpoint { get; set; } = "https://speedy-nodes-nyc.moralis.io/YOUR_API_KEY/eth/mainnet";
        public string Network { get; set; } = "mainnet";
    }

    public class TelosOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://api.telos.net";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "4667b205c6838ef70ff7988f6e8257e8be0e1284a2f59699054a018f743b1d11";
    }

    public class ActivityPubOASISProviderSettings : ProviderSettingsBase
    {
        public string BaseUrl { get; set; } = "https://mastodon.social/api/v1";
        public string UserAgent { get; set; } = "OASIS-ActivityPub-Provider/1.0";
        public string AcceptHeader { get; set; } = "application/json";
        public int TimeoutSeconds { get; set; } = 30;
        public bool EnableCaching { get; set; } = true;
        public int CacheExpirationMinutes { get; set; } = 15;
    }

    public class GoogleCloudOASISProviderSettings : ProviderSettingsBase
    {
        public string ProjectId { get; set; } = "oasis-project";
        public string BucketName { get; set; } = "oasis-storage";
        public string CredentialsPath { get; set; }
        public string FirestoreDatabaseId { get; set; } = "(default)";
        public string BigQueryDatasetId { get; set; } = "oasis_data";
        public bool EnableStorage { get; set; } = true;
        public bool EnableFirestore { get; set; } = true;
        public bool EnableBigQuery { get; set; } = true;
    }
}
