
namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public class ManagerResult<T>
    {
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
        public T Result { get; set; }

        public ManagerResult()
        {

        }

        public ManagerResult(T value)
        {
            Result = value;
        }
    }
}
