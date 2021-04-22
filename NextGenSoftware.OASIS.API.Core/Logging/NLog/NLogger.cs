
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NLog;

namespace NextGenSoftware.OASIS.API.Core.Logging.NLog
{
    public class NLogger : IOASISLogger
    {
        public void Log(string message, LogType type)
        {
            var logger = LogManager.GetCurrentClassLogger();
            //var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
            //NLog.LogManager.Shutdown();
            //logger.ConditionalDebug()
            //logger.Log()

            switch (type)
            {
                case LogType.Debug:
                    logger.Debug(message);
                    break;

                case LogType.Error:
                    logger.Error(message);
                    break;

                case LogType.Info:
                    logger.Info(message);
                    break;

                case LogType.Warn:
                    logger.Warn(message);
                    break;
            }  
        }
    }
}
