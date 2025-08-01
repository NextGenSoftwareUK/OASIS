namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;

public sealed class ExchangeTokenRequest : BaseExchangeRequest
{
    public BaseAccountRequest MintAccount { get; set; }
}