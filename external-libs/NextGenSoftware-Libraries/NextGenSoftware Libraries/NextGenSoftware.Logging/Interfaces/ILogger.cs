using System;
using System.Collections.Generic;

namespace NextGenSoftware.Logging.Interfaces
{
    public interface ILogger
    {
        List<ILogProvider> LogProviders { get; set; }

        void AddLogProvider(ILogProvider logProvider);
        void AddLogProviders(IEnumerable<ILogProvider> logProviders);
        void Log(string message, LogType type, bool logToFile = true, bool logToConsole = true, bool showWorkingAnimation = false, bool noLineBreaks = false, bool insertExtraNewLineAfterLogMessage = false, int? indentLogMessagesBy = 1, bool nextMessageOnSameLine = false);
        void Log(string message, LogType type, ConsoleColor consoleColour, bool logToFile = true, bool logToConsole = true, bool showWorkingAnimation = false, bool noLineBreaks = false, bool insertExtraNewLineAfterLogMessage = false, int? indentLogMessagesBy = 1, bool nextMessageOnSameLine = false);
    }
}