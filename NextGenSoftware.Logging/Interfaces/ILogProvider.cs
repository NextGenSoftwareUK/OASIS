
using System;

namespace NextGenSoftware.Logging
{
    public interface ILogProvider
    {
        void Log(string message, LogType type, bool showWorkingAnimation = false, bool logOnNewLine = true, bool insertExtraNewLineAfterLogMessage = false, int ? indentLogMessagesBy = 1, bool nextMessageOnSameLine = false);
        void Log(string message, LogType type, ConsoleColor consoleColour, bool showWorkingAnimation = false, bool logOnNewLine = true, bool insertExtraNewLineAfterLogMessage = false, int? indentLogMessagesBy = 1, bool nextMessageOnSameLine = false);
        void Shutdown();
    }
}