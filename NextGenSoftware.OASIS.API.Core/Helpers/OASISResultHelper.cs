using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class OASISResultHelper
    {
        public static string BuildInnerMessageError(List<string> innerMessages)
        {
            string result = "";
            foreach (string innerMessage in innerMessages)
                result = string.Concat(result, innerMessage, "\n\n");

            return result;
        }
    }

    //public static class OASISResultHelper<T>
    //{
    //    public static OASISResult<T> CopyResult(OASISResult<T> fromResult, ref OASISResult<T> toResult)
    //    {
    //        toResult.Exception = fromResult.Exception;
    //        toResult.IsError = fromResult.IsError;
    //        toResult.IsSaved = fromResult.IsSaved;
    //        toResult.IsWarning = fromResult.IsWarning;
    //        toResult.Message = fromResult.Message;
    //        toResult.MetaData = fromResult.MetaData;

    //        return toResult;
    //    }

    //    public static OASISResult<T> CopyResult(OASISResult<T> fromResult, OASISResult<T> toResult)
    //    {
    //        return CopyResult(fromResult, ref toResult);
    //    }
    //}

    public static class OASISResultHolonToHolonHelper<T1, T2> 
        where T1 : IHolonBase
        where T2 : IHolonBase //, new()
    {
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsWarning = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;
           // toResult.Result = fromResult.Result;

            return toResult;
        }

        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult)
        {
            return CopyResult(fromResult, ref toResult);
        }
    }

    public static class OASISResultCollectionToCollectionHelper<T1, T2>
       where T1 : IEnumerable<IHolonBase>
       where T2 : IEnumerable<IHolonBase> 
    {
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsSaved = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;

            return toResult;
        }

        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult)
        {
            return CopyResult(fromResult, ref toResult);
        }
    }

    public static class OASISResultCollectionToHolonHelper<T1, T2>
       where T1 : IEnumerable<IHolonBase>
       where T2 : IHolonBase
    {
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsSaved = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;

            return toResult;
        }

        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult)
        {
            return CopyResult(fromResult, ref toResult);
        }
    }

    public static class OASISResultHolonToCollectionHelper<T1, T2>
       where T1 : IHolonBase
       where T2 : IEnumerable<IHolonBase> 
    {
        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsSaved = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;

            return toResult;
        }

        public static OASISResult<T2> CopyResult(OASISResult<T1> fromResult, OASISResult<T2> toResult)
        {
            return CopyResult(fromResult, ref toResult);
        }
    }
}