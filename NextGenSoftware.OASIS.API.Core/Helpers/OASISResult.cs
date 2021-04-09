
namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public class OASISResult<T>
    {
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
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
