namespace Domain.Entities;

public sealed class TrustProperty : BaseEntity
{
    public Guid TrustId { get; set; }
    public string PropertyAddress { get; set; } = string.Empty;
    public string PropertyType { get; set; } = "Single Family Residential";
    public decimal PropertyValue { get; set; }
    public int TotalSquareFootage { get; set; }
    public decimal AnnualRentalIncome { get; set; }
    public decimal AnnualExpenses { get; set; }
    public decimal NetIncome { get; set; }
    public int TokenSupply { get; set; }
    public decimal TokenPrice { get; set; }
    public string TokenNaming { get; set; } = string.Empty;
    public string TitleDocumentHash { get; set; } = string.Empty;
    public string AppraisalDocumentHash { get; set; } = string.Empty;
    public string InsuranceDocumentHash { get; set; } = string.Empty;
    public string SurveyDocumentHash { get; set; } = string.Empty;

    // Navigation properties
    public Trust Trust { get; set; } = null!;
    public ICollection<ContractGeneration> ContractGenerations { get; set; } = [];
}
