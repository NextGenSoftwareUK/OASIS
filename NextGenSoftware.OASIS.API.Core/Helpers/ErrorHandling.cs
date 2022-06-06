using System;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class ErrorHandling
    {
        //These are global overrides to the method calls below.
        //TODO: Need to add these to the OASISDNA so can be confgiured in the config file as well as code.
        public static bool ShowStackTrace { get; set; } = false; //This should ONLY be used in DEBUG/DEV mode.
        public static bool ThrowExceptionsOnErrors { get; set; } = false; //This should ONLY be used in DEBUG/DEV mode.
        public static bool ThrowExceptionsOnWarnings { get; set; } = false; //This should ONLY be used in DEBUG/DEV mode.
        public static bool LogAllErrors { get; set; } = true;
        public static bool LogAllWarnings { get; set; } = true;

        //WARNING: ONLY set includeStackTrace to true for debug/dev mode due to performance overhead. This param should never be needed because the ShowStackTrace flag will be used for Dev/Debug mode. 

        public static void HandleError<T1, T2>(ref OASISResult<T1> result, string errorMessage, bool log = true, bool includeStackTrace = false, bool throwException = false, bool addToInnerMessages = false, bool incrementErrorCount = true, Exception ex = null, string detailedMessage = "", bool onlyLogToInnerMessages = false, OASISResult<T2> innerResult = null)
        {
            if (innerResult != null)
            {
                result.InnerMessages.AddRange(innerResult.InnerMessages);
                result.IsWarning = innerResult.IsWarning;
                result.WarningCount += innerResult.WarningCount;
            }

            HandleError(ref result, errorMessage, log, includeStackTrace, throwException, addToInnerMessages, incrementErrorCount, ex, detailedMessage, onlyLogToInnerMessages);
        }


        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, bool log = true, bool includeStackTrace = false, bool throwException = false, bool addToInnerMessages = false, bool incrementErrorCount = true, Exception ex = null, string detailedMessage = "", bool onlyLogToInnerMessages = false)
        {
            //NOTE: If you are throwing an exception then you do not need to show an additional stack trace here because the exception has it already! ;-)
            if (includeStackTrace || ShowStackTrace)
                errorMessage = string.Concat(errorMessage, "StackTrace:\n", Environment.StackTrace);

            result.IsSaved = false;
            result.IsLoaded = false;
            result.IsError = true;

            if (!onlyLogToInnerMessages)
            {
                result.Message = errorMessage;

                if (!string.IsNullOrEmpty(detailedMessage))
                    result.DetailedMessage = detailedMessage;
            }

            if (ex != null)
                result.Exception = ex;

            if (addToInnerMessages)
            {
                if (!string.IsNullOrEmpty(detailedMessage))
                    result.InnerMessages.Add($"{errorMessage}\n\nDetails:\n{detailedMessage}");
                else
                    result.InnerMessages.Add(errorMessage);
            }

            if (ex != null)
                result.Exception = ex;

            if (incrementErrorCount)
                result.ErrorCount++;

            if (log || LogAllErrors)
                LoggingManager.Log(errorMessage, Enums.LogType.Error);

            if (throwException || ThrowExceptionsOnErrors)
                throw new Exception(errorMessage, ex);
        }

        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, bool onlyLogToInnerMessages)
        {
            HandleError(ref result, errorMessage, true, false, false, false, true, null, "", onlyLogToInnerMessages);
        }

        public static void HandleError<T1, T2>(ref OASISResult<T1> result, string errorMessage, bool onlyLogToInnerMessages, OASISResult<T2> innerResult)
        {
            HandleError(ref result, errorMessage, true, false, false, false, true, null, "", onlyLogToInnerMessages, innerResult);
        }

        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, Exception ex, bool onlyLogToInnerMessages = false)
        {
            HandleError(ref result, errorMessage, true, false, false, false, true, ex, "", onlyLogToInnerMessages);
        }

        public static void HandleError<T1, T2>(ref OASISResult<T1> result, string errorMessage, Exception ex, bool onlyLogToInnerMessages = false, OASISResult<T2> innerResult = null)
        {
            HandleError(ref result, errorMessage, true, false, false, false, true, ex, "", onlyLogToInnerMessages, innerResult);
        }

        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, string detailedMessage, bool onlyLogToInnerMessages = false)
        {
            HandleError(ref result, errorMessage, true, false, false, false, true, null, detailedMessage, onlyLogToInnerMessages);
        }

        public static void HandleError<T1, T2>(ref OASISResult<T1> result, string errorMessage, string detailedMessage, OASISResult<T2> innerResult = null)
        {
            HandleError(ref result, errorMessage, true, false, false, false, true, null, detailedMessage, false, innerResult);
        }

        public static void HandleError<T1, T2>(ref OASISResult<T1> result, string errorMessage, string detailedMessage, bool onlyLogToInnerMessages = false, OASISResult<T2> innerResult = null)
        {
            HandleError(ref result, errorMessage, true, false, false, false, true, null, detailedMessage, onlyLogToInnerMessages, innerResult);
        }

        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, string detailedMessage, Exception ex, bool onlyLogToInnerMessages = false)
        {
            HandleError(ref result, errorMessage, true, false, false, false, true, ex, detailedMessage, onlyLogToInnerMessages);
        }

        public static void HandleError<T1, T2>(ref OASISResult<T1> result, string errorMessage, string detailedMessage, Exception ex, bool onlyLogToInnerMessages = false, OASISResult<T2> innerResult = null)
        {
            HandleError(ref result, errorMessage, true, false, false, false, true, ex, detailedMessage, onlyLogToInnerMessages, innerResult);
        }

        public static void HandleWarning<T>(ref OASISResult<T> result, string message, bool log = true, bool includeStackTrace = false, bool throwException = false, bool addToInnerMessages = true, bool incrementWarningCount = true, Exception ex = null, string detailedMessage = "", bool onlyLogToInnerMessages = false)
        {
            //NOTE: If you are throwing an exception then you do not need to show an additional stack trace here because the exception has it already! ;-)
            if (includeStackTrace || ShowStackTrace)
                message = string.Concat(message, "StackTrace:\n", Environment.StackTrace);

            result.IsWarning = true;

            if (!onlyLogToInnerMessages)
            {
                result.Message = message;

                if (!string.IsNullOrEmpty(detailedMessage))
                    result.DetailedMessage = detailedMessage;
            }

            if (ex != null)
                result.Exception = ex;

            if (addToInnerMessages)
            {
                if (!string.IsNullOrEmpty(detailedMessage))
                    result.StackTraces.Add(detailedMessage); 
                    //result.InnerMessages.Add($"{message}\n\nDetails:\n{detailedMessage}");
                else
                    result.InnerMessages.Add(message);
            }

            if (incrementWarningCount)
                result.WarningCount++;

            if (log || LogAllWarnings)
                LoggingManager.Log(message, Enums.LogType.Warn);

            if (throwException || ThrowExceptionsOnWarnings)
                throw new Exception(message, ex);
        }


        public static void HandleWarning<T1, T2>(ref OASISResult<T1> result, string message, bool log = true, bool includeStackTrace = false, bool throwException = false, bool addToInnerMessages = true, bool incrementWarningCount = true, Exception ex = null, string detailedMessage = "", bool onlyLogToInnerMessages = false, OASISResult<T2> innerResult = null)
        {
            if (innerResult != null)
            {
                result.InnerMessages.AddRange(innerResult.InnerMessages);
                result.IsWarning = innerResult.IsWarning;
                result.WarningCount += innerResult.WarningCount;
            }

            HandleWarning(ref result, message, log, includeStackTrace, throwException, addToInnerMessages, incrementWarningCount, ex, detailedMessage, onlyLogToInnerMessages);
        }

        public static void HandleWarning<T>(ref OASISResult<T> result, string message, bool onlyLogToInnerMessages)
        {
            HandleWarning(ref result, message, true, false, false, true, true, null, "", onlyLogToInnerMessages);
        }

        public static void HandleWarning<T1, T2>(ref OASISResult<T1> result, string message, bool onlyLogToInnerMessages, OASISResult<T2> innerResult)
        {
            HandleWarning(ref result, message, true, false, false, true, true, null, "", onlyLogToInnerMessages, innerResult);
        }

        public static void HandleWarning<T>(ref OASISResult<T> result, string message, Exception ex, bool onlyLogToInnerMessages = false)
        {
            HandleWarning(ref result, message, true, false, false, true, true, ex, "", onlyLogToInnerMessages);
        }

        public static void HandleWarning<T1, T2>(ref OASISResult<T1> result, string message, Exception ex, bool onlyLogToInnerMessages = false, OASISResult<T2> innerResult = null)
        {
            HandleWarning(ref result, message, true, false, false, true, true, ex, "", onlyLogToInnerMessages, innerResult);
        }

        public static void HandleWarning<T>(ref OASISResult<T> result, string errorMessage, string detailedMessage, bool onlyLogToInnerMessages = false)
        {
            HandleWarning(ref result, errorMessage, true, false, false, true, true, null, detailedMessage, onlyLogToInnerMessages);
        }

        public static void HandleWarning<T1, T2>(ref OASISResult<T1> result, string errorMessage, string detailedMessage, OASISResult<T2> innerResult = null)
        {
            HandleWarning(ref result, errorMessage, true, false, false, true, true, null, detailedMessage, false, innerResult);
        }

        public static void HandleWarning<T1, T2>(ref OASISResult<T1> result, string errorMessage, string detailedMessage, bool onlyLogToInnerMessages = false, OASISResult<T2> innerResult = null)
        {
            HandleWarning(ref result, errorMessage, true, false, false, true, true, null, detailedMessage, onlyLogToInnerMessages, innerResult);
        }

        public static void HandleWarning<T>(ref OASISResult<T> result, string errorMessage, string detailedMessage, Exception ex, bool onlyLogToInnerMessages = false)
        {
            HandleWarning(ref result, errorMessage, true, false, false, true, true, ex, detailedMessage, onlyLogToInnerMessages);
        }

        public static void HandleWarning<T1, T2>(ref OASISResult<T1> result, string errorMessage, string detailedMessage, Exception ex, bool onlyLogToInnerMessages = false, OASISResult<T2> innerResult = null)
        {
            HandleWarning(ref result, errorMessage, true, false, false, true, true, ex, detailedMessage, onlyLogToInnerMessages, innerResult);
        }
    }
}