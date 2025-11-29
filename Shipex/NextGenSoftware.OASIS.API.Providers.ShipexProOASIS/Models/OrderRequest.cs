namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

/// <summary>
/// Request model for creating a shipping order
/// </summary>
public class OrderRequest
{
    public Guid QuoteId { get; set; }
    public string SelectedCarrier { get; set; } = string.Empty;
    public CustomerInfo CustomerInfo { get; set; } = new();
}

/// <summary>
/// Customer information for shipping
/// </summary>
public class CustomerInfo
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Address? Address { get; set; }
}

