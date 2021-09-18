namespace NextGenSoftware.OASIS.API.Core.Models.Common
{
    public class ApiResponse<T>
    {
        public string Message { get; set; }
        public int Code { get; set; }
        public T Payload { get; set; }

        public ApiResponse()
        {
            Message = ApiConstantsContents.Successfully;
            Code = ApiConstantsCodes.Successfully;
        }
    }
}