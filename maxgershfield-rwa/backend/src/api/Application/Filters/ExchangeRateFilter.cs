namespace Application.Filters;

/// <summary>
/// Represents a filter used for querying exchange rate records.
/// Allows filtering by source token, destination token, exchange rate value,
/// and creation date, in addition to standard pagination inherited from <see cref="BaseFilter"/>.
/// </summary>
/// <param name="FromTokenId">Optional ID of the source token for filtering.</param>
/// <param name="ToTokenId">Optional ID of the destination token for filtering.</param>
/// <param name="Rate">Optional specific rate value to filter by.</param>
/// <param name="Date">Optional date to filter exchange rates created on that day.</param>
public sealed record ExchangeRateFilter(
    Guid? FromTokenId,
    Guid? ToTokenId,
    decimal? Rate,
    DateTimeOffset? Date) : BaseFilter;