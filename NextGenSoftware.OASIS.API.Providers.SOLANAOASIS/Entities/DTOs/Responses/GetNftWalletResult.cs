namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;

public sealed class GetNftWalletResult
{
    public TokenWalletFilterList Accounts { get; set; }
    public TokenWalletBalance[] Balances { get; set; }
}