using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.Common
{
    public static class LoggingManager
    {
        public static LoggingFramework CurrentLoggingFramework = LoggingFramework.Default;
        //private static IOASISLogger _logger = null;

        public static Logger Logger { get; set; } = new Logger();

        //public static bool SupressConsoleLogging { get; set; } = false;

        public static void Init(bool logToConsole = true, bool logToFile = true, string releativePathToLogFolder = "Logs", string logFileName = "OASIS.log", int maxLogFileSize = 1000000, LoggingMode fileLoggingMode = LoggingMode.WarningsErrorsInfoAndDebug, LoggingMode consoleLoggingMode = LoggingMode.WarningsErrorsAndInfo, Logger logger = null, bool insertExtraNewLineAfterLogMessage = false, int indentLogMessagesBy = 1, bool showColouredLogs = true, ConsoleColor debugColour = ConsoleColor.White, ConsoleColor infoColour = ConsoleColor.Green, ConsoleColor warningColour = ConsoleColor.Yellow, ConsoleColor errorColour = ConsoleColor.Red)
        {
            InitLogger(logger);
            Logger.AddLogProvider(new DefaultLogProvider(logToConsole, logToFile, releativePathToLogFolder, logFileName, maxLogFileSize, insertExtraNewLineAfterLogMessage, indentLogMessagesBy, showColouredLogs, debugColour, infoColour, warningColour, errorColour));
            InitLogProvider();

            LogConfig.FileLoggingMode = fileLoggingMode;
            LogConfig.ConsoleLoggingMode = consoleLoggingMode;
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

        public static void BeginLogAction(string message, LogType logType)
        {
            Log(message, logType, true, true, true, false, false, 1, true);
        }

        public static void EndLogAction(string message, LogType logType)
        {
            Log(message, logType, true, true, false, false, false, 0);
        }

        public static void Log(string message, LogType type, bool logToFile = true, bool logToConsole = true, bool showWorkingAnimation = false, bool noLineBreaks = false, bool insertExtraNewLineAfterLogMessage = false, int? indentLogMessagesBy = 1, bool nextMessageOnSameLine = false)
        {
            Logger.Log($"{message}", type, logToFile, logToConsole, showWorkingAnimation, noLineBreaks, insertExtraNewLineAfterLogMessage, indentLogMessagesBy, nextMessageOnSameLine);
            //Console.WriteLine(message);

        //    if (nextMessageOnSameLine)
        //        Logger.Log($"{message}", type, showWorkingAnimation, noLineBreaks, insertExtraNewLineAfterLogMessage, indentLogMessagesBy, nextMessageOnSameLine);
        //    else
        //        Logger.Log($"\n {message}", type, showWorkingAnimation, noLineBreaks, insertExtraNewLineAfterLogMessage, indentLogMessagesBy, nextMessageOnSameLine);
        }

        public static void Log(string message, LogType type, ConsoleColor consoleColour, bool logToFile = true, bool logToConsole = true, bool showWorkingAnimation = false, bool noLineBreaks = false, bool insertExtraNewLineAfterLogMessage = false, int? indentLogMessagesBy = 1, bool nextMessageOnSameLine = false)
        {
            Logger.Log($"{message}", type, consoleColour, logToFile, logToConsole, showWorkingAnimation, noLineBreaks, insertExtraNewLineAfterLogMessage, indentLogMessagesBy, nextMessageOnSameLine);

            //if (nextMessageOnSameLine)
            //    Logger.Log($"{message}", type, consoleColour, showWorkingAnimation, noLineBreaks, insertExtraNewLineAfterLogMessage, indentLogMessagesBy, nextMessageOnSameLine);
            //else
            //    Logger.Log($"\n {message}", type, consoleColour, showWorkingAnimation, noLineBreaks, insertExtraNewLineAfterLogMessage, indentLogMessagesBy, nextMessageOnSameLine);
        }

        public static void Log<T>(string message, LogType type, ref OASISResult<T> result, bool logToFile = true, bool logToConsole = true, bool logToInnerMessages = true, bool logToMessage = true, bool showWorkingAnimation = false, bool noLineBreaks = false, bool insertExtraNewLineAfterLogMessage = false, int? indentLogMessagesBy = 1, bool nextMessageOnSameLine = false)
        {
            if (logToMessage)
                result.Message = message;

            if (logToInnerMessages)
                result.InnerMessages.Add(message);

            Log(message, type, logToFile, logToConsole, showWorkingAnimation, noLineBreaks, insertExtraNewLineAfterLogMessage, indentLogMessagesBy, nextMessageOnSameLine);
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