using System;
using NextGenSoftware.Logging;

namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IHoloNETDNA
    {
        bool InsertExtraNewLineAfterLogMessage { get; set; }
        int IndentLogMessagesBy { get; set; } 
        string AgentPubKey { get; set; }
        bool AutoShutdownHolochainConductor { get; set; }
        bool AutoStartHolochainConductor { get; set; }
        byte[][] CellId { get; set; }
        ConsoleColor DebugColour { get; set; }
        string DnaHash { get; set; }
        EnforceRequestToResponseIdMatchingBehaviour EnforceRequestToResponseIdMatchingBehaviour { get; set; }
        ConsoleColor ErrorColour { get; set; }
        ErrorHandlingBehaviour ErrorHandlingBehaviour { get; set; }
        string FullPathToCompiledHappFolder { get; set; }
        string FullPathToExternalHCToolBinary { get; set; }
        string FullPathToExternalHolochainConductorBinary { get; set; }
        string FullPathToRootHappFolder { get; set; }
        string HolochainConductorAdminURI { get; set; }
        string HolochainConductorAppAgentURI { get; set; }
        string HolochainConductorConfigPath { get; set; }
        HolochainConductorModeEnum HolochainConductorMode { get; set; }
        HolochainConductorEnum HolochainConductorToUse { get; set; }
        ConsoleColor InfoColour { get; set; }
        string InstalledAppId { get; set; }
        string LogFileName { get; set; }
        LoggingMode FileLoggingMode { get; set; }
        LoggingMode ConsoleLoggingMode { get; set; }
        string LogPath { get; set; }
        bool LogToConsole { get; set; }
        bool LogToFile { get; set; }
        int MaxLogFileSize { get; set; }
        int NumberOfRetriesToLogToFile { get; set; }
        bool OnlyAllowOneHolochainConductorToRunAtATime { get; set; }
        int RetryLoggingToFileEverySeconds { get; set; }
        int SecondsToWaitForHolochainConductorToStart { get; set; }
        bool ShowColouredLogs { get; set; }
        bool ShowHolochainConductorWindow { get; set; }
        bool ShutDownALLHolochainConductors { get; set; }
        ConsoleColor WarningColour { get; set; }
    }
}