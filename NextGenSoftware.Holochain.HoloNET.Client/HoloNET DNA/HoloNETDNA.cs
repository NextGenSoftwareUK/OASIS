
using System;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.Logging;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public class HoloNETDNA : IHoloNETDNA
    {
        /// <summary>
        /// The Holochain Conductor config path. This defaults to AppData\Roaming\holochain\holochain\config\conductor-config.yml (this is where the conductor will as default install the config file).
        /// </summary>
        public string HolochainConductorConfigPath { get; set; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\holochain\\holochain\\config\\conductor-config.yml";

        /// <summary>
        /// The default URI for the Admin port on the Holochain Conductor. Defaults to ws://localhost:7777.
        /// </summary>
        public string HolochainConductorAdminURI { get; set; } = "ws://localhost:7777";

        /// <summary>
        /// The default URI for the AppAgent port on the Holochain Conductor. Defaults to ws://localhost:8888.
        /// </summary>
        public string HolochainConductorAppAgentURI { get; set; } = "ws://localhost:8888";

        /// <summary>
        /// The AgentPubKey to use for Zome calls. If this is not set then HoloNET will automatically retrieve this along with the DnaHash after it connects (if the Connect method defaults are not overriden).
        /// </summary>
        public string AgentPubKey { get; set; } = "";

        /// <summary>
        /// The DnaHash to use for Zome calls. If this is not set then HoloNET will automatically retrieve this along with the AgentPubKey after it connects (if the Connect method defaults are not overriden).
        /// </summary>
        public string DnaHash { get; set; } = "";

        /// <summary>
        /// THe CellId to use for Zome calls (this contains the DnaHash and AgentPubKey). If this is not set then HoloNET will automatically retrieve this along with the AgentPubKey after it connects (if the Connect method defaults are not overriden).
        /// </summary>
        public byte[][] CellId { get; set; }

        /// <summary>
        /// The Installed AppId that the AppAgent WebSocket will connect to (optional). If this is blank then it will be a generic App WebSocket and can connect to any hApp.
        /// </summary>
        public string InstalledAppId { get; set; }

        /// <summary>
        /// The full path to the root of the hApp that HoloNET will start the Holochain Conductor (currenty uses hc.exe) with and then make zome calls to.
        /// </summary>
        public string FullPathToRootHappFolder { get; set; }

        /// <summary>
        /// The full path to the compiled hApp that HoloNET will start the Holochain Conductor (currenty uses hc.exe) with and then make zome calls to.
        /// </summary>
        public string FullPathToCompiledHappFolder { get; set; }

        /// <summary>
        /// Tells HoloNET how to auto-start the Holochain Conductor. See the documentation at https://github.com/holochain-open-dev/holochain-client-csharp for more info.
        /// </summary>
        public HolochainConductorModeEnum HolochainConductorMode { get; set; } = HolochainConductorModeEnum.UseSystemGlobal;

        /// <summary>
        /// This is the Holochain Conductor to use for the auto-start Holochain Conductor feature. It can be either `Holochain` or `Hc`.
        /// </summary>
        public string FullPathToExternalHolochainConductorBinary { get; set; } //= "HolochainBinaries\\holochain.exe";

        /// <summary>
        /// The full path to the Holochain Conductor exe (holochain.exe) that HoloNET will auto-start if `HolochainConductorToUse` is set to `Holochain`.
        /// </summary>
        public string FullPathToExternalHCToolBinary { get; set; } //= "HolochainBinaries\\hc.exe";

        //public string FullPathToHolochainAppDNA { get; set; }

        /// <summary>
        /// The seconds to wait for the Holochain Conductor to start before attempting to connect to it.
        /// </summary>
        public int SecondsToWaitForHolochainConductorToStart { get; set; } = 5;

        /// <summary>
        /// Set this to true if you with HoloNET to auto-start the Holochain Conductor defined in the `FullPathToExternalHolochainConductorBinary parameter if `HolochainConductorToUse` is `Holochain`, otherwise if it`s `Hc` then it will use `FullPathToExternalHCToolBinary`. Default is true.
        /// </summary>
        public bool AutoStartHolochainConductor { get; set; } = true;

        /// <summary>
        /// Set this to true if you wish HoloNET to show the Holochain Conductor window whilst it is starting it (will be left open until the conductor is automatically shutdown again when HoloNET disconects if `AutoShutdownHolochainConductor` is true.)
        /// </summary>
        public bool ShowHolochainConductorWindow { get; set; } = false;

        /// <summary>
        /// Set this to true if you wish HoloNET to auto-shutdown the Holochain Conductor after it disconnects. Default is true.
        /// </summary>
        public bool AutoShutdownHolochainConductor { get; set; } = true;

        /// <summary>
        /// Set this to true if you wish HoloNET to auto-shutdown ALL Holochain Conductors after it disconnects. Default is false. Set this to true if you wish to make sure there are none left running to prevent memory leaks. You can also of course manually call the ShutDownAllConductors if you wish.
        /// </summary>
        public bool ShutDownALLHolochainConductors { get; set; } = false;

        /// <summary>
        /// This is the Holochain Conductor to use for the auto-start Holochain Conductor feature. It can be either `HolochainProductionConductor` or `HcDevTool`.
        /// </summary>
        public HolochainConductorEnum HolochainConductorToUse { get; set; } = HolochainConductorEnum.HolochainProductionConductor;

        /// <summary>
        /// Set this to true if you wish HoloNET to allow only ONE Holochain Conductor to run at a time. The default is false.
        /// </summary>
        public bool OnlyAllowOneHolochainConductorToRunAtATime { get; set; } = false;

        /// <summary>
        /// Set whether you wish to ignore, warn or error when the response id received does not have a matching request id. If it is Ignore it will do nothing, if it is Warn it will give a warning in the Message property of responses returned but still continue, if it is Error it will give a message in the Message property, set the IsError flag and abort the request.
        /// </summary>
        public EnforceRequestToResponseIdMatchingBehaviour EnforceRequestToResponseIdMatchingBehaviour { get; set; } = EnforceRequestToResponseIdMatchingBehaviour.Error;

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
        /// This is the max file size the log file can be (in bytes) before it creates a new file. The default is 1000000 bytes (1 MB).
        /// </summary>
        public int MaxLogFileSize { get; set; } = 1000000;

        /// <summary>
        /// The logging path (will defualt to AppData\Roaming\NextGenSoftware\HoloNET\Logs). NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public string LogPath { get; set; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\NextGenSoftware\\HoloNET\\Logs";

        /// <summary>
        /// The log file name (default is HoloNET.log). NOTE: This is only relevant if the built-in DefaultLogger is used.
        /// </summary>
        public string LogFileName { get; set; } = "HoloNET.log";

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

        /// <summary>
        /// An enum that specifies what to do when anm error occurs. The options are: `AlwaysThrowExceptionOnError`, `OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent` & `NeverThrowExceptions`). The default is `OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent` meaning it will only throw an error if the `OnError` event has not been subscribed to. This delegates error handling to the caller. If no event has been subscribed then HoloNETClient will throw an error. `AlwaysThrowExceptionOnError` will always throw an error even if the `OnError` event has been subscribed to. The `NeverThrowException` enum option will never throw an error even if the `OnError` event has not been subscribed to. Regardless of what enum is selected, the error will always be logged using whatever ILogger`s have been injected into the constructor or set on the static Logging.Loggers property.
        /// </summary>
        public ErrorHandlingBehaviour ErrorHandlingBehaviour { get; set; } = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent;

        //34 settings :)

        //TODO: Possibly add these as defaults (used for the Connect methods).
        //retrieveAgentPubKeyAndDnaHashMode
        //retrieveAgentPubKeyAndDnaHashFromConductor
        //retrieveAgentPubKeyAndDnaHashFromSandbox
        //automaticallyAttemptToRetrieveFromConductorIfSandBoxFails
        //automaticallyAttemptToRetrieveFromSandBoxIfConductorFails
        //updateIHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved

        //with these 40 settings! :)
    }
}