namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security
{
    /// <summary>Programmatic clients (STAR API, mobile) send refresh token in JSON; browsers use the refreshToken cookie.</summary>
    public class RefreshTokenRequest
    {
        public string? RefreshToken { get; set; }
    }
}
