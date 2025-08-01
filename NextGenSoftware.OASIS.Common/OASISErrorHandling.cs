using System.ComponentModel.Design;
using NextGenSoftware.ErrorHandling;
using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.Common
{
    public static class OASISErrorHandling
    {
        //These are global overrides to the method calls below.
        public static bool ShowStackTrace { get; set; } = false; //This should ONLY be used in DEBUG/DEV mode.
        public static bool ThrowExceptionsOnErrors { get; set; } = false; //This should ONLY be used in DEBUG/DEV mode. Even if this is true it will only throw an exception if ErrorHandlingBehaviour is set to 'AlwaysThrowExceptionOnError' or if it is 'OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent' and OnError has not been subscribed to.  
        public static bool ThrowExceptionsOnWarnings { get; set; } = false; //This should ONLY be used in DEBUG/DEV mode. Even if this is true it will only throw an exception if WarningHandlingBehaviour is set to 'AlwaysThrowExceptionOnWarning' or if it is 'OnlyThrowExceptionIfNoWarningHandlerSubscribedToOnWarningEvent' and OnWarning has not been subscribed to.  
        public static bool LogAllErrors { get; set; } = true;
        public static bool LogAllWarnings { get; set; } = true;
        //public static ErrorHandlingBehaviour ErrorHandlingBehaviour { get; set; } = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent;
        //public static WarningHandlingBehaviour WarningHandlingBehaviour { get; set; } = WarningHandlingBehaviour.OnlyThrowExceptionIfNoWarningHandlerSubscribedToOnWarningEvent;

        public static ErrorHandlingBehaviour ErrorHandlingBehaviour
        {
            get
            {
                return ErrorHandling.ErrorHandling.ErrorHandlingBehaviour;
            }
            set
            {
                ErrorHandling.ErrorHandling.ErrorHandlingBehaviour = value;
            }
        }

        public static WarningHandlingBehaviour WarningHandlingBehaviour
        {
            get
            {
                return ErrorHandling.ErrorHandling.WarningHandlingBehaviour;
            }
            set
            {
                ErrorHandling.ErrorHandling.WarningHandlingBehaviour = value;
            }
        }

        public delegate void Error(object sender, OASISErrorEventArgs e);
        public static event Error OnError;

        public delegate void Warning(object sender, OASISWarningEventArgs e);
        public static event Warning OnWarning;

        //WARNING: ONLY set includeStackTrace to true for debug/dev mode due to performance overhead. This param should never be needed because the ShowStackTrace flag will be used for Dev/Debug mode. 

        public static void HandleError<T1, T2>(ref OASISResult<T1> result, string errorMessage, bool logToFile = true, bool logToConsole = true, bool includeStackTrace = false, bool throwException = false, bool addToInnerMessages = false, bool incrementErrorCount = true, Exception ex = null, string detailedMessage = "", bool onlyLogToInnerMessages = false, OASISResult<T2> innerResult = null)
        {
            if (innerResult != null)
            {
                result.InnerMessages.AddRange(innerResult.InnerMessages);
                result.IsWarning = innerResult.IsWarning;
                result.WarningCount += innerResult.WarningCount;
            }

            HandleError(ref result, errorMessage, ex, logToFile, logToConsole, includeStackTrace, throwException, addToInnerMessages, incrementErrorCount, detailedMessage, onlyLogToInnerMessages);
        }


        public static void HandleError(string errorMessage, Exception ex = null, bool logToFile = true, bool logToConsole = true, bool includeStackTrace = false, bool throwException = false)
        {
            //NOTE: If you are throwing an exception then you do not need to show an additional stack trace here because the exception has it already! ;-)
            if (includeStackTrace || ShowStackTrace)
                errorMessage = string.Concat(errorMessage, "StackTrace:\n", Environment.StackTrace);

            if (logToFile || logToConsole || LogAllErrors)
                LoggingManager.Log($"\n{errorMessage}", LogType.Error);

            OnError?.Invoke(null, new OASISErrorEventArgs { Reason = errorMessage, Exception = ex });

            switch (ErrorHandlingBehaviour)
            {
                case ErrorHandlingBehaviour.AlwaysThrowExceptionOnError:
                    {
                        if (throwException || ThrowExceptionsOnErrors)
                            throw new Exception(errorMessage, ex);
                    }
                    break;

                case ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent:
                    {
                        if (OnError == null)
                        {
                            if (throwException || ThrowExceptionsOnErrors)
                                throw new Exception(errorMessage, ex);
                        }
                    }
                    break;
            }
        }

        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, Exception ex = null, bool logToFile = true, bool logToConsole = true, bool includeStackTrace = false, bool throwException = false, bool addToInnerMessages = false, bool incrementErrorCount = true, string detailedMessage = "", bool onlyLogToInnerMessages = false)
        {
            ////NOTE: If you are throwing an exception then you do not need to show an additional stack trace here because the exception has it already! ;-)
            //if (includeStackTrace || ShowStackTrace)
            //    errorMessage = string.Concat(errorMessage, "StackTrace:\n", Environment.StackTrace);

            HandleError(errorMessage, ex, logToFile, logToConsole, includeStackTrace, throwException);

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


            //if (log || LogAllErrors)
            //    LoggingManager.Log(errorMessage, LogType.Error);

            //OnError?.Invoke(null, new OASISErrorEventArgs { Reason = errorMessage, Exception = ex });

            //switch (ErrorHandlingBehaviour)
            //{
            //    case ErrorHandlingBehaviour.AlwaysThrowExceptionOnError:
            //        {
            //            if (throwException || ThrowExceptionsOnErrors)
            //                throw new Exception(errorMessage, ex);
            //        }
            //        break;

            //    case ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent:
            //        {
            //            if (OnError == null)
            //            {
            //                if (throwException || ThrowExceptionsOnErrors)
            //                    throw new Exception(errorMessage, ex);
            //            }
            //        }
            //        break;
            //}
        }


        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, bool onlyLogToInnerMessages)
        {
            HandleError(ref result, errorMessage, null, true, true, false, false, false, true, "", onlyLogToInnerMessages);
        }

        public static void HandleError<T1, T2>(ref OASISResult<T1> result, string errorMessage, bool onlyLogToInnerMessages, OASISResult<T2> innerResult)
        {
            HandleError(ref result, errorMessage, true, true, false, false, false, true, null, "", onlyLogToInnerMessages, innerResult);
        }

        //public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, Exception ex, bool onlyLogToInnerMessages = false)
        //{
        //    HandleError(ref result, errorMessage, ex, true, false, false, false, true, "", onlyLogToInnerMessages);
        //}

        public static void HandleError<T1, T2>(ref OASISResult<T1> result, string errorMessage, Exception ex, bool onlyLogToInnerMessages = false, OASISResult<T2> innerResult = null)
        {
            HandleError(ref result, errorMessage, true, true, false, false, false, true, ex, "", onlyLogToInnerMessages, innerResult);
        }

        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, string detailedMessage, bool onlyLogToInnerMessages = false)
        {
            HandleError(ref result, errorMessage, null, true, true, false, false, false, true, detailedMessage, onlyLogToInnerMessages);
        }

        public static void HandleError<T1, T2>(ref OASISResult<T1> result, string errorMessage, string detailedMessage, OASISResult<T2> innerResult = null)
        {
            HandleError(ref result, errorMessage, true, true, false, false, false, true, null, detailedMessage, false, innerResult);
        }

        public static void HandleError<T1, T2>(ref OASISResult<T1> result, string errorMessage, string detailedMessage, bool onlyLogToInnerMessages = false, OASISResult<T2> innerResult = null)
        {
            HandleError(ref result, errorMessage, true, true, false, false, false, true, null, detailedMessage, onlyLogToInnerMessages, innerResult);
        }

        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, string detailedMessage, Exception ex, bool onlyLogToInnerMessages = false)
        {
            HandleError(ref result, errorMessage, ex, true, true, false, false, false, true, detailedMessage, onlyLogToInnerMessages);
        }

        public static void HandleError<T1, T2>(ref OASISResult<T1> result, string errorMessage, string detailedMessage, Exception ex, bool onlyLogToInnerMessages = false, OASISResult<T2> innerResult = null)
        {
            HandleError(ref result, errorMessage, true, true, false, false, false, true, ex, detailedMessage, onlyLogToInnerMessages, innerResult);
        }

        //public static bool CheckForTransactionErrors(ref OASISResult<ITransactionRespone> transactionResult, bool automaticallyHandleError = true, string errorMessage = "Error occured during the transaction. Reason: ", string detailedMessage = "", bool log = true, bool includeStackTrace = false, bool throwException = false, bool addToInnerMessages = false, bool incrementErrorCount = true, bool onlyLogToInnerMessages = false)
        //{
        //    //TODO: Check that this captures all errors that can be returned.
        //    if (!string.IsNullOrEmpty(transactionResult.Result.TransactionResult) && !transactionResult.Result.TransactionResult.ToLower().Contains("error"))
        //    {
        //        //transactionResult.Result.IsSuccess = true;
        //        transactionResult.IsSaved = true;
        //    }
        //    else
        //        transactionResult.IsError = true;

        //    if (automaticallyHandleError && transactionResult.IsError)
        //        HandleError(ref transactionResult, $"{errorMessage} {transactionResult.Result.TransactionResult}", onlyLogToInnerMessages, includeStackTrace, throwException, addToInnerMessages, incrementErrorCount, null, detailedMessage, onlyLogToInnerMessages);

        //    return transactionResult.IsError;
        //}

        //public static bool CheckForTransactionErrors(ref OASISResult<INFTTransactionRespone> transactionResult, bool automaticallyHandleError = true, string errorMessage = "Error occured during the transaction. Reason: ", string detailedMessage = "", bool log = true, bool includeStackTrace = false, bool throwException = false, bool addToInnerMessages = false, bool incrementErrorCount = true, bool onlyLogToInnerMessages = false)
        //{
        //    //TODO: Check that this captures all errors that can be returned.
        //    if (!string.IsNullOrEmpty(transactionResult.Result.TransactionResult) && !transactionResult.Result.TransactionResult.ToLower().Contains("error"))
        //    {
        //        //transactionResult.Result.IsSuccess = true;
        //        transactionResult.IsSaved = true;
        //    }
        //    else
        //        transactionResult.IsError = true;

        //    if (automaticallyHandleError && transactionResult.IsError)
        //        HandleError(ref transactionResult, $"{errorMessage} {transactionResult.Result.TransactionResult}", onlyLogToInnerMessages, includeStackTrace, throwException, addToInnerMessages, incrementErrorCount, null, detailedMessage, onlyLogToInnerMessages);

        //    return transactionResult.IsError;
        //}

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
                LoggingManager.Log($"\n{message}", LogType.Warning);

            OnWarning?.Invoke(null, new OASISWarningEventArgs { Reason = message, Exception = ex });

            switch (WarningHandlingBehaviour)
            {
                case WarningHandlingBehaviour.AlwaysThrowExceptionOnWarning:
                    {
                        if (throwException || ThrowExceptionsOnWarnings)
                            throw new Exception(message, ex);
                    }
                    break;

                case WarningHandlingBehaviour.OnlyThrowExceptionIfNoWarningHandlerSubscribedToOnWarningEvent:
                    {
                        if (OnError == null)
                        {
                            if (throwException || ThrowExceptionsOnWarnings)
                                throw new Exception(message, ex);
                        }
                    }
                    break;
            }
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

        public static string ProcessMessage<T>(OASISResult<T> result, string noResultErrorMessage = "No result returned.", string successMessage = "Operation Successful")
        {
            if (result != null)
            {
                if (!string.IsNullOrEmpty(result.Message))
                    return result.Message;

                else if (result.Result == null)
                    return noResultErrorMessage;

                else if (!result.IsError)
                    return successMessage;
                else
                    return "Unknown error occured";
            }
            else
                return "Unknown error occured";
        }
    }
}