namespace Application.Extensions.Mappers;

public static class RwaTokenPriceHistoryMapper
{
    public static GetRwaTokenPriceHistoryResponse ToRead(this RwaTokenPriceHistory priceHistory)
        => new(
            priceHistory.Id,
            priceHistory.RwaTokenId,
            priceHistory.OldPrice,
            priceHistory.NewPrice,
            priceHistory.ChangedAt);
}