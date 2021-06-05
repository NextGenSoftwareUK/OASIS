
namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class OASISResultHelper<T1, T2>
    {
        public static void CopyResult(OASISResult<T1> fromResult, ref OASISResult<T2> toResult)
        {
            toResult.Exception = fromResult.Exception;
            toResult.IsError = fromResult.IsError;
            toResult.IsSaved = fromResult.IsSaved;
            toResult.IsSaved = fromResult.IsWarning;
            toResult.Message = fromResult.Message;
            toResult.MetaData = fromResult.MetaData;
        }
    }
}