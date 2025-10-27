
namespace NextGenSoftware.Logging
{
    public abstract class LogProviderBase
    {
        protected bool ContinueFileLogging(LogType type)
        {
            if (type == LogType.Info && !(LogConfig.FileLoggingMode == LoggingMode.WarningsErrorsInfoAndDebug || LogConfig.FileLoggingMode == LoggingMode.WarningsErrorsAndInfo))
                return false;

            if (type == LogType.Debug && LogConfig.FileLoggingMode != LoggingMode.WarningsErrorsInfoAndDebug)
                return false;

            if (type == LogType.Warning && !(LogConfig.FileLoggingMode == LoggingMode.WarningsErrorsInfoAndDebug || LogConfig.FileLoggingMode == LoggingMode.WarningsErrorsAndInfo || LogConfig.FileLoggingMode == LoggingMode.WarningsAndErrors))
                return false;

            return true;
        }

        protected bool ContinueConsoleLogging(LogType type)
        {
            if (type == LogType.Info && !(LogConfig.ConsoleLoggingMode == LoggingMode.WarningsErrorsInfoAndDebug || LogConfig.ConsoleLoggingMode == LoggingMode.WarningsErrorsAndInfo))
                return false;

            if (type == LogType.Debug && LogConfig.ConsoleLoggingMode != LoggingMode.WarningsErrorsInfoAndDebug)
                return false;

            if (type == LogType.Warning && !(LogConfig.ConsoleLoggingMode == LoggingMode.WarningsErrorsInfoAndDebug || LogConfig.ConsoleLoggingMode == LoggingMode.WarningsErrorsAndInfo || LogConfig.ConsoleLoggingMode == LoggingMode.WarningsAndErrors))
                return false;

            return true;
        }
    }
}