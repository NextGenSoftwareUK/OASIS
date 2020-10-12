using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security
{
    public class AuthenticateResponse
    {
        public IAvatar Avatar { get; set; }
        public bool IsError { get; set; }
        public string Message { get; set; }
    }
}