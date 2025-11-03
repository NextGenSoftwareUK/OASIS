using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class TransactionHelper
    {
        public static bool CheckForTransactionErrors(ref OASISResult<ITransactionRespone> transactionResult, bool automaticallyHandleError = true, string errorMessage = "Error occured during the transaction. Reason: ", string detailedMessage = "", bool log = true, bool includeStackTrace = false, bool throwException = false, bool addToInnerMessages = false, bool incrementErrorCount = true, bool onlyLogToInnerMessages = false)
        {
            //TODO: Check that this captures all errors that can be returned.
            if (!string.IsNullOrEmpty(transactionResult.Result.TransactionResult) && !transactionResult.Result.TransactionResult.ToLower().Contains("error"))
            {
                //transactionResult.Result.IsSuccess = true;
                transactionResult.IsSaved = true;
            }
            else
                transactionResult.IsError = true;

            if (automaticallyHandleError && transactionResult.IsError)
                OASISErrorHandling.HandleError(ref transactionResult, $"{errorMessage} {transactionResult.Result.TransactionResult}");
            //OASISErrorHandling.HandleError(ref transactionResult, $"{errorMessage} {transactionResult.Result.TransactionResult}", null, true, true, onlyLogToInnerMessages, includeStackTrace, throwException, addToInnerMessages, incrementErrorCount, detailedMessage, onlyLogToInnerMessages);

            return transactionResult.IsError;
        }

        public static bool CheckForTransactionErrors(ref OASISResult<IWeb4NFTTransactionRespone> transactionResult, bool automaticallyHandleError = true, string errorMessage = "Error occured during the transaction. Reason: ", string detailedMessage = "", bool log = true, bool includeStackTrace = false, bool throwException = false, bool addToInnerMessages = false, bool incrementErrorCount = true, bool onlyLogToInnerMessages = false)
        {
            //TODO: Check that this captures all errors that can be returned.
            if (!string.IsNullOrEmpty(transactionResult.Result.TransactionResult) && !transactionResult.Result.TransactionResult.ToLower().Contains("error"))
            {
                //transactionResult.Result.IsSuccess = true;
                transactionResult.IsSaved = true;
            }
            else
                transactionResult.IsError = true;

            if (automaticallyHandleError && transactionResult.IsError)
                OASISErrorHandling.HandleError(ref transactionResult, $"{errorMessage} {transactionResult.Result.TransactionResult}");
                //OASISErrorHandling.HandleError(ref transactionResult, $"{errorMessage} {transactionResult.Result.TransactionResult}", null, true, true, onlyLogToInnerMessages, includeStackTrace, throwException, addToInnerMessages, incrementErrorCount, detailedMessage, onlyLogToInnerMessages);

            return transactionResult.IsError;
        }
    }
}