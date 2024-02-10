using System;
using System.Collections.Generic;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public static class LoggingManager
    {
        public static LoggingFramework CurrentLoggingFramework = LoggingFramework.Default;
        //private static IOASISLogger _logger = null;

        public static Logger Logger { get; set; } = new Logger();

        public static void Init(bool logToConsole = true, bool logToFile = true, string releativePathToLogFolder = "Logs", string logFileName = "OASIS.log", int maxLogFileSize = 1000000, Logger logger = null, bool addAdditionalSpaceAfterEachLogEntry = false, bool showColouredLogs = true, ConsoleColor debugColour = ConsoleColor.White, ConsoleColor infoColour = ConsoleColor.Green, ConsoleColor warningColour = ConsoleColor.Yellow, ConsoleColor errorColour = ConsoleColor.Red)
        {
            InitLogger(logger);
            Logger.AddLogProvider(new DefaultLogProvider(logToConsole, logToFile, releativePathToLogFolder, logFileName, maxLogFileSize, addAdditionalSpaceAfterEachLogEntry, showColouredLogs, debugColour, infoColour, warningColour, errorColour));
            InitLogProvider();
        }

        public static void Init(IEnumerable<ILogProvider> logProviders, bool alsoUseDefaultLogger = false)
        {
            InitLogger();
            Logger.AddLogProviders(logProviders);

            if (alsoUseDefaultLogger)
                Logger.AddLogProvider(new DefaultLogProvider());

            InitLogProvider();
        }

        public static void Init(ILogProvider logProvider, bool alsoUseDefaultLogger = false)
        {
            InitLogger();
            Logger.AddLogProvider(logProvider);

            if (alsoUseDefaultLogger)
                Logger.AddLogProvider(new DefaultLogProvider());

            InitLogProvider();
        }

        public static void Init(Logger logger)
        {
            InitLogger(logger);
            InitLogProvider();
        }

        public static void Log(string message, LogType type)
        {
            Logger.Log(message, type);
        }

        public static void Log<T>(string message, LogType type, ref OASISResult<T> result, bool logToInnerMessages = true, bool logToMessage = true)
        {
            if (logToMessage)
                result.Message = message;

            if (logToInnerMessages)
                result.InnerMessages.Add(message);

            Log(message, type);
        }

        private static void InitLogger(Logger logger = null)
        {
            if (logger != null)
                Logger = logger;

            else if (Logger == null)
                Logger = new Logger();
        }

        private static void InitLogProvider()
        {
            if (Logger.LogProviders.Count == 0)
                Logger.AddLogProvider(new DefaultLogProvider());
        }
    }
}