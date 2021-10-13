
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Logging.NLog;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public static class LoggingManager
    {
        public static LoggingFramework CurrentLoggingFramework = LoggingFramework.NLog;
        private static IOASISLogger _logger = null;

        public static void Log(string message, LogType type)
        {
            if (_logger == null)
            {
                switch (CurrentLoggingFramework)
                {
                    case LoggingFramework.NLog:
                        _logger = new NLogger();
                        break;
                }
            }

            _logger.Log(message, type);
        }
    }
}
