namespace Application.DTOs.RwaTokenPriceHistory.Responses;

public readonly record struct GetRwaTokenPriceHistoryResponse(
    Guid Id,
    Guid RwaTokenId,
    decimal OldPrice,
    decimal NewPrice,
    DateTimeOffset ChangedAt
);