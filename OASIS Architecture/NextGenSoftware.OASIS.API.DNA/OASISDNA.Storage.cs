using System;
using System.Collections.Generic;
using NextGenSoftware.ErrorHandling;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Configuration;

namespace NextGenSoftware.OASIS.API.DNA
{
    public class StorageProviderSettings
    {
        //public bool LogSwitchingProvidersToConsole { get; set; } = true;
        //public bool LogSwitchingProvidersToFile { get; set; } = true;
        public bool LogSwitchingProviders { get; set; } = true;
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
        /// <summary>When true, <see cref="AutoFailOverLocalProviders"/> is used by native/offline-first hosts to walk local-capable storage providers (e.g. SQLite, MongoDB, LocalFile, HoloOASIS) without treating remote APIs as the next hop.</summary>
        public bool AutoFailOverLocalProvidersEnabled { get; set; }
        /// <summary>Comma-separated <see cref="NextGenSoftware.OASIS.API.Core.Enums.ProviderType"/> names tried in order when switching to offline-first / local storage failover (HyperDrive native path).</summary>
        public string AutoFailOverLocalProviders { get; set; }
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
        public ArweaveOASISSettings ArweaveOASIS { get; set; }

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
        public AztecOASISProviderSettings AztecOASIS { get; set; }

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
        public string ResendKey { get; set; }
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

    public class ArweaveOASISSettings : ProviderSettingsBase
    {
        // ConnectionString format: "wallet=/path/to/wallet.json&gateway=https://arweave.net"
        // Or with embedded JWK:   "walletjson=<base64>&gateway=https://arweave.net"
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
        
        // Rust DNA Template Configuration (moved from STARDNA). Paths use forward slashes for cross-platform; .NET Path.Combine normalizes.
        public string STARBasePath { get; set; } = ""; // Base path for STAR/Rust templates. Blank = resolve at runtime (e.g. same folder as app); then Rust paths below are relative to this or absolute.
        public string RustDNARSMTemplateFolder { get; set; } = "DNATemplates/RustDNATemplates/RSM";  // Rust DNA Templates that hAPPs are built from (relative to STARBasePath).
        public string RustTemplateLib { get; set; } = "core/lib.rs"; // relative to RustDNARSMTemplateFolder above.
        public string RustTemplateHolon { get; set; } = "core/holon.rs"; // relative to RustDNARSMTemplateFolder above.
        public string RustTemplateValidation { get; set; } = "core/validation.rs"; // relative to RustDNARSMTemplateFolder above.
        public string RustTemplateCreate { get; set; } = "crud/create.rs"; // relative to RustDNARSMTemplateFolder above.
        public string RustTemplateRead { get; set; } = "crud/read.rs";  // relative to RustDNARSMTemplateFolder above.
        public string RustTemplateUpdate { get; set; } = "crud/update.rs"; // relative to RustDNARSMTemplateFolder above.
        public string RustTemplateDelete { get; set; } = "crud/delete.rs"; // relative to RustDNARSMTemplateFolder above.
        public string RustTemplateList { get; set; } = "crud/list.rs"; // relative to RustDNARSMTemplateFolder above.
        public string RustTemplateInt { get; set; } = "types/int.rs"; // relative to RustDNARSMTemplateFolder above.
        public string RustTemplateString { get; set; } = "types/string.rs"; // relative to RustDNARSMTemplateFolder above.
        public string RustTemplateBool { get; set; } = "types/bool.rs"; // relative to RustDNARSMTemplateFolder above.
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
        /// <summary>Real, configurable per-month cap for general (non-replication, non-failover) requests, used by OASISHyperDrive.GetQuotaLimit's "Requests" operation type - previously hardcoded to a fixed 1000 with no DNA-configurable field to back it.</summary>
        public int MaxRequestsPerMonth { get; set; } = 1000;
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

    /// <summary>
    /// Stripe API keys used for subscription billing.
    /// Environment variables (STRIPE_SECRET_KEY, STRIPE_PUBLISHABLE_KEY, STRIPE_WEBHOOK_SECRET) always win.
    /// These fields act as local-dev fallbacks only — never commit real keys here.
    /// </summary>
    public class StripeSettings
    {
        public string SecretKey { get; set; } = "";
        public string PublishableKey { get; set; } = "";
        public string WebhookSecret { get; set; } = "";
        /// <summary>
        /// Stripe Price IDs for each plan. Set these in Railway env vars (STRIPE_PRICE_BRONZE etc.)
        /// or here for local dev. Get the IDs from Stripe Dashboard → Products → your plan → Price ID.
        /// </summary>
        public string PriceBronze { get; set; } = "";
        public string PriceSilver { get; set; } = "";
        public string PriceGold { get; set; } = "";
        public string PriceEnterprise { get; set; } = "";
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
        public string Name { get; set; }
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

}
