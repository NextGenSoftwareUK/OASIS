using System;
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
}
