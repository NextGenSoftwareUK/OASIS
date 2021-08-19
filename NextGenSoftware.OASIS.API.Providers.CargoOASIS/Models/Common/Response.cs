using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common
{
    public class Response<T>
    {
        public ResponseStatus ResponseStatus { get; set; }
        public string Message { get; set; }
        public T Payload { get; set; }

        public Response()
        {
            ResponseStatus = ResponseStatus.Success;
            Message = "Ok";
        }
    }
}