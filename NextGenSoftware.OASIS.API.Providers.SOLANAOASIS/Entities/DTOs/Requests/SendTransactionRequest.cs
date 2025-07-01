namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;

public sealed class SendTransactionRequest : BaseExchangeRequest
{
    public ulong Lampposts { get; set; }
}