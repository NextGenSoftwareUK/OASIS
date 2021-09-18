using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common
{
    public sealed class Response<T> where T : new()
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public T Payload { get; set; }

        public Response()
        {
            Code = (int)ResponseStatus.Successfully;
            Message = ResponseStatusConstants.Successfully;
            Payload = new T();
        }
    }
}