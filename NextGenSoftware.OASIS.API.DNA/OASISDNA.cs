
namespace NextGenSoftware.OASIS.API.DNA
{
    public class OASISDNA
    {
        public OASIS OASIS { get; set; }
    }

    public class OASIS
    {
        public string CurrentLiveVersion { get; set; }
        public string CurrentStagingVersion { get; set; }
        public string OASISVersion { get; set; }
        public string Terms { get; set; }
        public Logging Logging { get; set; }
        public ErrorHandlingSettings ErrorHandling { get; set; }
        public SecuritySettings Security { get; set; }
        public EmailSettings Email { get; set; }
        public StorageProviderSettings StorageProviders { get; set; }
    }

    public class SecuritySettings
    {
        public bool HideVerificationToken { get; set; }  
        public bool HideRefreshTokens { get; set; }
        public string SecretKey { get; set; }
        public int RemoveOldRefreshTokensAfterXDays{ set; get;}
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
    }

    public class Logging
    {
        public string LoggingFramework { get; set; }
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
        public bool AutoReplicationEnabled { get; set; }
        public bool AutoFailOverEnabled { get; set; }
        public bool AutoLoadBalanceEnabled { get; set; }
        public int AutoLoadBalanceReadPollIntervalMins { get; set; }
        public int AutoLoadBalanceWritePollIntervalMins { get; set; }
        public string AutoReplicationProviders { get; set; }
        public string AutoFailOverProviders { get; set; }
        public string AutoLoadBalanceProviders { get; set; }
        public string OASISProviderBootType { get; set; }
        public AzureOASISProviderSettings AzureCosmosDBOASIS { get; set; }
        public HoloOASISProviderSettings HoloOASIS { get; set; }
        public MongoDBOASISProviderSettings MongoDBOASIS { get; set; }
        public EOSIOASISProviderSettings EOSIOOASIS { get; set; }
        public TelosOASISProviderSettings TelosOASIS { get; set; }
        public SEEDSOASISProviderSettings SEEDSOASIS { get; set; }
        public ThreeFoldOASISProviderSettings ThreeFoldOASIS { get; set; }
        public EthereumOASISProviderSettings EthereumOASIS { get; set; }
        public SQLLiteDBOASISSettings SQLLiteDBOASIS { get; set; }
        public IPFSOASISSettings IPFSOASIS { get; set; }
        public Neo4jOASISSettings Neo4jOASIS { get; set; }
        public SolanaOASISSettings SolanaOASIS { get; set; }
        public CargoOASISSettings CargoOASIS { get; set; }
        public LocalFileOASISSettings LocalFileOASIS { get; set; }
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
    
    public class CargoOASISSettings : ProviderSettingsBase
    {
        public string SingingMessage { get; set; }
        public string PrivateKey { get; set; }
        public string HostUrl { get; set; }
    }
    
    public class SolanaOASISSettings : ProviderSettingsBase
    {
        public string WalletMnemonicWords { get; set; }
    }

    public class HoloOASISProviderSettings : ProviderSettingsBase
    {
        //public HolochainVersion HolochainVersion { get; set; }
        public string HolochainVersion { get; set; }
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

    public class TelosOASISProviderSettings : ProviderSettingsBase
    {
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
}
