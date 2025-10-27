using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Enums;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models
{
    public class Response<T> where T : new()
    {
        public ResponseStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public T Payload { get; set; }

        public Response()
        {
            StatusCode = ResponseStatusCode.Success;
            Message = "OK";
            Payload = new T();
        }
    }
}