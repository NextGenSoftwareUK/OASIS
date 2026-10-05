namespace Application.DTOs.Trust;

public record PropertyResponse(
    Guid Id,
    Guid TrustId,
    string PropertyAddress,
    string PropertyType,
    decimal PropertyValue,
    int TotalSquareFootage,
    decimal AnnualRentalIncome,
    decimal AnnualExpenses,
    decimal NetIncome,
    int TokenSupply,
    decimal TokenPrice,
    string TokenNaming,
    string Status,
    DateTime CreatedAt
);
