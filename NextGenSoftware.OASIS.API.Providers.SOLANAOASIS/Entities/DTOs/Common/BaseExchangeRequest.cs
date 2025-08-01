namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common;

public abstract class BaseExchangeRequest
{
    public BaseAccountRequest FromAccount { get; set; }
    public BaseAccountRequest ToAccount { get; set; }
    public string MemoText { get; set; }
    public ulong Amount { get; set; }

    public (bool, string) IsRequestValid()
    {
        if (string.IsNullOrEmpty(FromAccount?.PublicKey))
            return (false, "FromAccount PublicKey not provided");

        if (string.IsNullOrEmpty(ToAccount?.PublicKey))
            return (false, "ToAccount PublicKey not provided");

        if (Amount <= 0)
            return (false, "Amount is less or equal to zero");

        return (true, "Request Is Valid");
    }
}