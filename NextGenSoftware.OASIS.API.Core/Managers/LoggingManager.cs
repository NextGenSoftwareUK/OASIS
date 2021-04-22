
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Logging.NLog;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public static class LoggingManager
    {
        public static LoggingFramework CurrentLoggingFramework = LoggingFramework.NLog;

        public static void Log(string message, LogType type)
        {
            IOASISLogger logger = null;

            switch (CurrentLoggingFramework)
            {
                case LoggingFramework.NLog:
                    logger = new NLogger();
                    break;
            }

            logger.Log(message, type);
        }
    }
}
