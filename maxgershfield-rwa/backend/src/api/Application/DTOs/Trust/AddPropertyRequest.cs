namespace Application.DTOs.Trust;

public record AddPropertyRequest(
    string PropertyAddress,
    string PropertyType,
    decimal PropertyValue,
    int TotalSquareFootage,
    decimal AnnualRentalIncome,
    decimal AnnualExpenses,
    string TitleDocumentHash,
    string AppraisalDocumentHash,
    string InsuranceDocumentHash,
    string SurveyDocumentHash
);
