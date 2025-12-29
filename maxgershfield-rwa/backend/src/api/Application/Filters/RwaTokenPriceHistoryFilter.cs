namespace Application.Filters;

public record RwaTokenPriceHistoryFilter(
    Guid? Id,
    Guid? RwaTokenId,
    decimal? OldPrice,
    decimal? NewPrice,
    DateTime? ChangedAt
) : BaseFilter;