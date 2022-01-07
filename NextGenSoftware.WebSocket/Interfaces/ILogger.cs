
namespace NextGenSoftware.WebSocket
{
    public interface ILogger
    {
        void Log(string message, LogType type);
    }

    public enum LogType
    {
        Debug,
        Info,
        Warn,
        Error
    }
}