
namespace NextGenSoftware.ErrorHandling
{
    public static class ErrorHandling
    {
       // public delegate void Error(object sender, ErrorEventArgs e);
       // public static event Error OnError;

        public static ErrorHandlingBehaviour ErrorHandlingBehaviour { get; set; } = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent;
        public static WarningHandlingBehaviour WarningHandlingBehaviour { get; set; } = WarningHandlingBehaviour.OnlyThrowExceptionIfNoWarningHandlerSubscribedToOnWarningEvent;

        //public static LoggingFramework CurrentLoggingFramework = LoggingFramework.Default;

        //TODO: Try to get this working later, problem is when we add logging we get circular refrences so need to find a workaround for that! ;-)
        //public static Logger Logger { get; set; } = new Logger();

        //public static void Init(bool logToConsole = true, bool logToFile = true, string releativePathToLogFolder = "Logs", string logFileName = "ErrorHandling.log", Logger logger = null, bool addAdditionalSpaceAfterEachLogEntry = false, bool showColouredLogs = true, ConsoleColor debugColour = ConsoleColor.White, ConsoleColor infoColour = ConsoleColor.Green, ConsoleColor warningColour = ConsoleColor.Yellow, ConsoleColor errorColour = ConsoleColor.Red)
        //{
        //    InitLogger(logger);
        //    Logger.AddLogProvider(new DefaultLogProvider(logToConsole, logToFile, releativePathToLogFolder, logFileName, addAdditionalSpaceAfterEachLogEntry, showColouredLogs, debugColour, infoColour, warningColour, errorColour));
        //    InitLogProvider();
        //}

        //public static void Init(IEnumerable<ILogProvider> logProviders, bool alsoUseDefaultLogger = false)
        //{
        //    InitLogger();
        //    Logger.AddLogProviders(logProviders);

        //    if (alsoUseDefaultLogger)
        //        Logger.AddLogProvider(new DefaultLogProvider());

        //    InitLogProvider();
        //}

        //public static void Init(ILogProvider logProvider, bool alsoUseDefaultLogger = false)
        //{
        //    InitLogger();
        //    Logger.AddLogProvider(logProvider);

        //    if (alsoUseDefaultLogger)
        //        Logger.AddLogProvider(new DefaultLogProvider());

        //    InitLogProvider();
        //}

        //public static void Init(Logger logger)
        //{
        //    InitLogger(logger);
        //    InitLogProvider();
        //}

        //public static void HandleError<T1, T2>(string message, Exception exceptionRaised, Exception exceptionToThrow) 
        //    where T1 : ErrorEventArgs, new()
        //    where T2 : Exception, new()
        //{
        //    message = string.Concat(message, "\nError Details: ", exceptionRaised != null ? exceptionRaised.ToString() : "");
        //    Logger.Log(message, LogType.Error);

        //    OnError?.Invoke(null, new T1 { Reason = message, ErrorDetails = exceptionRaised });

        //    switch (ErrorHandlingBehaviour)
        //    {
        //        case ErrorHandlingBehaviour.AlwaysThrowExceptionOnError:
        //            throw new Exception(message, exceptionRaised);

        //        case ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent:
        //            {
        //                if (OnError == null)
        //                    throw new Exception(message, exceptionRaised);
        //            }
        //            break;
        //    }
        //}

        //TODO: Try to make this generic later on...
        //public static void HandleError(string message, Exception exceptionRaised)
        //{
        //    message = string.Concat(message, "\nError Details: ", exceptionRaised != null ? exceptionRaised.ToString() : "");
        //    Logger.Log(message, LogType.Error);

        //    OnError?.Invoke(null, new ErrorEventArgs { Reason = message, ErrorDetails = exceptionRaised });

        //    switch (ErrorHandlingBehaviour)
        //    {
        //        case ErrorHandlingBehaviour.AlwaysThrowExceptionOnError:
        //            throw new Exception(message, exceptionRaised);

        //        case ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent:
        //            {
        //                if (OnError == null)
        //                    throw new Exception(message, exceptionRaised);
        //            }
        //            break;
        //    }
        //}

        //private static void InitLogger(Logger logger = null)
        //{
        //    if (logger != null)
        //        Logger = logger;

        //    else if (Logger == null)
        //        Logger = new Logger();
        //}

        //private static void InitLogProvider()
        //{
        //    if (Logger.LogProviders.Count == 0)
        //        Logger.AddLogProvider(new DefaultLogProvider());
        //}
    }
}