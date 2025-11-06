using System;

namespace NextGenSoftware.Logging.Interfaces
{
    public interface IDefaultLogProvider
    {
        int IndentLogMessagesBy { get; set; }
        bool InsertExtraNewLineAfterLogMessage { get; set; }
        string LogDirectory { get; set; }
        string LogFileName { get; set; }
        bool LogToConsole { get; set; }
        bool LogToFile { get; set; }
        int NumberOfRetriesToLogToFile { get; set; }
        int RetryLoggingToFileEverySeconds { get; set; }

        event DefaultLogProvider.Error OnError;

        void Log(string message, LogType type, bool showWorkingAnimation = false, bool logOnNewLine = true, bool insertExtraNewLineAfterLogMessage = false, int? indentLogMessagesBy = 1);
        void Log(string message, LogType type, ConsoleColor consoleColour, bool showWorkingAnimation = false, bool logOnNewLine = true, bool insertExtraNewLineAfterLogMessage = false, int? indentLogMessagesBy = 1);
        void Shutdown();
    }
}