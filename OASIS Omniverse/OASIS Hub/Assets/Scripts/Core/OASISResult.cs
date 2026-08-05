using System;

namespace OASIS.Omniverse.UnityHost.Core
{
    [Serializable]
    public class OASISResult<T>
    {
        public T Result;
        public bool IsError;
        public string Message;

        public static OASISResult<T> Success(T result, string message = "OK")
        {
            return new OASISResult<T>
            {
                Result = result,
                IsError = false,
                Message = message
            };
        }

        public static OASISResult<T> Error(string message)
        {
            return new OASISResult<T>
            {
                IsError = true,
                Message = message
            };
        }
    }
}

