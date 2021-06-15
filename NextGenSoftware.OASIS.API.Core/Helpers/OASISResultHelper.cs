using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class OASISResultHolonToHolonHelper<T1, T2> 
        where T1 : IHolon
        where T2 : IHolon //, new()
    {
        public static void CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            //toResult.Result = Mapper<T1, T2>.MapBaseHolonProperties(fromResult.Result);
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsSaved = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;
        }
    }

    public static class OASISResultCollectionToCollectionHelper<T1, T2>
       where T1 : IEnumerable<IHolon>
       where T2 : IEnumerable<IHolon> //, new()
    {
        public static void CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            //toResult.Result = Mapper<T1, T2>.MapBaseHolonProperties(fromResult.Result); //TODO: Be ideal to get this working! :)
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsSaved = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;
        }
    }

    public static class OASISResultCollectionToHolonHelper<T1, T2>
       where T1 : IEnumerable<IHolon>
       where T2 : IHolon //, new()
    {
        public static void CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            //toResult.Result = Mapper<T1, T2>.MapBaseHolonProperties(fromResult.Result); //TODO: Be ideal to get this working! :)
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsSaved = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;
        }
    }

    public static class OASISResultHolonToCollectionHelper<T1, T2>
       where T1 : IHolon
       where T2 : IEnumerable<IHolon> //, new()
    {
        public static void CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            //toResult.Result = Mapper<T1, T2>.MapBaseHolonProperties(fromResult.Result); //TODO: Be ideal to get this working! :)
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsSaved = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;
        }
    }
}