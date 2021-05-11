
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public class OASISResult<T>
    {
        public OASISResult<T> InnerResult { get; set; }
        public Dictionary<string, string> MetaData = new Dictionary<string, string>();
        public bool IsError { get; set; }
        public bool IsWarning { get; set; }
        public bool IsSaved { get; set; }
        public string ErrorMessage { get; set; }
        public string Message { get; set; }
        public T Result { get; set; }

        public OASISResult()
        {

        }

        public OASISResult(T value)
        {
            Result = value;
        }
    }
}
