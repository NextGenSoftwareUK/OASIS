namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;

/// <summary>
/// DTO for Radix account fungible resource balance
/// </summary>
public class AccountFungibleResourceBalanceDto
{
    public FungibleResourceBalance FungibleResourceBalance { get; set; } = new();
}

public class FungibleResourceBalance
{
    public string Amount { get; set; } = "0";
}

