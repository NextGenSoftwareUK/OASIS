
//using NextGenSoftware.Logging;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Logging.NLog;

//namespace NextGenSoftware.OASIS.API.Core.Managers
//{
//    public static class LoggingManager
//    {
//        public static LoggingFramework CurrentLoggingFramework = LoggingFramework.NLog;
//        private static IOASISLogger _logger = null;

//        public static void Log(string message, LogType type)
//        {
//            if (_logger == null)
//            {
//                switch (CurrentLoggingFramework)
//                {
//                    case LoggingFramework.NLog:
//                        _logger = new NLogger();
//                        break;
//                }
//            }

//            _logger.Log(message, type);
//        }

//        public static void Log<T>(string message, LogType type, ref OASISResult<T> result, bool logToInnerMessages = true, bool logToMessage = true)
//        {
//            if (logToMessage)
//                result.Message = message;

//            if (logToInnerMessages)
//                result.InnerMessages.Add(message);

//            Log(message, type);
//        }
//    }
//}
